using BepInEx.Configuration;

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
    public static ConfigEntry<string> m_hotkey_time_speed_reverse;

    public void load(DDPlugin plugin) {
        this.m_plugin = plugin;

        // General
        m_enabled = this.m_plugin.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
        m_time_speed = this.m_plugin.Config.Bind<float>("General", "Initial Time Scale", 1.0f, "Initial time scale (float, default 1 [game default time scale], > 1 faster clock, < 1 slower clock, < 0 reverse time)");
        m_time_speed_delta = this.m_plugin.Config.Bind<float>("General", "Time Scale Delta", 0.25f, "Change in time scale with each up/down hotkey tick (float, default 0.25).");
        m_twenty_four_hour_format = this.m_plugin.Config.Bind<bool>("General", "24-hour Time Format", false, "If true then display time in 24-hour format, if false then display as game default AM/PM.");
        m_day_begin_hour = this.m_plugin.Config.Bind<int>("General", "Day Begin Hour", 8, "First hour of a new day (int, between 0 [midnight] and 22 [10pm], default 8 [8am]).  Changes to this value require a game restart.");
        m_day_end_hour = this.m_plugin.Config.Bind<int>("General", "Day End Hour", 21, "Store closing hour (int, between 1 [1am] and 23 [11pm], default 21 [9pm]).  If Day End Hour is >= Day Begin Hour then both will reset to game defaults.  Changes to this value require a game restart.");
        if (m_day_begin_hour.Value < 0 || m_day_begin_hour.Value > 22) {
            m_day_begin_hour.Value = 8;
        }
        if (m_day_end_hour.Value <= 0 || m_day_end_hour.Value > 23) {
            m_day_end_hour.Value = 21;
        }
        if (m_day_begin_hour.Value >= m_day_end_hour.Value) {
            m_day_begin_hour.Value = 8;
            m_day_end_hour.Value = 21;
        }
        TimeManagementPlugin.GameClock.m_day_begin_hour = m_day_begin_hour.Value;
        TimeManagementPlugin.GameClock.m_day_end_hour = m_day_end_hour.Value;
        DDPlugin._info_log($"Day Begin Hour: {TimeManagementPlugin.GameClock.m_day_begin_hour}");
        DDPlugin._info_log($"Day End Hour: {TimeManagementPlugin.GameClock.m_day_end_hour}");

        // Hotkeys
        m_hotkey_modifier = this.m_plugin.Config.Bind<string>("Hotkeys", "Hotkey - Modifier", "LeftControl,RightControl", "Comma-separated list of Unity Keycodes used as the special modifier key (i.e. ctrl,alt,command) one of which is required to be down for hotkeys to work.  Set to '' (blank string) to not require a special key (not recommended).  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
        m_hotkey_time_stop_toggle = this.m_plugin.Config.Bind<string>("Hotkeys", "Time Start/Stop Toggle Hotkey", "Alpha0,Keypad0", "Comma-separated list of Unity Keycodes, any of which will toggle the passage of time.  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
        m_hotkey_time_speed_up = this.m_plugin.Config.Bind<string>("Hotkeys", "Time Scale Increment Hotkey", "Equals,KeypadPlus", "Comma-separated list of Unity Keycodes, any of which will increase the time speed.  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
        m_hotkey_time_speed_down = this.m_plugin.Config.Bind<string>("Hotkeys", "Time Scale Decrement Hotkey", "Minus,KeypadMinus", "Comma-separated list of Unity Keycodes, any of which will decrease the time speed.  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
        m_hotkey_time_speed_reverse = this.m_plugin.Config.Bind<string>("Hotkeys", "Time Scale Reverse Hotkey", "Alpha9,Keypad9", "Comma-separated list of Unity Keycodes, any of which will reverse the time speed.  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
    }
}