using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime;
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

	public class CustomMaterialHandler : MonoBehaviour {
		
		private class CustomTexture {
			public string m_key;
			public string m_path;
			private Texture2D m_texture;
			public Texture2D Texture {
				get {
					return this.get_texture();
				}
			}
			private bool m_load_failed;

			public CustomTexture(string key, string path) {
				this.m_key = key;
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
					DDPlugin._debug_log($"CustomTexture.get_texture - loading '{this.m_key}' from file '{this.m_path}'.");
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

		private class CustomTextureDict {

			private Dictionary<string, CustomTexture> m_textures = new Dictionary<string, CustomTexture>();

			public bool add_item(string key, string path) {
				if (this.m_textures.TryGetValue(key, out CustomTexture texture)) {
					DDPlugin._warn_log($"* CustomTextureDict.add_item WARNING - duplicate key detected; only the first will be used (original: '{texture.m_path}', duplicate: '{path}').");
					return false;
				}
				string ext = path.Substring(path.Length - 4).ToLower();
				if (ext == ".png") {
					this.m_textures[key] = new CustomTexture(key, path);
				} else if (ext == ".txt") {
					if (new FileInfo(path).Length > 1024) {
						DDPlugin._info_log($"CustomTextureDict.add_item INFO - '{path}' is too large to be a link token file; ignored.");
						return false;
					}
					try {
						string link_key = File.ReadAllText(path);
						if (!this.m_textures.TryGetValue(link_key, out CustomTexture link_texture)) {
							DDPlugin._warn_log($"* CustomTextureDict.add_item WARNING - invalid texture link to '{link_key}' in '{path}'; linked texture file is not present in loaded database.");
							return false;
						}
						this.m_textures[key] = link_texture;
					} catch (Exception e) {
						DDPlugin._info_log($"* CustomTextureDict.add_item WARNING - exception occured when reading '{path}'; ignored.  Exception: " + e);
						return false;
					}
				}
				return true;
			}
		}

		private class PatchedKeyList {
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

		private class TextureDumper : MonoBehaviour {
			private static TextureDumper m_instance = null;
			public static TextureDumper Instance {
				get {
					return m_instance;
				}
			}
			private string m_dump_root;
			List<string> m_already_written = new List<string>();

			public static void dump_textures() {
				if (!Settings.m_enabled.Value || m_instance != null) {
					return;
				}
				m_instance = CGameManager.Instance.gameObject.AddComponent<TextureDumper>();
			}

			private void Awake() {
				this.m_dump_root = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Settings.m_subdir.Value, "__dump__");
				this.StartCoroutine(this.coroutine_dump_textures());
			}

			private IEnumerator coroutine_dump_textures() {
				
				bool QUIT_WHEN_DONE = true;
				bool DESTROY_CUSTOMERS = true;
				
				const float START_X = 8.5f;
				const float START_Z = 0f;
				const float INC_X = 1.5f;
				const float INC_Z = -1.5f;
				const int COUNT_PER_COL = 8;

				DDPlugin._info_log("Starting dump_textures coroutine.");
				GameObject temp_customers_parent = new GameObject("Temp_Customers_Parent");
				List<Customer> customers = new List<Customer>();
				float x = START_X;
				float y = 0f;
				float z = START_Z;
				int column_counter = 0;
				DDPlugin._info_log("Instantiating all customer presets.");
				foreach (Customer prefab in new Customer[] { CustomerManager.Instance.m_CustomerFemalePrefab, CustomerManager.Instance.m_CustomerPrefab }) {
					foreach (CC_CharacterData character_data in prefab.m_CharacterCustom.Presets.Presets) {
						Customer customer = GameObject.Instantiate(prefab, temp_customers_parent.transform);
						customers.Add(customer);
						customer.gameObject.SetActive(true);
						customer.transform.position = new Vector3(x, y, z);
						z += INC_Z;
						if (column_counter++ == COUNT_PER_COL) {
							x += INC_X;
							z = START_Z;
							column_counter = 0;
						}
						customer.m_CharacterCustom.CharacterName = character_data.CharacterName;
						customer.m_CharacterCustom.Initialize();
						yield return null;
					}
				}
				List<string> dumped_paths = new List<string>();
				foreach (Customer customer in customers) {
					string[] names = new string[256];
					int name_index = 0;
					Dictionary<int, string> written_file_hashes = new Dictionary<int, string>();

					string get_path() {
						if (name_index < 3) {
							return null;
						}
						string path = (names[2][0] == 'F' ? "Female" : "Male");
						for (int index = 3; index < name_index; index++) {
							path += "/" + names[index];
						}
						return path;
					}

					byte[] get_texture_bytes(Texture2D texture) {
						RenderTexture render_texture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
						Graphics.Blit(texture, render_texture);
						RenderTexture original_render_texture = RenderTexture.active;
						RenderTexture.active = render_texture;
						Texture2D new_texture = new Texture2D(texture.width, texture.height);
						new_texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
						new_texture.Apply();
						RenderTexture.active = original_render_texture;
						RenderTexture.ReleaseTemporary(render_texture);
						return new_texture.EncodeToPNG();
					}

					int get_bytes_hash(byte[] bytes) {
						int result = 17;
						foreach (byte b in bytes) {
							result = result * 31 + b.GetHashCode();
						}
						return result;
					}

					void check_transform(Transform transform) {
						names[name_index++] = transform.name.Replace(" Variant(Clone)", "");
						int renderer_index = 0;
						foreach (SkinnedMeshRenderer renderer in transform.GetComponents<SkinnedMeshRenderer>()) {
							names[name_index++] = $"renderer_{renderer_index:D3}";
							int material_index = 0;
							foreach (Material material in renderer.materials) {
								names[name_index++] = $"material_{material_index:D3}";
								foreach (string property_name in material.GetTexturePropertyNames()) {
									if (!(material.GetTexture(property_name) is Texture2D texture)) {
										continue;
									}
									names[name_index++] = property_name;
									string path = get_path();
									if (dumped_paths.Contains(path)) {
										material_index++;
										name_index--;
										continue;
									}
									byte[] bytes = get_texture_bytes(texture);
									int hash = get_bytes_hash(bytes);
									string file_path = Path.Combine(this.m_dump_root, path);
									Directory.CreateDirectory(Path.GetDirectoryName(file_path));
									if (written_file_hashes.TryGetValue(hash, out string existing_path)) {
										file_path += ".txt";
										DDPlugin._info_log($"--> hash 0x{hash:X16} exists; linking '{path}' to '{existing_path}'.");
										File.WriteAllText(file_path, existing_path);
									} else {
										file_path += ".png";
										DDPlugin._info_log($"--> writing {bytes.Length} bytes to '{file_path}'.");
										File.WriteAllBytes(file_path, bytes);
										written_file_hashes[hash] = path;
									}
									dumped_paths.Add(path);
									name_index--;
								}
								material_index++;
								name_index--;
							}
							renderer_index++;
							name_index--;
						}
						foreach (Transform child in transform) {
							check_transform(child);
						}
						name_index--;
					}
					
					check_transform(customer.transform);
					if (DESTROY_CUSTOMERS) {
						GameObject.Destroy(customer.gameObject);
					}
					yield return new WaitForSeconds(0.1f);
				}
				DDPlugin._info_log("dump_textures coroutine completed.");
				GameObject.Destroy(this);
				m_instance = null;
				if (QUIT_WHEN_DONE) {
					Application.Quit();
				}
			}
		}

		private static CustomMaterialHandler m_instance = null;
		public static CustomMaterialHandler Instance {
			get {
				return m_instance;
			}
		}
		private CustomTextureDict m_textures = null;
		private PatchedKeyList m_patched_keys = new PatchedKeyList();

		private void Awake() {
			m_instance = this;
			this.initialize();
		}

		public void dump_textures() {
			TextureDumper.dump_textures();
		}

		private bool harmony_patch_CharacterCustomization_setApparel(CharacterCustomization __instance, int selection, int slot, int materialSelection) {
			return true;
		}

		private bool harmony_patch_CharacterCustomization_setApparelByName(CharacterCustomization __instance, string name, int slot, int materialSelection) {
			try {
				if (!Settings.m_enabled.Value || TextureDumper.Instance != null) {
					return true;
				}
				/*
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
				*/
				return true;
			} catch (Exception e) {
				DDPlugin._error_log("** apparel_ensure_patched_texture_list ERROR - " + e);
			}
			return true;
		}

		public void initialize() {
			this.reload_textures();
		}

		private string key_from_path(string path, string base_path) {
			string key = path.Replace("\\", "/").Substring((base_path == null ? 0 : base_path.Length)).Trim('/');
			for (int index = key.Length - 1; index >= 0; index--) {
				if (key[index] == '/') {
					return key;
				}
				if (key[index] == '.') {
					return key.Substring(0, index);
				}
			}
			return key;
		}

		public void reload_textures() {
			try {
				string root_dir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Settings.m_subdir.Value);
				DDPlugin._info_log($"Assembling custom texture information from directory, '{root_dir}'.");
				if (!Directory.Exists(root_dir)) {
					DDPlugin._info_log("Directory does not exist; creating.");
					Directory.CreateDirectory(root_dir);
				}
				this.m_textures = new CustomTextureDict();
				this.m_patched_keys.reset();
				int total_counter = 0;
				foreach (string mod_dir in Directory.GetDirectories(root_dir)) {
					string mod_name = Path.GetFileName(mod_dir);
					if (mod_name.IsNullOrWhiteSpace() || mod_name[0] == '_' || mod_name[0] == '.') {
						continue;
					}
					int local_counter = 0;
					foreach (string wildcard in new string[] {"*.png", "*.txt"}) { 
						foreach (string file_path in Directory.GetFiles(mod_dir, wildcard, SearchOption.AllDirectories)) {
							string key = this.key_from_path(file_path, mod_dir);
							DDPlugin._debug_log(key);
							if (this.m_textures.add_item(key, file_path)) {
								local_counter++;
							}
						}
					}
					DDPlugin._info_log($"--> {mod_name}: {local_counter} textures.");
					total_counter += local_counter;
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
					return CustomMaterialHandler.Instance.harmony_patch_CharacterCustomization_setApparel(__instance, selection, slot, materialSelection);
				}
			}

			[HarmonyPatch(typeof(CharacterCustomization), "setApparelByName")]
			class HarmonyPatch_CharacterCustomization_setApparelByName {
				private static bool Prefix(CharacterCustomization __instance, string name, int slot, int materialSelection) {
					return CustomMaterialHandler.Instance.harmony_patch_CharacterCustomization_setApparelByName(__instance, name, slot, materialSelection);
				}
			}
		}
	}

	class Model {
		[HarmonyPatch(typeof(CGameManager), "Awake")]
		class HarmonyPatch_CGameManager_Awake {
			private static void Postfix() {
				CGameManager.Instance.gameObject.AddComponent<CustomMaterialHandler>();
				CEventManager.AddListener<CEventPlayer_GameDataFinishLoaded>(on_game_data_finish_loaded);
			}
		}

		private static void on_game_data_finish_loaded(CEventPlayer_GameDataFinishLoaded evt) {
			try {
				
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
					/*/
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
					*/

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
