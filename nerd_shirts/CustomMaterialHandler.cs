using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using CC;

public class CustomMaterialHandler : MonoBehaviour {
    private static CustomMaterialHandler m_instance = null;
    public static CustomMaterialHandler Instance {
        get {
            return m_instance;
        }
    }
    private CustomTextureDict m_textures = null;
    
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

}