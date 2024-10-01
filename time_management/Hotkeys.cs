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
    private const int HOTKEY_TIME_STOP_TOGGLE = 1;
    private const int HOTKEY_TIME_SPEED_UP = 2;
    private const int HOTKEY_TIME_SPEED_DOWN = 3;
    private static Dictionary<int, List<KeyCode>> m_hotkeys = null;

    public static void load() {
        m_hotkeys = new Dictionary<int, List<KeyCode>>();
        set_hotkey(Settings.m_hotkey_modifier.Value, HOTKEY_MODIFIER);
        set_hotkey(Settings.m_hotkey_time_stop_toggle.Value, HOTKEY_TIME_STOP_TOGGLE);
        set_hotkey(Settings.m_hotkey_time_speed_up.Value, HOTKEY_TIME_SPEED_UP);
        set_hotkey(Settings.m_hotkey_time_speed_down.Value, HOTKEY_TIME_SPEED_DOWN);
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
            if (is_hotkey_down(HOTKEY_TIME_STOP_TOGGLE)) {
                TimeManagementPlugin.GameClock.m_is_time_stopped = !TimeManagementPlugin.GameClock.m_is_time_stopped;
            } else if (is_hotkey_down(HOTKEY_TIME_SPEED_UP)) {
                Settings.m_time_speed.Value += Settings.m_time_speed_delta.Value;
            } else if (is_hotkey_down(HOTKEY_TIME_SPEED_DOWN)) {
                Settings.m_time_speed.Value -= Settings.m_time_speed_delta.Value;
            }
        }
    }
}
