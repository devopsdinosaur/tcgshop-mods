using HarmonyLib;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using CC;
using UnityEngine.SceneManagement;

class TextureDumper : MonoBehaviour {
    private static TextureDumper m_instance = null;
    public static TextureDumper Instance {
        get {
            return m_instance;
        }
    }
    private string[] m_info_strings = new string[6];
    private string m_dump_root;
    private List<string> m_already_written = new List<string>();

    public static void dump_textures() {
        if (!Settings.m_enabled.Value || !Settings.m_test_enabled.Value || m_instance != null) {
            return;
        }
        m_instance = CGameManager.Instance.gameObject.AddComponent<TextureDumper>();
    }

    private void Awake() {
        this.m_dump_root = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Settings.m_subdir.Value, "__dump__");
        this.StartCoroutine(this.coroutine_dump_textures());
        this.StartCoroutine(this.coroutine_update_info_text());
    }

    private IEnumerator coroutine_dump_textures() {
        
        bool QUIT_WHEN_DONE = true;
        bool DESTROY_CUSTOMERS = true;

        const float START_X = 8.5f;
        const float START_Z = 0f;
        const float INC_X = 1.5f;
        const float INC_Z = -1.5f;
        const int COUNT_PER_COL = 8;

        DDPlugin._info_log("Starting dump_textures coroutine.");
        GameObject temp_customers_parent = new GameObject("Temp_Customers_Parent");
        List<Customer> customers = new List<Customer>();
        float x = START_X;
        float y = 0f;
        float z = START_Z;
        int column_counter = 0;
        this.m_info_strings[0] = "Instantiating all customer presets.";
        this.m_info_strings[3] = "Game will close when completed.  See LogOutput.txt for details.";
        yield return null;
        DDPlugin._info_log("Instantiating all customer presets.");
        foreach (Customer prefab in new Customer[] { CustomerManager.Instance.m_CustomerFemalePrefab, CustomerManager.Instance.m_CustomerPrefab }) {
            foreach (CC_CharacterData character_data in prefab.m_CharacterCustom.Presets.Presets) {
                Customer customer = GameObject.Instantiate(prefab, temp_customers_parent.transform);
                customers.Add(customer);
                customer.gameObject.SetActive(true);
                customer.transform.position = new Vector3(x, y, z);
                z += INC_Z;
                if (column_counter++ == COUNT_PER_COL) {
                    x += INC_X;
                    z = START_Z;
                    column_counter = 0;
                }
                customer.m_CharacterCustom.CharacterName = character_data.CharacterName;
                customer.m_CharacterCustom.Initialize();
                yield return null;
            }
        }
        List<string> dumped_paths = new List<string>();
        int customer_index = 1;
        this.m_info_strings[0] = "Dumping Textures (this will massively lag gfx and take a while)...";
        int file_count = 0;
        foreach (Customer customer in customers) {
            this.m_info_strings[1] = $"Processing Customer Preset: {customer_index} / {customers.Count}";
            this.m_info_strings[2] = $"{file_count} files written to '{m_dump_root}'";
            yield return new WaitForSeconds(0.2f);
            customer_index++;
            string[] names = new string[256];
            int name_index = 0;
            Dictionary<int, string> written_file_hashes = new Dictionary<int, string>();

            string get_path() {
                if (name_index < 3) {
                    return null;
                }
                string path = (names[2][0] == 'F' ? "Female" : "Male");
                for (int index = 3; index < name_index; index++) {
                    path += "/" + names[index];
                }
                return path;
            }

            byte[] get_texture_bytes(Texture2D texture) {
                RenderTexture render_texture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
                Graphics.Blit(texture, render_texture);
                RenderTexture original_render_texture = RenderTexture.active;
                RenderTexture.active = render_texture;
                Texture2D new_texture = new Texture2D(texture.width, texture.height);
                new_texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                new_texture.Apply();
                RenderTexture.active = original_render_texture;
                RenderTexture.ReleaseTemporary(render_texture);
                return new_texture.EncodeToPNG();
            }

            int get_bytes_hash(byte[] bytes) {
                int result = 17;
                foreach (byte b in bytes) {
                    result = result * 31 + b.GetHashCode();
                }
                return result;
            }

            void check_transform(Transform transform) {
                names[name_index++] = transform.name.Replace(" Variant(Clone)", "");
                int renderer_index = 0;
                foreach (SkinnedMeshRenderer renderer in transform.GetComponents<SkinnedMeshRenderer>()) {
                    names[name_index++] = $"renderer_{renderer_index:D3}";
                    int material_index = 0;
                    foreach (Material material in renderer.materials) {
                        names[name_index++] = $"material_{material_index:D3}";
                        foreach (string property_name in material.GetTexturePropertyNames()) {
                            if (!(material.GetTexture(property_name) is Texture2D texture)) {
                                continue;
                            }
                            names[name_index++] = property_name;
                            string path = get_path();
                            if (dumped_paths.Contains(path)) {
                                material_index++;
                                name_index--;
                                continue;
                            }
                            byte[] bytes = get_texture_bytes(texture);
                            int hash = get_bytes_hash(bytes);
                            string file_path = Path.Combine(this.m_dump_root, path);
                            Directory.CreateDirectory(Path.GetDirectoryName(file_path));
                            if (written_file_hashes.TryGetValue(hash, out string existing_path)) {
                                file_path += ".txt";
                                DDPlugin._info_log($"--> hash 0x{hash:X16} exists; linking '{path}' to '{existing_path}'.");
                                File.WriteAllText(file_path, existing_path);
                            } else {
                                file_path += ".png";
                                DDPlugin._info_log($"--> writing {bytes.Length} bytes to '{file_path}'.");
                                File.WriteAllBytes(file_path, bytes);
                                written_file_hashes[hash] = path;
                            }
                            file_count++;
                            dumped_paths.Add(path);
                            name_index--;
                        }
                        material_index++;
                        name_index--;
                    }
                    renderer_index++;
                    name_index--;
                }
                foreach (Transform child in transform) {
                    check_transform(child);
                }
                name_index--;
            }

            check_transform(customer.transform);
            if (DESTROY_CUSTOMERS) {
                GameObject.Destroy(customer.gameObject);
            }
            yield return new WaitForSeconds(0.1f);
        }
        DDPlugin._info_log("dump_textures coroutine completed.");
        GameObject.Destroy(this);
        m_instance = null;
        if (QUIT_WHEN_DONE) {
            Application.Quit();
        }
    }

    private IEnumerator coroutine_update_info_text() {
        Transform offset_from = GameUIScreen.Instance.m_DayText.transform;
        Vector3 offset_direction = Vector3.down;
        float font_size = 18;
        float offset_distance = 50f;
        TextMeshProUGUI info_text = GameObject.Instantiate<TextMeshProUGUI>(GameUIScreen.Instance.m_ShopLevelText, offset_from.parent);
        info_text.transform.localPosition = offset_from.localPosition + offset_direction * offset_distance + Vector3.right * 5f;
        info_text.name = "NerdShirtsPlugin_Dump_Info_Text";
        info_text.alignment = TextAlignmentOptions.TopLeft;
        info_text.enableAutoSizing = true;
        info_text.enableWordWrapping = false;
        info_text.fontSizeMin = info_text.fontSizeMax = font_size;
        info_text.gameObject.SetActive(true);
        foreach (GameObject root_obj in SceneManager.GetActiveScene().GetRootGameObjects()) {
            if (root_obj.name == "Canvas") {
                UnityUtils.enum_descendants(root_obj.transform, delegate(Transform transform) {
                    transform.gameObject.SetActive(transform == root_obj.transform);
                    return true;
                });
                break;
            }
        }
        Transform transform = info_text.transform;
        while (transform != null) {
            transform.gameObject.SetActive(true);
            transform = transform.parent;
        }
        foreach (Transform child in info_text.transform.parent) {
            child.gameObject.SetActive(child == info_text.transform);
        }
        for (;;) {
            info_text.text = string.Join("\n\n", this.m_info_strings);
            yield return new WaitForSeconds(0.1f);
        }
    }

    [HarmonyPatch(typeof(InputManager), "GetKeyDownAction")]
    class HarmonyPatch_InputManager_GetKeyDownAction {
        private static void Postfix(ref bool __result) {
            if (m_instance != null) {
                __result = false;
            }
        }
    }
}
