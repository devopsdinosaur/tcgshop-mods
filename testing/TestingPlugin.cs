using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public static class PluginInfo {

	public const string TITLE = "Testing";
	public const string NAME = "testing";
	public const string SHORT_DESCRIPTION = "Just for debugging";

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
public class SuperDavePlugin : DDPlugin {
	private Harmony m_harmony = new Harmony(PluginInfo.GUID);

	private void Awake() {
		logger = this.Logger;
		try {
			this.m_harmony.PatchAll();
			logger.LogInfo($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	[HarmonyPatch(typeof(Customer), "IsSmelly")]
	class HarmonyPatch_Customer_IsSmelly {

		private static bool Prefix(ref bool __result) {
			__result = false;
			return false;
		}
	}

	[HarmonyPatch(typeof(Customer), "SetSmelly")]
	class HarmonyPatch_Customer_SetSmelly {

		private static bool Prefix(ref bool ___m_IsSmelly, ref int ___m_SmellyMeter) {
			___m_IsSmelly = false;
			___m_SmellyMeter = 0;
			return false;
		}
	}

	[HarmonyPatch(typeof(LightManager), "Awake")]
	class HarmonyPatch_LightManager_Awake {

		private static void Postfix(LightManager __instance) {
			__instance.m_TimerLerpSpeed = 0f; //-2f;

            TextMeshProUGUI template = GameUIScreen.Instance.m_ShopLevelText;
			TextMeshProUGUI info_text = GameObject.Instantiate<TextMeshProUGUI>(template, template.transform.parent);
			info_text.transform.localPosition += Vector3.down * 50;
			info_text.name = "CustomCustomers_Rollover_Info_Text";
			info_text.text = "Hello there!!!!!!";
			info_text.gameObject.SetActive(true);
        }
	}

    [HarmonyPatch(typeof(WorkerManager), "GetWorkerData")]
    class HarmonyPatch_WorkerManager_GetWorkerData {
        private static void Postfix(WorkerData __result) {
			__result.shopLevelRequired = 0;
        }
    }

    /*
	[HarmonyPatch(typeof(), "")]
	class HarmonyPatch_ {
		private static bool Prefix() {
			
			return true;
		}
	}

	[HarmonyPatch(typeof(), "")]
	class HarmonyPatch_ {
		private static void Postfix() {
			
		}
	}
	*/
}
