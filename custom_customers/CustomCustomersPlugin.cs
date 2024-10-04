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

	public const string VERSION = "0.0.5";

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
	
	class CustomerBrainWorm : MonoBehaviour {
		private Customer m_customer = null;
		private static bool m_are_all_frozen = false;
		private bool m_is_frozen = false;
		private float m_original_speed = 0f;
		private float m_original_animation_speed = 0f;

		public static class DecisionParams {
			// Each of these max_time_xxx's represent the amount of time a customer
			// will 'think' in the given state before it's considered a failure.
			// The failure count will then be incremented then the logic will
			// check the current count against
			// Random.Range(failure_count_range_min, failure_count_range_max)
			// for next level failure, i.e. eventually just leaving.
			public static int failure_count_range_min = 1;
			public static int failure_count_range_max = 5;
			public static float max_time_check_scan_item_out_of_bound = 3f;
			public static float max_time_idle = 2f;
			public static float max_time_look_for_card_shelf = 8f;
			public static float max_time_look_for_shelf = 8f;
			public static float max_time_look_for_table = 8f;
			public static float max_time_take_item_from_card_shelf = 1f;
			public static float max_time_take_item_from_shelf = 1f;
			public static float max_time_walk_to_shelf = 1f;
		}

		class HarmonyPatches {
			[HarmonyPatch(typeof(Customer), "ActivateCustomer")]
			class HarmonyPatch_Customer_ActivateCustomer {
				private static void Postfix(Customer __instance) {
					ensure_infected(__instance);
				}
			}

			[HarmonyPatch(typeof(Customer), "Update")]
			class HarmonyPatch_Customer_Update {
				private static bool Prefix(Customer __instance, Quaternion ___m_TargetLerpRotation, float ___m_RotationLerpSpeed, 
					ref Vector3 ___m_LastFramePos, ref bool ___m_IsCheckScanItemOutOfBound, ref float ___m_CheckScanItemOutOfBoundTimer,
					ref bool ___m_IsBeingSprayed, ref float ___m_BeingSprayedResetTimer, ref float ___m_BeingSprayedResetTimeMax,
					ref float ___m_Timer, ref int ___m_FailFindItemAttemptCount, List<Item> ___m_ItemInBagList, 
					List<InteractableCard3d> ___m_CardInBagList, ref bool ___m_UnableToFindQueue, bool ___m_IsInsideShop,
					ref bool ___m_HasTookItemFromShelf, ref bool ___m_HasTookCardFromShelf, InteractableCashierCounter m_CurrentQueueCashierCounter,
					bool m_IsAtPayingPosition
				) {
					return ensure_infected(__instance).update(__instance, ___m_TargetLerpRotation, ___m_RotationLerpSpeed, 
						ref ___m_LastFramePos, ref ___m_IsCheckScanItemOutOfBound, ref ___m_CheckScanItemOutOfBoundTimer,
						ref ___m_IsBeingSprayed, ref ___m_BeingSprayedResetTimer, ref ___m_BeingSprayedResetTimeMax,
						ref ___m_Timer, ref ___m_FailFindItemAttemptCount, ___m_ItemInBagList,
						___m_CardInBagList, ref ___m_UnableToFindQueue, ___m_IsInsideShop,
						ref ___m_HasTookItemFromShelf, ref ___m_HasTookCardFromShelf, m_CurrentQueueCashierCounter,
						m_IsAtPayingPosition
					);
				}
			}
		}

		private void Awake() {
		}

		public static List<CustomerBrainWorm> ensure_infected() {
			List<CustomerBrainWorm> result = new List<CustomerBrainWorm>();
			foreach (Customer customer in Resources.FindObjectsOfTypeAll<Customer>()) {
				result.Add(ensure_infected(customer));
			}
			return result;
		}

		public static CustomerBrainWorm ensure_infected(Customer customer) {
			CustomerBrainWorm worm = customer.gameObject.GetComponent<CustomerBrainWorm>();
			if (worm == null) {
				worm = customer.gameObject.AddComponent<CustomerBrainWorm>();
				worm.m_customer = customer;
			}
			return worm;
		}

		public static void toggle_freeze() {
			m_are_all_frozen = !m_are_all_frozen;
			foreach (CustomerBrainWorm worm in ensure_infected()) {
				worm.freeze(m_are_all_frozen);
			}
		}

		private void freeze(bool freeze) {
			if (freeze == this.m_is_frozen) {
				return;
			}
			if (this.m_is_frozen = freeze) {
				this.m_original_speed = (float) ReflectionUtils.get_field_value(this.m_customer, "m_ExtraSpeedMultiplier");
				this.m_original_animation_speed = this.m_customer.m_Anim.speed;
				this.m_customer.SetExtraSpeedMultiplier(0);
				this.m_customer.m_Anim.speed = 0;
			} else {
				this.m_customer.SetExtraSpeedMultiplier(this.m_original_speed);
				this.m_customer.m_Anim.speed = this.m_original_animation_speed;
			}
		}

		public static bool set_speed_multiplier(Customer customer, ref float ___m_ExtraSpeedMultiplier, float extraSpeedMultiplier = 0f) {
			if (!Settings.m_enabled.Value) {
				return false;
			}
			___m_ExtraSpeedMultiplier = (ensure_infected(customer).m_is_frozen ? 0 : (Settings.m_walk_speed_multiplier.Value != 0 ? Settings.m_walk_speed_multiplier.Value : extraSpeedMultiplier));
			return Settings.m_walk_speed_multiplier.Value != 0;
		}

		private float get_delta_time(bool is_fixed = false) {
			return (this.m_is_frozen ? 0 : (is_fixed ? Time.fixedDeltaTime : Time.deltaTime));
		}

		private bool did_state_fail(int failure_count) {
			return failure_count > UnityEngine.Random.Range(DecisionParams.failure_count_range_min, DecisionParams.failure_count_range_max);
		}

		private void set_state(ECustomerState state) {
			this.m_customer.m_CurrentState = state;
		}

		private bool update(Customer __instance, Quaternion ___m_TargetLerpRotation, float ___m_RotationLerpSpeed, 
			ref Vector3 ___m_LastFramePos, ref bool ___m_IsCheckScanItemOutOfBound, ref float ___m_CheckScanItemOutOfBoundTimer,
			ref bool ___m_IsBeingSprayed, ref float ___m_BeingSprayedResetTimer, ref float ___m_BeingSprayedResetTimeMax,
			ref float ___m_Timer, ref int ___m_FailFindItemAttemptCount, List<Item> ___m_ItemInBagList, 
			List<InteractableCard3d> ___m_CardInBagList, ref bool ___m_UnableToFindQueue, bool ___m_IsInsideShop,
			ref bool ___m_HasTookItemFromShelf, ref bool ___m_HasTookCardFromShelf, InteractableCashierCounter m_CurrentQueueCashierCounter,
			bool m_IsAtPayingPosition
		) {
			try {
				if (!__instance.m_IsActive) {
					return false;
				}
				float delta_time = this.get_delta_time();
				float fixed_delta_time = this.get_delta_time(true);
				__instance.transform.rotation = Quaternion.Lerp(__instance.transform.rotation, ___m_TargetLerpRotation, fixed_delta_time * ___m_RotationLerpSpeed);
				__instance.m_CurrentMoveSpeed = (___m_LastFramePos - __instance.transform.position).magnitude * 50f;
				___m_LastFramePos = __instance.transform.position;
				__instance.m_Anim.SetFloat("MoveSpeed", __instance.m_CurrentMoveSpeed);
				if (___m_IsCheckScanItemOutOfBound) {
					if ((___m_CheckScanItemOutOfBoundTimer += delta_time) > DecisionParams.max_time_check_scan_item_out_of_bound) {
						___m_CheckScanItemOutOfBoundTimer = 0f;
						ReflectionUtils.invoke_method(__instance, "CheckItemOutOfCashierBound");
					}
				}
				if (___m_IsBeingSprayed) {
					if ((___m_BeingSprayedResetTimer += delta_time) > ___m_BeingSprayedResetTimeMax) {
						___m_BeingSprayedResetTimer = 0f;
						___m_IsBeingSprayed = false;
						__instance.m_Anim.SetBool("IsBeingSprayed", value: false);
					}
				}
				if (__instance.m_CurrentState == ECustomerState.Idle) {
					if ((___m_Timer += delta_time) <= DecisionParams.max_time_idle) {
						return false;
					}
					___m_Timer = 0f;
					if (CPlayerData.m_IsShopOpen) {
						ReflectionUtils.invoke_method(__instance, "DetermineShopAction");
					} else if (this.did_state_fail(___m_FailFindItemAttemptCount)) {
						if (___m_ItemInBagList.Count > 0 || ___m_CardInBagList.Count > 0) {
							ReflectionUtils.invoke_method(__instance, "ThinkWantToPay");
						} else {
							ReflectionUtils.invoke_method(__instance, "ExitShop");
						}
					} else {
						ReflectionUtils.invoke_method(__instance, "GoShopNotOpenState");
					}
					return false;
				}
				if (__instance.m_CurrentState == ECustomerState.WantToBuyItem) {
					if (___m_UnableToFindQueue) {
						if ((___m_Timer += delta_time) > DecisionParams.max_time_look_for_shelf) {
							___m_Timer = 0f;
							ReflectionUtils.invoke_method(__instance, "AttemptFindShelf");
						}
					}
				} else if (__instance.m_CurrentState == ECustomerState.WantToBuyCard) {
					if (___m_UnableToFindQueue) {
						if ((___m_Timer += delta_time) > DecisionParams.max_time_look_for_card_shelf) {
							___m_Timer = 0f;
							ReflectionUtils.invoke_method(__instance, "AttemptFindCardShelf");
						}
					}
				} else if (__instance.m_CurrentState == ECustomerState.WantToPlayGame) {
					if (___m_UnableToFindQueue) {
						if ((___m_Timer += delta_time) > DecisionParams.max_time_look_for_table) {
							___m_Timer = 0f;
							ReflectionUtils.invoke_method(__instance, "AttemptFindPlayTable");
						}
					}
				} else if (__instance.m_CurrentState == ECustomerState.WalkToShelf) {
					if (!___m_IsInsideShop) {
						if ((___m_Timer += delta_time) > DecisionParams.max_time_walk_to_shelf) {
							___m_Timer = 0f;
							if (!CPlayerData.m_IsShopOnceOpen) {
								ReflectionUtils.invoke_method(__instance, "GetShopNotOpenState");
							} else if (LightManager.GetHasDayEnded()) {
								___m_FailFindItemAttemptCount = int.MaxValue;
								ReflectionUtils.invoke_method(__instance, "GetShopNotOpenState");
							}
						}
					}
				} else if (__instance.m_CurrentState == ECustomerState.TakingItemFromShelf) {
					if (!___m_HasTookItemFromShelf) {
						if ((___m_Timer += delta_time) > DecisionParams.max_time_take_item_from_shelf) {
							___m_Timer = 0f;
							___m_HasTookItemFromShelf = true;
							ReflectionUtils.invoke_method(__instance, "TakeItemFromShelf");
						}
					} else {
						this.set_state(ECustomerState.Idle);
					}
				} else if (__instance.m_CurrentState == ECustomerState.TakingItemFromCardShelf) {
					if (!___m_HasTookCardFromShelf) {
						if ((___m_Timer += delta_time) > DecisionParams.max_time_take_item_from_card_shelf) {
							___m_Timer = 0f;
							___m_HasTookCardFromShelf = true;
							ReflectionUtils.invoke_method(__instance, "TakeCardFromShelf");
						}
					} else {
						this.set_state(ECustomerState.Idle);
					}
				}
				return false;
			} catch (Exception e) {
				DDPlugin._error_log("** CustomerBrainWorm.update ERROR - " + e);
			}
			return true;
		}
	}

	public class __Testing__ {
		[HarmonyPatch(typeof(CustomerManager), "GetCustomerBuyItemChance")]
		class HarmonyPatch_CustomerManager_GetCustomerBuyItemChance {
			private static void Postfix(float currentPrice, float marketPrice, ref int __result) {
                __result = 100;
                DDPlugin._debug_log($"currentPrice: {currentPrice}, marketPrice: {marketPrice}, buyItemChance: {__result}");
			}
		}

		[HarmonyPatch(typeof(Customer), "TakeItemFromShelf")]
		class HarmonyPatch_Customer_TakeItemFromShelf {

			const int MIN_VOLUME = 100;
			const int MAX_VOLUME = 100;

			const int PER_ITEM_BUY_CHANCE_REDUCTION = 5;

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
							max_item_count = ___m_CurrentItemCompartment.GetItemCount();
						}
						max_item_count = Mathf.Clamp(max_item_count, 0, ___m_CurrentItemCompartment.GetItemCount());
						float itemPrice = CPlayerData.GetItemPrice(lastItem.GetItemType());
						float itemMarketPrice = CPlayerData.GetItemMarketPrice(lastItem.GetItemType());
						int customerBuyItemChance = CSingleton<CustomerManager>.Instance.GetCustomerBuyItemChance(itemPrice, itemMarketPrice);
						int current_item_buy_chance = customerBuyItemChance;
						int desired_item_count = 0;
						bool already_blabbed = false;
						for (int i = 0; i < max_item_count; i++) {
							if (!already_blabbed) {
								already_blabbed = (bool) ReflectionUtils.invoke_method(__instance, "BuyItemSpeechPopup", new object[] {customerBuyItemChance, itemPrice, itemData.name});
							}
							if (UnityEngine.Random.Range(0, 100) < current_item_buy_chance) {
								desired_item_count++;
								current_item_buy_chance -= PER_ITEM_BUY_CHANCE_REDUCTION;
							}
						}
						DDPlugin._debug_log($"max_item_count: {max_item_count}, desired_item_count: {desired_item_count}");
						for (int i = 0; i < desired_item_count; i++) {
							if (current_volume >= (float) max_volume || __instance.m_CurrentCostTotal >= __instance.m_MaxMoney || (lastItem = ___m_CurrentItemCompartment.GetLastItem()) == null) {
								break;
							}
							___m_CurrentItemCompartment.RemoveItem(lastItem);
							lastItem.m_Mesh.enabled = true;
							___m_ItemInBagList.Add(lastItem);
							lastItem.SetCurrentPrice(itemPrice);
							lastItem.LerpToTransform(__instance.m_ShoppingBagTransform, __instance.m_ShoppingBagTransform);
							lastItem.SetHideItemAfterFinishLerp();
							__instance.m_CurrentCostTotal += itemPrice;
							current_volume += lastItem.GetItemVolume();
						}
						DDPlugin._debug_log($"customer hash: {__instance.GetHashCode()}, current_volume: {current_volume}, max_volume: {max_volume}, current_cost: {__instance.m_CurrentCostTotal}, max_money: {__instance.m_MaxMoney}");
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

		public static void hotkey_triggered_test_method_2() {
			CustomerBrainWorm.toggle_freeze();
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
Max Cash:   {this.m_customer.m_MaxMoney:#0.00}
Item Total: {this.m_customer.m_CurrentCostTotal:#0.00}";
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
		private static bool set_speed_multiplier(Customer __instance, ref float ___m_ExtraSpeedMultiplier, float extraSpeedMultiplier = 0f) {
			return !CustomerBrainWorm.set_speed_multiplier(__instance, ref ___m_ExtraSpeedMultiplier, extraSpeedMultiplier);
		}

		[HarmonyPatch(typeof(Customer), "SetExtraSpeedMultiplier")]
		class HarmonyPatch_Customer_SetExtraSpeedMultiplier {
			private static bool Prefix(Customer __instance, float extraSpeedMultiplier, ref float ___m_ExtraSpeedMultiplier) {
				return !set_speed_multiplier(__instance, ref ___m_ExtraSpeedMultiplier, extraSpeedMultiplier);
			}
		}

		[HarmonyPatch(typeof(Customer), "ResetExtraSpeedMultiplier")]
		class HarmonyPatch_Customer_ResetExtraSpeedMultiplier {
			private static bool Prefix(Customer __instance, ref float ___m_ExtraSpeedMultiplier) {
				return !set_speed_multiplier(__instance, ref ___m_ExtraSpeedMultiplier);
			}
		}

		[HarmonyPatch(typeof(Customer), "Update")]
		class HarmonyPatch_Customer_Update {
			private static bool Prefix(Customer __instance, ref float ___m_ExtraSpeedMultiplier) {
				set_speed_multiplier(__instance, ref ___m_ExtraSpeedMultiplier);
				return true;
			}
		}
	}
}
