﻿using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using CC;

public static class PluginInfo {

	public const string TITLE = "Nerd Shirts";
	public const string NAME = "nerd_shirts";
	public const string SHORT_DESCRIPTION = "Create custom shirts for customers and workers!";

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
public class NerdShirtsPlugin : DDPlugin {
	private Harmony m_harmony = new Harmony(PluginInfo.GUID);

	private void Awake() {
		logger = this.Logger;
		try {
			this.m_plugin_info = PluginInfo.to_dict();
			Settings.Instance.load(this);
			DDPlugin.set_log_level(Settings.m_log_level.Value);
			CustomMaterialHandler.Instance.initialize();
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

	public class CustomMaterialHandler {
		
		class CustomTexture {
			public string m_material_key;
			public string m_name;
			public string m_path;
			private Texture2D m_texture;
			public Texture2D Texture {
				get {
					return this.get_texture();
				}
			}
			private bool m_load_failed;

			public CustomTexture(string material_key, string name, string path) {
				this.m_material_key = material_key;
				this.m_name = name;
				this.m_path = path;
				this.m_texture = null;
				this.m_load_failed = false;
			}

			private Texture2D get_texture() {
				try {
					if (this.m_load_failed) {
						return null;
					}
					if (this.m_texture != null) {
						return this.m_texture;
					}
					DDPlugin._debug_log($"CustomTexture.get_texture - loading '{this.m_name}' from file '{this.m_path}'.");
					this.m_texture = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_UNorm, new TextureCreationFlags());
					this.m_texture.LoadImage(File.ReadAllBytes(this.m_path));
					this.m_texture.filterMode = FilterMode.Point;
					this.m_texture.wrapMode = TextureWrapMode.Clamp;
					this.m_texture.wrapModeU = TextureWrapMode.Clamp;
					this.m_texture.wrapModeV = TextureWrapMode.Clamp;
					this.m_texture.wrapModeW = TextureWrapMode.Clamp;
					this.m_load_failed = false;
					return this.m_texture;
				} catch (Exception e) {
					DDPlugin._error_log("** CustomTexture.get_texture ERROR - " + e);
					DDPlugin._error_log("** NOTE: Texture load failed.  This texture will no longer be considered for replacement during this run or until a manual reload is triggered.");
				}
				this.m_load_failed = true;
				return null;
			}
		}

		private static CustomMaterialHandler m_instance = null;
		public static CustomMaterialHandler Instance {
			get {
				if (m_instance == null) {
					m_instance = new CustomMaterialHandler();
				}
				return m_instance;
			}
		}
		private Dictionary<string, List<CustomTexture>> m_textures = new Dictionary<string, List<CustomTexture>>();
		public class PatchedKeyList {
			public struct PatchedKey {
				public int m_instance_id;
				public string m_name;
			}
			private List<PatchedKey> m_patched_keys = new List<PatchedKey>();

			public void add_patch(int instance_id, string name) {
				this.m_patched_keys.Add(new PatchedKey() {
					m_instance_id = instance_id, 
					m_name = name
				});
			}

			public bool is_patched(int instance_id, string name) {
				foreach (PatchedKey key in this.m_patched_keys) {
					if (key.m_instance_id == instance_id && key.m_name == name) {
						return true;
					}
				}
				return false;
			}

			public void reset() {
				this.m_patched_keys.Clear();
			}
		}
		private PatchedKeyList m_patched_keys = new PatchedKeyList();

		private bool apparel_ensure_patched_texture_list(CharacterCustomization __instance, string name, int slot, int materialSelection) {
			try {
				if (!Settings.m_enabled.Value || !this.m_textures.ContainsKey(name)) {
					return true;
				}
				scrObj_Apparel apparel_wrapper = __instance.ApparelTables[slot];
				int apparel_index = -1;
				for (int index = 0; index < apparel_wrapper.Items.Count; index++) {
					if (apparel_wrapper.Items[index].Name == name) {
						apparel_index = index;
						break;
					}
				}
				if (apparel_index == -1) {
					return false;
				}
				scrObj_Apparel.Apparel apparel = apparel_wrapper.Items[apparel_index];
				foreach (CC_Apparel_Material_Collection collection in apparel.Materials) {
					if (collection == null || collection.MaterialDefinitions == null || collection.MaterialDefinitions.Count == 0 || this.m_patched_keys.is_patched(collection.GetHashCode(), name)) {
						continue;
					}
					DDPlugin._debug_log($"Patching '{name}' in {apparel.DisplayName}.{collection.Label} [hash: {collection.GetHashCode()}] apparel collection.");
					this.m_patched_keys.add_patch(collection.GetHashCode(), name);
					foreach (CustomTexture texture in this.m_textures[name]) {
						collection.MaterialDefinitions.Add(new CC_Apparel_Material_Definition() {
							MainTint = Color.white,
							TintR = Color.white,
							TintB = Color.white,
							TintG = Color.white,
							Print = texture.Texture
						});
					}
					collection.MaterialDefinitions.Clear();
					DDPlugin._debug_log(collection.MaterialDefinitions.Count);
				}
				return true;
			} catch (Exception e) {
				DDPlugin._error_log("** apparel_ensure_patched_texture_list ERROR - " + e);
			}
			return true;
		}

		private bool apparel_set_apparel(CharacterCustomization __instance, int selection, int slot, int materialSelection) {
			return true;
		}

		public static void dump_textures() {
			string root_dir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Settings.m_subdir.Value);
			string dump_root = Path.Combine(root_dir, "__dump__");
			DDPlugin._info_log($"Dumping apparel textures to '{dump_root}'.");
			foreach (Customer prefab in new Customer[] {CustomerManager.Instance.m_CustomerFemalePrefab, CustomerManager.Instance.m_CustomerPrefab}) {
				foreach (scrObj_Apparel apparel_table in prefab.m_CharacterCustom.ApparelTables) {
					//DDPlugin._debug_log($"{prefab.name}.{apparel_table.Label}");
					foreach (scrObj_Apparel.Apparel apparel in apparel_table.Items) {
						//DDPlugin._debug_log($"{prefab.name}.{apparel_table.Label}.{apparel.DisplayName}");
						foreach (CC_Apparel_Material_Collection collection in apparel.Materials) {
							//DDPlugin._debug_log($"{prefab.name}.{apparel_table.Label}.{apparel.DisplayName}.{collection.Label}");
							for (int index = 0; index < collection.MaterialDefinitions.Count; index++) {
								DDPlugin._debug_log($"{prefab.name}.{apparel_table.Label}.{apparel.DisplayName}.{collection.Label}.{index}");
								CC_Apparel_Material_Definition definition = collection.MaterialDefinitions[index];
								string directory = Path.Combine(dump_root, prefab.name, apparel_table.Label, apparel.DisplayName, collection.Label);
								//Directory.CreateDirectory(directory);

							}
						}
					}
				}
			}
			Application.Quit();
		}

		public void initialize() {
			this.reload_textures();
		}

		public void reload_textures() {
			try {
				string root_dir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Settings.m_subdir.Value);
				DDPlugin._info_log($"Assembling custom texture information from directory, '{root_dir}'.");
				if (!Directory.Exists(root_dir)) {
					DDPlugin._info_log("Directory does not exist; creating.");
					Directory.CreateDirectory(root_dir);
				}
				this.m_textures.Clear();
				this.m_patched_keys.reset();
				int total_counter = 0;
				foreach (string mod_dir in Directory.GetDirectories(root_dir)) {
					string mod_name = Path.GetFileName(mod_dir);
					if (mod_name.IsNullOrWhiteSpace() || mod_name[0] == '_' || mod_name[0] == '.') {
						continue;
					}
					foreach (string textures_dir in Directory.GetDirectories(mod_dir)) {
						string texture_key = Path.GetFileName(textures_dir);
						int local_counter = 0;
						foreach (string file in Directory.GetFiles(textures_dir, "*.png", SearchOption.AllDirectories)) {
							string full_path = Path.Combine(mod_dir, file);
							if (!m_textures.ContainsKey(texture_key)) {
								m_textures[texture_key] = new List<CustomTexture>();
							}
							this.m_textures[texture_key].Add(new CustomTexture(texture_key, Path.GetFileNameWithoutExtension(full_path), full_path));
							local_counter++;
						}
						DDPlugin._info_log($"--> {mod_name}: {local_counter} textures.");
						total_counter += local_counter;
					}
				}
				DDPlugin._info_log($"Found {total_counter} total textures.");
			} catch (Exception e) {
				DDPlugin._error_log("** CustomMaterialHandler.reload_textures ERROR - " + e);
			}
		}

