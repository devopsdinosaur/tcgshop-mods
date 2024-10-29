using System.Collections.Generic;

public class ApparelInfo {
    public const int NUM_SLOTS = 4;
    public const int SLOT_BODY = 0;
    public const int SLOT_LEGS = 1;
    public const int SLOT_FEET = 2;
    public const int SLOT_ACCESSORY = 3;
    private static readonly string[] SLOT_STRINGS = {"Body", "Legs", "Feet", "Accessory"};
    public static readonly string APPAREL_NONE = "None";

    private static List<ApparelInfo> m_all_apparel = new List<ApparelInfo>();
    private GenderInfo m_gender;
    public GenderInfo Gender {
        get {
            return this.m_gender;
        }
    }
    private int m_slot;
    public int Slot {
        get {
            return this.m_slot;
        }
    }
    public string SlotString {
        get {
            return slot_string(this.m_slot);
        }
    }
    private string m_name;
    public string Name {
        get {
            return this.m_name;
        }
    }

    public static ApparelInfo create_or_use_existing(int gender, int slot, string name) {
        foreach (ApparelInfo existing_item in m_all_apparel) {
            if (existing_item.Gender.Gender == gender && existing_item.Slot == slot && existing_item.Name == name) {
                return existing_item;
            }
        }
        ApparelInfo new_item = new ApparelInfo(gender, slot, name);
        m_all_apparel.Add(new_item);
        return new_item;
    }

    private ApparelInfo(int gender, int slot, string name) {
        this.m_gender = gender;
        this.m_slot = slot;
        this.m_name = name;
    }

    public static string slot_string(int slot) {
        return (slot >= 0 && slot < SLOT_STRINGS.Length ? SLOT_STRINGS[slot] : APPAREL_NONE);
    }
}
