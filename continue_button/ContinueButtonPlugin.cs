using BepInEx;
using HarmonyLib;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public static class PluginInfo {

	public const string TITLE = "Continue Button";
	public const string NAME = "continue_button";
	public const string SHORT_DESCRIPTION = "Adds a button to the title menu to quickly load the auto-save slot.  Also adds command-line parameter (--continue) for no-click start [great for mod debugging].";

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
public class ContinueButtonPlugin : DDPlugin {
	private Harmony m_harmony = new Harmony(PluginInfo.GUID);
	//private static bool m_is_first_load = true;

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

    [HarmonyPatch(typeof(TitleScreen), "Start")]
    class HarmonyPatch_TitleScreen_Start {
        private static void Postfix(TitleScreen __instance) {
            try {
				if (!Settings.m_enabled.Value) {
					return;
				}
                Transform parent = __instance.transform.GetChild(0).Find("AnimGrp");
                Dictionary<string, Transform> buttons = new Dictionary<string, Transform>();
                foreach (Transform child in parent) {
                    buttons[child.name] = child;
                }
                buttons["ContinueBtn"].position = buttons["NewGameBtn"].position + Vector3.up * (buttons["NewGameBtn"].position.y - buttons["LoadGameBtn"].transform.position.y);
				Button button = buttons["ContinueBtn"].GetChild(0).GetChild(1).Find("BtnRaycast").GetComponent<Button>();
				button.onClick.AddListener((UnityAction) delegate {
                    SoundManager.GenericLightTap();
                    CSingleton<CGameManager>.Instance.m_CurrentSaveLoadSlotSelectedIndex = 0;
                    CSingleton<CGameManager>.Instance.m_IsManualSaveLoad = true;
                });
				buttons["ContinueBtn"].gameObject.SetActive(true);
				// This requires async check for load to be safe and is not faster than clicking the button.
				//if (m_is_first_load) {
				//	m_is_first_load = false;
				//	foreach (string arg in System.Environment.GetCommandLineArgs()) {
				//		if (arg.ToLower() == "--continue") {
				//			AutoContinuer.initialize(__instance, buttons);
				//		}
				//	}
				//}
            } catch (Exception e) {
                DDPlugin._error_log("** HarmonyPatch_TitleScreen_Start.Postfix ERROR - " + e);
            }
        }
    }

	class AutoContinuer : MonoBehaviour {
		private static TitleScreen m_title_screen;
		private static Dictionary<string, Transform> m_buttons;

		public static void initialize(TitleScreen title_screen, Dictionary<string, Transform> buttons) {
			m_title_screen = title_screen;
			m_buttons = buttons;
            m_title_screen.gameObject.AddComponent<AutoContinuer>();
        }

        private void Awake() {
			this.StartCoroutine(this.coroutine_auto_continue());
		}

		private IEnumerator coroutine_auto_continue() {
			foreach (Transform button in m_buttons.Values) {
				DDPlugin._info_log($"button: {button}");
				button.gameObject.SetActive(false);
			}
			yield return new WaitForSeconds(0.5f);
			m_title_screen.OnPressLoadGame();
		}
	}
}
