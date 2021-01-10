using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ProjectileBase))]
public class StealthSpell : MonoBehaviour
{
    [Tooltip("Duration of stealth")]
    public float duration = 5f;
    // Start is called before the first frame update
    ProjectileBase m_ProjectileBase;
    GameObject m_CharacterModel;
    float m_TimeStarted = 0;

    private void OnEnable()
    {
        m_ProjectileBase = GetComponent<ProjectileBase>();
        DebugUtility.HandleErrorIfNullGetComponent<ProjectileBase, StealthSpell>(m_ProjectileBase, this, gameObject);

        m_ProjectileBase.onShoot += OnShoot;
    }

    void OnShoot()
    {
        m_CharacterModel = m_ProjectileBase.owner.transform.Find("GFX").gameObject;
        if (m_CharacterModel != null) {
            m_CharacterModel.SetActive(false);
            m_TimeStarted = Time.time;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time - m_TimeStarted > duration) {
            m_CharacterModel.SetActive(true);
            Destroy(gameObject, 0);
        }
    }
}
