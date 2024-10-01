﻿using BepInEx;
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
		//[HarmonyPatch(typeof(CustomerManager), "GetCustomerBuyItemChance")]
		class HarmonyPatch_CustomerManager_GetCustomerBuyItemChance {
			private static void Postfix(float currentPrice, float marketPrice, ref int __result) {
                __result = 100;
                DDPlugin._debug_log($"currentPrice: {currentPrice}, marketPrice: {marketPrice}, buyItemChance: {__result}");
			}
		}

		//[HarmonyPatch(typeof(Customer), "TakeItemFromShelf")]
		class HarmonyPatch_Customer_TakeItemFromShelf {

			const int MIN_VOLUME = 100;
			const int MAX_VOLUME = 100;



			private static bool Prefix(Customer __instance, ref bool ___m_IsInsideShop, Shelf ___m_CurrentShelf, ShelfCompartment ___m_CurrentItemCompartment, bool ___m_IsSmelly, List<Item> ___m_ItemInBagList, ref int ___m_FailFindItemAttemptCount, ref Coroutine ___m_DecideFinishShopping, ref bool ___m_HasTookItemFromShelf) {
				try {
					DDPlugin._debug_log("TakeItemFromShelf");
					___m_IsInsideShop = true;
					if (!___m_CurrentShelf) {
						__instance.m_CurrentState = ECustomerState.Idle;
						return false;
					}
					if (___m_CurrentItemCompartment.GetItemCount() > 0 && ___m_CurrentShelf.IsValidObject()) {
						Item lastItem = ___m_CurrentItemCompartment.GetLastItem();
						ItemData itemData = InventoryBase.GetItemData(lastItem.GetItemType());
						int max_item_count;
						int max_volume = UnityEngine.Random.Range(MIN_VOLUME, MAX_VOLUME);
						float current_volume = 0f;
						//if (itemData.isNotBoosterPack) {
						//	if (max_item_count > 1) {
						//		max_item_count /= 2;
						//	} else if (UnityEngine.Random.Range(0, 100) < 75) {
						//		max_item_count = 1;
						//	}
						//}
						if (lastItem.GetItemType() == EItemType.Deodorant) {
							max_item_count = ((!___m_IsSmelly) ? UnityEngine.Random.Range(0, 4) : 0);
						} else {
							max_item_count = UnityEngine.Random.Range(0, ___m_CurrentItemCompartment.GetItemCount() + 1 + (___m_CurrentItemCompartment.GetItemCount() > 3 ? 0 : Mathf.Max(Mathf.RoundToInt((50f - itemData.GetItemVolume()) / 10f), 0))); //UnityEngine.Random.Range(0, Mathf.Clamp(___m_CurrentItemCompartment.GetItemCount() + 1 + num, 4, 14));
						}
						max_item_count = Mathf.Clamp(max_item_count, 0, ___m_CurrentItemCompartment.GetItemCount());
						bool flag = false;
						for (int i = 0; i < max_item_count; i++) {
							lastItem = ___m_CurrentItemCompartment.GetLastItem();
							if (!lastItem) {
								continue;
							}
							float itemPrice = CPlayerData.GetItemPrice(lastItem.GetItemType());
							float itemMarketPrice = CPlayerData.GetItemMarketPrice(lastItem.GetItemType());
							int customerBuyItemChance = CSingleton<CustomerManager>.Instance.GetCustomerBuyItemChance(itemPrice, itemMarketPrice);
							if (!flag) {
								flag = (bool) ReflectionUtils.invoke_method(__instance, "BuyItemSpeechPopup", new object[] {customerBuyItemChance, itemPrice, itemData.name});
							}
							if (UnityEngine.Random.Range(0, 100) < customerBuyItemChance) {
								___m_CurrentItemCompartment.RemoveItem(lastItem);
								lastItem.m_Mesh.enabled = true;
								___m_ItemInBagList.Add(lastItem);
								lastItem.SetCurrentPrice(itemPrice);
								lastItem.LerpToTransform(__instance.m_ShoppingBagTransform, __instance.m_ShoppingBagTransform);
								lastItem.SetHideItemAfterFinishLerp();
								__instance.m_CurrentCostTotal += itemPrice;
								current_volume += lastItem.GetItemVolume();
								if (current_volume >= (float) max_volume || __instance.m_CurrentCostTotal >= __instance.m_MaxMoney) {
									break;
								}
							}
						}
						DDPlugin._debug_log($"hash: {__instance.GetHashCode()}, current_volume: {current_volume}, max_volume: {max_volume}, current_cost: {__instance.m_CurrentCostTotal}, max_money: {__instance.m_MaxMoney}");
						if (current_volume > 0f) {
							__instance.m_ShoppingBagTransform.gameObject.SetActive(value: true);
							__instance.m_Anim.SetBool("HoldingBag", value: true);
							__instance.m_Anim.SetTrigger("GrabItem");
						} else {
							___m_FailFindItemAttemptCount++;
						}
						if (___m_DecideFinishShopping != null) {
							__instance.StopCoroutine(___m_DecideFinishShopping);
						}
						___m_DecideFinishShopping = __instance.StartCoroutine((IEnumerator) ReflectionUtils.invoke_method(__instance, "DecideIfFinishShopping"));
					} else {
						__instance.m_Anim.SetTrigger("ShakeHead");
						___m_HasTookItemFromShelf = false;
						___m_FailFindItemAttemptCount++;
						__instance.m_CurrentState = ECustomerState.Idle;
					}
					return false;
				} catch (Exception e) {
					DDPlugin._error_log("** HarmonyPatch_Customer_TakeItemFromShelf ERROR - " + e);
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(Customer), "ActivateCustomer")]
		class HarmonyPatch_Customer_ActivateCustomer {

			class CustomCustomers_FuzzifyAnimator : MonoBehaviour {
				const float MIN_FUZZY_TIME = 0;
				const float MAX_FUZZY_TIME = 10;
				public Customer m_customer = null;
				private Vector3 m_original_position;
				private float m_fuzzy_time;
				private float m_elapsed;

				private void Awake() {
					this.m_fuzzy_time = UnityEngine.Random.Range(MIN_FUZZY_TIME, MAX_FUZZY_TIME);
					this.m_elapsed = 0;
					this.m_original_position = this.m_customer.m_Anim.rootPosition;
					this.m_customer.m_Anim.enabled = false;
				}

				private void Update() {
					if ((this.m_elapsed += Time.deltaTime) > this.m_fuzzy_time) {
						this.m_customer.m_Anim.enabled = true;
						GameObject.Destroy(this);
						return;
					}
					this.m_customer.m_Anim.rootPosition = this.m_original_position;
				}
			}
			
			private static void Postfix(Customer __instance) {
				//__instance.gameObject.AddComponent<CustomCustomers_FuzzifyAnimator>().m_customer = __instance;
			}
		}
		
		public static void hotkey_triggered_test_method() {
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

	class RolloverInfo {
		private static TextMeshProUGUI m_customer_info_text = null;
		private static CustomerCollider m_hit_collider = null;

		class CustomerCollider : MonoBehaviour {
			public Customer m_customer = null;

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
					if (!Settings.m_enabled.Value || !Settings.m_rollover_customer_info_enabled.Value) {
						return;
					}
					Ray ray = new Ray(__instance.m_Cam.transform.position, __instance.m_Cam.transform.forward);
					int mask = LayerMask.GetMask("Physics");
					CustomerCollider collider;
					if (Physics.Raycast(ray, out var hit, __instance.m_RayDistance * 3, mask) && (collider = hit.transform.GetComponent<CustomerCollider>()) != null) {
						(m_hit_collider = collider).show_tooltip();
					} else if (m_hit_collider != null) {
						CustomerCollider.hide_tooltip();
						m_hit_collider = null;
					}
				} catch (Exception e) {
					DDPlugin._error_log("** CustomerCollider.raycast_customers ERROR - " + e);
				}
			}

			public void show_tooltip() {
				m_customer_info_text.text = @$"
Max Cash:   {this.m_customer.m_MaxMoney}
Item Total: {this.m_customer.m_CurrentCostTotal}";
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
					__instance.m_CustomerCountMax = Mathf.RoundToInt(__instance.m_CustomerCountMax * Settings.m_spawn_max_customer_multiplier.Value);
				}
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
}
