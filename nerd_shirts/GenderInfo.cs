using System.Collections.Generic;
using UnityEngine;

public class GenderInfo {
    public const int NUM_GENDERS = 3;
    public const int GENDER_FEMALE = 0;
    public const int GENDER_MALE = 1;
    public const int GENDER_NON_BINARY = 2;
    private static readonly string[] GENDER_STRINGS = {"Female", "Male", "Non-Binary"};
    private static readonly GenderInfo INVALID_GENDER = new GenderInfo(-1);
    private static Dictionary<int, GenderInfo> m_prefab_hash_map = new Dictionary<int, GenderInfo>();
    private int m_gender;
    public int Gender {
        get {
            return this.m_gender;
        }
    }
    public string GenderString {
        get {
            return (this.m_gender >= 0 && this.m_gender < GENDER_STRINGS.Length ? GENDER_STRINGS[this.m_gender] : "Unknown");
        }
    }

    public GenderInfo(int gender) {
        this.m_gender = gender;
    }

    public GenderInfo(string gender_string) {
        this.m_gender = gender_from_string(gender_string);
    }

    public static implicit operator GenderInfo(int gender) {
        return new GenderInfo(gender);
    }

    public static int gender_from_string(string value) {
        for (int gender = 0; gender < NUM_GENDERS; gender++) {
            if (value == GENDER_STRINGS[gender]) {
                return gender;
            }
        }
        return -1;
    }

    public static string gender_string(int gender) {
        return (gender >= 0 && gender < GENDER_STRINGS.Length ? GENDER_STRINGS[gender] : "None");
    }

    public static GenderInfo get_gender_from_prefab_hash(int prefab_hash) {
        if (m_prefab_hash_map.TryGetValue(prefab_hash, out var gender)) {
            return gender;
        }
        return INVALID_GENDER;
    }

    public static void set_gender_prefab_hash(int prefab_hash, int gender) {
        m_prefab_hash_map[prefab_hash] = gender;
    }

    public override string ToString() {
        return this.GenderString;
    }
}