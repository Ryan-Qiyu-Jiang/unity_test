using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerSpellsManager : MonoBehaviour
{
    [Tooltip("List of Spell the player will start with")]
    public List<SpellController> startingSpells = new List<SpellController>();
    [Tooltip("Spell cast to interact with objects for E key")]
    public SpellController interactionController;
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

    [Tooltip("Field of view when not aiming")]
    public float defaultFOV = 60f;

    [Tooltip("Delay before switching Spell a second time, to avoid recieving multiple inputs from mouse wheel")]
    public float SpellSwitchDelay = 1f;
    [Tooltip("Layer to set FPS Spell gameObjects to")]
    public LayerMask FPSSpellLayer;

    public bool isPointingAtEnemy { get; private set; }
    public int basicSpellIndex { get; } = 0;
    public int altSpellIndex { get; } = 1;
    public int utilitySpellIndex { get; } = 2;
    public int ultimateSpellIndex { get; } = 3;
    public UnityAction<SpellController> onSwitchedToSpell;

    SpellController[] m_SpellSlots = new SpellController[4]; // 9 available Spell slots
    PlayerInputHandler m_InputHandler;
    PlayerCharacterController m_PlayerCharacterController;
    Vector3 m_LastCharacterPosition;
    Vector3 m_SpellMainLocalPosition;
    Vector3 m_SpellRecoilLocalPosition;
    Vector3 m_AccumulatedRecoil;


    private void Start()
    {
        m_InputHandler = GetComponent<PlayerInputHandler>();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerInputHandler, PlayerSpellsManager>(m_InputHandler, this, gameObject);

        m_PlayerCharacterController = GetComponent<PlayerCharacterController>();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerCharacterController, PlayerSpellsManager>(m_PlayerCharacterController, this, gameObject);

        onSwitchedToSpell += OnSpellSwitched;

        // Add starting Spells
        for (int i=0;i<4;i++) {
            SwitchToSpellIndex(startingSpells[i], i);
        }
        interactionController = ReadySpell(interactionController);
    }


    private void Update()
    {
        // handle shooting
        SpellController basicSpell = m_SpellSlots[basicSpellIndex];
        basicSpell.HandleShootInputs(
            m_InputHandler.GetFireInputDown(),
            m_InputHandler.GetFireInputHeld(),
            m_InputHandler.GetFireInputReleased());
        
        SpellController altSpell = m_SpellSlots[altSpellIndex];
        altSpell.HandleShootInputs(
            m_InputHandler.GetAltInputDown(),
            m_InputHandler.GetAltInputHeld(),
            m_InputHandler.GetAltInputReleased());

        SpellController utilitySpell = m_SpellSlots[utilitySpellIndex];
        utilitySpell.HandleShootInputs(
            m_InputHandler.GetUtilityInputDown(),
            m_InputHandler.GetUtilityInputHeld(),
            m_InputHandler.GetUtilityInputReleased());

        SpellController ultimateSpell = m_SpellSlots[ultimateSpellIndex];
        ultimateSpell.HandleShootInputs(
            m_InputHandler.GetUltimateInputDown(),
            m_InputHandler.GetUltimateInputHeld(),
            m_InputHandler.GetUltimateInputReleased());

        SpellController interactSpell = interactionController;
        interactSpell.HandleShootInputs(
            m_InputHandler.GetInteractInputDown(),
            m_InputHandler.GetInteractInputHeld(),
            m_InputHandler.GetInteractInputReleased());

        // Pointing at enemy handling
        // isPointingAtEnemy = false;
        // if(Physics.Raycast(SpellCamera.transform.position, SpellCamera.transform.forward, out RaycastHit hit, 1000, -1, QueryTriggerInteraction.Ignore))
        // {
        //     if(hit.collider.GetComponentInParent<EnemyController>())
        //     {
        //         isPointingAtEnemy = true;
        //     }
        // }
    }


    // Update various animated features in LateUpdate because it needs to override the animated arm position
    private void LateUpdate()
    {
        UpdateSpellRecoil();
        // UpdateSpellSwitching();

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
        if (newSpellIndex <= 4 && newSpellIndex >= 0)
        {
            SpellController SpellInstance = ReadySpell(SpellPrefab);
            m_SpellSlots[newSpellIndex] = SpellInstance;
            if (onSwitchedToSpell != null)
            {
                onSwitchedToSpell.Invoke(SpellInstance);
            }
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

    // // Updates the animated transition of switching Spells
    // void UpdateSpellSwitching()
    // {
    //     // Calculate the time ratio (0 to 1) since Spell switch was triggered
    //     float switchingTimeFactor = 0f;
    //     if (SpellSwitchDelay == 0f)
    //     {
    //         switchingTimeFactor = 1f;
    //     }
    //     else
    //     {
    //         switchingTimeFactor = Mathf.Clamp01((Time.time - m_TimeStartedSpellSwitch) / SpellSwitchDelay);
    //     }

    //     // Handle transiting to new switch state
    //     if(switchingTimeFactor >= 1f)
    //     {
    //         if (m_SpellSwitchState == SpellSwitchState.PutDownPrevious)
    //         {
    //             // Deactivate old Spell
    //             SpellController oldSpell = GetSpellAtSlotIndex(activeSpellIndex);
    //             if (oldSpell != null)
    //             {
    //                 oldSpell.ShowSpell(false);
    //             }

    //             activeSpellIndex = m_SpellSwitchNewSpellIndex;
    //             switchingTimeFactor = 0f;

    //             // Activate new Spell
    //             SpellController newSpell = GetSpellAtSlotIndex(activeSpellIndex);
    //             if (onSwitchedToSpell != null)
    //             {
    //                 onSwitchedToSpell.Invoke(newSpell);
    //             }

    //             if(newSpell)
    //             {
    //                 m_TimeStartedSpellSwitch = Time.time;
    //                 m_SpellSwitchState = SpellSwitchState.PutUpNew;
    //             }
    //             else
    //             {
    //                 // if new Spell is null, don't follow through with putting Spell back up
    //                 m_SpellSwitchState = SpellSwitchState.Down;
    //             }
    //         }
    //         else if (m_SpellSwitchState == SpellSwitchState.PutUpNew)
    //         {
    //             m_SpellSwitchState = SpellSwitchState.Up;
    //         }
    //     }

    //     // Handle moving the Spell socket position for the animated Spell switching
    //     if (m_SpellSwitchState == SpellSwitchState.PutDownPrevious)
    //     {
    //         m_SpellMainLocalPosition = Vector3.Lerp(defaultSpellPosition.localPosition, defaultSpellPosition.localPosition, switchingTimeFactor);
    //     }
    //     else if (m_SpellSwitchState == SpellSwitchState.PutUpNew)
    //     {
    //         m_SpellMainLocalPosition = Vector3.Lerp(defaultSpellPosition.localPosition, defaultSpellPosition.localPosition, switchingTimeFactor);
    //     }
    // }

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
