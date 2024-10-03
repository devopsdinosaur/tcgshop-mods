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
    public static ConfigEntry<float> m_movement_speed_multiplier;
    
    public void load(DDPlugin plugin) {
        this.m_plugin = plugin;

        // General
        m_enabled = this.m_plugin.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
        m_movement_speed_multiplier = this.m_plugin.Config.Bind<float>("General", "Movement Speed Multiplier", 1.0f, "Multiplier applied to player speed (float, default 1 [no change], > 1 faster, < 1 slower).");
    }
}