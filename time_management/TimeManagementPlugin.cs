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
		public enum TimeOfDayIndex {
			Morning,
			Afternoon,
			Evening,
			Night,
			DayEnd
		};

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

        [HarmonyPatch(typeof(LightManager), "EvaluateTimeClock")]
        class HarmonyPatch_LightManager_EvaluateTimeClock {
            private static bool Prefix(LightManager __instance, ref float ___m_TimeMinFloat, ref int ___m_TimeMin, ref int ___m_TimeHour, ref int ___m_TImeOfDayIndex, ref bool ___m_IsLerpingSunIntensity, ref float ___m_LerpStartBrightness, float ___m_GlobalBrightness, ref bool ___m_HasDayEnded) {
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
					if (___m_TimeHour == Settings.m_day_end_hour.Value) {
						if (___m_TImeOfDayIndex == ) {
						__instance.m_TimeOfDayString = "Day End";
						___m_HasDayEnded = true;
						___m_TImeOfDayIndex = 4;
						m_IsLerpingSunIntensity = false;
						m_SkyboxBlender.rotationSpeed = 0f;
						CEventManager.QueueEvent(new CEventPlayer_OnDayEnded());
					} else if (___m_TimeHour > 4 && ___m_TimeHour < 12) {
						if (___m_TImeOfDayIndex != 0) {
							__instance.m_TimeOfDayString = "Morning";
							___m_TImeOfDayIndex = 0;
							___m_IsLerpingSunIntensity = false;
							SoundManager.BlendToMusic("BGM_ShopDay", 1f, isLinearBlend: true);
						}
					} else if (___m_TimeHour >= 12 && ___m_TimeHour < 16) {
						if (___m_TImeOfDayIndex != 1) {
							__instance.m_TimeOfDayString = "Afternoon";
							___m_TImeOfDayIndex = 1;
							___m_IsLerpingSunIntensity = false;
						}
					} else if (___m_TimeHour >= 16 && ___m_TimeHour < 19) {
						if (___m_TImeOfDayIndex != 2) {
							__instance.m_TimeOfDayString = "Evening";
							___m_TImeOfDayIndex = 2;
							___m_IsLerpingSunIntensity = true;
							___m_LerpStartBrightness = ___m_GlobalBrightness;
							SoundManager.BlendToMusic("BGM_ShopNight", 0.1f, isLinearBlend: true);
						}
					} else if (___m_TimeHour <= 4 || ___m_TimeHour >= 19) {
						if (___m_TImeOfDayIndex != 3) {
							__instance.m_TimeOfDayString = "Night";
							___m_TImeOfDayIndex = 3;
							___m_IsLerpingSunIntensity = true;
							___m_LerpStartBrightness = ___m_GlobalBrightness;
						}
					}
                    return false;
                } catch (Exception e) {
                    DDPlugin._error_log("** HarmonyPatch_LightManager_EvaluateTimeClock.Prefix ERROR - " + e);
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(LightManager), "Update")]
		class HarmonyPatch_LightManager_Update {
			private static bool Prefix(LightManager __instance, bool ___m_FinishLoading, bool ___m_IsShopLightOn, ref float ___m_ShopLightOnTimer, bool ___m_HasDayEnded, ref float ___m_TimeMinFloat, ref int ___m_TimeMin, ref int ___m_TimeHour, ref float ___m_SunlightRotationLerpTimer, ref bool ___m_IsBlendingSkybox, ref bool ___m_IsStopBlendingSkybox, ref float ___m_Timer, ref int ___m_SkyboxIndex) {
				try {
					if (!Settings.m_enabled.Value) {
						return true;
					}
                    if (!___m_FinishLoading) {
                        return false;
                    }
                    ReflectionUtils.invoke_method(__instance, "UpdateLightTimeData");
                    if (___m_IsShopLightOn) {
                        ___m_ShopLightOnTimer += Time.deltaTime;
                    }
                    if (!CPlayerData.m_IsShopOnceOpen || ___m_HasDayEnded) {
                        return false;
                    }
					if (___m_TimeHour < Settings.m_day_begin_hour.Value) {
						___m_TimeMinFloat = 0;
						___m_TimeHour = Settings.m_day_begin_hour.Value;
					} else if (___m_TimeHour >= Settings.m_day_end_hour.Value) {
						___m_TimeMinFloat = 0;
						___m_TimeHour = Settings.m_day_end_hour.Value;
					}
					__instance.m_TimerLerpSpeed = (m_is_time_stopped ? 0 : (Settings.m_time_speed.Value < 0 ?
						(___m_TimeHour == Settings.m_day_begin_hour.Value && ___m_TimeMinFloat <= 0 ? 0 : Settings.m_time_speed.Value) :
                        (___m_TimeHour == Settings.m_day_end_hour.Value ? 0 : Settings.m_time_speed.Value)
                    ));
					if ((___m_TimeMinFloat += Time.deltaTime * __instance.m_TimerLerpSpeed) < 0) {
						___m_TimeHour--;
						___m_TimeMin = 59;
					} else {
						___m_TimeMin = Mathf.FloorToInt(___m_TimeMinFloat);
					}
					if (___m_TimeMin >= 60) {
						if (___m_TimeHour < Settings.m_day_end_hour.Value) {
							___m_TimeHour++;
						}
						___m_TimeMinFloat = 0;
						___m_TimeMin = 0;
					}
                    ReflectionUtils.invoke_method(__instance, "EvaluateTimeClock");
                    ReflectionUtils.invoke_method(__instance, "EvaluateLerpSunIntensity");
                    ___m_SunlightRotationLerpTimer += Time.deltaTime * 0.0013888889f * __instance.m_TimerLerpSpeed;
                    __instance.m_Sunlight.rotation = Quaternion.Lerp(__instance.m_SunlightLerpStartPos.rotation, __instance.m_SunlightLerpEndPos.rotation, ___m_SunlightRotationLerpTimer);
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
				} catch (Exception e) {
					DDPlugin._error_log("** HarmonyPatch_LightManager_Update.Prefix ERROR - " + e);
				}
				return true;
			}
		}
	}

    /*
	[HarmonyPatch(typeof(LightManager), "EvaluateTimeClock")]
	class HarmonyPatch_LightManager_EvaluateTimeClock {
		private static bool Prefix(LightManager __instance, ref int ___m_TimeMin, ref float ___m_TimeMinFloat, ref int ___m_TimeHour) {
			try {
				if (!Settings.m_enabled.Value) {
					return true;
				}
				if (___m_TimeMin >= 60) {
					___m_TimeMinFloat = 0f;
					___m_TimeMin = 0;
					if (++___m_TimeHour == 24) {
						___m_TimeHour = 0;
					}
				}
				if (___m_TimeHour == Settings.m_day_end_hour.Value && ___m_TimeMin >= 0) {
					___m_TimeHour = Settings.m_day_end_hour.Value;
					___m_TimeMin = 0;
				}

				string text2 = m_TimeMin.ToString();
				if (m_TimeMin < 10) {
					text2 = "0" + m_TimeMin;
				}
				int num = m_TimeHour;
				if (m_TimeHour > 12) {
					num = m_TimeHour - 12;
				}
				string text3 = num.ToString();
				if (num < 10) {
					text3 = "0" + num;
				}
				m_TimeString = text3 + ":" + text2 + text;
				if (m_TimeHour == 18 && m_TimeMin == 30) {
					m_TimeOfDayString = "Night";
					m_TImeOfDayIndex = 3;
					m_IsLerpingSunIntensity = true;
					m_LerpStartBrightness = m_GlobalBrightness;
				} else if (m_TimeHour == 16 && m_TimeMin == 0) {
					m_TimeOfDayString = "Evening";
					m_TImeOfDayIndex = 2;
					m_IsLerpingSunIntensity = true;
					m_LerpStartBrightness = m_GlobalBrightness;
					SoundManager.BlendToMusic("BGM_ShopNight", 0.1f, isLinearBlend: true);
				} else if (m_TimeHour == 12 && m_TimeMin == 0) {
					m_TimeOfDayString = "Afternoon";
					m_TImeOfDayIndex = 1;
					m_IsLerpingSunIntensity = false;
				} else if (m_TimeHour == 8 && m_TimeMin == 0) {
					m_TimeOfDayString = "Morning";
					m_TImeOfDayIndex = 0;
					m_IsLerpingSunIntensity = false;
					m_HasDayEnded = false;
				} else if (m_TimeHour == 21 && m_TimeMin == 0) {
					m_TimeOfDayString = "Day End";
					m_HasDayEnded = true;
					m_TImeOfDayIndex = 4;
					m_IsLerpingSunIntensity = false;
					m_SkyboxBlender.rotationSpeed = 0f;
					CEventManager.QueueEvent(new CEventPlayer_OnDayEnded());
				}
				return false;
			} catch (Exception e) {
				DDPlugin._error_log("** HarmonyPatch_LightManager_EvaluateTimeClock ERROR - " + e);
			}
			return true;
		}
	}
	*/
}
