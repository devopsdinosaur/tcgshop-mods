﻿using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections.Generic;
using CC;
using Pathfinding;

public static class PluginInfo {

	public const string TITLE = "Nerd Shirts";
	public const string NAME = "nerd_shirts";
	public const string SHORT_DESCRIPTION = "Framework mod for allowing the replacement, modification, and addition of all character model textures.  By itself this mod adds 80s cartoon decals.  Create your own and share them!  Mesh and model support coming soon.";

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

		[HarmonyPatch(typeof(CGameManager), "LoadMainLevelAsync")]
		class HarmonyPatch_CGameManager_LoadMainLevelAsync {
			private static bool Prefix(CGameManager __instance, string sceneName, int loadGameIndex) {
				ApparelManager.initialize();
				CustomMaterialHandler.initialize();
				TextureTester.initialize();
				return true;
			}
		}

		[HarmonyPatch(typeof(CGameManager), "Update")]
		class HarmonyPatch_CGameManager_Update {
			private static void Postfix(CGameManager __instance) {
				Hotkeys.Updaters.keypress_update();
			}
		}
	}
}
