using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AddMaterial {
    public class MaterialTexture {
        private string m_name;
        private List<CustomTexture> m_textures = new List<CustomTexture>();
        private Vector2 m_scale;
        private Vector2 m_offset;

        public static Dictionary<string, MaterialTexture> create_dict(AddMaterial parent, Dictionary<string, ShaderInfo.TextureInfo> info) { 
            Dictionary<string, MaterialTexture> textures = new Dictionary<string, MaterialTexture>();
            foreach (var kvp in info) {
                if (string.IsNullOrEmpty(kvp.Value.path)) {
                    continue;
                }
                MaterialTexture texture = new MaterialTexture();
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
                DDPlugin._debug_log($"AddMaterial.Texture - material: {parent.m_name}, texture: {texture.m_name}, file_count: {texture.m_textures.Count}, scale: {texture.m_scale}, offset: {texture.m_offset}");
            }
            return textures;
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
        if ((material.m_shader = Shader.Find(material.m_shader_info.shader)) == null) {
            DDPlugin._warn_log($"* AddMaterial.create WARNING - unable to find Unity shader named '{material.m_shader_info.shader}'; this material will be ignored (file: {material.m_file_path}).");
            return null;
        }
        if ((material.m_textures = MaterialTexture.create_dict(material, material.m_shader_info.textures)) == null) {
            return null;
        }
        material.m_material = new Material(material.m_shader);
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

    public void apply(Renderer renderer) {
        jhjdkjshdfjkhdf
    }
}