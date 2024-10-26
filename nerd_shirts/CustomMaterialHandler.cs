﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using CC;

public class CustomMaterialHandler : MonoBehaviour {

    private class PatchedKeyList {
        public struct PatchedKey {
            public int m_instance_id;
            public string m_name;
        }
        private List<PatchedKey> m_patched_keys = new List<PatchedKey>();

        public void add_patch(int instance_id, string name) {
            this.m_patched_keys.Add(new PatchedKey() {
                m_instance_id = instance_id,
                m_name = name
            });
        }

        public bool is_patched(int instance_id, string name) {
            foreach (PatchedKey key in this.m_patched_keys) {
                if (key.m_instance_id == instance_id && key.m_name == name) {
                    return true;
                }
            }
            return false;
        }

        public void reset() {
            this.m_patched_keys.Clear();
        }
    }

    private static CustomMaterialHandler m_instance = null;
    public static CustomMaterialHandler Instance {
        get {
            return m_instance;
        }
    }
    private CustomTextureDict m_textures = null;
    private Dictionary<string, List<string>> m_apparel_names = null;
    private string[] m_force_wear = null;
    private PatchedKeyList m_patched_keys = new PatchedKeyList();

    private bool harmony_patch_CharacterCustomization_setApparel(CharacterCustomization __instance, int selection, int slot, int materialSelection) {
        return true;
    }

    private bool harmony_patch_CharacterCustomization_setApparelByName(CharacterCustomization __instance, string name, int slot, int materialSelection) {
        try {
            if (!Settings.m_enabled.Value || TextureDumper.Instance != null) {
                return true;
            }
            
            return true;
        } catch (Exception e) {
            DDPlugin._error_log("** apparel_ensure_patched_texture_list ERROR - " + e);
        }
        return true;
    }

    private class HarmonyPatches {
        [HarmonyPatch(typeof(CharacterCustomization), "ApplyCharacterVars")]
        class HarmonyPatch_CharacterCustomization_ApplyCharacterVars {
            private static bool Prefix(CharacterCustomization __instance, CC_CharacterData characterData) {
                // Polo_Shirt_01, Cargo_Pants, Boots_01, None
                // 1, 0, 0, 0
                if (!characterData.CharacterPrefab.StartsWith("Male")) {
                    return true;
                }
                //characterData.ApparelNames = new List<string>() {"Polo_Shirt_01", "Cargo_Pants", "Boots_01", "None"};
                //characterData.ApparelMaterials = new List<int>() {1, 0, 0, 0};
                //DDPlugin._info_log(characterData.CharacterPrefab);
                //DDPlugin._info_log(String.Join(", ", characterData.ApparelNames));
                //DDPlugin._info_log(String.Join(", ", characterData.ApparelMaterials));
                return true;
            }
        }

        [HarmonyPatch(typeof(CharacterCustomization), "setApparel")]
        class HarmonyPatch_CharacterCustomization_setApparel {
            private static bool Prefix(CharacterCustomization __instance, int selection, int slot, int materialSelection) {
                return CustomMaterialHandler.Instance.harmony_patch_CharacterCustomization_setApparel(__instance, selection, slot, materialSelection);
            }
        }

        [HarmonyPatch(typeof(CharacterCustomization), "setApparelByName")]
        class HarmonyPatch_CharacterCustomization_setApparelByName {
            private static bool Prefix(CharacterCustomization __instance, string name, int slot, int materialSelection) {
                return CustomMaterialHandler.Instance.harmony_patch_CharacterCustomization_setApparelByName(__instance, name, slot, materialSelection);
            }
        }
    }

    public static void initialize() {
        try {
            if (m_instance != null) {
                return;
            }
            m_instance = CGameManager.Instance.gameObject.AddComponent<CustomMaterialHandler>();
            CEventManager.AddListener<CEventPlayer_GameDataFinishLoaded>(m_instance.on_game_data_finish_loaded);
            m_instance.reload_textures();
            m_instance.m_force_wear = Settings.m_apparel_force_wear.Value.Replace(" ", "").Split(',');
        }  catch (Exception e) {
            DDPlugin._error_log("** CustomMaterialHandler.initialize ERROR - " + e);
        }
    }

