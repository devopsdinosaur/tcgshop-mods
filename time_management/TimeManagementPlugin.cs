using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;
using TMPro;

public static class PluginInfo {

	public const string TITLE = "Time Management";
	public const string NAME = "time_management";
	public const string SHORT_DESCRIPTION = "Change shop open/close hours.  Slow down, speed up, stop, and even reverse time using configurable hotkeys.";

	public const string VERSION = "0.0.3";

	public const string AUTHOR = "devopsdinosaur";
	public const string GAME_TITLE = "TCG Shop Simulator";
	public const string GAME = "tcgshop";
	public const string GUID = AUTHOR + "." + GAME + "." + NAME;
	public const string REPO = "tcgshop-mods";

	public static Dictionary<string, string> to_dict() {
		Dictionary<string, string> info = new Dictionary<string, string>();
		foreach (FieldInfo field in typeof(PluginInfo).GetFields((BindingFlags) 0xFFFFFFF)) {
			info[field.Name.ToLower()] = (string) field.GetValue(null);
		}
		return info;
	}
}

[BepInPlugin(PluginInfo.GUID, PluginInfo.TITLE, PluginInfo.VERSION)]
public class TimeManagementPlugin : DDPlugin {
	private Harmony m_harmony = new Harmony(PluginInfo.GUID);

