using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Pathfinding;

public static class PluginInfo {

	public const string TITLE = "Mind Control";
	public const string NAME = "mind_control";
	public const string SHORT_DESCRIPTION = "Completely change the AI parameters controlling customer decisions.  Buy more, play more, stay longer, even freeze completely!";

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
public class MindControlPlugin : DDPlugin {
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
	
	public class CustomerBrainWorm : MonoBehaviour {
		private Customer m_customer = null;
		private static bool m_are_all_frozen = false;
		private bool m_is_frozen = false;
		private float m_original_speed = 0f;
		private float m_original_animation_speed = 0f;
		private float m_base_speed = 1f;

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
			public static float max_time_wait_for_seat_at_table_single_attempt = 2f;
			public static float max_time_walk_to_play_table = 2f;
			public static float max_time_walk_to_seat = 3f;
			public static float max_time_walk_to_shelf = 1f;

			// Random chances (0-100)
			public static int chance_idle_at_play_table_if_no_seat = 40;

			// Chance multipliers
			public static float chance_multiplier_buy_item = 1;

			// Random ranges 
			public static float range_min_animation_speed_multiplier = 0.9f;
			public static float range_max_animation_speed_multiplier = 2.5f;
			public static int range_min_play_table_emote_delay = 5;
			public static int range_max_play_table_emote_delay = 30;
			public static int range_min_wait_for_seat_at_table_all_attempts = 5;
			public static int range_max_wait_for_seat_at_table_all_attempts = 30;
		}

		class HarmonyPatches {
			[HarmonyPatch(typeof(Customer), "ActivateCustomer")]
			class HarmonyPatch_Customer_ActivateCustomer {
				private static void Postfix(Customer __instance) {
					if (Settings.m_enabled.Value) {
						ensure_infected(__instance).activate_customer(__instance);
					}
				}
			}

			[HarmonyPatch(typeof(CustomerManager), "GetCustomerBuyItemChance")]
			class HarmonyPatch_CustomerManager_GetCustomerBuyItemChance {
				private static void Postfix(float currentPrice, float marketPrice, ref int __result) {
					if (Settings.m_enabled.Value) {
						get_customer_buy_item_chance(currentPrice, marketPrice, ref __result);
					}
				}
			}

			[HarmonyPatch(typeof(Customer), "Update")]
			class HarmonyPatch_Customer_Update {
				private static bool Prefix(Customer __instance, ref Quaternion ___m_TargetLerpRotation, float ___m_RotationLerpSpeed, ref Vector3 ___m_LastFramePos, ref bool ___m_IsCheckScanItemOutOfBound, ref float ___m_CheckScanItemOutOfBoundTimer, ref bool ___m_IsBeingSprayed, ref float ___m_BeingSprayedResetTimer, ref float ___m_BeingSprayedResetTimeMax, ref float ___m_Timer, ref int ___m_FailFindItemAttemptCount, List<Item> ___m_ItemInBagList, List<InteractableCard3d> ___m_CardInBagList, ref bool ___m_UnableToFindQueue, bool ___m_IsInsideShop, ref bool ___m_HasTookItemFromShelf, ref bool ___m_HasTookCardFromShelf, InteractableCashierCounter ___m_CurrentQueueCashierCounter, bool ___m_IsAtPayingPosition, InteractablePlayTable ___m_CurrentPlayTable, int ___m_CurrentPlayTableSeatIndex, Vector3 ___m_LerpStartPos, Vector3 ___m_TargetLerpPos, ref float ___m_SecondaryTimer, float ___m_SecondaryTimerMax, ref float ___m_TimerMax, ref bool ___m_ReachedEndOfPath, ref float ___m_LastRepath, Transform ___m_TargetTransform, ref bool ___m_IsWaitingForPathCallback, Path ___m_Path, ref int ___m_CurrentWaypoint, ref bool ___m_IsSmelly, float ___m_ModifiedSpeed, float ___m_ExtraSpeedMultiplier) {
					return (Settings.m_enabled.Value ? ensure_infected(__instance).update(__instance, ref ___m_TargetLerpRotation, ___m_RotationLerpSpeed, ref ___m_LastFramePos, ref ___m_IsCheckScanItemOutOfBound, ref ___m_CheckScanItemOutOfBoundTimer, ref ___m_IsBeingSprayed, ref ___m_BeingSprayedResetTimer, ref ___m_BeingSprayedResetTimeMax, ref ___m_Timer, ref ___m_FailFindItemAttemptCount, ___m_ItemInBagList, ___m_CardInBagList, ref ___m_UnableToFindQueue, ___m_IsInsideShop, ref ___m_HasTookItemFromShelf, ref ___m_HasTookCardFromShelf, ___m_CurrentQueueCashierCounter, ___m_IsAtPayingPosition, ___m_CurrentPlayTable, ___m_CurrentPlayTableSeatIndex, ___m_LerpStartPos, ___m_TargetLerpPos, ref ___m_SecondaryTimer, ___m_SecondaryTimerMax, ref ___m_TimerMax, ref ___m_ReachedEndOfPath, ref ___m_LastRepath, ___m_TargetTransform, ref ___m_IsWaitingForPathCallback, ___m_Path, ref ___m_CurrentWaypoint, ref ___m_IsSmelly, ___m_ModifiedSpeed, ___m_ExtraSpeedMultiplier) : true);
				}
			}
		}

