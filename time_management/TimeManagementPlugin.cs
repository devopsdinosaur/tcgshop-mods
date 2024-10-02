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
	public const string SHORT_DESCRIPTION = "Slow down, speed up, stop, and even reverse time using configurable hotkeys.";

	public const string VERSION = "0.0.1";

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
		public const int TODI_MORNING = 0;
		public const int TODI_AFTERNOON = 1;
		public const int TODI_EVENING = 2;
		public const int TODI_NIGHT = 3;
		public const int TODI_DAY_END = 4;
		public static string[] TODI_STRINGS = {
			"Morning",
			"Afternoon",
			"Evening",
			"Night",
			"Day End"
		};
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

		private static bool ensure_time_of_day_index(int todi, ref int ___m_TImeOfDayIndex, LightManager __instance, int ___m_TimeHour, int ___m_TimeMin) {
			if (___m_TImeOfDayIndex == todi) {
				DDPlugin._debug_log($"[Current Game Time: {___m_TimeHour:D2}:{___m_TimeMin:D2}] ___m_TImeOfDayIndex == todi == {todi}");
				return false;
			}
			__instance.m_TimeOfDayString = TODI_STRINGS[(___m_TImeOfDayIndex = todi)];
			DDPlugin._debug_log($"[Current Game Time: {___m_TimeHour:D2}:{___m_TimeMin:D2}] Time of Day Index changed to '{__instance.m_TimeOfDayString}' [{___m_TImeOfDayIndex}]");
			return true;
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
				this.StartCoroutine(this.main_routine());
				this.StartCoroutine(this.skybox_routine());
				this.StartCoroutine(this.sun_routine());
			}

			public void add_time(float seconds) {
				this.m_time += seconds;
				this.m_hour = Mathf.FloorToInt(this.m_time / SECONDS_PER_HOUR);
				this.m_minute = Mathf.FloorToInt(this.m_time % SECONDS_PER_HOUR) / SECONDS_PER_MINUTE;
				this.m_second = Mathf.FloorToInt(this.m_time % SECONDS_PER_HOUR) % SECONDS_PER_MINUTE;
			}

			private IEnumerator main_routine() {
				while (true) {
					yield return null;
					//DDPlugin._debug_log($"time: {this.m_time}, hour: {this.m_hour}, minute: {this.m_minute}");
				}
			}

			private IEnumerator skybox_routine() {
				this.m_skybox_blender.updateLighting = false;
				this.m_skybox_blender.updateReflections = false;
				bool is_first_blend = true;
				void blend(int index) {
					//DDPlugin._debug_log($"[{this.m_hour}:{this.m_minute}] blend({index} [current: {this.m_skybox_index}])");
					if (this.m_skybox_index == index) {
						return;
					}
					this.m_skybox_blender.blendValue = 1f;
					if (!is_first_blend) {
						this.m_skybox_blender.blendSpeed = 0.3f * Settings.m_time_speed.Value;
					} else {
						this.m_skybox_blender.blendSpeed = 1f;
						is_first_blend = false;
					}
					this.m_skybox_index = index;
					//this.m_skybox_blender.Blend(this.m_skybox_index);
					RenderSettings.skybox = this.m_skybox_blender.skyboxMaterials[this.m_skybox_index];
					DDPlugin._debug_log($"[{this.m_hour}:{this.m_minute}] Blending to {index}, speed: {this.m_skybox_blender.blendSpeed}");
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
				const int SUN_SECONDS = (STOP_HOUR_EVENING - STOP_HOUR_NIGHT + 1) * SECONDS_PER_HOUR;
				for (int j = 0; j < LightManager.Instance.m_AmbientLightList.Count; j++) {
					LightManager.Instance.m_AmbientLightList[j].intensity = 0;
				}
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
						for (int j = 0; j < LightManager.Instance.m_SunlightList.Count; j++) {
							LightManager.Instance.m_SunlightList[j].intensity = 
								this.m_hour == STOP_HOUR_NIGHT ? Mathf.Lerp(0, 1, this.m_minute / 59) :
								this.m_hour == STOP_HOUR_EVENING ? Mathf.Lerp(1, 0, this.m_minute / 59) :
								1;
						}
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
			private static bool Prefix(LightManager __instance, ref bool ___m_FinishLoading) {
				try {
					if (!Settings.m_enabled.Value) {
						return true;
					}
					if (___m_FinishLoading) {
						return false;
					}
					___m_FinishLoading = true;
					ReflectionUtils.invoke_method(__instance, "ResetSunlightIntensity");
					return false;
				} catch (Exception e) {
					DDPlugin._error_log("** HarmonyPatch_LightManager_Init.Prefix ERROR - " + e);
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(LightManager), "ResetSunlightIntensity")]
		class HarmonyPatch_LightManager_ResetSunlightIntensity {
			private static bool Prefix(LightManager __instance, bool ___m_FinishLoading, bool ___m_IsShopLightOn, ref float ___m_ShopLightOnTimer, bool ___m_HasDayEnded, ref float ___m_TimeMinFloat, ref int ___m_TimeMin, ref int ___m_TimeHour, ref float ___m_SunlightRotationLerpTimer, ref bool ___m_IsBlendingSkybox, ref bool ___m_IsStopBlendingSkybox, ref float ___m_Timer, ref int ___m_SkyboxIndex) {
				try {
					if (!Settings.m_enabled.Value) {
						return true;
					}
					
					LightSpinner.Instance.start(m_day_begin_hour, 0);
					ReflectionUtils.get_field(__instance, "m_HasDayEnded").SetValue(__instance, false);
					CPlayerData.m_IsShopOnceOpen = true;
					//ReflectionUtils.invoke_method(__instance, "EvaluateTimeClock");
					CEventManager.QueueEvent(new CEventPlayer_OnDayStarted());
					return false;

					__instance.m_SkyboxBlender.Blend(0);
					__instance.m_SkyboxBlender.blendValue = 1f;
					___m_IsShopLightOn = false;
					__instance.m_SunlightGrp.SetActive(value: true);
					__instance.m_ShoplightGrp.SetActive(value: false);
					__instance.m_NightlightGrp.SetActive(value: false);
					ReflectionUtils.invoke_method(__instance, "EvaluateWorldUIBrightness");
					__instance.StartCoroutine(delay_update_env(__instance));
					return false;
				} catch (Exception e) {
					DDPlugin._error_log("** HarmonyPatch_LightManager_EvaluateTimeClock.Prefix ERROR - " + e);
				}
				return true;
			}

			private static IEnumerator delay_update_env(LightManager __instance) {
				yield return new WaitForSeconds(0.01f);
				__instance.m_SkyboxBlender.Blend(0);
				__instance.m_SkyboxBlender.blendValue = 1f;
				yield return new WaitForSeconds(0.01f);
				DynamicGI.UpdateEnvironment();
				__instance.m_SkyboxBlender.blendValue = 0f;
				ReflectionUtils.get_field(__instance, "m_HasDayEnded").SetValue(__instance, false);
				ReflectionUtils.get_field(__instance, "m_TimeHour").SetValue(__instance, m_day_begin_hour);
				ReflectionUtils.get_field(__instance, "m_TimeMin").SetValue(__instance, 0);
				ReflectionUtils.get_field(__instance, "m_TimeMinFloat").SetValue(__instance, 0f);
				ReflectionUtils.invoke_method(__instance, "EvaluateTimeClock");
				CEventManager.QueueEvent(new CEventPlayer_OnDayStarted());
				SoundManager.BlendToMusic("BGM_ShopDay", 1f, isLinearBlend: true);
			}
		}

		[HarmonyPatch(typeof(LightManager), "EvaluateTimeClock")]
        class HarmonyPatch_LightManager_EvaluateTimeClock {
            private static bool Prefix(LightManager __instance, ref float ___m_TimeMinFloat, ref int ___m_TimeMin, ref int ___m_TimeHour, ref int ___m_TImeOfDayIndex, ref bool ___m_IsLerpingSunIntensity, ref float ___m_LerpStartBrightness, float ___m_GlobalBrightness, ref bool ___m_HasDayEnded, ref bool ___m_IsSunLightOn) {
                try {
                    if (!Settings.m_enabled.Value) {
                        return true;
                    }
                    __instance.m_TimeString = (Settings.m_twenty_four_hour_format.Value ?
                        String.Format("{0:D2}:{1:D2}", ___m_TimeHour, ___m_TimeMin) :
                        String.Format("{0:D2}:{1:D2} {2:S}", (___m_TimeHour > 12 ? ___m_TimeHour - 12 : ___m_TimeHour), ___m_TimeMin, (___m_TimeHour >= 12 ? "PM" : "AM"))
                    );
					ensure_tooltip_object();
					m_time_speed_text.text = $"[Time Speed: {Settings.m_time_speed.Value:#0.00}s/m{(m_is_time_stopped ? " |Stopped|" : "")}]";
					//if (___m_TimeHour == m_day_end_hour && !___m_HasDayEnded) {
					//	___m_HasDayEnded = true;
					//	CEventManager.QueueEvent(new CEventPlayer_OnDayEnded());
					//}
					/*
					if (___m_TimeHour == m_day_end_hour) {
						if (ensure_time_of_day_index(TODI_DAY_END, ref ___m_TImeOfDayIndex, __instance, ___m_TimeHour, ___m_TimeMin)) {
							___m_HasDayEnded = true;
							___m_IsSunLightOn = false;
							___m_IsLerpingSunIntensity = false;
							__instance.m_SkyboxBlender.rotationSpeed = 0f;
							CEventManager.QueueEvent(new CEventPlayer_OnDayEnded());
						}
					} else if (___m_TimeHour > 4 && ___m_TimeHour < 12) {
						if (ensure_time_of_day_index(TODI_MORNING, ref ___m_TImeOfDayIndex, __instance, ___m_TimeHour, ___m_TimeMin)) {
							___m_IsSunLightOn = true;
							___m_IsLerpingSunIntensity = false;
							SoundManager.BlendToMusic("BGM_ShopDay", 1f, isLinearBlend: true);
						}
					} else if (___m_TimeHour >= 12 && ___m_TimeHour < 16) {
						if (ensure_time_of_day_index(TODI_AFTERNOON, ref ___m_TImeOfDayIndex, __instance, ___m_TimeHour, ___m_TimeMin)) {
							___m_IsSunLightOn = true;
							___m_IsLerpingSunIntensity = false;
						}
					} else if (___m_TimeHour >= 16 && ___m_TimeHour < 19) {
						if (ensure_time_of_day_index(TODI_EVENING, ref ___m_TImeOfDayIndex, __instance, ___m_TimeHour, ___m_TimeMin)) {
							___m_IsSunLightOn = true;
							___m_IsLerpingSunIntensity = true;
							___m_LerpStartBrightness = ___m_GlobalBrightness;
							SoundManager.BlendToMusic("BGM_ShopNight", 0.1f, isLinearBlend: true);
						}
					} else if (___m_TimeHour <= 4 || ___m_TimeHour >= 19) {
						if (ensure_time_of_day_index(TODI_NIGHT, ref ___m_TImeOfDayIndex, __instance, ___m_TimeHour, ___m_TimeMin)) {
							___m_IsSunLightOn = false;
							___m_IsLerpingSunIntensity = true;
							___m_LerpStartBrightness = ___m_GlobalBrightness;
						}
					}
					*/
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

			private static bool Prefix(LightManager __instance, bool ___m_FinishLoading, bool ___m_HasDayEnded, ref int ___m_TimeMin, ref int ___m_TimeHour) {
				try {
					if (!Settings.m_enabled.Value) {
						return true;
					}
                    if (!___m_FinishLoading) {
                        return false;
                    }
                    ReflectionUtils.invoke_method(__instance, "UpdateLightTimeData");
                    ___m_TimeHour = LightSpinner.Instance.Hour;
					___m_TimeMin = LightSpinner.Instance.Minute;
					if (___m_TimeHour != m_prev_hour || ___m_TimeMin != m_prev_minute) {
						ReflectionUtils.invoke_method(__instance, "EvaluateTimeClock");
						m_prev_hour = ___m_TimeHour;
						m_prev_minute = ___m_TimeMin;
					}
                    __instance.m_TimerLerpSpeed = (m_is_time_stopped /*|| !CPlayerData.m_IsShopOnceOpen*/ || ___m_HasDayEnded ? 0 : (Settings.m_time_speed.Value < 0 ?
						(___m_TimeHour == m_day_begin_hour && ___m_TimeMin <= 0 ? 0 : Settings.m_time_speed.Value) :
                        (___m_TimeHour == m_day_end_hour ? 0 : Settings.m_time_speed.Value)
                    ));
					LightSpinner.Instance.add_time(Time.deltaTime * __instance.m_TimerLerpSpeed * 60);
					return false;
					/*
					if ((___m_TimeMinFloat += Time.deltaTime * __instance.m_TimerLerpSpeed) < 0) {
						___m_TimeHour--;
						___m_TimeMin = 59;
					} else {
						___m_TimeMin = Mathf.FloorToInt(___m_TimeMinFloat);
					}
					if (___m_TimeMin >= 60) {
						if (___m_TimeHour < m_day_end_hour) {
							___m_TimeHour++;
						}
						___m_TimeMinFloat = 0;
						___m_TimeMin = 0;
					}
                    ReflectionUtils.invoke_method(__instance, "EvaluateTimeClock");
                    ReflectionUtils.invoke_method(__instance, "EvaluateLerpSunIntensity");
                    //___m_SunlightRotationLerpTimer += Time.deltaTime * 0.0013888889f * __instance.m_TimerLerpSpeed;
                    //__instance.m_SunlightGrp.SetActive(true);
					//int total_minutes = ___m_TimeHour * 60 + ___m_TimeMin;
					//float percent = ((float) total_minutes) / 1440.0f;
					//float degrees = Mathf.Lerp(0, 360, percent);
					//DDPlugin._debug_log($"total_minutes: {total_minutes}, percent: {percent}, degrees: {degrees}, {__instance.m_SunlightLerpStartPos.rotation}");
					//__instance.m_Sunlight.rotation = Quaternion.Euler(0, 0, degrees);
					//__instance.m_Sunlight.rotation = Quaternion.Lerp(__instance.m_SunlightLerpStartPos.rotation, __instance.m_SunlightLerpEndPos.rotation, percent);
					__instance.m_SkyboxBlender.rotationSpeed = __instance.m_SkyboxRotateSpeed * __instance.m_TimerLerpSpeed;
                    if (!___m_IsBlendingSkybox && !___m_IsStopBlendingSkybox) {
                        ___m_Timer += Time.deltaTime * __instance.m_TimerLerpSpeed;
                        if (___m_Timer > __instance.m_TimeTillNextSkybox[___m_SkyboxIndex]) {
                            ___m_Timer = 0f;
                            ___m_IsBlendingSkybox = true;
                            __instance.m_SkyboxBlender.blendSpeed = 1f / __instance.m_SkyboxBlendDuration[___m_SkyboxIndex] * __instance.m_TimerLerpSpeed;
                            __instance.m_SkyboxBlender.Blend(___m_SkyboxIndex + 1);
                        }
                    } else {
                        if (!___m_IsBlendingSkybox || ___m_IsStopBlendingSkybox) {
                            return false;
                        }
                        ___m_Timer += Time.deltaTime * __instance.m_TimerLerpSpeed;
                        if (___m_Timer >= __instance.m_SkyboxBlendDuration[___m_SkyboxIndex]) {
                            ___m_Timer = 0f;
                            ___m_SkyboxIndex++;
                            ___m_IsBlendingSkybox = false;
                            if (___m_SkyboxIndex >= 2) {
                                ___m_IsStopBlendingSkybox = true;
                                __instance.m_SkyboxBlender.Stop();
                            }
                        }
                    }
                    return false;
					*/
				} catch (Exception e) {
					DDPlugin._error_log("** HarmonyPatch_LightManager_Update.Prefix ERROR - " + e);
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(LightManager), "EvaluateLerpSunIntensity")]
		class HarmonyPatch_LightManager_EvaluateLerpSunIntensity {
			private static bool Prefix(LightManager __instance) {
				try {
					if (!Settings.m_enabled.Value) {
						return true;
					}

					return false;

					for (int i = 0; i < __instance.m_SunlightList.Count; i++) {
						__instance.m_SunlightList[i].intensity = 1f;
					}
					return false;
				} catch (Exception e) {
					DDPlugin._error_log("** HarmonyPatch_LightManager_EvaluateLerpSunIntensity.Prefix ERROR - " + e);
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(LightManager), "EvaluateWorldUIBrightness")]
		class HarmonyPatch_LightManager_EvaluateWorldUIBrightness {
			private static bool Prefix(LightManager __instance, ref float ___m_GlobalBrightness, List<float> ___m_OriginalItemLightIntensityList, List<float> ___m_OriginalAmbientLightIntensityList, ref Color ___m_BillboardTargetLerpColor, Color ___m_BillboardTextOriginalColor) {
				try {
					if (!Settings.m_enabled.Value) {
						return true;
					}
					
					___m_GlobalBrightness = 0f;

					___m_GlobalBrightness = Mathf.Clamp(___m_GlobalBrightness, 0f, 1f);
					for (int i = 0; i < __instance.m_ItemLightList.Count; i++) {
						__instance.m_ItemLightList[i].intensity = ___m_GlobalBrightness * ___m_OriginalItemLightIntensityList[i];
					}
					for (int j = 0; j < __instance.m_AmbientLightList.Count; j++) {
						__instance.m_AmbientLightList[j].intensity = ___m_GlobalBrightness * ___m_OriginalAmbientLightIntensityList[j];
					}
					PriceTagUISpawner.SetAllPriceTagUIBrightness(___m_GlobalBrightness);
					Card3dUISpawner.SetAllCardUIBrightness(___m_GlobalBrightness);
					___m_BillboardTargetLerpColor = ___m_BillboardTextOriginalColor * ___m_GlobalBrightness;
					___m_BillboardTargetLerpColor.a = 1f;
					__instance.m_BillboardText.color = ___m_BillboardTargetLerpColor;
					__instance.m_CardBackMat.SetColor("_EmissionColor", __instance.m_CardBackMatOriginalEmissionColor * Mathf.Lerp(0.2f, 1f, ___m_GlobalBrightness));
					for (int k = 0; k < __instance.m_ItemMatList.Count; k++) {
						if ((bool) __instance.m_ItemMatList[k]) {
							__instance.m_ItemMatList[k].SetColor("_Color", __instance.m_ItemMatOriginalColorList[k] * Mathf.Lerp(0.2f, 1f, ___m_GlobalBrightness));
						}
					}
					return false;
				} catch (Exception e) {
					DDPlugin._error_log("** HarmonyPatch_LightManager_EvaluateLerpSunIntensity.Prefix ERROR - " + e);
				}
				return true;
			}
		}
	}
}
