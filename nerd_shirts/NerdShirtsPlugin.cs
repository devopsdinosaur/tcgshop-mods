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
using ch.sycoforge.Decal;

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
public class CustomCustomersPlugin : DDPlugin {
	private Harmony m_harmony = new Harmony(PluginInfo.GUID);

	private void Awake() {
		logger = this.Logger;
		try {
			this.plugin_info = PluginInfo.to_dict();
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
				CEventManager.AddListener<CEventPlayer_GameDataFinishLoaded>(on_game_data_finish_loaded);
			}
		}

		private static void on_game_data_finish_loaded(CEventPlayer_GameDataFinishLoaded evt) {
			try {
				//CustomAssetManager.Instance.load_asset_bundle("decal_test_bundle", Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "decal_test_bundle"));
				//Application.Quit();
			} catch (Exception e) {
				DDPlugin._error_log("** on_game_data_finish_loaded ERROR - " + e);
			}
		}

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
					Transform shirt_transform = UnityUtils.find_first_descendant(customer.transform, "Upper_Body").GetChild(0);
					//AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "decal_projector_bundle"));
					//Shader decal_shader = bundle.LoadAsset<Material>("Assets/Materials/Decal.mat").shader;
					//Material material = load_material("C:/tmp/textures/horde_crest.png", decal_shader);
					//GameObject projector_object = GameObject.Instantiate(bundle.LoadAsset<GameObject>("Assets/Prefabs/Decal_Projector.prefab"));
					//foreach (Component component in projector_object.GetComponents<Component>()) {
					//	DDPlugin._debug_log(component.GetType());
					//}
					GameObject obj = new GameObject("Customer_Decal_Projector");
					obj.transform.SetParent(shirt_transform);
					obj.transform.position = shirt_transform.position + Vector3.forward * 1.0f;
					obj.transform.localScale = new Vector3(10, 10, 10);
					obj.transform.LookAt(shirt_transform.position);
					EasyDecal projector = obj.AddComponent<EasyDecal>();
					projector.DecalMaterial = load_material("C:/tmp/textures/thundercats.png", Shader.Find("Standard"));
					//projector.Mask = LayerMask.NameToLayer() || 
					//projector.BakeOnAwake = true;
					projector.Technique = ProjectionTechnique.Box;

					projector.gameObject.SetActive(true);
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
