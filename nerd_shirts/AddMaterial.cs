using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AddMaterial {
    public class MaterialTexture {
        private AddMaterial m_parent;
        private string m_name;
        public string Name {
            get {
                return this.m_name;
            }
        }
        private List<CustomTexture> m_textures = new List<CustomTexture>();
        private Vector2 m_scale;
        private Vector2 m_offset;
        private int m_selected_texture_index = 0;
        private Dictionary<int, int> m_selected_indeces = new Dictionary<int, int>();
        public Dictionary<int, int> SelectedIndeces {
            get {
                return this.m_selected_indeces;
            }
        }

        public static Dictionary<string, MaterialTexture> create_dict(AddMaterial parent, Dictionary<string, ShaderInfo.TextureInfo> info) { 
            Dictionary<string, MaterialTexture> textures = new Dictionary<string, MaterialTexture>();
            foreach (var kvp in info) {
                if (string.IsNullOrEmpty(kvp.Value.path)) {
                    continue;
                }
                MaterialTexture texture = new MaterialTexture();
                texture.m_parent = parent;
                string path_key = parent.m_trunk_key + "/" + kvp.Value.path;
                foreach (string item_key in parent.m_child_items.Keys) {
                    if (item_key.StartsWith(path_key)) {
                        texture.m_textures.Add(parent.m_child_items[item_key]);
                    }
                }
                if (texture.m_textures.Count == 0) {
                    DDPlugin._warn_log($"* AddMaterial.create WARNING - no texture files were found in '{kvp.Key}' texture path key '{path_key}' (file: {parent.m_file_path}).");
                }
                texture.m_name = kvp.Key;
                texture.m_scale = new Vector2(kvp.Value.scale.x, kvp.Value.scale.y);
                texture.m_offset = new Vector2(kvp.Value.offset.x, kvp.Value.offset.y);
                textures[texture.m_name] = texture;
                DDPlugin._debug_log($"AddMaterial.Texture - material: {parent.Key}, texture: {texture.m_name}, file_count: {texture.m_textures.Count}, scale: {texture.m_scale}, offset: {texture.m_offset}");
            }
            return textures;
        }

        public void apply(Renderer renderer, Dictionary<string, int> selected_indeces) {
            if (this.m_selected_indeces.ContainsKey(renderer.GetHashCode())) {
                return;
            }
            DDPlugin._debug_log($"AddMaterial.Texture.apply - material: {this.m_parent.m_name}, texture: {this.m_name}, renderer: {renderer.name}, key: {this.m_parent.m_key}");
            if (this.m_textures.Count > 1) {
                if (!selected_indeces.TryGetValue(this.m_name, out this.m_selected_texture_index)) {
                    this.m_selected_texture_index = UnityEngine.Random.Range(0, this.m_textures.Count);
                    DDPlugin._debug_log($"Using random index: {this.m_selected_texture_index}");
                } else {
                    DDPlugin._debug_log($"Using same index as others in LOD group: {this.m_selected_texture_index}");
                }
            }
            this.m_selected_indeces[renderer.GetHashCode()] = this.m_selected_texture_index;
            this.m_parent.m_material.SetTexture(this.m_name, this.m_textures[this.m_selected_texture_index].Texture);
            this.m_parent.m_material.SetTextureScale(this.m_name, this.m_scale);
            this.m_parent.m_material.SetTextureOffset(this.m_name, this.m_offset);
            Material[] new_materials = new Material[renderer.materials.Length + 1];
            for (int index = 0; index < renderer.materials.Length; index++) {
                new_materials[index] = renderer.materials[index];
            }
            new_materials[new_materials.Length - 1] = this.m_parent.m_material;
            renderer.materials = new_materials;
        }
    }
    private string m_name;
    private string m_file_path;
    private ShaderInfo m_shader_info;
    private Dictionary<string, CustomTexture> m_child_items;
    private string m_key;
    public string Key {
        get {
            return this.m_key;
        }
    }
    private string m_trunk_key;
    private Shader m_shader;
    private Material m_material;
    private Dictionary<string, MaterialTexture> m_textures;

    public static AddMaterial create(string file_path, string key, ShaderInfo info, Dictionary<string, CustomTexture> child_items) {
        if (info == null) {
            return null;
        }
        AddMaterial material = new AddMaterial();
        material.m_file_path = file_path;
        material.m_trunk_key = key;
        material.m_key = key.Replace("/__add__/", "/");
        material.m_name = Path.GetFileName(key);
        material.m_shader_info = info;
        material.m_child_items = child_items;
        if ((material.m_shader = CustomMaterialHandler.Instance.get_shader(material.m_shader_info.shader)) == null) {
            DDPlugin._warn_log($"* AddMaterial.create WARNING - unable to find shader named '{material.m_shader_info.shader}' from game or asset bundles; this material will be ignored (file: {material.m_file_path}).");
            return null;
        }
        if ((material.m_textures = MaterialTexture.create_dict(material, material.m_shader_info.textures)) == null) {
            return null;
        }
        material.m_material = new Material(material.m_shader);
        material.m_material.name = material.m_name;
        material.m_material.renderQueue = (material.m_shader_info.renderQueue == -1 ? material.m_shader.renderQueue : material.m_shader_info.renderQueue);
        foreach (KeyValuePair<string, int> kvp in info.ints) {
            material.m_material.SetInt(kvp.Key, kvp.Value);
        }
        foreach (KeyValuePair<string, float> kvp in info.floats) {
            material.m_material.SetFloat(kvp.Key, kvp.Value);
        }
        foreach (KeyValuePair<string, ShaderInfo.ColorInfo> kvp in info.colors) {
            material.m_material.SetColor(kvp.Key, new Color(kvp.Value.r, kvp.Value.g, kvp.Value.b, kvp.Value.a));
        }
        return material;
    }

    public void apply(Renderer renderer, string renderer_key) {
        // There is definitely a better way to handle this on the front-end, but currently need
        // to get the other renderers in an LOD group to ensure we're not using a different
        // randomly selected texture with different LODs in the same group, i.e. decals would
        // change when getting closer to an object.
        string[] names = renderer_key.Split('/');
        string transform_name = names[names.Length - 2];
        int renderer_index = int.Parse(names[names.Length - 1].Substring(9));
        //DDPlugin._debug_log($"transform: {transform_name}, renderer_index: {renderer_index}");
        string[] transform_name_words = transform_name.Split('_');
        Dictionary<string, int> selected_indeces = new Dictionary<string, int>();
        if (transform_name_words.Length > 1 && transform_name_words[transform_name_words.Length - 1].StartsWith("LOD")) {
            foreach (Transform child in renderer.transform.parent) {
                if (child == renderer.transform) {
                    continue;
                }
                //DDPlugin._debug_log(child.name);
                int check_index = 0;
                foreach (Renderer lod_renderer in child.gameObject.GetComponents<Renderer>()) {
                    if (check_index++ == renderer_index) {
                        //DDPlugin._debug_log(lod_renderer.name);
                        foreach (Material material in lod_renderer.materials) {
                            //DDPlugin._debug_log(material.name);
                            if (material.name != this.m_name + " (Instance)") {
                                continue;
                            }
                            foreach (AddMaterial add_material in CustomMaterialHandler.Instance.Textures.AddMaterials.Values) {
                                //DDPlugin._debug_log($"{add_material.m_name} - {this.m_name}");
                                if (add_material.m_name != this.m_name) {
                                    continue;
                                }
                                //DDPlugin._debug_log("=================== " + add_material.m_name);
                                foreach (MaterialTexture texture in add_material.m_textures.Values) {
                                    //DDPlugin._debug_log(texture.Name + " " + string.Join(", ", texture.SelectedIndeces.Keys));
                                    if (texture.SelectedIndeces.TryGetValue(lod_renderer.GetHashCode(), out int selected_index)) {
                                        //DDPlugin._debug_log($"selected_indeces[{texture.Name}] = {selected_index}");
                                        selected_indeces[texture.Name] = selected_index;
                                    }
                                }
                            }
                            break;
                        }
                        break;
                    }
                }
            }
        }
        foreach (MaterialTexture texture in this.m_textures.Values) {
            texture.apply(renderer, selected_indeces);
        }
    }

    public static AddMaterial clone(AddMaterial other, string key) {
        if (other == null) {
            return null;
        }
        AddMaterial material = new AddMaterial();
        material.m_name = other.m_name;
        material.m_file_path = other.m_file_path;
        material.m_shader_info = other.m_shader_info;
        material.m_child_items = other.m_child_items;
        material.m_trunk_key = key;
        material.m_key = key.Replace("/__add__/", "/");
        material.m_shader = other.m_shader;
        material.m_material = other.m_material;
        material.m_textures = other.m_textures;
        DDPlugin._debug_log($"AddMaterial.clone - src_key: '{other.m_key}', dst_key: '{material.m_key}'");
        return material;
    }
}