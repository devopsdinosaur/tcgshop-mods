﻿using HarmonyLib;
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
    public CustomTextureDict Textures {
        get {
            return this.m_textures;
        }
    }
    private Dictionary<string, Shader> m_shaders = new Dictionary<string, Shader>();

    private void harmony_patch_CharacterCustomization_setApparel_postfix(CharacterCustomization __instance, int selection, int slot, int materialSelection) {
        if (Settings.m_enabled.Value && TextureDumper.Instance == null) {
            this.m_textures.apply_matching_textures(__instance.transform);
        }
    }

    private bool harmony_patch_CharacterCustomization_setApparelByName(CharacterCustomization __instance, string name, int slot, int materialSelection) {
        //DDPlugin._debug_log($"customer name: {__instance.CharacterName}, apparel name: {name}, slot: {slot}, mat: {materialSelection}");
        return true;
    }

    private class HarmonyPatches {
        [HarmonyPatch(typeof(CharacterCustomization), "setApparel")]
        class HarmonyPatch_CharacterCustomization_setApparel {
            private static void Postfix(CharacterCustomization __instance, int selection, int slot, int materialSelection) {
                CustomMaterialHandler.Instance.harmony_patch_CharacterCustomization_setApparel_postfix(__instance, selection, slot, materialSelection);
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
                foreach (string wildcard in new string[] {"*.png", "*.txt", "*.json"}) {
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
            string shader_bundles_dir = Path.Combine(root_dir, "__shader_bundles__");
            DDPlugin._info_log($"Loading custom shaders from '{shader_bundles_dir}'.");
            foreach (string file_path in Directory.GetFiles(shader_bundles_dir, "*.assetbundle", SearchOption.AllDirectories)) {
                try {
                    DDPlugin._debug_log($"==> file: {file_path}");
                    AssetBundle bundle = AssetBundle.LoadFromFile(file_path);
                    foreach (string key in bundle.GetAllAssetNames()) {
                        if (!key.EndsWith(".mat")) {
                            continue;
                        }
                        string name = Path.GetFileName(key);
                        name = name.Substring(0, name.Length - 4);
                        DDPlugin._debug_log($"   --> asset: {key}, shader_name: {name}");
                        this.m_shaders[name] = bundle.LoadAsset<Material>(key).shader;
                    }
                } catch (Exception e) {
                    DDPlugin._error_log($"** CustomMaterialHandler.reload_textures ERROR - unable to extract shaders from '{file_path}'.  Error: " + e);
                }
            }
            DDPlugin._info_log($"Found {this.m_shaders.Count} total shaders.");
            this.m_textures.load_textures_post_process();
        } catch (Exception e) {
            DDPlugin._error_log("** CustomMaterialHandler.reload_textures ERROR - " + e);
        }
    }

    public Shader get_shader(string name) {
        name = name.ToLower();
        if (this.m_shaders.TryGetValue(name, out Shader shader)) {
            return shader;
        }
        return Shader.Find(name);
    }
}