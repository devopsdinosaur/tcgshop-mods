using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;

public static class PluginInfo {

	public const string TITLE = "Custom Customers";
	public const string NAME = "custom_customers";
	public const string SHORT_DESCRIPTION = "Configurable tweaks (walk speed, exact change, spending money, etc) to customers to improve quality of life for your shop!";

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
			Settings.Instance.load(this);
			this.plugin_info = PluginInfo.to_dict();
			this.create_nexus_page();
			this.m_harmony.PatchAll();
			PluginUpdater.create(this.gameObject, logger);
			logger.LogInfo($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	class WalkSpeed {
		private static bool set_speed_multiplier(ref float ___m_ExtraSpeedMultiplier) {
			if (Settings.m_enabled.Value && Settings.m_walk_speed_multiplier.Value > 0) {
				___m_ExtraSpeedMultiplier = Settings.m_walk_speed_multiplier.Value;
				return true;
			}
			return false;
		}

		[HarmonyPatch(typeof(Customer), "SetExtraSpeedMultiplier")]
		class HarmonyPatch_Customer_SetExtraSpeedMultiplier {
			private static bool Prefix(ref float ___m_ExtraSpeedMultiplier) {
				return !set_speed_multiplier(ref ___m_ExtraSpeedMultiplier);
			}
		}

		[HarmonyPatch(typeof(Customer), "ResetExtraSpeedMultiplier")]
		class HarmonyPatch_Customer_ResetExtraSpeedMultiplier {
			private static bool Prefix(ref float ___m_ExtraSpeedMultiplier) {
				return !set_speed_multiplier(ref ___m_ExtraSpeedMultiplier);
			}
		}

		[HarmonyPatch(typeof(Customer), "Update")]
		class HarmonyPatch_Customer_Update {
			private static bool Prefix(ref float ___m_ExtraSpeedMultiplier) {
				set_speed_multiplier(ref ___m_ExtraSpeedMultiplier);
				return true;
			}
		}
	}

	class ExactChange {
		[HarmonyPatch(typeof(Customer), "GetRandomPayAmount")]
		class HarmonyPatch_Customer_GetRandomPayAmount {
			private static bool Prefix(float limit, ref float __result) {
				if (Settings.m_enabled.Value && Settings.m_always_exact_change.Value) {
					__result = limit;
					return false;
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(CustomerManager), "GetCustomerExactChangeChance")]
		class HarmonyPatch_CustomerManager_GetCustomerExactChangeChance {
			private static bool Prefix(ref int __result) {
				if (Settings.m_enabled.Value && Settings.m_always_exact_change.Value) {
					__result = 100;
					return false;
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(InteractableCustomerCash), "SetIsCard")]
		class HarmonyPatch_InteractableCustomerCash_SetIsCard {
			private static bool Prefix(ref bool isCard) {
				if (Settings.m_enabled.Value && Settings.m_always_exact_change.Value) {
					isCard = false;
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(InteractableCashierCounter), "SetCustomerPaidAmount")]
		class HarmonyPatch_InteractableCashierCounter_SetCustomerPaidAmount {
			private static bool Prefix(ref bool isUseCard) {
				if (Settings.m_enabled.Value && Settings.m_always_exact_change.Value) {
					isUseCard = false;
				}
				return true;
			}
		}
	}

	class MaxMoney {
		[HarmonyPatch(typeof(CustomerManager), "GetCustomerMaxMoney")]
		class HarmonyPatch_CustomerManager_GetCustomerMaxMoney {
			private static void Postfix(ref float __result) {
				if (Settings.m_enabled.Value && Settings.m_max_money_multiplier.Value > 0) {
					__result *= __result;
				}
			}
		}
	}
}
