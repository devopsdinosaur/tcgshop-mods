using System;
using System.Collections.Generic;
using UnityEngine;
using CC;
using System.Collections;

class TextureTester : MonoBehaviour {
    private static TextureTester m_instance = null;
    public static TextureTester Instance {
        get {
            return m_instance;
        }
    }
    private GameObject m_parent = null;
    private Coroutine m_spawn_routine = null;

    private void Awake() {
        m_instance = this;
        this.m_parent = new GameObject("NerdShirts_TextureTester_Parent");
    }

    public IEnumerator coroutine_spawn_all_presets() {
        const float START_X = 8.5f;
        const float START_Z = 0f;
        const float INC_X = 1.5f;
        const float INC_Z = -1.5f;
        const int COUNT_PER_COL = 8;

        float x = START_X;
        float y = 0f;
        float z = START_Z;
        int column_counter = 0;
        foreach (Customer prefab in new Customer[] { CustomerManager.Instance.m_CustomerFemalePrefab, CustomerManager.Instance.m_CustomerPrefab }) {
            foreach (CC_CharacterData character_data in prefab.m_CharacterCustom.Presets.Presets) {
                Customer customer = GameObject.Instantiate(prefab, this.m_parent.transform);
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
        this.m_spawn_routine = null;
    }

    public void destroy_all_spawns() {
        foreach (Transform transform in this.m_parent.transform) {
            GameObject.Destroy(transform);
        }
    }
}