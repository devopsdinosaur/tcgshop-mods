using System;
using System.IO;
using System.Collections.Generic;

class CustomTextureDict {
    private Dictionary<string, CustomTexture> m_textures = new Dictionary<string, CustomTexture>();

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
        }
        return true;
    }
}