		private void activate_customer(Customer __instance) {
			__instance.m_Anim.speed = (float) __instance.m_Anim.speed * UnityEngine.Random.Range(DecisionParams.range_min_animation_speed_multiplier, DecisionParams.range_max_animation_speed_multiplier);

		}

		private void Awake() {
		}

		private bool did_state_fail(int failure_count) {
			return failure_count > UnityEngine.Random.Range(DecisionParams.failure_count_range_min, DecisionParams.failure_count_range_max);
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

		private static void get_customer_buy_item_chance(float currentPrice, float marketPrice, ref int __result) {
			__result = Mathf.FloorToInt((float) __result * DecisionParams.chance_multiplier_buy_item);
		}

		private float get_time() {
			return (this.m_is_frozen ? 0 : Time.time);
		}

		private float get_delta_time(bool is_fixed = false) {
			return (this.m_is_frozen ? 0 : (is_fixed ? Time.fixedDeltaTime : Time.deltaTime));
		}

		private void set_state(ECustomerState state) {
			this.m_customer.m_CurrentState = state;
		}

		private bool update(Customer __instance, ref Quaternion ___m_TargetLerpRotation, float ___m_RotationLerpSpeed, ref Vector3 ___m_LastFramePos, ref bool ___m_IsCheckScanItemOutOfBound, ref float ___m_CheckScanItemOutOfBoundTimer, ref bool ___m_IsBeingSprayed, ref float ___m_BeingSprayedResetTimer, ref float ___m_BeingSprayedResetTimeMax, ref float ___m_Timer, ref int ___m_FailFindItemAttemptCount, List<Item> ___m_ItemInBagList, List<InteractableCard3d> ___m_CardInBagList, ref bool ___m_UnableToFindQueue, bool ___m_IsInsideShop, ref bool ___m_HasTookItemFromShelf, ref bool ___m_HasTookCardFromShelf, InteractableCashierCounter ___m_CurrentQueueCashierCounter, bool ___m_IsAtPayingPosition, InteractablePlayTable ___m_CurrentPlayTable, int ___m_CurrentPlayTableSeatIndex, Vector3 ___m_LerpStartPos, Vector3 ___m_TargetLerpPos, ref float ___m_SecondaryTimer, float ___m_SecondaryTimerMax, ref float ___m_TimerMax, ref bool ___m_ReachedEndOfPath, ref float ___m_LastRepath, Transform ___m_TargetTransform, ref bool ___m_IsWaitingForPathCallback, Path ___m_Path, ref int ___m_CurrentWaypoint, ref bool ___m_IsSmelly, float ___m_ModifiedSpeed, float ___m_ExtraSpeedMultiplier) {
			try {
				if (!__instance.m_IsActive) {
					return false;
				}
				float delta_time = this.get_delta_time();
				float fixed_delta_time = this.get_delta_time(true);
				float current_time = this.get_time();
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
				} else if (__instance.m_CurrentState == ECustomerState.ReadyToPay) {
					if (!___m_CurrentQueueCashierCounter) {
						ReflectionUtils.invoke_method(__instance, "ThinkWantToPay");
						return false;
					}
					if (___m_IsAtPayingPosition) {
						return false;
					}
				} else if (__instance.m_CurrentState == ECustomerState.WalkToPlayTable) {
					if ((___m_Timer += delta_time) > DecisionParams.max_time_walk_to_play_table) {
						if (!___m_IsInsideShop && !CPlayerData.m_IsShopOpen) {
							ReflectionUtils.invoke_method(__instance, "GoShopNotOpenState");
						} else if (!___m_IsInsideShop && LightManager.GetHasDayEnded()) {
							___m_FailFindItemAttemptCount = int.MaxValue;
							ReflectionUtils.invoke_method(__instance, "GoShopNotOpenState");
						} else if ((bool) ___m_CurrentPlayTable && !___m_CurrentPlayTable.IsQueueEmpty(___m_CurrentPlayTableSeatIndex)) {
							ReflectionUtils.invoke_method(__instance, "AttemptFindPlayTable");
						}
					}
				} else {
					if (__instance.m_CurrentState == ECustomerState.PlayingAtTable) {
						___m_Timer = Mathf.Clamp(___m_Timer + delta_time, 0f, 1f);
						__instance.transform.position = Vector3.Lerp(___m_LerpStartPos, ___m_TargetLerpPos, ___m_Timer * DecisionParams.max_time_walk_to_seat);
						if ((___m_SecondaryTimer += delta_time) >= ___m_SecondaryTimerMax) {
							__instance.m_Anim.SetInteger("RandomPlayIndex", UnityEngine.Random.Range(0, 5));
							__instance.m_Anim.SetTrigger("PlayGameEmote");
							___m_SecondaryTimerMax = UnityEngine.Random.Range(DecisionParams.range_min_play_table_emote_delay, DecisionParams.range_max_play_table_emote_delay);
							___m_SecondaryTimer = 0f;
						}
						return false;
					}
				}
				if (__instance.m_CurrentState == ECustomerState.QueueingPlayTable) {
					bool flag = false;
					if ((___m_Timer += delta_time) > DecisionParams.max_time_wait_for_seat_at_table_single_attempt) {
						___m_Timer = 0f;
						___m_TimerMax -= DecisionParams.max_time_wait_for_seat_at_table_single_attempt;
						if (!LightManager.GetHasDayEnded()) {
							ReflectionUtils.invoke_method(__instance, "OnReachedPathEnd");
						} else {
							flag = true;
						}
					}
					if (!(___m_Timer > ___m_TimerMax || flag)) {
						return false;
					}
					___m_CurrentPlayTable.CustomerUnbookQueueIndex(___m_CurrentPlayTableSeatIndex);
					___m_Timer = 0f;
					___m_TimerMax = 0f;
					if (UnityEngine.Random.Range(0, 100) > DecisionParams.chance_idle_at_play_table_if_no_seat) {
						if (___m_ItemInBagList.Count > 0 || ___m_CardInBagList.Count > 0) {
							ReflectionUtils.invoke_method(__instance, "ThinkWantToPay");
						} else {
							ReflectionUtils.invoke_method(__instance, "ExitShop");
						}
					} else {
						this.set_state(ECustomerState.Idle);
						___m_Timer = DecisionParams.max_time_idle;
					}
					return false;
				} else if (__instance.m_CurrentState == ECustomerState.EndingPlayTableGame) {
					___m_Timer += delta_time;
					__instance.transform.position = Vector3.Lerp(___m_LerpStartPos, ___m_TargetLerpPos, ___m_Timer * DecisionParams.max_time_walk_to_seat);
					if (___m_Timer <= ___m_TimerMax) {
						return false;
					}
					___m_Timer = 0f;
					___m_TimerMax = 0f;
					if (UnityEngine.Random.Range(0, 100) <= DecisionParams.chance_idle_at_play_table_if_no_seat) {
						if (___m_ItemInBagList.Count > 0 || ___m_CardInBagList.Count > 0) {
							ReflectionUtils.invoke_method(__instance, "ThinkWantToPay");
						} else {
							ReflectionUtils.invoke_method(__instance, "ExitShop");
						}
					} else {
						this.set_state(ECustomerState.Idle);
						___m_Timer = DecisionParams.max_time_idle;
					}
					return false;
				}
				if (!___m_ReachedEndOfPath && current_time > ___m_LastRepath + __instance.m_RepathRate && __instance.m_Seeker.IsDone() && (bool) ___m_TargetTransform) {
					___m_LastRepath = current_time;
					__instance.m_Seeker.StartPath(__instance.transform.position, ___m_TargetTransform.position, __instance.OnPathComplete);
					___m_IsWaitingForPathCallback = true;
				}
				if (___m_Path == null) {
					return false;
				}
				while (Vector3.Distance(__instance.transform.position, ___m_Path.vectorPath[___m_CurrentWaypoint]) < __instance.m_NextWaypointDistance) {
					if (!___m_IsInsideShop && __instance.m_CurrentState != ECustomerState.ExitingShop) {
						___m_IsInsideShop = CustomerManager.CheckIsInsideShop(base.transform.position);
						if (___m_IsInsideShop) {
							ReflectionUtils.invoke_method(__instance, "OnCustomerReachInsideShop");
						}
					} else if (___m_IsInsideShop && __instance.m_CurrentState == ECustomerState.ExitingShop && CustomerManager.CheckIsInsideShop(__instance.transform.position)) {
						___m_IsInsideShop = false;
						if (___m_IsSmelly) {
							___m_IsSmelly = false;
							CSingleton<CustomerManager>.Instance.RemoveFromSmellyCustomerList(__instance);
						}
					}
					if (___m_CurrentWaypoint + 1 < ___m_Path.vectorPath.Count) {
						___m_CurrentWaypoint++;
						continue;
					}
					if (!___m_ReachedEndOfPath) {
						___m_ReachedEndOfPath = true;
						ReflectionUtils.invoke_method(__instance, "OnReachedPathEnd");
					}
					ReflectionUtils.invoke_method(__instance, "WaypointEndUpdate");
					break;
				}
				__instance.transform.position = Vector3.MoveTowards(base.transform.position, ___m_Path.vectorPath[___m_CurrentWaypoint], ___m_ModifiedSpeed * ___m_ExtraSpeedMultiplier * delta_time);
				if (!___m_ReachedEndOfPath) {
					Vector3 vector = ___m_Path.vectorPath[___m_CurrentWaypoint] - __instance.transform.position;
					vector.y = 0f;
					if (vector != Vector3.zero) {
						___m_TargetLerpRotation = Quaternion.LookRotation(vector, Vector3.up);
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
		
	}
}
