using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class PluginInfo {

	public const string TITLE = "Custom Customers";
	public const string NAME = "custom_customers";
	public const string SHORT_DESCRIPTION = "Configurable tweaks (walk speed, exact change, spending money, etc) to customers to improve quality of life for your shop!";

	public const string VERSION = "0.0.7";

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

	public class __Testing__ {
		
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

	class RolloverInfo {
		private static TextMeshProUGUI m_customer_info_text = null;
		private static CustomerCollider m_hit_collider = null;

		class CustomerCollider : MonoBehaviour {
			public Customer m_customer = null;
			private static int m_layer_physics = 0;
			private static int m_layer_shop_model = 0;

			public static void add_collider_to_customer(Customer __instance) {
				try {
					const string NAME = "CustomCustomers_Customer_Collider_Object";
					Transform collider_object = __instance.transform.Find(NAME);
					if (collider_object != null) {
						return;
					}
					Transform prefab = WorkerManager.Instance.m_WorkerPrefab.transform.Find("WorkerCollider");
					if (prefab == null) {
						DDPlugin._error_log("** HarmonyPatch_Customer_Start ERROR - unable to find Worker -> WorkerCollider prefab gamebject.");
						return;
					}
					GameObject obj = GameObject.Instantiate<GameObject>(prefab.gameObject, __instance.transform);
					obj.name = NAME;
					BoxCollider box = obj.GetComponent<BoxCollider>();
					GameObject.Destroy(obj.GetComponent<WorkerCollider>());
					obj.AddComponent<CustomerCollider>().m_customer = __instance;
					obj.layer = LayerMask.NameToLayer("Physics");
					obj.SetActive(true);
				} catch (Exception e) {
					DDPlugin._error_log("** CustomerCollider.add_collider_to_customer ERROR - " + e);
				}
			}

			public static void ensure_tooltip_object() {
				if (m_customer_info_text != null) {
					return;
				}
				m_customer_info_text = GameObject.Instantiate<TextMeshProUGUI>(GameUIScreen.Instance.m_ShopLevelText, CenterDot.Instance.m_DotImage.transform.parent);
				m_customer_info_text.transform.localPosition = CenterDot.Instance.m_DotImage.transform.localPosition + Vector3.down * 20f;
				m_customer_info_text.name = "CustomCustomers_Rollover_Info_Text";
				m_customer_info_text.alignment = TextAlignmentOptions.TopLeft;
				m_customer_info_text.enableAutoSizing = true;
				m_customer_info_text.enableWordWrapping = false;
				m_customer_info_text.fontSizeMin = m_customer_info_text.fontSizeMax = Settings.m_rollover_customer_info_font_size.Value;
				m_customer_info_text.gameObject.SetActive(false);
			}

			public static void hide_tooltip() {
				m_customer_info_text.gameObject.SetActive(false);
			}

			public static void raycast_customers(InteractionPlayerController __instance) {
				try {
					if (!Settings.m_enabled.Value || !(Settings.m_rollover_customer_info_enabled.Value || Settings.m_rollover_shop_info_enabled.Value)) {
						return;
					}
					Ray ray = new Ray(__instance.m_Cam.transform.position, __instance.m_Cam.transform.forward);
					if (m_layer_physics == 0) {
						m_layer_physics = LayerMask.GetMask("Physics");
					}
					if (m_layer_shop_model == 0) {
						m_layer_shop_model = LayerMask.GetMask("ShopModel");
					}
					int mask = (Settings.m_rollover_customer_info_enabled.Value ? m_layer_physics : 0) | (Settings.m_rollover_shop_info_enabled.Value ? m_layer_shop_model : 0);
					CustomerCollider collider;
					if (Physics.Raycast(ray, out var hit, __instance.m_RayDistance * 5, mask)) {
						if (Settings.m_rollover_customer_info_enabled.Value && (collider = hit.transform.GetComponent<CustomerCollider>()) != null) {
							(m_hit_collider = collider).show_tooltip();
							return;
						} else if (Settings.m_rollover_shop_info_enabled.Value && (hit.transform.GetComponent<InteractableCashierCounter>() != null || hit.transform.GetComponent<InteractableOpenCloseSign>() != null)) {
							show_shop_tooltip();
							return;
						}
					}	
					hide_tooltip();
					m_hit_collider = null;
				} catch (Exception e) {
					DDPlugin._error_log("** CustomerCollider.raycast_customers ERROR - " + e);
				}
			}

			public void show_tooltip() {
				m_customer_info_text.text = @$"
Max Cash: {this.m_customer.m_MaxMoney:#0.00}
Item Total: {this.m_customer.m_CurrentCostTotal:#0.00}
Cur State: {Enum.GetName(typeof(ECustomerState), this.m_customer.m_CurrentState)}
";
				m_customer_info_text.gameObject.SetActive(true);
			}

			public static void show_shop_tooltip() {
				Dictionary<ECustomerState, int> state_counts = new Dictionary<ECustomerState, int>();
				foreach (ECustomerState state in Enum.GetValues(typeof(ECustomerState))) {
					state_counts[state] = 0;
				}
				List<Customer> customers = (List<Customer>) ReflectionUtils.get_field_value(CustomerManager.Instance, "m_CustomerList");
				int active_customer_count = 0;
				foreach (Customer customer in customers) {
					if (customer.gameObject.activeSelf) {
						state_counts[customer.m_CurrentState]++;
						active_customer_count++;
					}
				}
				m_customer_info_text.text = @$"
Shop Status: {(CPlayerData.m_IsShopOpen ? "Open" : "Closed")}
Customers (Total: {active_customer_count} / {Mathf.Max(CustomerManager.Instance.m_CustomerCountMax, active_customer_count)} [max])
- Browsing: {state_counts[ECustomerState.WalkToShelf] + state_counts[ECustomerState.TakingItemFromShelf] + state_counts[ECustomerState.WalkToCardShelf] + state_counts[ECustomerState.TakingItemFromCardShelf]}
- Exiting: {state_counts[ECustomerState.ExitingShop]}
- Idle: {state_counts[ECustomerState.Idle]}
- In Line: {state_counts[ECustomerState.QueuingToPay] + state_counts[ECustomerState.ReadyToPay]}
- Playing: {state_counts[ECustomerState.PlayingAtTable]}
- Thinking: {state_counts[ECustomerState.WantToPay]}
- Waiting for Table: {state_counts[ECustomerState.WantToPlayGame]}
";
				m_customer_info_text.gameObject.SetActive(true);
			}

			class HarmonyPatches {
				[HarmonyPatch(typeof(LightManager), "Awake")]
				class HarmonyPatch_LightManager_Awake {
					private static void Postfix(LightManager __instance) {
						CustomerCollider.ensure_tooltip_object();
					}
				}

				[HarmonyPatch(typeof(InteractionPlayerController), "RaycastNormalState")]
				class HarmonyPatch_InteractionPlayerController_RaycastNormalState {
					private static void Postfix(InteractionPlayerController __instance) {
						CustomerCollider.raycast_customers(__instance);
					}
				}

				[HarmonyPatch(typeof(InteractionPlayerController), "RaycastCashCounterState")]
				class HarmonyPatch_InteractionPlayerController_RaycastCashCounterState {
					private static void Postfix(InteractionPlayerController __instance) {
						CustomerCollider.hide_tooltip();
					}
				}

				[HarmonyPatch(typeof(Customer), "Start")]
				class HarmonyPatch_Customer_Start {
					private static void Postfix(Customer __instance) {
						CustomerCollider.add_collider_to_customer(__instance);
					}
				}
			}
		}
	}

	class Spawn {
		[HarmonyPatch(typeof(CustomerManager), "Start")]
		class HarmonyPatch_CustomerManager_Start {
			private static bool Prefix(CustomerManager __instance) {
				try {
					GameObject prefabs_parent = new GameObject("Customer_Prefabs_Parent");
					prefabs_parent.transform.SetParent(__instance.m_CustomerParentGrp.parent);
					__instance.m_CustomerFemalePrefab.transform.SetParent(prefabs_parent.transform);
					__instance.m_CustomerPrefab.transform.SetParent(prefabs_parent.transform);
					GameObject trash_parent = new GameObject("Destined_to_Die");
					trash_parent.transform.SetParent(__instance.m_CustomerParentGrp.parent);
					List<GameObject> children = new List<GameObject>();
					for (int index = 0; index < __instance.m_CustomerParentGrp.childCount; index++) {
						children.Add(__instance.m_CustomerParentGrp.GetChild(index).gameObject);
					}
					foreach (GameObject child in children) {
						child.transform.SetParent(trash_parent.transform);
					}
					GameObject.Destroy(trash_parent);
					for (int index = 0; index < Settings.m_max_customer_models.Value; index++) {
						Customer customer = GameObject.Instantiate((UnityEngine.Random.Range(1, 100) <= Settings.m_spawn_percent_female.Value ? __instance.m_CustomerFemalePrefab : __instance.m_CustomerPrefab), __instance.m_CustomerParentGrp);
						customer.name = $"CustomCustomer_{index:D3}";
						customer.transform.SetSiblingIndex(index);
					}
					return true;
				} catch (Exception e) {
					DDPlugin._error_log("** HarmonyPatch_CustomerManager_Start.Prefix ERROR - " + e);
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(CustomerManager), "EvaluateMaxCustomerCount")]
		class HarmonyPatch_CustomerManager_EvaluateMaxCustomerCount {
			private static void Postfix(CustomerManager __instance) {
				if (!Settings.m_enabled.Value) {
					return;
				}
				if (Settings.m_spawn_frequency_multiplier.Value > 0) {
					__instance.m_TimePerCustomer *= Settings.m_spawn_frequency_multiplier.Value;
				}
				if (Settings.m_spawn_max_customer_multiplier.Value > 0) {
					__instance.m_CustomerCountMax = Mathf.Min(Mathf.RoundToInt(__instance.m_CustomerCountMax * Settings.m_spawn_max_customer_multiplier.Value), Settings.m_max_customer_models.Value);
				}
			}
		}
	}

	class WalkSpeed {
		private static bool set_speed_multiplier(ref float ___m_ExtraSpeedMultiplier) {
			if (Settings.m_enabled.Value && Settings.m_walk_speed_multiplier.Value > 0) {
				___m_ExtraSpeedMultiplier = Settings.m_walk_speed_multiplier.Value;
				return false;
			}
			return true;
		}

		[HarmonyPatch(typeof(Customer), "SetExtraSpeedMultiplier")]
		class HarmonyPatch_Customer_SetExtraSpeedMultiplier {
			private static bool Prefix(ref float ___m_ExtraSpeedMultiplier) {
				return !set_speed_multiplier( ref ___m_ExtraSpeedMultiplier);
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
}
