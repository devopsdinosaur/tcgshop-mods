using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public class ShaderInfo {
    [JsonProperty("shader", Required = Required.Always)]
    public string shader;
    public class TextureInfo {
        [JsonProperty("path", Required = Required.Always)]
        public string path;
        public class Vector2Info {
            [JsonProperty("x", Required = Required.Always)]
            public float x;
            [JsonProperty("y", Required = Required.Always)]
            public float y;
        }
        [JsonProperty("scale", Required = Required.Always)]
        public Vector2Info scale;
        [JsonProperty("offset", Required = Required.Always)]
        public Vector2Info offset;
    }
    [JsonProperty("textures", Required = Required.Always)]
    public Dictionary<string, TextureInfo> textures;
    [JsonProperty("ints", Required = Required.Always)]
    public Dictionary<string, int> ints;
    [JsonProperty("floats", Required = Required.Always)]
    public Dictionary<string, float> floats;
    public class ColorInfo {
        [JsonProperty("r", Required = Required.Always)]
        public float r;
        [JsonProperty("g", Required = Required.Always)]
        public float g;
        [JsonProperty("b", Required = Required.Always)]
        public float b;
        [JsonProperty("a", Required = Required.Always)]
        public float a;
    }
    [JsonProperty("colors", Required = Required.Always)]
    public Dictionary<string, ColorInfo> colors;

    public static ShaderInfo parse(string path) {
        try {
            return JsonConvert.DeserializeObject<ShaderInfo>(File.ReadAllText(path));
        } catch (Exception e) {
            DDPlugin._warn_log($"** ShaderInfo.parse ERROR - JSON error occurred while parsing '{path}'; this material will be ignored.\nError - " + e);
        }
        return null;
    }
}