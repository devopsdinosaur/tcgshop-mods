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

		private static bool ensure_time_of_day_index(int todi, ref int ___m_TImeOfDayIndex, LightManager __instance, int ___m_TimeHour, int ___m_TimeMin) {
			if (___m_TImeOfDayIndex == todi) {
				DDPlugin._debug_log($"[Current Game Time: {___m_TimeHour:D2}:{___m_TimeMin:D2}] ___m_TImeOfDayIndex == todi == {todi}");
				return false;
			}
			__instance.m_TimeOfDayString = TODI_STRINGS[(___m_TImeOfDayIndex = todi)];
			DDPlugin._debug_log($"[Current Game Time: {___m_TimeHour:D2}:{___m_TimeMin:D2}] Time of Day Index changed to '{__instance.m_TimeOfDayString}' [{___m_TImeOfDayIndex}]");
			return true;
		}

		[HarmonyPatch(typeof(LightManager), "ResetSunlightIntensity")]
		class HarmonyPatch_LightManager_ResetSunlightIntensity {
			private static bool Prefix(LightManager __instance, bool ___m_FinishLoading, bool ___m_IsShopLightOn, ref float ___m_ShopLightOnTimer, bool ___m_HasDayEnded, ref float ___m_TimeMinFloat, ref int ___m_TimeMin, ref int ___m_TimeHour, ref float ___m_SunlightRotationLerpTimer, ref bool ___m_IsBlendingSkybox, ref bool ___m_IsStopBlendingSkybox, ref float ___m_Timer, ref int ___m_SkyboxIndex) {
				try {
					if (!Settings.m_enabled.Value) {
						return true;
					}
					__instance.m_SkyboxBlender.Blend(0);
					__instance.m_SkyboxBlender.blendValue = 1f;
					___m_IsShopLightOn = false;
					__instance.m_SunlightGrp.SetActive(value: true);
					__instance.m_ShoplightGrp.SetActive(value: false);
					__instance.m_NightlightGrp.SetActive(value: false);
					ReflectionUtils.invoke_method(__instance, "EvaluateWorldUIBrightness");
					StartCoroutine(delay_update_env());
					return false;
				} catch (Exception e) {
					DDPlugin._error_log("** HarmonyPatch_LightManager_EvaluateTimeClock.Prefix ERROR - " + e);
				}
				return true;
			}

			private static IEnumerator delay_update_env() {

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
					/*
					if (___m_TimeHour == Settings.m_day_end_hour.Value) {
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
                    if (___m_TimeHour < Settings.m_day_begin_hour.Value) {
						___m_TimeMinFloat = 0;
						___m_TimeHour = Settings.m_day_begin_hour.Value;
					} else if (___m_TimeHour >= Settings.m_day_end_hour.Value) {
						___m_TimeMinFloat = 0;
						___m_TimeHour = Settings.m_day_end_hour.Value;
					}
					__instance.m_TimerLerpSpeed = (m_is_time_stopped || !CPlayerData.m_IsShopOnceOpen || ___m_HasDayEnded ? 0 : (Settings.m_time_speed.Value < 0 ?
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
                    __instance.m_SunlightGrp.SetActive(true);
					int total_minutes = ___m_TimeHour * 60 + ___m_TimeMin;
					float degrees = Mathf.Lerp(0, 360, total_minutes / 1440);
					DDPlugin._debug_log($"{degrees}");
					//Quaternion rotation = Quaternion.Euler(
					__instance.m_Sunlight.rotation = __instance.m_SunlightLerpStartPos.rotation; //Quaternion.Lerp(__instance.m_SunlightLerpStartPos.rotation, __instance.m_SunlightLerpEndPos.rotation, ___m_SunlightRotationLerpTimer);
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

		[HarmonyPatch(typeof(LightManager), "EvaluateLerpSunIntensity")]
		class HarmonyPatch_LightManager_EvaluateLerpSunIntensity {
			private static bool Prefix(LightManager __instance) {
				try {
					if (!Settings.m_enabled.Value) {
						return true;
					}
					DDPlugin._debug_log(".");
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
					
					___m_GlobalBrightness = 1f;

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
