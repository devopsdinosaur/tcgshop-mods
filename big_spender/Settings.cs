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
    public static ConfigEntry<int> m_per_item_grab_chance_reduction;

    public void load(DDPlugin plugin) {
        this.m_plugin = plugin;

        // General
        m_enabled = this.m_plugin.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
        m_per_item_grab_chance_reduction = this.m_plugin.Config.Bind<int>("General", "Per Item Grab Chance Reduction", 5, "Each time a customer picks up an item the chance of getting another is reduced by the total amount of grabbed items times this number (int, default 5 [game default], set to lower number to increase chances of getting more items [go negative for spending fever!]).");
    }
}