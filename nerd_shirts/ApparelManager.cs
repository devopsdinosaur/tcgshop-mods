using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CC;

public class ApparelManager : MonoBehaviour {
    private static ApparelManager m_instance = null;
    public static ApparelManager Instance {
        get {
            return m_instance;
        }
    }
    private ApparelInfo[] m_all_apparel = null;
    private Dictionary<string, ApparelInfo> m_all_apparel_dict;
    private ApparelOutfit[][] m_preset_outfits;
    private ApparelInfo[][] m_gender_sorted_apparel;
    private ApparelInfo[][] m_slot_sorted_apparel;
    private ApparelInfo[][][] m_gender_and_slot_sorted_apparel;
    private ApparelInfo[][][] m_force_wear_apparel;

    private bool harmony_patch_ApplyCharacterVars_prefix(CharacterCustomization __instance, CC_CharacterData characterData) {
        try {
            if (!Settings.m_enabled.Value) {
                return true;
            }
            if (this.m_all_apparel == null) {
                this.update_apparel_structs();
            }
            List<string> new_names = new List<string>();
            List<int> new_selections = new List<int>();
            //DDPlugin._debug_log("1");
            ApparelInfo[][] force_items = this.m_force_wear_apparel[GenderInfo.get_gender_from_prefab_hash(characterData.CharacterPrefab.GetHashCode()).Gender];
            for (int slot = 0; slot < ApparelInfo.NUM_SLOTS; slot++) {
                //DDPlugin._debug_log("2");
                ApparelInfo[] force_slot = force_items[slot];
                //DDPlugin._debug_log("2.5");
                void set_apparel_and_random_material_selection(string new_name) {
                    //DDPlugin._debug_log(new_name);
                    foreach (scrObj_Apparel.Apparel apparel in __instance.ApparelTables[slot].Items) {
                        //DDPlugin._debug_log("3");
                        if (apparel.Name == new_name) {
                            //DDPlugin._debug_log("4");
                            if (apparel.Materials.Count == 0) {
                                //DDPlugin._warn_log($"* ApparelManager.harmony_patch_ApplyCharacterVars_prefix WARNING - '{new_name}' apparel item has no associated material definitions for current prefab customization instance; cannot force wear, falling back to original item.");
                                break;
                            }
                            new_names.Add(new_name);
                            new_selections.Add(UnityEngine.Random.Range(0, apparel.Materials.Count - 1));
                            return;
                        }
                    }
                    new_names.Add(characterData.ApparelNames[slot]);
                    new_selections.Add(characterData.ApparelMaterials[slot]);
                }
                if (force_slot.Length == 0) {
                    //DDPlugin._debug_log("default_name");
                    //DDPlugin._debug_log(characterData.ApparelNames[slot]);
                    new_names.Add(characterData.ApparelNames[slot]);
                    new_selections.Add(characterData.ApparelMaterials[slot]);
                } else if (force_items.Length == 1) {
                    set_apparel_and_random_material_selection(force_slot[0].Name);
                } else {
                    set_apparel_and_random_material_selection(force_slot[UnityEngine.Random.Range(0, force_slot.Length - 1)].Name);
                }
            }
            characterData.ApparelNames = new_names;
            characterData.ApparelMaterials = new_selections;
            //DDPlugin._debug_log(string.Join(", ", characterData.ApparelNames));
            //DDPlugin._debug_log(string.Join(", ", characterData.ApparelMaterials));
            return true;
        } catch (Exception e) {
            DDPlugin._error_log("** ApparelManager.harmony_patch_ApplyCharacterVars_prefix ERROR - " + e);
        }
        return true;
    }

    class HarmonyPatches {
        [HarmonyPatch(typeof(CharacterCustomization), "ApplyCharacterVars")]
        class HarmonyPatch_CharacterCustomization_ApplyCharacterVars {
            private static bool Prefix(CharacterCustomization __instance, CC_CharacterData characterData) {
                return ApparelManager.Instance.harmony_patch_ApplyCharacterVars_prefix(__instance, characterData);
            }
        }
    }

    public static void initialize() {
        try {
            if (m_instance != null) {
                return;
            }
            m_instance = CGameManager.Instance.gameObject.AddComponent<ApparelManager>();
        } catch (Exception e) {
            DDPlugin._error_log("** ApparelInfoManager.initialize ERROR - " + e);
        }
    }