		private class HarmonyPatches {
			[HarmonyPatch(typeof(CharacterCustomization), "setApparel")]
			class HarmonyPatch_CharacterCustomization_setApparel {
				private static bool Prefix(CharacterCustomization __instance, int selection, int slot, int materialSelection) {
					return CustomMaterialHandler.Instance.apparel_set_apparel(__instance, selection, slot, materialSelection);
				}
			}

			[HarmonyPatch(typeof(CharacterCustomization), "setApparelByName")]
			class HarmonyPatch_CharacterCustomization_setApparelByName {
				private static bool Prefix(CharacterCustomization __instance, string name, int slot, int materialSelection) {
					return CustomMaterialHandler.Instance.apparel_ensure_patched_texture_list(__instance, name, slot, materialSelection);
				}
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
				//CustomAssetManager.Instance.load_asset_bundle("decal_test_bundle", Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "decal_test_bundle"));
				//Application.Quit();
				DDPlugin._debug_log($"{CustomerManager.Instance.m_CustomerPrefab.m_CharacterCustom.Presets.Presets.Count}");
				DDPlugin._debug_log($"{CustomerManager.Instance.m_CustomerFemalePrefab.m_CharacterCustom.Presets.Presets.Count}");
			} catch (Exception e) {
				DDPlugin._error_log("** on_game_data_finish_loaded ERROR - " + e);
			}
		}

