﻿using BepInEx.Configuration;

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
    public static ConfigEntry<bool> m_always_exact_change;
    public static ConfigEntry<bool> m_auto_populate_credit;
    public static ConfigEntry<bool> m_rollover_customer_info_enabled;
    public static ConfigEntry<int> m_rollover_customer_info_font_size;
    public static ConfigEntry<bool> m_rollover_shop_info_enabled;
    public static ConfigEntry<float> m_max_money_multiplier;
    public static ConfigEntry<float> m_spawn_frequency_multiplier;
    public static ConfigEntry<float> m_spawn_max_customer_multiplier;
    public static ConfigEntry<int> m_max_customer_models;
    public static ConfigEntry<float> m_walk_speed_multiplier;

    public void load(DDPlugin plugin) {
        this.m_plugin = plugin;

        // General
        m_enabled = this.m_plugin.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
        m_auto_populate_credit = this.m_plugin.Config.Bind<bool>("General", "Auto-Enter Credit Amount", true, "Set to true to auto-populate credit window with the correct amount (default true).");
        m_always_exact_change = this.m_plugin.Config.Bind<bool>("General", "Exact Change", false, "Set to true to always have customers pay with the exact amount of cash for the purchase.");
        m_max_money_multiplier = this.m_plugin.Config.Bind<float>("General", "Max Money Multiplier", 1.0f, "Multiplier applied to customer maximum spending amount (float, default 1 [no change], > 1 more, < 1 less).");
        m_walk_speed_multiplier = this.m_plugin.Config.Bind<float>("General", "Walk Speed Multiplier", 1.0f, "Multiplier applied to customer walk speed (float, default 1 [no change], > 1 faster, < 1 slower).");
        m_rollover_customer_info_enabled = this.m_plugin.Config.Bind<bool>("General", "Rollover Customer Info - Enabled", true, "Set to false to disable customer info when rolled over with cursor crosshair.");
        m_rollover_customer_info_font_size = this.m_plugin.Config.Bind<int>("General", "Rollover Customer Info - Font Size", 18, "Font size of rollover customer info text (int, default 18).");
        m_rollover_shop_info_enabled = this.m_plugin.Config.Bind<bool>("General", "Rollover Shop Info - Enabled", true, "Set to false to disable shop info when rolling over the shop open sign or cash register area.");
        m_spawn_frequency_multiplier = this.m_plugin.Config.Bind<float>("General", "Spawn - Frequency Multiplier", 1.0f, "Multiplier (note: smaller number == faster spawn!) applied to the time interval between customer spawns (float, default 1 [no change], > 1 less frequent, < 1 more frequent).");
        m_spawn_max_customer_multiplier = this.m_plugin.Config.Bind<float>("General", "Spawn - Max Customer Multiplier", 1.0f, "Multiplier applied to the maximum number of simultaneous spawned customers [up to the number of 'Max Customer Models'] (float, default 1 [no change], > 1 more, < 1 less).");
        m_max_customer_models = this.m_plugin.Config.Bind<int>("General", "Spawn - Max Customer Models", DEFAULT_MAX_CHARACTER_MODELS, $"Number of customer game object models created at game start (requires game reload if changed).  The number of spawned customers cannot exceed this number, as the Spawn() method just chooses an inactive model from this list and makes it visible.  Note that increasing this value might impact game performance (int, default {DEFAULT_MAX_CHARACTER_MODELS}, max {ABSOLUTE_MAX_CHARACTER_MODELS}).");
        if (m_max_customer_models.Value < 0) {
            m_max_customer_models.Value = DEFAULT_MAX_CHARACTER_MODELS;
        } else if (m_max_customer_models.Value > ABSOLUTE_MAX_CHARACTER_MODELS) {
            m_max_customer_models.Value = ABSOLUTE_MAX_CHARACTER_MODELS;
        }
    }
}