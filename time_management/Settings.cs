﻿using BepInEx.Configuration;

public class Settings {
    private static Settings m_instance = null;
    public static Settings Instance {
        get {
            if (m_instance == null) {
                m_instance = new Settings();
            }
            return m_instance;
        }
    }
    private DDPlugin m_plugin = null;

    // General
    public static ConfigEntry<bool> m_enabled;
    public static ConfigEntry<float> m_time_speed;
    public static ConfigEntry<float> m_time_speed_delta;
    public static ConfigEntry<bool> m_twenty_four_hour_format;
    public static ConfigEntry<int> m_day_begin_hour;
    public static ConfigEntry<int> m_day_end_hour;

    // Hotkeys
    public static ConfigEntry<string> m_hotkey_modifier;
    public static ConfigEntry<string> m_hotkey_time_stop_toggle;
    public static ConfigEntry<string> m_hotkey_time_speed_up;
    public static ConfigEntry<string> m_hotkey_time_speed_down;
    public static ConfigEntry<string> m_hotkey_time_reverse_toggle;

    public void load(DDPlugin plugin) {
        this.m_plugin = plugin;

        // General
        m_enabled = this.m_plugin.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
        m_time_speed = this.m_plugin.Config.Bind<float>("General", "Initial Time Scale", 1.0f, "Initial time scale (float, default 1 [game default time scale], > 1 faster clock, < 1 slower clock)");
        m_time_speed_delta = this.m_plugin.Config.Bind<float>("General", "Time Scale Delta", 0.1f, "Change in time scale with each up/down hotkey tick (float).");
        m_twenty_four_hour_format = this.m_plugin.Config.Bind<bool>("General", "24-hour Time Format", false, "If true then display time in 24-hour format, if false then display as game default AM/PM.");
        m_day_begin_hour = this.m_plugin.Config.Bind<int>("General", "Day Begin Hour", 8, "First hour of a new day (int, between 0 [midnight] and 23 [11pm], default 8 [8am]).");
        m_day_end_hour = this.m_plugin.Config.Bind<int>("General", "Day End Hour", 21, "Store opening hour (int, between 0 [midnight] and 23 [11pm], default 21 [9pm]).");

        // Hotkeys
        m_hotkey_modifier = this.m_plugin.Config.Bind<string>("Hotkeys", "Hotkey - Modifier", "LeftControl,RightControl", "Comma-separated list of Unity Keycodes used as the special modifier key (i.e. ctrl,alt,command) one of which is required to be down for hotkeys to work.  Set to '' (blank string) to not require a special key (not recommended).  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
        m_hotkey_time_stop_toggle = this.m_plugin.Config.Bind<string>("Hotkeys", "Time Start/Stop Toggle Hotkey", "Alpha0,Keypad0", "Comma-separated list of Unity Keycodes, any of which will toggle the passage of time.  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
        m_hotkey_time_speed_up = this.m_plugin.Config.Bind<string>("Hotkeys", "Time Scale Increment Hotkey", "Equals,KeypadPlus", "Comma-separated list of Unity Keycodes, any of which will increase the time speed.  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
        m_hotkey_time_speed_down = this.m_plugin.Config.Bind<string>("Hotkeys", "Time Scale Decrement Hotkey", "Minus,KeypadMinus", "Comma-separated list of Unity Keycodes, any of which will decrease the time speed.  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
        m_hotkey_time_reverse_toggle = this.m_plugin.Config.Bind<string>("Hotkeys", "Time Reverse Toggle Hotkey", "Home", "Comma-separated list of Unity Keycodes, any of which will toggle reverse/forward time change.  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
    }
}