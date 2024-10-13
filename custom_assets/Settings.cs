using BepInEx.Configuration;

public class Settings {
    const int DEFAULT_MAX_CHARACTER_MODELS = 100;
    const int ABSOLUTE_MAX_CHARACTER_MODELS = 500;

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
    
    public void load(DDPlugin plugin) {
        this.m_plugin = plugin;

        // General
        m_enabled = this.m_plugin.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
        
    }
}