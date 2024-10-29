using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class CustomTextureDict {
    private Dictionary<string, CustomTexture> m_textures = new Dictionary<string, CustomTexture>();
    public Dictionary<string, CustomTexture> Textures {
        get {
            return this.m_textures;
        }
    }
    private Dictionary<string, string> m_shader_paths = new Dictionary<string, string>();
    public Dictionary<string, string> ShaderPaths {
        get {
            return this.m_shader_paths;
        }
    }
    private Dictionary<string, AddMaterial> m_add_materials = new Dictionary<string, AddMaterial>();
    private Dictionary<int, List<int>> m_already_added_materials = new Dictionary<int, List<int>>();

    public bool add_item(string key, string path) {
        if (this.m_textures.TryGetValue(key, out CustomTexture texture)) {
            DDPlugin._warn_log($"* CustomTextureDict.add_item WARNING - duplicate key detected; only the first will be used (original: '{texture.m_path}', duplicate: '{path}').");
            return false;
        }
        string ext = path.Substring(path.Length - 4).ToLower();
        if (ext == ".png") {
            this.m_textures[key] = new CustomTexture(key, path);
        } else if (ext == ".txt") {
            if (new FileInfo(path).Length > 1024) {
                DDPlugin._info_log($"CustomTextureDict.add_item INFO - '{path}' is too large to be a link token file; ignored.");
                return false;
            }
            try {
                string link_key = File.ReadAllText(path);
                if (!this.m_textures.TryGetValue(link_key, out CustomTexture link_texture)) {
                    DDPlugin._warn_log($"* CustomTextureDict.add_item WARNING - invalid texture link to '{link_key}' in '{path}'; linked texture file is not present in loaded database.");
                    return false;
                }
                this.m_textures[key] = link_texture;
            } catch (Exception e) {
                DDPlugin._info_log($"* CustomTextureDict.add_item WARNING - exception occured when reading '{path}'; ignored.  Exception: " + e);
                return false;
            }
        } else if (Path.GetFileName(path) == "__shader__.json") {
            this.m_shader_paths[key] = path;
        }
        return true;
    }

    public void apply_matching_textures(Transform parent) {
        try {
            foreach (string key in this.m_textures.Keys) {
                if (key.Contains("/__add__/")) {
                    continue;
                }
                string[] names = key.Split('/');
                int name_index = 0;

                Transform check_renderers(Transform transform) {
                    int renderer_index = 0;
                    foreach (SkinnedMeshRenderer renderer in transform.gameObject.GetComponents<SkinnedMeshRenderer>()) {
                        if ($"renderer_{renderer_index++:D3}" != names[name_index]) { 
                            continue;
                        }
                        name_index++;
                        int material_index = 0;
                        foreach (Material material in renderer.materials) {
                            if ($"material_{material_index++:D3}" != names[name_index]) {
                                continue;
                            }
                            name_index++;
                            foreach (string property_name in material.GetTexturePropertyNames()) {
                                if (property_name != names[name_index]) {
                                    continue;
                                }
                                material.SetTexture(property_name, this.m_textures[key].Texture);
                                name_index--;
                                List<string> parallel_keys = new List<string>();
                                string add_root_key = null;
                                for (int index = 0; index < name_index; index++) {
                                    add_root_key = (index == 0 ? names[index] : add_root_key + "/" + names[index]);
                                }
                                add_root_key += "/__add__";
                                foreach (KeyValuePair<string, AddMaterial> kvp in this.m_add_materials) {
                                    if (!kvp.Key.StartsWith(add_root_key) || (this.m_already_added_materials.TryGetValue(renderer.GetHashCode(), out List<int> hashes) && hashes.Contains(kvp.Value.GetHashCode()))) {
                                        continue;
                                    }
                                    kvp.Value.apply(renderer);
                                }
                                return transform;
                            }
                            name_index--;
                        }
                        name_index--;
                    }
                    name_index--;
                    return null;
                }

                Transform check_match(Transform transform) {
                    //DDPlugin._debug_log($"name_index: {name_index}, names[name_index]: {names[name_index]}, transform.name: {transform.name}");
                    if (name_index >= names.Length - 2 || ((name_index == 0 && !transform.name.StartsWith(names[name_index])) || (name_index > 0 && names[name_index] != transform.name.Replace(" Variant(Clone)", "")))) {
                        return null;
                    }
                    if (names[++name_index].StartsWith("renderer_")) {
                        return check_renderers(transform);
                    }
                    foreach (Transform child in transform) {
                        Transform match = check_match(child);
                        if (match != null) {
                            return match;
                        }
                    }
                    name_index--;
                    return null;
                }

                check_match(parent);
            }
        } catch (Exception e) {
            DDPlugin._error_log("** CustomTextureDict.apply_matching_textures ERROR - " + e);
        }
    }

    public Dictionary<string, CustomTexture> get_textures_with_key_prefix(string prefix) {
        Dictionary<string, CustomTexture> result = new Dictionary<string, CustomTexture>();
        foreach (string key in this.m_textures.Keys) {
            if (key.StartsWith(prefix)) {
                result[key] = this.m_textures[key];
            }
        }
        return result;
    }

    public void load_textures_post_process() {
        DDPlugin._info_log($"Post-processing loaded textures to configure custom materials.");
        foreach (string key in this.m_shader_paths.Keys) {
            try {
                string base_key = Path.GetDirectoryName(key).Replace("\\", "/");
                DDPlugin._debug_log(base_key);
                AddMaterial material = AddMaterial.create(
                    this.m_shader_paths[key],
                    base_key,
                    ShaderInfo.parse(this.m_shader_paths[key]),
                    this.get_textures_with_key_prefix(base_key)
                );
                if (material != null) {
                    this.m_add_materials[material.Key] = material;
                }
            } catch (Exception e) {
                DDPlugin._warn_log($"* AddMaterial.create WARNING - an exception occurred while creating Unity material using settings in '{this.m_shader_paths[key]}'; this material will be ignored.\nError: " + e);
            }
        }
        DDPlugin._info_log($"Processed {this.m_add_materials} custom materials.");
    }

}