    private string key_from_path(string path, string base_path) {
        string key = path.Replace("\\", "/").Substring((base_path == null ? 0 : base_path.Length)).Trim('/');
        for (int index = key.Length - 1; index >= 0; index--) {
            if (key[index] == '/') {
                return key;
            }
            if (key[index] == '.') {
                return key.Substring(0, index);
            }
        }
        return key;
    }

    private void on_game_data_finish_loaded(CEventPlayer_GameDataFinishLoaded evt) {
        this.update_apparel_dict();
    }

    public void reload_textures() {
        try {
            string root_dir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Settings.m_subdir.Value);
            DDPlugin._info_log($"Assembling custom texture information from directory, '{root_dir}'.");
            if (!Directory.Exists(root_dir)) {
                DDPlugin._info_log("Directory does not exist; creating.");
                Directory.CreateDirectory(root_dir);
            }
            this.m_textures = new CustomTextureDict();
            this.m_patched_keys.reset();
            int total_counter = 0;
            foreach (string mod_dir in Directory.GetDirectories(root_dir)) {
                string mod_name = Path.GetFileName(mod_dir);
                if (mod_name[0] == '_' || mod_name[0] == '.') {
                    continue;
                }
                int local_counter = 0;
                foreach (string wildcard in new string[] { "*.png", "*.txt" }) {
                    foreach (string file_path in Directory.GetFiles(mod_dir, wildcard, SearchOption.AllDirectories)) {
                        string key = this.key_from_path(file_path, mod_dir);
                        DDPlugin._debug_log($"==> key: '{key}', path: '{file_path}'.");
                        if (this.m_textures.add_item(key, file_path)) {
                            local_counter++;
                        }
                    }
                }
                DDPlugin._info_log($"--> {mod_name}: {local_counter} textures.");
                total_counter += local_counter;
            }
            DDPlugin._info_log($"Found {total_counter} total textures.");
        } catch (Exception e) {
            DDPlugin._error_log("** CustomMaterialHandler.reload_textures ERROR - " + e);
        }
    }

    private void update_apparel_dict() {
        try {
            if (this.m_apparel_names != null) {
                return;
            }
            this.m_apparel_names = new Dictionary<string, List<string>>();
            Dictionary<string, Customer> prefabs = new Dictionary<string, Customer>() {
                {"Female", CustomerManager.Instance.m_CustomerFemalePrefab},
                {"Male", CustomerManager.Instance.m_CustomerPrefab}
            };
            foreach (string key in prefabs.Keys) {
                this.m_apparel_names[key] = new List<string>();
                foreach (CC_CharacterData character_data in prefabs[key].m_CharacterCustom.Presets.Presets) {
                    foreach (string name in character_data.ApparelNames) {
                        if (name != "None" && !this.m_apparel_names[key].Contains(name)) {
                            this.m_apparel_names[key].Add(name);
                        }
                    }
                }
            }
            List<string> lines = new List<string>();
            foreach (string key in this.m_apparel_names.Keys) {
                this.m_apparel_names[key].Sort();
                lines.Add($"\n=== {key} ===\n");
                foreach (string name in this.m_apparel_names[key]) {
                    lines.Add(name);
                }
            }
            foreach (string name in this.m_force_wear) {
                if (!this.m_apparel_names["Female"].Contains(name) && !this.m_apparel_names["Male"].Contains(name)) {
                    DDPlugin._warn_log($"* CustomMaterialHandler.update_apparel_dict WARNING - '{name}' is not a valid apparel label; see list above.");
                }
            }
            DDPlugin._info_log($"Complete list of apparel names for use in 'Force Wear' setting:\n{string.Join("\n", lines)}\n");
        } catch (Exception e) {
            DDPlugin._error_log("** CustomMaterialHandler.update_apparel_dict ERROR - " + e);
        }
    }
}