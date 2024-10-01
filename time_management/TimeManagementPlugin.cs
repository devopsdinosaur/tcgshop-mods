using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;

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
	private static DateTime m_game_time = DateTime.MinValue;

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

	[HarmonyPatch(typeof(LightManager), "EvaluateTimeClock")]
	class HarmonyPatch_LightManager_EvaluateTimeClock {
		private static bool Prefix(LightManager __instance, int ___m_TimeMin, int ___m_TimeHour) {
			try {
				
				return true;

				if (!Settings.m_enabled.Value) {
					return true;
				}
				if (m_game_time == DateTime.MinValue) {
					m_game_time = new DateTime(0, 0, CPlayerData.m_CurrentDay, ___m_TimeHour, ___m_TimeMin, 0);
				}

				return false;
			} catch (Exception e) {
				DDPlugin._error_log("** HarmonyPatch_LightManager_EvaluateTimeClock ERROR - " + e);
			}
			return true;
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