    private void update_apparel_structs() {
        try {
            Customer[] prefabs = new Customer[GenderInfo.NUM_GENDERS] {
                CustomerManager.Instance.m_CustomerFemalePrefab,
                CustomerManager.Instance.m_CustomerPrefab,
                null // <-- TODO: Add non-binary prefab in assetbundle
            };
            List<string> force_wear_names = new List<string>();
            foreach (string _word in Settings.m_apparel_force_wear.Value.Replace(" ", "").Split(',')) {
                string word = _word.Trim();
                if (!string.IsNullOrEmpty(word) && !force_wear_names.Contains(word)) {
                    force_wear_names.Add(word);
                }
            }
            List<ApparelInfo> all_apparel = new List<ApparelInfo>();
            this.m_all_apparel_dict = new Dictionary<string, ApparelInfo>();
            List<ApparelInfo>[] gender_sorted_apparel = new List<ApparelInfo>[GenderInfo.NUM_GENDERS];
            List<ApparelInfo>[] slot_sorted_apparel = new List<ApparelInfo>[ApparelInfo.NUM_SLOTS];
            List<ApparelInfo>[][] gender_and_slot_sorted_apparel = new List<ApparelInfo>[GenderInfo.NUM_GENDERS][];
            List<ApparelInfo>[][] force_wear_apparel = new List<ApparelInfo>[GenderInfo.NUM_GENDERS][];
            this.m_preset_outfits = new ApparelOutfit[GenderInfo.NUM_GENDERS][];
            for (int slot = 0; slot < ApparelInfo.NUM_SLOTS; slot++) {
                slot_sorted_apparel[slot] = new List<ApparelInfo>();
            }
            for (int gender = 0; gender < GenderInfo.NUM_GENDERS; gender++) {
                gender_sorted_apparel[gender] = new List<ApparelInfo>();
                this.m_preset_outfits[gender] = new ApparelOutfit[(prefabs[gender] == null ? 0 : prefabs[gender].m_CharacterCustom.Presets.Presets.Count)];
                gender_and_slot_sorted_apparel[gender] = new List<ApparelInfo>[ApparelInfo.NUM_SLOTS];
                force_wear_apparel[gender] = new List<ApparelInfo>[ApparelInfo.NUM_SLOTS];
                for (int slot = 0; slot < ApparelInfo.NUM_SLOTS; slot++) {
                    gender_and_slot_sorted_apparel[gender][slot] = new List<ApparelInfo>();
                    force_wear_apparel[gender][slot] = new List<ApparelInfo>();
                }
                for (int index = 0; index < this.m_preset_outfits[gender].Length; index++) {
                    CC_CharacterData data = prefabs[gender].m_CharacterCustom.Presets.Presets[index];
                    GenderInfo.set_gender_prefab_hash(data.CharacterPrefab.GetHashCode(), gender);
                    ApparelOutfit outfit = this.m_preset_outfits[gender][index] = new ApparelOutfit(gender, data);
                    for (int slot = 0; slot < ApparelInfo.NUM_SLOTS; slot++) {
                        ApparelInfo apparel = outfit.Outfit[slot];
                        if (apparel.Name == ApparelInfo.APPAREL_NONE) {
                            continue;
                        }
                        if (!this.m_all_apparel_dict.ContainsKey(apparel.Name)) {
                            this.m_all_apparel_dict[apparel.Name] = apparel;
                        }
                        foreach (List<ApparelInfo> items in new List<ApparelInfo>[] {all_apparel, gender_sorted_apparel[gender], slot_sorted_apparel[apparel.Slot], gender_and_slot_sorted_apparel[gender][apparel.Slot]}) {
                            if (!items.Contains(apparel)) {
                                items.Add(apparel);
                            }
                        }
                        if (force_wear_names.Contains(apparel.Name)) {
                            force_wear_apparel[gender][apparel.Slot].Add(apparel);
                        }
                    }
                }
            }
            this.m_all_apparel = all_apparel.OrderBy(item => item.Name).ToArray();
            this.m_gender_sorted_apparel = new ApparelInfo[GenderInfo.NUM_GENDERS][];
            this.m_slot_sorted_apparel = new ApparelInfo[ApparelInfo.NUM_SLOTS][];
            this.m_gender_and_slot_sorted_apparel = new ApparelInfo[GenderInfo.NUM_GENDERS][][];
            this.m_force_wear_apparel = new ApparelInfo[GenderInfo.NUM_GENDERS][][];
            List<string> lines = new List<string>();
            for (int gender = 0; gender < GenderInfo.NUM_GENDERS; gender++) {
                lines.Add($"\n\n==== {GenderInfo.gender_string(gender)} ====");
                this.m_gender_sorted_apparel[gender] = gender_sorted_apparel[gender].OrderBy(item => item.Name).ToList().ToArray();
                this.m_gender_and_slot_sorted_apparel[gender] = new ApparelInfo[ApparelInfo.NUM_SLOTS][];
                this.m_force_wear_apparel[gender] = new ApparelInfo[ApparelInfo.NUM_SLOTS][];
                for (int slot = 0; slot < ApparelInfo.NUM_SLOTS; slot++) {
                    lines.Add($"\n++ {GenderInfo.gender_string(gender)}: {ApparelInfo.slot_string(slot)} ++\n");
                    this.m_gender_and_slot_sorted_apparel[gender][slot] = gender_and_slot_sorted_apparel[gender][slot].OrderBy(item => item.Name).ToArray();
                    this.m_force_wear_apparel[gender][slot] = force_wear_apparel[gender][slot].OrderBy(item => item.Name).ToArray();
                    foreach (ApparelInfo apparel in this.m_gender_and_slot_sorted_apparel[gender][slot]) {
                        lines.Add($"{apparel.Name}{(force_wear_apparel[gender][slot].Contains(apparel) ? " [** FORCE **]" : "")}");
                    }
                }
            }
            for (int slot = 0; slot < ApparelInfo.NUM_SLOTS; slot++) {
                this.m_slot_sorted_apparel[slot] = slot_sorted_apparel[slot].OrderBy(item => item.Name).ToArray();
            }
            DDPlugin._info_log($"Complete list of apparel names for use in 'Force Wear' setting (indicated with [** FORCE **] if flagged as such in setting):\n{string.Join("\n", lines)}\n");
        } catch (Exception e) {
            DDPlugin._error_log("** CustomMaterialHandler.update_apparel_structs ERROR - " + e);
        }
    }
}