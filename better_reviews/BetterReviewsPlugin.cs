using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class PluginInfo {

	public const string TITLE = "Better Reviews";
	public const string NAME = "better_reviews";
	public const string SHORT_DESCRIPTION = "Tired of bad reviews for stuff you can't control?  This mod adds lots of little tweaks to the customer review system or just lets you disable bad reviews altogether!";

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
public class CustomCustomersPlugin : DDPlugin {
	private Harmony m_harmony = new Harmony(PluginInfo.GUID);

	private void Awake() {
		logger = this.Logger;
		try {
			this.plugin_info = PluginInfo.to_dict();
			DDPlugin.m_log_level = (this.get_nexus_dir() != null ? LogLevel.Debug : LogLevel.Info);
			Settings.Instance.load(this);
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

	[HarmonyPatch(typeof(Customer), "AddReviewData")]
	class HarmonyPatch_Customer_AddReviewData {
		private static bool Prefix(ECustomerReviewType reviewType, int higherStarChanceAdd) {
			try {
				if (!Settings.m_enabled.Value) {
					return true;
				}
				if (Settings.m_value_multiplier.Value > 1 && higherStarChanceAdd != 0) {
					higherStarChanceAdd = Mathf.FloorToInt((float) higherStarChanceAdd * (higherStarChanceAdd > 0 ? Settings.m_value_multiplier.Value : 1 / Settings.m_value_multiplier.Value));
				}
				if (Settings.m_disable_negative_modifiers.Value) {
					higherStarChanceAdd = Mathf.Max(higherStarChanceAdd, 0);
				}
				if (Settings.m_ignored_review_types[reviewType].Value) {
					return false;
				}
			} catch (Exception e) {
				DDPlugin._error_log("** HarmonyPatch_Customer_AddReviewData.Prefix ERROR - " + e);
			}
			return true;
		}
	}
}