		[HarmonyPatch(typeof(CharacterCustomization), "ApplyCharacterVars")]
		class HarmonyPatch_CharacterCustomization_ApplyCharacterVars {
			private static bool Prefix(CharacterCustomization __instance, CC_CharacterData characterData) {
				// Polo_Shirt_01, Cargo_Pants, Boots_01, None
				// 1, 0, 0, 0
				if (!characterData.CharacterPrefab.StartsWith("Male")) {
					return true;
				}
				characterData.ApparelNames = new List<string>() {"Polo_Shirt_01", "Cargo_Pants", "Boots_01", "None"};
				characterData.ApparelMaterials = new List<int>() {1, 0, 0, 0};
				//DDPlugin._info_log(characterData.CharacterPrefab);
				//DDPlugin._info_log(String.Join(", ", characterData.ApparelNames));
				//DDPlugin._info_log(String.Join(", ", characterData.ApparelMaterials));
				return true;
			}
		}

		[HarmonyPatch(typeof(CustomerManager), "Init")]
		class HarmonyPatch_CustomerManager_SpawnGameStartCustomer {
			private static void Postfix(CustomerManager __instance) {
				try {
					GameObject testing_parent = new GameObject("Testing_Parent");
					testing_parent.transform.SetParent(__instance.m_CustomerParentGrp.transform.parent);
					for (int counter = 0; counter < 10; counter++) {
						Customer customer = GameObject.Instantiate(__instance.m_CustomerPrefab, testing_parent.transform);
						customer.gameObject.SetActive(true);
						customer.transform.position = new Vector3(12.1f, 0f, -5.02f - counter * 2f);
						//customer.RandomizeCharacterMesh();
						customer.m_CharacterCustom.CharacterName = $"Male{counter}";
						customer.m_CharacterCustom.Initialize();
						//customer.gameObject.AddComponent<CustomerDecalProjector>();
					}


					//AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "decal_projector_bundle"));
					//GameObject projector_object = GameObject.Instantiate(
					//	bundle.LoadAsset<GameObject>("assets/bundles/decal_projector_bundle/prefabs/basic_decal_projector.prefab"), 
					//	shirt_transform
					//);
					//projector_object.transform.localPosition = shirt_transform.forward * -1f;
					//projector_object.transform.LookAt(shirt_transform.position);
					//projector_object.transform.localScale *= 2f;
					//Transform debug_cube = customer.transform.Find("DebugCube");
					//MeshRenderer cube_renderer = debug_cube.GetComponent<MeshRenderer>();
					//projector_object.transform.position += projector_object.transform.up * (cube_renderer.bounds.size.y * 0.66f);

					//GameObject obj = new GameObject("Customer_Decal_Projector");
					//obj.transform.SetParent(shirt_transform);
					//obj.transform.position = shirt_transform.position + Vector3.forward * 1.0f;
					//obj.transform.localScale = new Vector3(10, 10, 10);
					//obj.transform.LookAt(shirt_transform.position);
					//EasyDecal projector = obj.AddComponent<EasyDecal>();
					//projector.DecalMaterial = load_material("C:/tmp/textures/thundercats.png", Shader.Find("Standard"));
					//projector.Mask = LayerMask.NameToLayer() || 
					//projector.BakeOnAwake = true;
					//projector.Technique = ProjectionTechnique.Box;

					//projector.gameObject.SetActive(true);
				} catch (Exception e) {
					DDPlugin._error_log("** HarmonyPatch_CustomerManager_SpawnGameStartCustomer.Postfix ERROR - " + e);
				}
			}
		}

		[HarmonyPatch(typeof(InteractionPlayerController), "Update")]
		class HarmonyPatch_InteractionPlayerController_Update {
			private static void Postfix() {
				//m_projector.transform.position = InteractionPlayerController.Instance.m_Cam.transform.position;
				//m_projector.transform.rotation = InteractionPlayerController.Instance.m_Cam.transform.rotation;
			}
		}
	}
}
