using BepInEx.Configuration;
using System;
using System.Collections.Generic;

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
    public static ConfigEntry<bool> m_disable_negative_modifiers;
    public static ConfigEntry<float> m_value_multiplier;
    public static Dictionary<ECustomerReviewType, ConfigEntry<bool>> m_ignored_review_types = new Dictionary<ECustomerReviewType, ConfigEntry<bool>>();

    public void load(DDPlugin plugin) {
        this.m_plugin = plugin;

        // General
        m_enabled = this.m_plugin.Config.Bind<bool>("General", "Enabled", true, "Set to false to disable this mod.");
        m_disable_negative_modifiers = this.m_plugin.Config.Bind<bool>("General", "Disable Negative Modifiers", false, "Set to true to have customers never affected by negative stuff, i.e. reviews are never bad.");
        m_value_multiplier = this.m_plugin.Config.Bind<float>("General", "Review Value Multiplier", 1.0f, "Increases the value/rating of positive review factors and decreases the value of negative factors (float, default 1 [no change], ignored if less than 1).");
        foreach (string name in Enum.GetNames(typeof(ECustomerReviewType))) {
            m_ignored_review_types[(ECustomerReviewType) Enum.Parse(typeof(ECustomerReviewType), name)] = this.m_plugin.Config.Bind<bool>("General", "Disable Review Type - " + name, false, $"Set to true to disable all '{name}' reviews.");
        }
    }
}