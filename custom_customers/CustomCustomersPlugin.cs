using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class PluginInfo {

	public const string TITLE = "Custom Customers";
	public const string NAME = "custom_customers";
	public const string SHORT_DESCRIPTION = "Configurable tweaks (walk speed, exact change, spending money, etc) to customers to improve quality of life for your shop!";

	public const string VERSION = "0.0.4";

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
			Settings.Instance.load(this);
			this.plugin_info = PluginInfo.to_dict();
			this.create_nexus_page();
			this.m_harmony.PatchAll();
			PluginUpdater.create(this.gameObject, logger);
			PluginUpdater.Instance.register("testing", 0.5f, Testing.Updaters.testing_update);
			logger.LogInfo($"{PluginInfo.GUID} v{PluginInfo.VERSION} loaded.");
		} catch (Exception e) {
			logger.LogError("** Awake FATAL - " + e);
		}
	}

	public class Testing {

		[HarmonyPatch(typeof(Customer), "Start")]
        class HarmonyPatch_Customer_Start {
            private static bool Prefix(Customer __instance) {
                if (__instance.m_DebugCube.GetComponent<CapsuleCollider>() == null) {
                    __instance.m_DebugCube.AddComponent<CapsuleCollider>();
                    __instance.m_DebugCube.SetActive(true);
                }
                return true;
            }
        }
		/*
		[HarmonyPatch(typeof(InteractionPlayerController), "RaycastNormalState")]
		class HarmonyPatch_InteractionPlayerController_RaycastNormalState {
			private static void Postfix(InteractionPlayerController __instance) {
				Ray ray = new Ray(__instance.m_Cam.transform.position, __instance.m_Cam.transform.forward);
				Customer hit_customer = null;
				RaycastHit closest_hit = new RaycastHit();
				closest_hit.distance = float.MaxValue;
				foreach (Customer check_customer in Resources.FindObjectsOfTypeAll<Customer>()) {
					//DDPlugin._debug_log(check_customer.name);
					RaycastHit check_hit;
					if (check_customer.gameObject.activeSelf && check_customer.GetComponent<CapsuleCollider>().Raycast(ray, out check_hit, __instance.m_RayDistance) && check_hit.distance < closest_hit.distance) {
						hit_customer = check_customer;
						closest_hit = check_hit;
					}
				}
				if (hit_customer == null) {
					return;
				}
				hit_customer.m_DebugCube.SetActive(false);
			}
		}
		*/

        public class Updaters {
			public static void testing_update() {
				//foreach (Customer customer in Resources.FindObjectsOfTypeAll<Customer>()) {
				//	if (!customer.gameObject.activeSelf || !customer.GetComponent<SphereCollider>().Raycast(new Ray(CC.CameraController.instance.) {
				//}
			}
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

	class WorkerWalkSpeed {
		private static bool set_speed_multiplier(ref float ___m_ExtraSpeedMultiplier) {
			if (Settings.m_enabled.Value && Settings.m_worker_walk_speed_multiplier.Value > 0) {
				___m_ExtraSpeedMultiplier = Settings.m_worker_walk_speed_multiplier.Value;
				return true;
			}
			return false;
		}

		[HarmonyPatch(typeof(Worker), "EvaluateWorkerAttribute")]
		class HarmonyPatch_Worker_EvaluateWorkerAttribute {
			private static void Postfix(ref float ___m_ExtraSpeedMultiplier) {
				set_speed_multiplier(ref ___m_ExtraSpeedMultiplier);
			}
		}

		[HarmonyPatch(typeof(Worker), "SetExtraSpeedMultiplier")]
		class HarmonyPatch_Worker_SetExtraSpeedMultiplier {
			private static bool Prefix(ref float ___m_ExtraSpeedMultiplier) {
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

		/*
		[HarmonyPatch(typeof(InteractableCustomerCash), "SetIsCard")]
		class HarmonyPatch_InteractableCustomerCash_SetIsCard {
			private static bool Prefix(ref bool isCard) {
				if (Settings.m_enabled.Value) {
					if (Settings.m_always_exact_change.Value) {
						isCard = false;
					} else if (Settings.m_auto_populate_credit.Value) {
						isCard = true;
					}
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(InteractableCashierCounter), "SetCustomerPaidAmount")]
		class HarmonyPatch_InteractableCashierCounter_SetCustomerPaidAmount {
			private static bool Prefix(ref bool isUseCard) {
				if (Settings.m_enabled.Value) {
					if (Settings.m_always_exact_change.Value) {
						isUseCard = false;
					} else if (Settings.m_auto_populate_credit.Value) {
						isUseCard = true;
					}
				}
				return true;
			}
		}
		*/

        [HarmonyPatch(typeof(UI_CreditCardScreen), "EnableCreditCardMode")]
        class HarmonyPatch_UI_CreditCardScreen_EnableCreditCardMode {
            private static void Postfix(bool isPlayer, ref float ___m_CurrentNumberValue, TextMeshProUGUI ___m_TotalPriceText, InteractableCashierCounter ___m_CashierCounter) {
				if (!Settings.m_enabled.Value || !isPlayer || !Settings.m_auto_populate_credit.Value) {
					return;
				}
                ___m_CurrentNumberValue = (float) ReflectionUtils.get_field_value(___m_CashierCounter, "m_TotalScannedItemCost");
				___m_TotalPriceText.text = GameInstance.GetPriceString(___m_CurrentNumberValue / GameInstance.GetCurrencyConversionRate());
            }
        }
    }

	class MaxMoney {
		[HarmonyPatch(typeof(CustomerManager), "GetCustomerMaxMoney")]
		class HarmonyPatch_CustomerManager_GetCustomerMaxMoney {
			private static void Postfix(ref float __result) {
				if (Settings.m_enabled.Value && Settings.m_max_money_multiplier.Value > 0) {
					__result *= Settings.m_max_money_multiplier.Value;
				}
			}
		}
	}
}
