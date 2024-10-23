using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class PluginInfo {

	public const string TITLE = "Quick Scan";
	public const string NAME = "quick_scan";
	public const string SHORT_DESCRIPTION = "Scan all items on the counter with a single click!  Works on employees too!";

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
public class QuickScanPlugin : DDPlugin {
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

	class Checkout {
		private static void scan_all_items(Customer __instance, Item item, ref int ___m_ItemScannedCount, InteractableCashierCounter ___m_CurrentQueueCashierCounter, ref float ___m_TotalScannedItemCost) {
			try {
				List<Item> customer_items = (List<Item>) ReflectionUtils.get_field_value(__instance, "m_ItemInBagList");
				foreach (InteractableScanItem scan_item in Resources.FindObjectsOfTypeAll<InteractableScanItem>()) {
					if (scan_item.m_Item == item || !customer_items.Contains(scan_item.m_Item)) {
						continue;
					}
					//DDPlugin._debug_log(scan_item.m_Item);
					scan_item.m_Item.m_Collider.enabled = false;
					scan_item.m_Item.m_Rigidbody.isKinematic = true;
					float currentPrice = scan_item.m_Item.GetCurrentPrice();
					___m_ItemScannedCount++;
					___m_CurrentQueueCashierCounter.AddScannedItemCostTotal(currentPrice, scan_item.m_Item.GetItemType());
					___m_TotalScannedItemCost += currentPrice;
					___m_TotalScannedItemCost = (float) Mathf.RoundToInt(___m_TotalScannedItemCost * GameInstance.GetCurrencyRoundDivideAmount()) / GameInstance.GetCurrencyRoundDivideAmount();
					ReflectionUtils.get_field(scan_item, "m_CurrentCustomer").SetValue(scan_item, null);
					Transform item_lerp_pos = (Transform) ReflectionUtils.get_field_value(scan_item, "m_ScannedItemLerpPos");
					scan_item.LerpToTransform(item_lerp_pos, item_lerp_pos);
					scan_item.SetHideItemAfterFinishLerp();
				}
				ReflectionUtils.invoke_method(__instance, "EvaluateFinishScanItem");
			} catch (Exception e) {
				DDPlugin._error_log("** scan_all_items ERROR - " + e);
			}
		}

		private static void scan_all_cards(Customer __instance, InteractableCard3d card, ref int ___m_ItemScannedCount, InteractableCashierCounter ___m_CurrentQueueCashierCounter, ref float ___m_TotalScannedItemCost) {
			try {
				List<InteractableCard3d> customer_cards = (List<InteractableCard3d>) ReflectionUtils.get_field_value(__instance, "m_CardInBagList");
				foreach (InteractableCard3d scan_card in Resources.FindObjectsOfTypeAll<InteractableCard3d>()) {
					if (scan_card == card || !customer_cards.Contains(scan_card)) {
						continue;
					}
					DDPlugin._debug_log(scan_card);
					scan_card.m_Collider.enabled = false;
					scan_card.m_Rigidbody.isKinematic = true;
					float currentPrice = scan_card.GetCurrentPrice();
					___m_ItemScannedCount++;
					___m_CurrentQueueCashierCounter.AddScannedCardCostTotal(currentPrice, scan_card.m_Card3dUI.m_CardUI.GetCardData());
					___m_TotalScannedItemCost += currentPrice;
					___m_TotalScannedItemCost = (float) Mathf.RoundToInt(___m_TotalScannedItemCost * GameInstance.GetCurrencyRoundDivideAmount()) / GameInstance.GetCurrencyRoundDivideAmount();
					ReflectionUtils.get_field(scan_card, "m_CurrentCustomer").SetValue(scan_card, null);
					Transform item_lerp_pos = (Transform) ReflectionUtils.get_field_value(scan_card, "m_ScannedItemLerpPos");
					scan_card.LerpToTransform(item_lerp_pos, item_lerp_pos);
					scan_card.SetHideItemAfterFinishLerp();
				}
				ReflectionUtils.invoke_method(__instance, "EvaluateFinishScanItem");
			} catch (Exception e) {
				DDPlugin._error_log("** scan_all_cards ERROR - " + e);
			}
		}

		[HarmonyPatch(typeof(Customer), "OnItemScanned")]
		class HarmonyPatch_Customer_OnItemScanned {
			private static void Postfix(Customer __instance, Item item, ref int ___m_ItemScannedCount, InteractableCashierCounter ___m_CurrentQueueCashierCounter, ref float ___m_TotalScannedItemCost) {
				if (!Settings.m_enabled.Value) {
					return;
				}
				scan_all_items(__instance, item, ref ___m_ItemScannedCount, ___m_CurrentQueueCashierCounter, ref ___m_TotalScannedItemCost);
				scan_all_cards(__instance, null, ref ___m_ItemScannedCount, ___m_CurrentQueueCashierCounter, ref ___m_TotalScannedItemCost);
			}
		}

		[HarmonyPatch(typeof(Customer), "OnCardScanned")]
		class HarmonyPatch_Customer_OnCardScanned {
			private static void Postfix(Customer __instance, InteractableCard3d card, ref int ___m_ItemScannedCount, InteractableCashierCounter ___m_CurrentQueueCashierCounter, ref float ___m_TotalScannedItemCost) {
				if (!Settings.m_enabled.Value) {
					return;
				}
				scan_all_items(__instance, null, ref ___m_ItemScannedCount, ___m_CurrentQueueCashierCounter, ref ___m_TotalScannedItemCost);
				scan_all_cards(__instance, card, ref ___m_ItemScannedCount, ___m_CurrentQueueCashierCounter, ref ___m_TotalScannedItemCost);
			}
		}

		[HarmonyPatch(typeof(SoundManager), "PlayAudio")]
		class HarmonyPatch_SoundManager_PlayAudio {
			const float MIN_TIME_BETWEEN_SCAN_SOUNDS = 0.5f;
			private static float m_time_of_last_scan_sound = float.MinValue;
			private static bool Prefix(string audioName) {
				try {
					if (!Settings.m_enabled.Value || audioName != "SFX_CheckoutScan") {
						return true;
					}
					if (Time.time - m_time_of_last_scan_sound < MIN_TIME_BETWEEN_SCAN_SOUNDS) {
						return false;
					}
					m_time_of_last_scan_sound = Time.time;
					return true;
				} catch (Exception e) {
					DDPlugin._error_log("** HarmonyPatch_SoundManager_PlayAudio.Prefix ERROR - " + e);
				}
				return true;
			}
		}

	}
}
