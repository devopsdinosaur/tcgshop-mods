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
public class TestingPlugin : DDPlugin {
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

	public class CheatRestock {
		public static void cheat_restock_everything() {
			DDPlugin._debug_log("-- hotkey_triggered_test_method --");
			foreach (Shelf shelf in ShelfManager.GetShelfList()) {
				DDPlugin._debug_log($"shelf: {shelf.name}");
				List<ShelfCompartment> compartments = (List<ShelfCompartment>) ReflectionUtils.get_field_value(shelf, "m_ItemCompartmentList");
				if (compartments == null) {
					continue;
				}
				foreach (ShelfCompartment compartment in compartments) {
					EItemType item_type = compartment.GetItemType();
					DDPlugin._debug_log($"[{shelf.name}] index: {compartment.GetIndex()}, item_type: {item_type}");
					if (item_type == EItemType.None || compartment.GetItemCount() == compartment.GetMaxItemCount()) {
						continue;
					}
					compartment.SpawnItem(compartment.GetMaxItemCount() - compartment.GetItemCount(), false);
				}
			}
		}
	}

	class Smelliness {
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
	}

	class __Worker__ {
		[HarmonyPatch(typeof(WorkerManager), "GetWorkerData")]
		class HarmonyPatch_WorkerManager_GetWorkerData {
			private static void Postfix(WorkerData __result) {
				__result.shopLevelRequired = 0;
			}
		}

		private static bool set_speed_multiplier(ref float ___m_ExtraSpeedMultiplier) {
			___m_ExtraSpeedMultiplier = 3;
			return true;
		}

		[HarmonyPatch(typeof(Worker), "EvaluateWorkerAttribute")]
		class HarmonyPatch_Worker_EvaluateWorkerAttribute {
			private static void Postfix(ref float ___m_ExtraSpeedMultiplier) {
				set_speed_multiplier(ref ___m_ExtraSpeedMultiplier);
			}
		}

		[HarmonyPatch(typeof(Worker), "SetExtraSpeedMultiplier")]
		class HarmonyPatch_Worker_SetExtraSpeedMultiplier {
			private static bool Prefix(Worker __instance, ref float ___m_ExtraSpeedMultiplier) {
				return !set_speed_multiplier(ref ___m_ExtraSpeedMultiplier);
			}
		}

		[HarmonyPatch(typeof(Worker), "ResetExtraSpeedMultiplier")]
		class HarmonyPatch_Worker_ResetExtraSpeedMultiplier {
			private static bool Prefix(ref float ___m_ExtraSpeedMultiplier) {
				return !set_speed_multiplier(ref ___m_ExtraSpeedMultiplier);
			}
		}

		[HarmonyPatch(typeof(Worker), "Update")]
		class HarmonyPatch_Worker_Update {
			private static bool Prefix(ref float ___m_ExtraSpeedMultiplier) {
				set_speed_multiplier(ref ___m_ExtraSpeedMultiplier);
				return true;
			}
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
