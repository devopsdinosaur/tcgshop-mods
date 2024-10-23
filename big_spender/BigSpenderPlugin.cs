using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class PluginInfo {

	public const string TITLE = "Big Spender";
	public const string NAME = "big_spender";
	public const string SHORT_DESCRIPTION = "No more single booster purchases!  Adjust customers' chance to buy and amount of items they pick up.";

	public const string VERSION = "0.0.2";

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
public class BigSpenderPlugin : DDPlugin {
	private Harmony m_harmony = new Harmony(PluginInfo.GUID);

	private void Awake() {
		logger = this.Logger;
		try {
			Settings.Instance.load(this);
			this.m_plugin_info = PluginInfo.to_dict();
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
				
			}
		}

		[HarmonyPatch(typeof(CGameManager), "Update")]
		class HarmonyPatch_CGameManager_Update {
			private static void Postfix(CGameManager __instance) {
				
			}
		}
	}

	class BigSpenderBrain : MonoBehaviour {
		public Customer m_customer;
		private Coroutine m_routine = null;

		public static void decide_if_finish_shopping(Customer customer, List<Item> ___m_ItemInBagList, List<InteractableCard3d> ___m_CardInBagList) {
			BigSpenderBrain brain = customer.gameObject.GetComponent<BigSpenderBrain>();
			if (brain.m_routine != null) {
				brain.StopCoroutine(brain.m_routine);
			}
			brain.m_routine = brain.StartCoroutine(brain.routine_decide_if_finish_shopping(___m_ItemInBagList, ___m_CardInBagList));
		}

		private IEnumerator routine_decide_if_finish_shopping(List<Item> ___m_ItemInBagList, List<InteractableCard3d> ___m_CardInBagList) {
			yield return new WaitForSeconds(1.5f);
			ReflectionUtils.get_field(this.m_customer, "m_HasTookItemFromShelf").SetValue(this.m_customer, false);
			ReflectionUtils.get_field(this.m_customer, "m_HasTookCardFromShelf").SetValue(this.m_customer, false);
			if ((UnityEngine.Random.Range(0, 100) <= Settings.m_chance_to_continue_shopping.Value && this.m_customer.m_CurrentCostTotal < this.m_customer.m_MaxMoney) || (___m_ItemInBagList.Count == 0 && ___m_CardInBagList.Count == 0)) {
				ReflectionUtils.invoke_method(this.m_customer, "DetermineShopAction");
			} else {
				ReflectionUtils.invoke_method(this.m_customer, "ThinkWantToPay");
			}
		}
	}

	[HarmonyPatch(typeof(Customer), "ActivateCustomer")]
	class HarmonyPatch_Customer_ActivateCustomer {
		private static void Postfix(Customer __instance) {
			__instance.gameObject.AddComponent<BigSpenderBrain>().m_customer = __instance;
		}
	}

	/*
	[HarmonyPatch(typeof(Customer), "DetermineShopAction")]
	class HarmonyPatch_Customer_DetermineShopAction {
		private static bool Prefix(Customer __instance, bool ___m_HasPlayedGame, bool ___m_IsAngry, bool ___m_HasCheckedOut) {
			try {
				if (EndOfDayReportScreen.IsActive()) {
					ReflectionUtils.invoke_method(__instance, "ExitShop");
				} else {
					if ((bool) ReflectionUtils.invoke_method(__instance, "StenchLeaveCheck")) {
						return false;
					}
					bool _flag = ShelfManager.GetShelfToBuyItem(__instance.m_TargetBuyItemList) != null;
					bool flag = ShelfManager.GetCardShelfToBuyCard() != null;
					bool flag2 = ShelfManager.GetPlayTableToPlay(findTableWithPlayerWaiting: false) != null;
					bool flag3 = ShelfManager.HasPlayTableWithPlayerWaiting();
					bool flag4 = false;
					if (!___m_HasPlayedGame && (flag2 || flag3) && (bool) ReflectionUtils.invoke_method(__instance, "HasNoItemOrCheckedOut") && !___m_IsAngry) {
						int num = UnityEngine.Random.Range(0, 100);
						if (LightManager.IsMorning()) {
							num += 15;
						}
						if (LightManager.IsEvening()) {
							num -= 30;
						}
						if (LightManager.IsNight()) {
							num -= 40;
						}
						if (flag3) {
							num -= 25;
						}
						if (num < Settings.m_chance_to_find_another_activity_if_idle.Value) {
							ReflectionUtils.invoke_method(__instance, "ThinkWantToPlayTable");
							flag4 = true;
						}
					}
					if (!flag4 && (flag || _flag) && !___m_HasCheckedOut) {
						int num2 = UnityEngine.Random.Range(0, 100);
						if (LightManager.IsAfternoon()) {
							num2 -= 15;
						}
						if (num2 < Settings.m_chance_to_find_another_activity_if_idle.Value) {
							ReflectionUtils.invoke_method(__instance, "ThinkWantToBuyCard");
							flag4 = true;
						}
					}
					if (!flag4) {
						if (!___m_HasCheckedOut) {
							ReflectionUtils.invoke_method(__instance, "ThinkWantToBuyItem");
						} else {
							ReflectionUtils.invoke_method(__instance, "ExitShop");
						}
					}
				}
				return false;
			} catch (Exception e) {
				DDPlugin._error_log("** HarmonyPatch_Customer_TakeItemFromShelf.Prefix ERROR - " + e);
			}
			return true;
		}
	}
	*/

	[HarmonyPatch(typeof(CustomerManager), "GetCustomerBuyItemChance")]
	class HarmonyPatch_CustomerManager_GetCustomerBuyItemChance {
		private static void Postfix(float currentPrice, float marketPrice, ref int __result) {
			if (Settings.m_enabled.Value && Settings.m_buy_item_chance_multiplier.Value > 0) {
				__result = (Settings.m_buy_item_chance_multiplier.Value >= 100 ? 100 : Mathf.FloorToInt((float) __result * Settings.m_buy_item_chance_multiplier.Value));
			}
		}
	}

	[HarmonyPatch(typeof(Customer), "TakeItemFromShelf")]
	class HarmonyPatch_Customer_TakeItemFromShelf {
		private static bool Prefix(Customer __instance, ref bool ___m_IsInsideShop, Shelf ___m_CurrentShelf, ShelfCompartment ___m_CurrentItemCompartment, bool ___m_IsSmelly, List<Item> ___m_ItemInBagList, List<InteractableCard3d> ___m_CardInBagList, ref int ___m_FailFindItemAttemptCount, ref bool ___m_HasTookItemFromShelf) {
			try {
				//DDPlugin._debug_log("TakeItemFromShelf");
				___m_IsInsideShop = true;
				if (!___m_CurrentShelf) {
					__instance.m_CurrentState = ECustomerState.Idle;
					return false;
				}
				if (___m_CurrentItemCompartment.GetItemCount() > 0 && ___m_CurrentShelf.IsValidObject()) {
					Item lastItem = ___m_CurrentItemCompartment.GetLastItem();
					ItemData itemData = InventoryBase.GetItemData(lastItem.GetItemType());
					int max_item_count;
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
							already_blabbed = (bool) ReflectionUtils.invoke_method(__instance, "BuyItemSpeechPopup", new object[] {customerBuyItemChance, itemPrice, itemData.name, lastItem.GetItemType()});
						}
						//DDPlugin._debug_log($"[{i}] current_item_buy_chance: {current_item_buy_chance}");
						if (UnityEngine.Random.Range(0, 100) <= current_item_buy_chance) {
							desired_item_count++;
							current_item_buy_chance -= Settings.m_per_item_grab_chance_reduction.Value;
						}
					}
					//DDPlugin._debug_log($"max_item_count: {max_item_count}, desired_item_count: {desired_item_count}");
					int grabbed_count = 0;
					for (int i = 0; i < desired_item_count; i++) {
						if (__instance.m_CurrentCostTotal >= __instance.m_MaxMoney || (lastItem = ___m_CurrentItemCompartment.GetLastItem()) == null) {
							break;
						}
						___m_CurrentItemCompartment.RemoveItem(lastItem);
						lastItem.m_Mesh.enabled = true;
						___m_ItemInBagList.Add(lastItem);
						lastItem.SetCurrentPrice(itemPrice);
						lastItem.LerpToTransform(__instance.m_ShoppingBagTransform, __instance.m_ShoppingBagTransform);
						lastItem.SetHideItemAfterFinishLerp();
						__instance.m_CurrentCostTotal += itemPrice;
						grabbed_count++;
					}
					//DDPlugin._debug_log($"customer hash: {__instance.GetHashCode()}, grabbed_count: {grabbed_count}, current_cost: {__instance.m_CurrentCostTotal}, max_money: {__instance.m_MaxMoney}");
					if (grabbed_count > 0) {
						__instance.m_ShoppingBagTransform.gameObject.SetActive(value: true);
						__instance.m_Anim.SetBool("HoldingBag", value: true);
						__instance.m_Anim.SetTrigger("GrabItem");
					} else {
						___m_FailFindItemAttemptCount++;
					}
					BigSpenderBrain.decide_if_finish_shopping(__instance, ___m_ItemInBagList, ___m_CardInBagList);
				} else {
					__instance.m_Anim.SetTrigger("ShakeHead");
					___m_HasTookItemFromShelf = false;
					___m_FailFindItemAttemptCount++;
					__instance.m_CurrentState = ECustomerState.Idle;
				}
				return false;
			} catch (Exception e) {
				DDPlugin._error_log("** HarmonyPatch_Customer_TakeItemFromShelf.Prefix ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Customer), "TakeCardFromShelf")]
	class HarmonyPatch_Customer_TakeCardFromShelf {
		private static bool Prefix(Customer __instance, ref bool ___m_IsInsideShop, CardShelf ___m_CurrentCardShelf, InteractableCardCompartment ___m_CurrentCardCompartment, List<Item> ___m_ItemInBagList, List<InteractableCard3d> ___m_CardInBagList, ref int ___m_FailFindItemAttemptCount, ref bool ___m_HasTookItemFromShelf, ref bool ___m_HasTookCardFromShelf) {
			try {
				//DDPlugin._debug_log("TakeCardFromShelf");
				___m_IsInsideShop = true;
				if (!___m_CurrentCardShelf) {
					__instance.m_CurrentState = ECustomerState.Idle;
					return false;
				}
				if (___m_CurrentCardCompartment.m_StoredCardList.Count > 0 && ___m_CurrentCardShelf.IsValidObject()) {
					InteractableCard3d interactableCard3d = ___m_CurrentCardCompartment.m_StoredCardList[0];
					float cardPrice = CPlayerData.GetCardPrice(interactableCard3d.m_Card3dUI.m_CardUI.GetCardData());
					float cardMarketPrice = CPlayerData.GetCardMarketPrice(interactableCard3d.m_Card3dUI.m_CardUI.GetCardData());
					int customerBuyItemChance = CSingleton<CustomerManager>.Instance.GetCustomerBuyItemChance(cardPrice, cardMarketPrice);
					bool flag = true;
					if (UnityEngine.Random.Range(0, 100) > customerBuyItemChance) {
						flag = false;
						___m_FailFindItemAttemptCount++;
					}
					ReflectionUtils.invoke_method(__instance, "BuyCardSpeechPopup", new object[] {customerBuyItemChance, cardPrice, __instance.m_CurrentCostTotal + cardPrice <= __instance.m_MaxMoney});
					if (cardPrice > 0f && flag && __instance.m_CurrentCostTotal + cardPrice <= __instance.m_MaxMoney && interactableCard3d.IsDisplayedOnShelf()) {
						___m_CurrentCardCompartment.RemoveCardFromShelf(__instance.m_ShoppingBagTransform, __instance.m_ShoppingBagTransform);
						___m_CardInBagList.Add(interactableCard3d);
						interactableCard3d.SetCurrentPrice(cardPrice);
						interactableCard3d.SetHideItemAfterFinishLerp();
						__instance.m_CurrentCostTotal += cardPrice;
						__instance.m_ShoppingBagTransform.gameObject.SetActive(value: true);
						__instance.m_Anim.SetBool("HoldingBag", value: true);
						__instance.m_Anim.SetTrigger("GrabItem");
					}
					BigSpenderBrain.decide_if_finish_shopping(__instance, ___m_ItemInBagList, ___m_CardInBagList);
				} else {
					__instance.m_Anim.SetTrigger("ShakeHead");
					___m_HasTookItemFromShelf = false;
					___m_HasTookCardFromShelf = false;
					___m_FailFindItemAttemptCount++;
					__instance.m_CurrentState = ECustomerState.Idle;
				}
				return false;
			} catch (Exception e) {
				DDPlugin._error_log("** HarmonyPatch_Customer_TakeItemFromShelf.Prefix ERROR - " + e);
			}
			return true;
		}
	}
}
