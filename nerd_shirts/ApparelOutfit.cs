using CC;

public class ApparelOutfit {
    private CC_CharacterData m_data;
    private int m_gender;
    public int Gender {
        get {
            return this.m_gender;
        }
    }
    public string PresetName {
        get {
            return this.m_data.CharacterName;
        }
    }
    public string PrefabKey {
        get {
            return this.m_data.CharacterPrefab;
        }
    }
    private ApparelInfo[] m_outfit = new ApparelInfo[ApparelInfo.NUM_SLOTS];
    public ApparelInfo[] Outfit {
        get {
            return this.m_outfit;
        }
    }

    public ApparelOutfit(int gender, CC_CharacterData data) {
        this.m_gender = gender;
        this.m_data = data;
        for (int slot = 0; slot < ApparelInfo.NUM_SLOTS; slot++) {
            this.m_outfit[slot] = ApparelInfo.create_or_use_existing(gender, slot, (slot >= data.ApparelNames.Count ? ApparelInfo.APPAREL_NONE : data.ApparelNames[slot]));
        }
    }
}