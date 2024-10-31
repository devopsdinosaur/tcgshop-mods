using System;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class CustomTexture {
    public static readonly CustomTexture PLACEHOLDER = new CustomTexture("__placeholder__", "");
    public string m_key;
    public string m_path;
    private Texture2D m_texture;
    public Texture2D Texture {
        get {
            return this.get_texture();
        }
    }
    private bool m_load_failed;

    public CustomTexture(string key, string path) {
        this.m_key = key;
        this.m_path = path;
        this.m_texture = null;
        this.m_load_failed = false;
    }

    private Texture2D get_texture() {
        try {
            if (this.m_load_failed) {
                return null;
            }
            if (this.m_texture != null) {
                return this.m_texture;
            }
            DDPlugin._debug_log($"CustomTexture.get_texture - loading '{this.m_key}' from file '{this.m_path}'.");
            this.m_texture = new Texture2D(1, 1, GraphicsFormat.R8G8B8A8_UNorm, new TextureCreationFlags());
            this.m_texture.LoadImage(File.ReadAllBytes(this.m_path));
            this.m_texture.filterMode = FilterMode.Point;
            this.m_texture.wrapMode = TextureWrapMode.Clamp;
            this.m_texture.wrapModeU = TextureWrapMode.Clamp;
            this.m_texture.wrapModeV = TextureWrapMode.Clamp;
            this.m_texture.wrapModeW = TextureWrapMode.Clamp;
            this.m_load_failed = false;
            return this.m_texture;
        } catch (Exception e) {
            DDPlugin._error_log("** CustomTexture.get_texture ERROR - " + e);
            DDPlugin._error_log("** NOTE: Texture load failed.  This texture will no longer be considered for replacement during this run or until a manual reload is triggered.");
        }
        this.m_load_failed = true;
        return null;
    }
}