using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.IO;

public static class PluginInfo {

	public const string TITLE = "Custom Assets";
	public const string NAME = "custom_assets";
	public const string SHORT_DESCRIPTION = "Allows mods to add any amount of custom assets to the game!";

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

	class Model {
		[HarmonyPatch(typeof(CGameManager), "Awake")]
		class HarmonyPatch_CGameManager_Awake {
			private static void Postfix() {
				CEventManager.AddListener<CEventPlayer_GameDataFinishLoaded>(on_game_data_finish_loaded);
			}
		}

		private static void on_game_data_finish_loaded(CEventPlayer_GameDataFinishLoaded evt) {
			try {
				CustomAssetManager.Instance.load_asset_bundle(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "testbundle"));
			} catch (Exception e) {
				DDPlugin._error_log("** on_game_data_finish_loaded ERROR - " + e);
			}
		}

		class CustomAssetManager : MonoBehaviour {
			private static CustomAssetManager m_instance = null;
			public static CustomAssetManager Instance {
				get {
					if (m_instance == null) {
						m_instance = CGameManager.Instance.gameObject.AddComponent<CustomAssetManager>();
					}
					return m_instance;
				}
			}
			private Dictionary<string, GameObject> m_prefabs = new Dictionary<string, GameObject>();

			public void load_asset_bundle(string path) {
				try {

				} catch (Exception e) {
					DDPlugin._error_log("** CustomAssetManager.load_asset_bundle ERROR - " + e);
				}
			}
		}

		private static Material load_material(string path) {
			try {
				Texture2D texture = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_UNorm, new TextureCreationFlags());
				texture.LoadImage(File.ReadAllBytes(path));
				texture.filterMode = FilterMode.Point;
				texture.wrapMode = TextureWrapMode.Clamp;
				texture.wrapModeU = TextureWrapMode.Clamp;
				texture.wrapModeV = TextureWrapMode.Clamp;
				texture.wrapModeW = TextureWrapMode.Clamp;
				Material material = new Material(Shader.Find("Standard"));
				material.mainTexture = texture;
				return material;
			} catch (Exception e) {
				DDPlugin._error_log("** load_texture ERROR - " + e);
			}
			return null;
		}

		[HarmonyPatch(typeof(CustomerManager), "Init")]
		class HarmonyPatch_CustomerManager_SpawnGameStartCustomer {
			private static void Postfix(CustomerManager __instance) {
				try {
					GameObject testing_parent = new GameObject("Testing_Parent");
					testing_parent.transform.SetParent(__instance.m_CustomerParentGrp.transform.parent);
					Customer customer = GameObject.Instantiate(__instance.m_CustomerFemalePrefab, testing_parent.transform);
					customer.gameObject.SetActive(true);
					customer.transform.position = new Vector3(12.1f, 1.64f, -5.02f) + Vector3.down * 1.5f;
					customer.RandomizeCharacterMesh();
					Material material = load_material("C:/tmp/textures/horde_crest.png");
					Transform shirt_transform = UnityUtils.find_first_descendant(customer.transform, "Upper_Body").GetChild(0);
					for (int index = 0; index < shirt_transform.childCount; index++) {
						SkinnedMeshRenderer renderer = shirt_transform.GetChild(index).GetComponent<SkinnedMeshRenderer>();
						renderer.material = material;
					}
				} catch (Exception e) {
					DDPlugin._error_log("** HarmonyPatch_CustomerManager_SpawnGameStartCustomer.Postfix ERROR - " + e);
				}
			}
		}
	}
}
