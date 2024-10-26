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
    public static ConfigEntry<string> m_log_level;
    public static ConfigEntry<string> m_subdir;
    public static ConfigEntry<string> m_apparel_force_wear;

    // Hotkeys
    public static ConfigEntry<string> m_hotkey_modifier;
    public static ConfigEntry<string> m_hotkey_dump_textures;
    public static ConfigEntry<string> m_hotkey_spawn_all_presets;
    public static ConfigEntry<string> m_hotkey_destroy_all_spawns;

    // Test Mode
    public static ConfigEntry<bool> m_test_mode_enabled;

    public void load(DDPlugin plugin) {
        this.m_plugin = plugin;

        // General
        m_enabled = this.m_plugin.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
        m_log_level = this.m_plugin.Config.Bind<string>("General", "Log Level", "info", "[Advanced] Logging level, one of: 'none' (no logging), 'error' (only errors), 'warn' (errors and warnings), 'info' (normal logging), 'debug' (extra log messages for debugging issues).  Not case sensitive [string, default info].  Debug level not recommended unless you're noticing issues with the mod.  Changes to this setting require an application restart.");
        m_subdir = this.m_plugin.Config.Bind<string>("General", "Texture Subfolder", this.m_plugin.m_plugin_info["name"], "Subfolder under this plugin's parent folder (i.e. <game>/BepInEx/plugins) which in turn contains a set of mod-specific texture directories.");
        m_apparel_force_wear = this.m_plugin.Config.Bind<string>("General", "Force Wear", "", "Comma-separated list of apparel items (i.e. Crop_Top_01, Polo_Shirt_01, etc) exactly matching (no wildcards) the second subfolder (directly under Female/Male) of the mod/dump textures folder.  The mod will make its best effort to force all customers (of the apparel's gender) to wear the specified item(s).  Check LogOutput.txt for information if this setting does not seem to work and see the Nexus homepage for this mod for troubleshooting tips.  Changes to this setting require a game reload.");

        // Hotkeys
        m_hotkey_modifier = this.m_plugin.Config.Bind<string>("Hotkeys", "Hotkey - Modifier", "LeftControl,RightControl", "Comma-separated list of Unity Keycodes used as the special modifier key (i.e. ctrl,alt,command) one of which is required to be down for hotkeys to work.  Set to '' (blank string) to not require a special key (not recommended).  See this link for valid Unity KeyCode strings (https://docs.unity3d.com/ScriptReference/KeyCode.html)");
        m_hotkey_dump_textures = this.m_plugin.Config.Bind<string>("Hotkeys", "Hotkey - Dump Textures (Test Mode only)", "F8", "Comma-separated list of Unity Keycodes, any of which will (when combined with 'modifier' key) dump all apparel textures.");
        m_hotkey_spawn_all_presets = this.m_plugin.Config.Bind<string>("Hotkeys", "Hotkey - Spawn All Customer Presets (Test Mode only)", "Z", "Comma-separated list of Unity Keycodes, any of which will (when combined with 'modifier' key) spawn all of the customer presets (32 of them) out on the street outside of the shop.");
        m_hotkey_destroy_all_spawns = this.m_plugin.Config.Bind<string>("Hotkeys", "Hotkey - Destroy All Spawns (Test Mode only)", "X", "Comma-separated list of Unity Keycodes, any of which will (when combined with 'modifier' key) removes all the zombie customers spawned with a previous press of the 'Spawn All Presets' hotkey.");

        // Test Mode
        m_test_mode_enabled = this.m_plugin.Config.Bind<bool>("Test Mode", "Test Mode - Enabled", false, "Set to true to enable test mode.  If this value is false then all 'Test Mode' settings will be ignored.  Changing this value requires a game restart.");
        
    }
}