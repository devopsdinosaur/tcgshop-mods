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
	public const string SHORT_DESCRIPTION = "Adjust customers' chance to buy and amount of items they pick up.";

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
public class BigSpenderPlugin : DDPlugin {
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

		public static void decide_if_finish_shopping(Customer customer) {
			BigSpenderBrain brain = customer.gameObject.GetComponent<BigSpenderBrain>();
			if (brain.m_routine != null) {
				brain.StopCoroutine(brain.m_routine);
			}
			brain.m_routine = brain.StartCoroutine(brain.routine_decide_if_finish_shopping());
		}

		private IEnumerator routine_decide_if_finish_shopping() {

		}
	}

	[HarmonyPatch(typeof(Customer), "ActivateCustomer")]
	class HarmonyPatch_Customer_ActivateCustomer {
		private static void Postfix(Customer __instance) {
			__instance.gameObject.AddComponent<BigSpenderBrain>().m_customer = __instance;
		}
	}

	[HarmonyPatch(typeof(Customer), "TakeItemFromShelf")]
	class HarmonyPatch_Customer_TakeItemFromShelf {
		private static bool Prefix(Customer __instance, ref bool ___m_IsInsideShop, Shelf ___m_CurrentShelf, ShelfCompartment ___m_CurrentItemCompartment, bool ___m_IsSmelly, List<Item> ___m_ItemInBagList, ref int ___m_FailFindItemAttemptCount, ref bool ___m_HasTookItemFromShelf) {
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
					int max_volume = int.MaxValue; //UnityEngine.Random.Range(MIN_VOLUME, MAX_VOLUME);
					float current_volume = 0f;
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
							already_blabbed = (bool) ReflectionUtils.invoke_method(__instance, "BuyItemSpeechPopup", new object[] { customerBuyItemChance, itemPrice, itemData.name });
						}
						if (UnityEngine.Random.Range(0, 100) < current_item_buy_chance) {
							desired_item_count++;
							current_item_buy_chance -= Settings.m_per_item_grab_chance_reduction.Value;
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
					BigSpenderBrain.decide_if_finish_shopping(__instance);
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
}
