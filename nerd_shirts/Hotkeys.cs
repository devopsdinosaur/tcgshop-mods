using System.Collections.Generic;
using UnityEngine;

class Hotkeys {
    private static Hotkeys m_instance = null;
    public static Hotkeys Instance {
        get {
            if (m_instance == null) {
                m_instance = new Hotkeys();
            }
            return m_instance;
        }
    }
    private const int HOTKEY_MODIFIER = 0;
    private const int HOTKEY_DUMP_TEXTURES = 1;
    private const int HOTKEY_SPAWN_ALL_PRESETS = 2;
    private const int HOTKEY_DESTROY_ALL_SPAWNS = 3;
    private static Dictionary<int, List<KeyCode>> m_hotkeys = null;

    public static void load() {
        m_hotkeys = new Dictionary<int, List<KeyCode>>();
        set_hotkey(Settings.m_hotkey_modifier.Value, HOTKEY_MODIFIER);
        set_hotkey(Settings.m_hotkey_dump_textures.Value, HOTKEY_DUMP_TEXTURES);
        set_hotkey(Settings.m_hotkey_spawn_all_presets.Value, HOTKEY_SPAWN_ALL_PRESETS);
        set_hotkey(Settings.m_hotkey_destroy_all_spawns.Value, HOTKEY_DESTROY_ALL_SPAWNS);
    }

    private static void set_hotkey(string keys_string, int key_index) {
        m_hotkeys[key_index] = new List<KeyCode>();
        foreach (string key in keys_string.Split(',')) {
            string trimmed_key = key.Trim();
            if (trimmed_key != "") {
                m_hotkeys[key_index].Add((KeyCode) System.Enum.Parse(typeof(KeyCode), trimmed_key));
            }
        }
    }

    private static bool is_modifier_hotkey_down() {
        if (m_hotkeys[HOTKEY_MODIFIER].Count == 0) {
            return true;
        }
        foreach (KeyCode key in m_hotkeys[HOTKEY_MODIFIER]) {
            if (Input.GetKey(key)) {
                return true;
            }
        }
        return false;
    }

    public static bool is_hotkey_down(int key_index) {
        foreach (KeyCode key in m_hotkeys[key_index]) {
            if (Input.GetKeyDown(key)) {
                return true;
            }
        }
        return false;
    }

    public class Updaters {
        public static void keypress_update() {
            if (!is_modifier_hotkey_down()) {
                return;
            }
            if (is_hotkey_down(HOTKEY_DUMP_TEXTURES)) {
                TextureDumper.dump_textures();
            } else if (is_hotkey_down(HOTKEY_SPAWN_ALL_PRESETS)) {
                TextureTester.Instance.spawn_all_presets();
            } else if (is_hotkey_down(HOTKEY_DESTROY_ALL_SPAWNS)) {
                DDPlugin._info_log("2");
                TextureTester.Instance.destroy_all_spawns();
            }
        }
    }
}
