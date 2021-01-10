using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemySpellsManager : MonoBehaviour
{
    [Tooltip("List of Spell the player will start with")]
    public List<SpellController> startingSpells = new List<SpellController>();
    [Tooltip("Secondary camera used to avoid seeing spells go throw geometries")]
    public Camera spellCamera;
    [Header("References")]
    [Tooltip("Parent transform where all Spell will be added in the hierarchy")]
    public Transform SpellParentSocket;
    [Tooltip("Position for Spells when active but not actively aiming")]
    public Transform defaultSpellPosition;

    [Header("Spell Recoil")]
    [Tooltip("This will affect how fast the recoil moves the Spell, the bigger the value, the fastest")]
    public float recoilSharpness = 50f;
    [Tooltip("Maximum distance the recoil can affect the Spell")]
    public float maxRecoilDistance = 0.5f;
    [Tooltip("How fast the Spell goes back to it's original position after the recoil is finished")]
    public float recoilRestitutionSharpness = 10f;
    [Tooltip("Layer to set FPS Spell gameObjects to")]
    public LayerMask FPSSpellLayer;

    const int MaxNumSpells = 8;
    SpellController[] m_SpellSlots = new SpellController[MaxNumSpells]; // 8 available Spell slots
    EnemyCharacterController m_EnemyCharacterController;
    Vector3 m_LastCharacterPosition;
    Vector3 m_SpellMainLocalPosition;
    Vector3 m_SpellRecoilLocalPosition;
    Vector3 m_AccumulatedRecoil;


    private void Start()
    {
        m_EnemyCharacterController = GetComponent<EnemyCharacterController>();
        DebugUtility.HandleErrorIfNullGetComponent<EnemyCharacterController, PlayerSpellsManager>(m_EnemyCharacterController, this, gameObject);

        // Add starting Spells
        for (int i=0;i<startingSpells.Count;i++) {
            SwitchToSpellIndex(startingSpells[i], i);
        }
    }

    public bool CastSpell(int spellIndex) {
        print(string.Format("casting spell : {0}", spellIndex));
        SpellController spell = m_SpellSlots[spellIndex];
        return spell.HandleShootInputs(
            true,  // down
            true,  // hold
            true); //release
    }

    public float CastableShots(int spellIndex)
    {
        SpellController spell = m_SpellSlots[spellIndex];
        if (spell.CanShoot()) {
            return spell.GetCurrentAmmo();
        }
        return 0;
    }

    // Update various animated features in LateUpdate because it needs to override the animated arm position
    private void LateUpdate()
    {
        UpdateSpellRecoil();
        // Set final Spell socket position based on all the combined animation influences
        SpellParentSocket.localPosition = m_SpellMainLocalPosition + m_SpellRecoilLocalPosition;
    }

    public SpellController ReadySpell(SpellController SpellPrefab)
    {
        SpellController SpellInstance = Instantiate(SpellPrefab, SpellParentSocket);
        SpellInstance.transform.localPosition = Vector3.zero;
        SpellInstance.transform.localRotation = Quaternion.identity;

        // Set owner to this gameObject so the Spell can alter projectile/damage logic accordingly
        SpellInstance.owner = gameObject;
        SpellInstance.sourcePrefab = SpellPrefab.gameObject;
        SpellInstance.ShowSpell(false);

        // Assign the first person layer to the Spell
        int layerIndex = Mathf.RoundToInt(Mathf.Log(FPSSpellLayer.value, 2)); // This function converts a layermask to a layer index
        foreach (Transform t in SpellInstance.gameObject.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer = layerIndex;
        }
        return SpellInstance;
    }
    // Switches to the given Spell index in Spell slots if the new index is a valid Spell that is different from our current one
    public void SwitchToSpellIndex(SpellController SpellPrefab, int newSpellIndex)
    {
        if (newSpellIndex <= m_SpellSlots.Length && newSpellIndex >= 0)
        {
            SpellController SpellInstance = ReadySpell(SpellPrefab);
            m_SpellSlots[newSpellIndex] = SpellInstance;
        }
    }

    public bool HasSpell(SpellController SpellPrefab)
    {
        // Checks if we already have a Spell coming from the specified prefab
        foreach(var w in m_SpellSlots)
        {
            if(w != null && w.sourcePrefab == SpellPrefab.gameObject)
            {
                return true;
            }
        }

        return false;
    }

    // Updates the Spell recoil animation
    void UpdateSpellRecoil()
    {
        // if the accumulated recoil is further away from the current position, make the current position move towards the recoil target
        if (m_SpellRecoilLocalPosition.z >= m_AccumulatedRecoil.z * 0.99f)
        {
            m_SpellRecoilLocalPosition = Vector3.Lerp(m_SpellRecoilLocalPosition, m_AccumulatedRecoil, recoilSharpness * Time.deltaTime);
        }
        // otherwise, move recoil position to make it recover towards its resting pose
        else
        {
            m_SpellRecoilLocalPosition = Vector3.Lerp(m_SpellRecoilLocalPosition, Vector3.zero, recoilRestitutionSharpness * Time.deltaTime);
            m_AccumulatedRecoil = m_SpellRecoilLocalPosition;
        }
    }

    public SpellController GetSpellAtSlotIndex(int index)
    {
        // find the active Spell in our Spell slots based on our active Spell index
        if(index >= 0 &&
            index < m_SpellSlots.Length)
        {
            return m_SpellSlots[index];
        }

        // if we didn't find a valid active Spell in our Spell slots, return null
        return null;
    }

    void OnSpellSwitched(SpellController newSpell)
    {
        if (newSpell != null)
        {
            newSpell.ShowSpell(true);
        }
    }
}
