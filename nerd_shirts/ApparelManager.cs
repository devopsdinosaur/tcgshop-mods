using System;
using System.Collections.Generic;
using UnityEngine;
using CC;

public class ApparelManager : MonoBehaviour {
    private static ApparelManager m_instance = null;
    public static ApparelManager Instance {
        get {
            return m_instance;
        }
    }

    public static void initialize() {
        try {
            if (m_instance != null) {
                return;
            }
            m_instance = CGameManager.Instance.gameObject.AddComponent<ApparelManager>();
            CEventManager.AddListener<CEventPlayer_GameDataFinishLoaded>(m_instance.on_game_data_finish_loaded);
        } catch (Exception e) {
            DDPlugin._error_log("** ApparelInfoManager.initialize ERROR - " + e);
        }
    }

    private void on_game_data_finish_loaded(CEventPlayer_GameDataFinishLoaded evt) {
        this.update_apparel_structs();
    }

    private void update_apparel_structs() {
        try {
            Dictionary<int, Customer> prefabs = new Dictionary<int, Customer>() {
                {ApparelInfo.GENDER_FEMALE, CustomerManager.Instance.m_CustomerFemalePrefab},
                {ApparelInfo.GENDER_MALE, CustomerManager.Instance.m_CustomerPrefab}
            };
            foreach (int key in prefabs.Keys) {
                //this.m_apparel_names[key] = new List<string>();
                foreach (CC_CharacterData character_data in prefabs[key].m_CharacterCustom.Presets.Presets) {
                    DDPlugin._debug_log($"{character_data.CharacterName}: {string.Join(", ", character_data.ApparelNames)}");
                    foreach (string name in character_data.ApparelNames) {
                        //if (name != "None" && !this.m_apparel_names[key].Contains(name)) {
                        //    this.m_apparel_names[key].Add(name);
                        //}
                    }
                }
            }
            List<string> lines = new List<string>();
            //foreach (string key in this.m_apparel_names.Keys) {
            //    this.m_apparel_names[key].Sort();
            //    lines.Add($"\n=== {key} ===\n");
            //    foreach (string name in this.m_apparel_names[key]) {
            //        lines.Add(name);
            //    }
            //}
            List<string> words = new List<string>();
            foreach (string _word in Settings.m_apparel_force_wear.Value.Replace(" ", "").Split(',')) {
                string word = _word.Trim();
                if (!string.IsNullOrEmpty(word)) {
                    words.Add(word);
                }
            }
            DDPlugin._info_log($"Complete list of apparel names for use in 'Force Wear' setting:\n{string.Join("\n", lines)}\n");
        } catch (Exception e) {
            DDPlugin._error_log("** CustomMaterialHandler.update_apparel_dict ERROR - " + e);
        }
    }
}