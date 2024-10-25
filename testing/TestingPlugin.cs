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
			DDPlugin._debug_log("-- cheat_restock_everything --");
			foreach (Shelf shelf in ShelfManager.Instance.m_ShelfList) {
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
			Queue<CardData> get_more_cards() {
				ReflectionUtils.invoke_method(CardOpeningSequence.Instance, "GetPackContent", new object[] {true, false, true, ECollectionPackType.BasicCardPack});
				return new Queue<CardData>((List<CardData>) ReflectionUtils.get_field_value(CardOpeningSequence.Instance, "m_SecondaryRolledCardDataList"));
			}
			Queue<CardData> cards = get_more_cards();
			foreach (CardShelf shelf in ShelfManager.Instance.m_CardShelfList) {
				DDPlugin._debug_log($"card shelf: {shelf.name}");
				foreach (InteractableCardCompartment compartment in (List<InteractableCardCompartment>) ReflectionUtils.get_field_value(shelf, "m_CardCompartmentList")) {
					if (cards.Count == 0) {
						cards = get_more_cards();
					}
					CardData card = cards.Dequeue();
					float market_price = CPlayerData.GetCardMarketPrice(card);
					CPlayerData.SetCardPrice(card, market_price + market_price * 0.2f);
					Card3dUIGroup cardUI = CSingleton<Card3dUISpawner>.Instance.GetCardUI();
					InteractableCard3d component = ShelfManager.SpawnInteractableObject(EObjectType.Card3d).GetComponent<InteractableCard3d>();
					cardUI.m_IgnoreCulling = true;
					cardUI.m_CardUI.SetFoilCullListVisibility(isActive: true);
					cardUI.m_CardUI.ResetFarDistanceCull();
					cardUI.m_CardUI.SetCardUI(card);
					cardUI.transform.position = component.transform.position;
					cardUI.transform.rotation = component.transform.rotation;
					component.SetCardUIFollow(cardUI);
					component.SetEnableCollision(isEnable: false);
					compartment.SetCardOnShelf(component);
					cardUI.m_IgnoreCulling = false;
				}
			}
		}
	}

	public class Debugging {
		[HarmonyPatch(typeof(CGameManager), "Awake")]
		class HarmonyPatch_CGameManager_Awake {
			private static void Postfix() {
				CEventManager.AddListener<CEventPlayer_GameDataFinishLoaded>(on_game_data_finish_loaded);
			}
		}

		private static void on_game_data_finish_loaded(CEventPlayer_GameDataFinishLoaded evt) {
			try {
				CGameManager.Instance.gameObject.AddComponent<Debugging.PositionInfo>();
			} catch (Exception e) {
				DDPlugin._error_log("** on_game_data_finish_loaded ERROR - " + e);
			}
		}

		public class PositionInfo : MonoBehaviour {
			private static string OBJECT_NAME = "TestingPlugin_Position_Info_Text";
			private TextMeshProUGUI m_info_text = null;

			private void Awake() {
				Transform offset_from = GameUIScreen.Instance.m_AddMoneyText.transform;
				Vector3 offset_direction = Vector3.down;
				float offset_distance = 20f;
				this.m_info_text = GameObject.Instantiate<TextMeshProUGUI>(GameUIScreen.Instance.m_ShopLevelText, offset_from.parent);
				m_info_text.transform.localPosition = offset_from.localPosition + offset_direction * offset_distance;
				this.m_info_text.name = OBJECT_NAME;
				this.m_info_text.alignment = TextAlignmentOptions.TopLeft;
				this.m_info_text.enableAutoSizing = true;
				this.m_info_text.enableWordWrapping = false;
				this.m_info_text.fontSizeMin = this.m_info_text.fontSizeMax = 18;
				this.m_info_text.gameObject.SetActive(true);
				this.StartCoroutine(this.update_routine());
			}

			private IEnumerator update_routine() {
				for (;;) {
					Vector3 pos = InteractionPlayerController.Instance.m_Cam.transform.position;
					this.m_info_text.text = $"X: {pos.x:#0.00}, Y: {pos.y:#0.00}, Z: {pos.z:#0.00}";
					yield return new WaitForSeconds(0.1f);
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

	class Misc {
		/*
		private static Material load_material(string path, Shader shader) {
			try {
				Texture2D texture = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_UNorm, new TextureCreationFlags());
				texture.LoadImage(File.ReadAllBytes(path));
				texture.filterMode = FilterMode.Point;
				texture.wrapMode = TextureWrapMode.Clamp;
				texture.wrapModeU = TextureWrapMode.Clamp;
				texture.wrapModeV = TextureWrapMode.Clamp;
				texture.wrapModeW = TextureWrapMode.Clamp;
				Material material = new Material(shader);
				material.mainTexture = texture;
				return material;
			} catch (Exception e) {
				DDPlugin._error_log("** load_texture ERROR - " + e);
			}
			return null;
		}
		*/
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

	[HarmonyPatch(typeof(), "")]
	class HarmonyPatch_ {
		private static bool Prefix() {
			try {

			} catch (Exception e) {
				DDPlugin._error_log("** HarmonyPatch_.Prefix ERROR - " + e);
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(), "")]
	class HarmonyPatch_ {
		private static void Postfix() {
			try {

			} catch (Exception e) {
				DDPlugin._error_log("** HarmonyPatch_.Postfix ERROR - " + e);
			}
		}
	}
	*/
}
