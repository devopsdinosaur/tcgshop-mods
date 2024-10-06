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
    public static ConfigEntry<int> m_chance_to_continue_shopping;
    public static ConfigEntry<float> m_buy_item_chance_multiplier;
    //public static ConfigEntry<int> m_chance_to_find_another_activity_if_idle;

    public void load(DDPlugin plugin) {
        this.m_plugin = plugin;

        // General
        m_enabled = this.m_plugin.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
        m_per_item_grab_chance_reduction = this.m_plugin.Config.Bind<int>("General", "Per Item Grab Chance Reduction", 5, "Each time a customer picks up an item the chance of getting another is reduced by the total amount of grabbed items times this number (int, default 5 [game default], set to lower number to increase chances of getting more items [go negative for spending fever!]).");
        m_chance_to_continue_shopping = this.m_plugin.Config.Bind<int>("General", "Chance to Continue Shopping", 50, "Chance to continue shopping after grabbing a set of one or more items or cards from a single shelf compartment/slot (int, default 50 [game default], set to higher number to increase chances of continuing shopping).");
        m_buy_item_chance_multiplier = this.m_plugin.Config.Bind<float>("General", "Buy Item Chance Multiplier", 1f, "Multiplier applied to the GetBuyItemChance result, which is a sliding scale of probability inversely proportional to sell price - market price (float, default 1f [no change], higher number == better odds of buying, >= 100f will always buy).");
        //m_chance_to_find_another_activity_if_idle = this.m_plugin.Config.Bind<int>("General", "Chance to Find Another Activity if Idle", 33, "Chance to attempt to find another activity (i.e. look for item/card or play or keep idling) instead of choosing to leave the shop (int, default 33 [game default], set to higher number to increase chances of continuing shopping/playing [note that even at 100% the customer will leave after a set number of failures (3-5 game default) to prevent infinite looping if shelves are empty etc.]).");
    }
}