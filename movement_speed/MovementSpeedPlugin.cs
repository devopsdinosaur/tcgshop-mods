using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class PluginInfo {

	public const string TITLE = "Movement Speed";
	public const string NAME = "movement_speed";
	public const string SHORT_DESCRIPTION = "Adds a configurable multiplier for player movement speed!";

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
public class MovementSpeedPlugin : DDPlugin {
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

	class __Mover__ {
		[HarmonyPatch(typeof(CMF.Mover), "SetVelocity")]
		class HarmonyPatch_CMF_Mover_SetVelocity {
			private static bool Prefix(ref Vector3 _velocity) {
				if (Settings.m_enabled.Value && Settings.m_movement_speed_multiplier.Value > 0) {
					_velocity *= Settings.m_movement_speed_multiplier.Value;
				}
				return true;
			}
		}
	}
}