	private void Awake() {
		logger = this.Logger;
		try {
			Settings.Instance.load(this);
			this.plugin_info = PluginInfo.to_dict();
			this.create_nexus_page();
			this.m_harmony.PatchAll();
			logger.LogInfo($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

    class __Global__ {
        [HarmonyPatch(typeof(CGameManager), "Awake")]
        class HarmonyPatch_CGameManager_Awake {
            private static void Postfix(CGameManager __instance) {
                Hotkeys.load();
            }
        }

        [HarmonyPatch(typeof(CGameManager), "Update")]
        class HarmonyPatch_CGameManager_Update {
            private static void Postfix(CGameManager __instance) {
                Hotkeys.Updaters.keypress_update();
            }
        }
    }

	public class GameClock {
		public static int m_day_begin_hour = 8;
		public static int m_day_end_hour = 21;
		public static bool m_is_time_stopped = false;
		private static TextMeshProUGUI m_time_speed_text;

		public static void ensure_tooltip_object() {
			if (m_time_speed_text != null) {
				return;
			}
			m_time_speed_text = GameObject.Instantiate<TextMeshProUGUI>(GameUIScreen.Instance.m_TimeText, GameUIScreen.Instance.m_TimeText.transform.parent);
			m_time_speed_text.transform.localPosition = GameUIScreen.Instance.m_TimeText.transform.localPosition + Vector3.down * 28f;
			m_time_speed_text.name = "TimeManagement_Time_Speed_Info_Text";
			m_time_speed_text.enableAutoSizing = true;
			m_time_speed_text.enableWordWrapping = false;
			m_time_speed_text.fontSizeMin = m_time_speed_text.fontSizeMax = 14;
			m_time_speed_text.gameObject.SetActive(true);
		}

		public static void set_time_speed(float speed) {
			Settings.m_time_speed.Value = speed;
			ReflectionUtils.invoke_method(LightManager.Instance, "EvaluateTimeClock");
		}

		class LightSpinner : MonoBehaviour {
			
			public const int SECONDS_PER_MINUTE = 60;
			public const int MINUTES_PER_HOUR = 60;
			public const int SECONDS_PER_HOUR = SECONDS_PER_MINUTE * MINUTES_PER_HOUR;
			public const int HOURS_PER_DAY = 24;
			public const int SECONDS_PER_DAY = SECONDS_PER_HOUR * HOURS_PER_DAY;

			public const int SKYBOX_INDEX_DAY = 0;
			public const int SKYBOX_INDEX_EVENING = 1;
			public const int SKYBOX_INDEX_NIGHT = 2;

			public const int STOP_HOUR_NIGHT = 5;
			public const int STOP_HOUR_MORNING = 12;
			public const int STOP_HOUR_AFTERNOON = 16;
			public const int STOP_HOUR_EVENING = 20;

			private static LightSpinner m_instance;
			public static LightSpinner Instance {
				get {
					if (m_instance == null) {
						m_instance = LightManager.Instance.gameObject.AddComponent<LightSpinner>();
					}
					return m_instance;
				}
			}
			private float m_time;
			private int m_hour;
			private int m_minute;
			private int m_second;
			public int Hour {
				get {
					return this.m_hour;
				}
			}
			public int Minute {
				get {
					return this.m_minute;
				}
			}
			private SkyboxBlender m_skybox_blender;
			private int m_skybox_index;

			private void Awake() {
				this.m_skybox_blender = LightManager.Instance.m_SkyboxBlender;
			}

			public void start(int hour, int minute) {
				this.StopAllCoroutines();
				this.m_time = hour * SECONDS_PER_HOUR + minute * SECONDS_PER_MINUTE;
				this.update_time();
				this.StartCoroutine(this.music_routine());
				this.StartCoroutine(this.skybox_routine());
				this.StartCoroutine(this.sun_routine());
			}

			public void add_time(float seconds) {
				this.m_time += seconds;
				this.update_time();
			}

			private void update_time() {
				this.m_hour = Mathf.FloorToInt(this.m_time / SECONDS_PER_HOUR);
				this.m_minute = Mathf.FloorToInt(this.m_time % SECONDS_PER_HOUR) / SECONDS_PER_MINUTE;
				this.m_second = Mathf.FloorToInt(this.m_time % SECONDS_PER_HOUR) % SECONDS_PER_MINUTE;
			}

			private IEnumerator music_routine() {
				const int SONG_INDEX_DAY = 0;
				const int SONG_INDEX_NIGHT = 1;
				string[] songs = {"BGM_ShopDay", "BGM_ShopNight"};
				int song_index = -1;
				void set_song(int index) {
					if (song_index == index) {
						return;
					}
					song_index = index;
					SoundManager.BlendToMusic(songs[index], 1f, true);
				}
				while (true) {
					set_song((this.m_hour >= STOP_HOUR_NIGHT && this.m_hour < STOP_HOUR_EVENING ? SONG_INDEX_DAY : SONG_INDEX_NIGHT));
					yield return new WaitForSeconds(1.0f);
				}
			}

			private IEnumerator skybox_routine() {
				
				// TODO: Blending wrecks lighting and washes out scene second time around; for now just change skybox mat directly.
				
				void blend(int index) {
					if (this.m_skybox_index != index) {
						RenderSettings.skybox = this.m_skybox_blender.skyboxMaterials[this.m_skybox_index = index];
					}
				}
				while (true) {
					this.m_skybox_blender.rotationSpeed = LightManager.Instance.m_SkyboxRotateSpeed * Settings.m_time_speed.Value;
					if (this.m_hour < STOP_HOUR_NIGHT || this.m_hour > STOP_HOUR_EVENING) {
						blend(SKYBOX_INDEX_NIGHT);
					} else if (this.m_hour >= STOP_HOUR_NIGHT && this.m_hour < STOP_HOUR_AFTERNOON) {
						blend(SKYBOX_INDEX_DAY);
					} else {
						blend(SKYBOX_INDEX_EVENING);
					}
					yield return new WaitForSeconds(0.1f);
				}
			}

			private IEnumerator sun_routine() {
				void set_intensity(List<Light> lights, float intensity) {
					foreach (Light light in lights) {
						light.intensity = intensity;
					}
				}
				const int SUN_SECONDS = (STOP_HOUR_EVENING - STOP_HOUR_NIGHT + 1) * SECONDS_PER_HOUR;
				set_intensity(LightManager.Instance.m_AmbientLightList, 1);
				set_intensity(LightManager.Instance.m_ItemLightList, 1);
				while (true) {
					if (this.m_hour >= STOP_HOUR_NIGHT && this.m_hour <= STOP_HOUR_EVENING) {
						LightManager.Instance.m_SunlightGrp.SetActive(true);
						LightManager.Instance.m_NightlightGrp.SetActive(false);
						LightManager.Instance.m_SunlightList[0].shadowStrength = 1f;
						LightManager.Instance.m_SunlightList[0].shadows = LightShadows.Soft;
						LightManager.Instance.m_Sunlight.rotation = Quaternion.Lerp(
							LightManager.Instance.m_SunlightLerpStartPos.rotation, 
							LightManager.Instance.m_SunlightLerpEndPos.rotation, 
							((float) ((this.m_hour - STOP_HOUR_NIGHT) * SECONDS_PER_HOUR + this.m_minute * SECONDS_PER_MINUTE + this.m_second)) / (float) SUN_SECONDS
						);
						set_intensity(LightManager.Instance.m_SunlightList, 
								this.m_hour == STOP_HOUR_NIGHT ? Mathf.Lerp(0, 1, this.m_minute / 59) :
								this.m_hour == STOP_HOUR_EVENING ? Mathf.Lerp(1, 0, this.m_minute / 59) :
								1
						);
					} else {
						LightManager.Instance.m_SunlightGrp.SetActive(false);
						LightManager.Instance.m_NightlightGrp.SetActive(true);
						LightManager.Instance.m_SunlightList[0].shadowStrength = 0f;
						LightManager.Instance.m_SunlightList[0].shadows = LightShadows.None;
					}
					yield return new WaitForSeconds(0.01f);
				}
			}
		}

		[HarmonyPatch(typeof(LightManager), "Init")]
		class HarmonyPatch_LightManager_Init {
			private static bool Prefix(LightManager __instance, ref bool ___m_FinishLoading, ref LightTimeData ___m_LightTimeData, ref bool ___m_HasDayEnded) {
				try {
					if (!Settings.m_enabled.Value) {
						return true;
					}
					if (___m_FinishLoading) {
						return false;
					}
					___m_FinishLoading = true;
					if (CPlayerData.m_LightTimeData == null) {
						ReflectionUtils.invoke_method(__instance, "ResetSunlightIntensity");
						return false;
					}
					___m_LightTimeData = CPlayerData.m_LightTimeData;
					if (___m_LightTimeData.m_TimeHour > m_day_end_hour || (___m_LightTimeData.m_TimeHour == m_day_end_hour && ___m_LightTimeData.m_TimeMin > 0)) {
						___m_LightTimeData.m_TimeHour = m_day_end_hour;
						___m_LightTimeData.m_TimeMin = 0;
					} else if (___m_LightTimeData.m_TimeHour < m_day_begin_hour) {
						___m_LightTimeData.m_TimeHour = m_day_begin_hour;
						___m_LightTimeData.m_TimeMin = 0;
					}
					LightSpinner.Instance.start(___m_LightTimeData.m_TimeHour, ___m_LightTimeData.m_TimeMin);
					ReflectionUtils.invoke_method(__instance, "EvaluateTimeClock");
					return false;
				} catch (Exception e) {
					DDPlugin._error_log("** HarmonyPatch_LightManager_Init.Prefix ERROR - " + e);
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(LightManager), "ResetSunlightIntensity")]
		class HarmonyPatch_LightManager_ResetSunlightIntensity {
			private static bool Prefix(LightManager __instance, ref bool ___m_HasDayEnded) {
				try {
					if (!Settings.m_enabled.Value) {
						return true;
					}
					LightSpinner.Instance.start(m_day_begin_hour, 0);
					ReflectionUtils.invoke_method(__instance, "EvaluateTimeClock");
					___m_HasDayEnded = false;
					CEventManager.QueueEvent(new CEventPlayer_OnDayStarted());
					return false;
				} catch (Exception e) {
					DDPlugin._error_log("** HarmonyPatch_LightManager_EvaluateTimeClock.Prefix ERROR - " + e);
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(LightManager), "EvaluateTimeClock")]
        class HarmonyPatch_LightManager_EvaluateTimeClock {
            private static bool Prefix(LightManager __instance, ref int ___m_TimeMin, ref int ___m_TimeHour) {
                try {
                    if (!Settings.m_enabled.Value) {
                        return true;
                    }
					___m_TimeHour = LightSpinner.Instance.Hour;
					___m_TimeMin = LightSpinner.Instance.Minute;
                    __instance.m_TimeString = (Settings.m_twenty_four_hour_format.Value ?
                        String.Format("{0:D2}:{1:D2}", ___m_TimeHour, ___m_TimeMin) :
                        String.Format("{0:D2}:{1:D2} {2:S}", (___m_TimeHour > 12 ? ___m_TimeHour - 12 : ___m_TimeHour), ___m_TimeMin, (___m_TimeHour >= 12 ? "PM" : "AM"))
                    );
					ensure_tooltip_object();
					m_time_speed_text.text = $"[Time Speed: {Settings.m_time_speed.Value:#0.00}m/s{(m_is_time_stopped ? " |Stopped|" : "")}]";
                    return false;
                } catch (Exception e) {
                    DDPlugin._error_log("** HarmonyPatch_LightManager_EvaluateTimeClock.Prefix ERROR - " + e);
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(LightManager), "Update")]
		class HarmonyPatch_LightManager_Update {
			static int m_prev_hour = -1;
			static int m_prev_minute = -1;

			private static bool Prefix(LightManager __instance, bool ___m_FinishLoading, ref bool ___m_HasDayEnded, ref int ___m_TimeMin, ref int ___m_TimeHour) {
				try {
					if (!Settings.m_enabled.Value) {
						return true;
					}
                    if (!___m_FinishLoading) {
                        return false;
                    }
					___m_TimeHour = LightSpinner.Instance.Hour;
					___m_TimeMin = LightSpinner.Instance.Minute;
                    ReflectionUtils.invoke_method(__instance, "UpdateLightTimeData");
					if (___m_TimeHour != m_prev_hour || ___m_TimeMin != m_prev_minute) {
						ReflectionUtils.invoke_method(__instance, "EvaluateTimeClock");
						m_prev_hour = ___m_TimeHour;
						m_prev_minute = ___m_TimeMin;
					}
					if (!___m_HasDayEnded && ___m_TimeHour == m_day_end_hour) {
						___m_HasDayEnded = true;
						CEventManager.QueueEvent(new CEventPlayer_OnDayEnded());
					}
                    if ((__instance.m_TimerLerpSpeed = (m_is_time_stopped || (!CPlayerData.m_IsShopOnceOpen && !Settings.m_run_time_before_open.Value) || ___m_HasDayEnded ? 0 : (Settings.m_time_speed.Value < 0 ?
						(___m_TimeHour == m_day_begin_hour && ___m_TimeMin <= 0 ? 0 : Settings.m_time_speed.Value) :
                        (___m_TimeHour == m_day_end_hour ? 0 : Settings.m_time_speed.Value)
					))) != 0) {
						LightSpinner.Instance.add_time(Time.deltaTime * __instance.m_TimerLerpSpeed * 60);
					}
					return false;
				} catch (Exception e) {
					DDPlugin._error_log("** HarmonyPatch_LightManager_Update.Prefix ERROR - " + e);
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(LightManager), "EvaluateLerpSunIntensity")]
		class HarmonyPatch_LightManager_EvaluateLerpSunIntensity {
			private static bool Prefix(LightManager __instance) {
				return !Settings.m_enabled.Value;
			}
		}

		[HarmonyPatch(typeof(LightManager), "EvaluateWorldUIBrightness")]
		class HarmonyPatch_LightManager_EvaluateWorldUIBrightness {
			private static bool Prefix(LightManager __instance, ref float ___m_GlobalBrightness, List<float> ___m_OriginalItemLightIntensityList, List<float> ___m_OriginalAmbientLightIntensityList, ref Color ___m_BillboardTargetLerpColor, Color ___m_BillboardTextOriginalColor) {
				return !Settings.m_enabled.Value;
			}
		}
	}
}
