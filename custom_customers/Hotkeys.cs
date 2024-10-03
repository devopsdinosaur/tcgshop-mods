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
    private const int HOTKEY_DEBUG = 1;
    private const int HOTKEY_DEBUG2 = 2;
    private static Dictionary<int, List<KeyCode>> m_hotkeys = null;

    public static void load() {
        m_hotkeys = new Dictionary<int, List<KeyCode>>();
        set_hotkey("", HOTKEY_MODIFIER);
        set_hotkey("F8", HOTKEY_DEBUG);
        set_hotkey("F9", HOTKEY_DEBUG2);
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
            if (is_hotkey_down(HOTKEY_DEBUG)) {
                CustomCustomersPlugin.__Testing__.hotkey_triggered_test_method();
            }
            if (is_hotkey_down(HOTKEY_DEBUG2)) {
                CustomCustomersPlugin.__Testing__.hotkey_triggered_test_method_2();
            }
        }
    }
}
