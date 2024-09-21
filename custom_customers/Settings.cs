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
    public static ConfigEntry<bool> m_always_exact_change;
    public static ConfigEntry<float> m_max_money_multiplier;
    public static ConfigEntry<float> m_walk_speed_multiplier;
    public static ConfigEntry<float> m_worker_walk_speed_multiplier;

    public void load(DDPlugin plugin) {
        this.m_plugin = plugin;

        // General
        m_enabled = this.m_plugin.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
        m_always_exact_change = this.m_plugin.Config.Bind<bool>("General", "Always Exact Change", false, "Set to true to always have customers pay with the exact amount of cash for the purchase.");
        m_max_money_multiplier = this.m_plugin.Config.Bind<float>("General", "Max Money Multiplier", 1.0f, "Multiplier applied to customer maximum spending amount (float, default 1 [no change], > 1 more, < 1 less).");
        m_walk_speed_multiplier = this.m_plugin.Config.Bind<float>("General", "Walk Speed Multiplier", 1.0f, "Multiplier applied to customer walk speed (float, default 1 [no change], > 1 faster, < 1 slower).");
        m_worker_walk_speed_multiplier = this.m_plugin.Config.Bind<float>("General", "Worker Walk Speed Multiplier", 1.0f, "Multiplier applied to worker walk speed (float, default 1 [no change], > 1 faster, < 1 slower).");
    }
}