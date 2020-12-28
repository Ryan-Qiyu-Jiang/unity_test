using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerSpellsManager : MonoBehaviour
{
    public enum SpellSwitchState
    {
        Up,
        Down,
        PutDownPrevious,
        PutUpNew,
    }

    [Tooltip("List of Spell the player will start with")]
    public List<SpellController> startingSpells = new List<SpellController>();

    [Header("References")]
    [Tooltip("Secondary camera used to avoid seeing Spell go throw geometries")]
    public Camera SpellCamera;
    [Tooltip("Parent transform where all Spell will be added in the hierarchy")]
    public Transform SpellParentSocket;
    [Tooltip("Position for Spells when active but not actively aiming")]
    public Transform defaultSpellPosition;
    [Tooltip("Position for Spells when aiming")]
    public Transform aimingSpellPosition;
    [Tooltip("Position for innactive Spells")]
    public Transform downSpellPosition;

    [Header("Spell Bob")]
    [Tooltip("Frequency at which the Spell will move around in the screen when the player is in movement")]
    public float bobFrequency = 10f;
    [Tooltip("How fast the Spell bob is applied, the bigger value the fastest")]
    public float bobSharpness = 10f;
    [Tooltip("Distance the Spell bobs when not aiming")]
    public float defaultBobAmount = 0.05f;
    [Tooltip("Distance the Spell bobs when aiming")]
    public float aimingBobAmount = 0.02f;

    [Header("Spell Recoil")]
    [Tooltip("This will affect how fast the recoil moves the Spell, the bigger the value, the fastest")]
    public float recoilSharpness = 50f;
    [Tooltip("Maximum distance the recoil can affect the Spell")]
    public float maxRecoilDistance = 0.5f;
    [Tooltip("How fast the Spell goes back to it's original position after the recoil is finished")]
    public float recoilRestitutionSharpness = 10f;

    [Header("Misc")]
    [Tooltip("Speed at which the aiming animatoin is played")]
    public float aimingAnimationSpeed = 10f;
    [Tooltip("Field of view when not aiming")]
    public float defaultFOV = 60f;
    [Tooltip("Portion of the regular FOV to apply to the Spell camera")]
    public float SpellFOVMultiplier = 1f;
    [Tooltip("Delay before switching Spell a second time, to avoid recieving multiple inputs from mouse wheel")]
    public float SpellSwitchDelay = 1f;
    [Tooltip("Layer to set FPS Spell gameObjects to")]
    public LayerMask FPSSpellLayer;

    public bool isAiming { get; private set; }
    public bool isPointingAtEnemy { get; private set; }
    public int activeSpellIndex { get; private set; }

    public UnityAction<SpellController> onSwitchedToSpell;
    public UnityAction<SpellController, int> onAddedSpell;
    public UnityAction<SpellController, int> onRemovedSpell;

    SpellController[] m_SpellSlots = new SpellController[9]; // 9 available Spell slots
    PlayerInputHandler m_InputHandler;
    PlayerCharacterController m_PlayerCharacterController;
    float m_SpellBobFactor;
    Vector3 m_LastCharacterPosition;
    Vector3 m_SpellMainLocalPosition;
    Vector3 m_SpellBobLocalPosition;
    Vector3 m_SpellRecoilLocalPosition;
    Vector3 m_AccumulatedRecoil;
    float m_TimeStartedSpellSwitch;
    SpellSwitchState m_SpellSwitchState;
    int m_SpellSwitchNewSpellIndex;

    private void Start()
    {
        activeSpellIndex = -1;
        m_SpellSwitchState = SpellSwitchState.Down;

        m_InputHandler = GetComponent<PlayerInputHandler>();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerInputHandler, PlayerSpellsManager>(m_InputHandler, this, gameObject);

        m_PlayerCharacterController = GetComponent<PlayerCharacterController>();
        DebugUtility.HandleErrorIfNullGetComponent<PlayerCharacterController, PlayerSpellsManager>(m_PlayerCharacterController, this, gameObject);

        onSwitchedToSpell += OnSpellSwitched;

        // Add starting Spells
        foreach (var Spell in startingSpells)
        {
            AddSpell(Spell);
        }
        SwitchSpell(true);
    }

    private void Update()
    {
        // shoot handling
        SpellController activeSpell = GetActiveSpell();
        print(string.Format("spell states: active:{0} m_spell:{1} state_up:{2} is_up:{3} Spell_index:{4}", activeSpell, 
                            m_SpellSwitchState, SpellSwitchState.Up, m_SpellSwitchState == SpellSwitchState.Up, activeSpellIndex));
        
        if (activeSpell && m_SpellSwitchState == SpellSwitchState.Up)
        {
            // handle aiming down sights
            isAiming = m_InputHandler.GetAimInputHeld();

            // handle shooting
            bool hasFired = activeSpell.HandleShootInputs(
                m_InputHandler.GetFireInputDown(),
                m_InputHandler.GetFireInputHeld(),
                m_InputHandler.GetFireInputReleased());

            // Handle accumulating recoil
            if (hasFired)
            {
                m_AccumulatedRecoil += Vector3.back * activeSpell.recoilForce;
                m_AccumulatedRecoil = Vector3.ClampMagnitude(m_AccumulatedRecoil, maxRecoilDistance);
            }
        }

        // Spell switch handling
        if (!isAiming &&
            (activeSpell == null || !activeSpell.isCharging) &&
            (m_SpellSwitchState == SpellSwitchState.Up || m_SpellSwitchState == SpellSwitchState.Down))
        {
            int switchSpellInput = 0;
            if (switchSpellInput != 0)
            {
                bool switchUp = switchSpellInput > 0;
                SwitchSpell(switchUp);
            }
            else
            {
                switchSpellInput = 0;
                if (switchSpellInput != 0)
                {
                    if (GetSpellAtSlotIndex(switchSpellInput - 1) != null)
                        SwitchToSpellIndex(switchSpellInput - 1);
                }
            }
        }

        // Pointing at enemy handling
        // isPointingAtEnemy = false;
        // if (activeSpell)
        // {
        //     if(Physics.Raycast(SpellCamera.transform.position, SpellCamera.transform.forward, out RaycastHit hit, 1000, -1, QueryTriggerInteraction.Ignore))
        //     {
        //         if(hit.collider.GetComponentInParent<EnemyController>())
        //         {
        //             isPointingAtEnemy = true;
        //         }
        //     }
        // }
    }


    // Update various animated features in LateUpdate because it needs to override the animated arm position
    private void LateUpdate()
    {
        UpdateSpellAiming();
        UpdateSpellBob();
        UpdateSpellRecoil();
        UpdateSpellSwitching();

        // Set final Spell socket position based on all the combined animation influences
        SpellParentSocket.localPosition = m_SpellMainLocalPosition + m_SpellBobLocalPosition + m_SpellRecoilLocalPosition;
    }

    // Iterate on all Spell slots to find the next valid Spell to switch to
    public void SwitchSpell(bool ascendingOrder)
    {
        int newSpellIndex = -1;
        int closestSlotDistance = m_SpellSlots.Length;
        for (int i = 0; i < m_SpellSlots.Length; i++)
        {
            // If the Spell at this slot is valid, calculate its "distance" from the active slot index (either in ascending or descending order)
            // and select it if it's the closest distance yet
            if (i != activeSpellIndex && GetSpellAtSlotIndex(i) != null)
            {
                int distanceToActiveIndex = GetDistanceBetweenSpellSlots(activeSpellIndex, i, ascendingOrder);

                if (distanceToActiveIndex < closestSlotDistance)
                {
                    closestSlotDistance = distanceToActiveIndex;
                    newSpellIndex = i;
                }
            }
        }

        // Handle switching to the new Spell index
        SwitchToSpellIndex(newSpellIndex);
    }

    // Switches to the given Spell index in Spell slots if the new index is a valid Spell that is different from our current one
    public void SwitchToSpellIndex(int newSpellIndex, bool force = false)
    {
        if (force || (newSpellIndex != activeSpellIndex && newSpellIndex >= 0))
        {
            // Store data related to Spell switching animation
            m_SpellSwitchNewSpellIndex = newSpellIndex;
            m_TimeStartedSpellSwitch = Time.time;

            // Handle case of switching to a valid Spell for the first time (simply put it up without putting anything down first)
            if(GetActiveSpell() == null)
            {
                m_SpellMainLocalPosition = defaultSpellPosition.localPosition;
                m_SpellSwitchState = SpellSwitchState.PutUpNew;
                activeSpellIndex = m_SpellSwitchNewSpellIndex;

                SpellController newSpell = GetSpellAtSlotIndex(m_SpellSwitchNewSpellIndex);
                if (onSwitchedToSpell != null)
                {
                    onSwitchedToSpell.Invoke(newSpell);
                }
            }
            // otherwise, remember we are putting down our current Spell for switching to the next one
            else
            {
                m_SpellSwitchState = SpellSwitchState.PutDownPrevious;
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

    // Updates Spell position and camera FoV for the aiming transition
    void UpdateSpellAiming()
    {
        if (m_SpellSwitchState == SpellSwitchState.Up)
        {
            SpellController activeSpell = GetActiveSpell();
            if (isAiming && activeSpell)
            {
                m_SpellMainLocalPosition = Vector3.Lerp(m_SpellMainLocalPosition, aimingSpellPosition.localPosition + activeSpell.aimOffset, aimingAnimationSpeed * Time.deltaTime);
            }
            else
            {
                m_SpellMainLocalPosition = Vector3.Lerp(m_SpellMainLocalPosition, defaultSpellPosition.localPosition, aimingAnimationSpeed * Time.deltaTime);
            }
        }
    }

    // Updates the Spell bob animation based on character speed
    void UpdateSpellBob()
    {
        if (Time.deltaTime > 0f)
        {
            Vector3 playerCharacterVelocity = (m_PlayerCharacterController.transform.position - m_LastCharacterPosition) / Time.deltaTime;

            // calculate a smoothed Spell bob amount based on how close to our max grounded movement velocity we are
            float characterMovementFactor = 0f;
            if (m_PlayerCharacterController.isGrounded)
            {
                characterMovementFactor = Mathf.Clamp01(playerCharacterVelocity.magnitude / (m_PlayerCharacterController.maxSpeedOnGround * m_PlayerCharacterController.sprintSpeedModifier));
            }
            m_SpellBobFactor = Mathf.Lerp(m_SpellBobFactor, characterMovementFactor, bobSharpness * Time.deltaTime);

            // Calculate vertical and horizontal Spell bob values based on a sine function
            float bobAmount = isAiming ? aimingBobAmount : defaultBobAmount;
            float frequency = bobFrequency;
            float hBobValue = Mathf.Sin(Time.time * frequency) * bobAmount * m_SpellBobFactor;
            float vBobValue = ((Mathf.Sin(Time.time * frequency * 2f) * 0.5f) + 0.5f) * bobAmount * m_SpellBobFactor;

            // Apply Spell bob
            m_SpellBobLocalPosition.x = hBobValue;
            m_SpellBobLocalPosition.y = Mathf.Abs(vBobValue);

            m_LastCharacterPosition = m_PlayerCharacterController.transform.position;
        }
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

    // Updates the animated transition of switching Spells
    void UpdateSpellSwitching()
    {
        // Calculate the time ratio (0 to 1) since Spell switch was triggered
        float switchingTimeFactor = 0f;
        if (SpellSwitchDelay == 0f)
        {
            switchingTimeFactor = 1f;
        }
        else
        {
            switchingTimeFactor = Mathf.Clamp01((Time.time - m_TimeStartedSpellSwitch) / SpellSwitchDelay);
        }

        // Handle transiting to new switch state
        if(switchingTimeFactor >= 1f)
        {
            if (m_SpellSwitchState == SpellSwitchState.PutDownPrevious)
            {
                // Deactivate old Spell
                SpellController oldSpell = GetSpellAtSlotIndex(activeSpellIndex);
                if (oldSpell != null)
                {
                    oldSpell.ShowSpell(false);
                }

                activeSpellIndex = m_SpellSwitchNewSpellIndex;
                switchingTimeFactor = 0f;

                // Activate new Spell
                SpellController newSpell = GetSpellAtSlotIndex(activeSpellIndex);
                if (onSwitchedToSpell != null)
                {
                    onSwitchedToSpell.Invoke(newSpell);
                }

                if(newSpell)
                {
                    m_TimeStartedSpellSwitch = Time.time;
                    m_SpellSwitchState = SpellSwitchState.PutUpNew;
                }
                else
                {
                    // if new Spell is null, don't follow through with putting Spell back up
                    m_SpellSwitchState = SpellSwitchState.Down;
                }
            }
            else if (m_SpellSwitchState == SpellSwitchState.PutUpNew)
            {
                m_SpellSwitchState = SpellSwitchState.Up;
            }
        }

        // Handle moving the Spell socket position for the animated Spell switching
        if (m_SpellSwitchState == SpellSwitchState.PutDownPrevious)
        {
            m_SpellMainLocalPosition = Vector3.Lerp(defaultSpellPosition.localPosition, defaultSpellPosition.localPosition, switchingTimeFactor);
        }
        else if (m_SpellSwitchState == SpellSwitchState.PutUpNew)
        {
            m_SpellMainLocalPosition = Vector3.Lerp(defaultSpellPosition.localPosition, defaultSpellPosition.localPosition, switchingTimeFactor);
        }
    }

    // Adds a Spell to our inventory
    public bool AddSpell(SpellController SpellPrefab)
    {
        // if we already hold this Spell type (a Spell coming from the same source prefab), don't add the Spell
        if(HasSpell(SpellPrefab))
        {
            return false;
        }

        // search our Spell slots for the first free one, assign the Spell to it, and return true if we found one. Return false otherwise
        for (int i = 0; i < m_SpellSlots.Length; i++)
        {
            // only add the Spell if the slot is free
            if(m_SpellSlots[i] == null)
            {
                // spawn the Spell prefab as child of the Spell socket
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

                m_SpellSlots[i] = SpellInstance;

                if(onAddedSpell != null)
                {
                    onAddedSpell.Invoke(SpellInstance, i);
                }

                return true;
            }
        }

        // Handle auto-switching to Spell if no Spells currently
        if (GetActiveSpell() == null)
        {
            SwitchSpell(true);
        }

        return false;
    }

    public bool RemoveSpell(SpellController SpellInstance)
    {
        // Look through our slots for that Spell
        for (int i = 0; i < m_SpellSlots.Length; i++)
        {
            // when Spell found, remove it
            if(m_SpellSlots[i] == SpellInstance)
            {
                m_SpellSlots[i] = null;

                if (onRemovedSpell != null)
                {
                    onRemovedSpell.Invoke(SpellInstance, i);
                }

                Destroy(SpellInstance.gameObject);

                // Handle case of removing active Spell (switch to next Spell)
                if(i == activeSpellIndex)
                {
                    SwitchSpell(true);
                }

                return true; 
            }
        }

        return false;
    }

    public SpellController GetActiveSpell()
    {
        return GetSpellAtSlotIndex(activeSpellIndex);
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

    // Calculates the "distance" between two Spell slot indexes
    // For example: if we had 5 Spell slots, the distance between slots #2 and #4 would be 2 in ascending order, and 3 in descending order
    int GetDistanceBetweenSpellSlots(int fromSlotIndex, int toSlotIndex, bool ascendingOrder)
    {
        int distanceBetweenSlots = 0;

        if (ascendingOrder)
        {
            distanceBetweenSlots = toSlotIndex - fromSlotIndex;
        }
        else
        {
            distanceBetweenSlots = -1 * (toSlotIndex - fromSlotIndex);
        }

        if (distanceBetweenSlots < 0)
        {
            distanceBetweenSlots = m_SpellSlots.Length + distanceBetweenSlots;
        }

        return distanceBetweenSlots;
    }

    void OnSpellSwitched(SpellController newSpell)
    {
        if (newSpell != null)
        {
            newSpell.ShowSpell(true);
        }
    }
}
