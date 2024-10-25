using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using CC;
using Pathfinding;

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

	class Model {
		[HarmonyPatch(typeof(CGameManager), "Awake")]
		class HarmonyPatch_CGameManager_Awake {
			private static void Postfix() {
				CGameManager.Instance.gameObject.AddComponent<CustomMaterialHandler>();
                CGameManager.Instance.gameObject.AddComponent<TextureTester>();
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
