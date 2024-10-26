
public class ApparelInfo {
    public const int GENDER_FEMALE = 0;
    public const int GENDER_MALE = 1;
    private static readonly string[] GENDER_STRINGS = {"Female", "Male"};
    public const int SLOT_BODY = 0;
    public const int SLOT_LEGS = 1;
    public const int SLOT_FEET = 2;
    public const int SLOT_ACCESSORY = 3;
    private static readonly string[] SLOT_STRINGS = {"Body", "Legs", "Feet", "Accessory"};

    private int m_gender;
    public int Gender {
        get {
            return this.m_gender;
        }
    }
    public string GenderString {
        get {
            return GENDER_STRINGS[this.m_gender];
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
            return SLOT_STRINGS[this.m_slot];
        }
    }
    private string m_name;
    public string Name {
        get {
            return this.m_name;
        }
    }

    public ApparelInfo(int gender, int slot, string name) {
        this.m_gender = gender;
        this.m_slot = slot;
        this.m_name = name;
    }
}
