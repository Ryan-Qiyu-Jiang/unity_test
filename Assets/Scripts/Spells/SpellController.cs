using UnityEngine;
using UnityEngine.Events;

public enum SpellShootType
{
    Manual,
    Automatic,
    Charge,
}

[System.Serializable]
public struct CrosshairData
{
    [Tooltip("The image that will be used for this Spell's crosshair")]
    public Sprite crosshairSprite;
    [Tooltip("The size of the crosshair image")]
    public int crosshairSize;
    [Tooltip("The color of the crosshair image")]
    public Color crosshairColor;
}

public class SpellController : MonoBehaviour
{
    [Header("Information")]
    [Tooltip("The name that will be displayed in the UI for this Spell")]
    public string SpellName;
    [Tooltip("The image that will be displayed in the UI for this Spell")]
    public Sprite SpellIcon;

    [Tooltip("Default data for the crosshair")]
    public CrosshairData crosshairDataDefault;
    [Tooltip("Data for the crosshair when targeting an enemy")]
    public CrosshairData crosshairDataTargetInSight;

    [Header("Internal References")]
    [Tooltip("The root object for the Spell, this is what will be deactivated when the Spell isn't active")]
    public GameObject SpellRoot;
    [Tooltip("Tip of the Spell, where the projectiles are shot")]
    public Transform SpellMuzzle;

    [Header("Shoot Parameters")]
    [Tooltip("The type of Spell wil affect how it shoots")]
    public SpellShootType shootType;
    [Tooltip("The projectile prefab")]
    public ProjectileBase projectilePrefab;
    [Tooltip("The spell damage modifier")]
    public float spellDamageModifier = 1f;
    [Tooltip("Minimum duration between two shots")]
    public float delayBetweenShots = 0.5f;
    [Tooltip("Angle for the cone in which the bullets will be shot randomly (0 means no spread at all)")]
    public float bulletSpreadAngle = 0f;
    [Tooltip("Amount of bullets per shot")]
    public int bulletsPerShot = 1;
    [Tooltip("Force that will push back the Spell after each shot")]
    [Range(0f, 2f)]
    public float recoilForce = 1;
    [Tooltip("Ratio of the default FOV that this Spell applies while aiming")]
    [Range(0f, 1f)]
    public float aimZoomRatio = 1f;
    [Tooltip("Translation to apply to Spell arm when aiming with this Spell")]
    public Vector3 aimOffset;

    [Header("Ammo Parameters")]
    [Tooltip("Amount of ammo reloaded per second")]
    public float ammoReloadRate = 1f;
    [Tooltip("Delay after the last shot before starting to reload")]
    public float ammoReloadDelay = 2f;
    [Tooltip("Maximum amount of ammo in the gun")]
    public float maxAmmo = 8;

    [Header("Charging parameters (charging Spells only)")]
    [Tooltip("Trigger a shot when maximum charge is reached")]
    public bool automaticReleaseOnCharged;
    [Tooltip("Duration to reach maximum charge")]
    public float maxChargeDuration = 2f;
    [Tooltip("Initial ammo used when starting to charge")]
    public float ammoUsedOnStartCharge = 1f;
    [Tooltip("Additional ammo used when charge reaches its maximum")]
    public float ammoUsageRateWhileCharging = 1f;

    [Header("Audio & Visual")]
    [Tooltip("Optional Spell animator for OnShoot animations")]
    public Animator SpellAnimator;
    [Tooltip("Prefab of the muzzle flash")]
    public GameObject muzzleFlashPrefab;
    [Tooltip("Unparent the muzzle flash instance on spawn")]
    public bool unparentMuzzleFlash;
    [Tooltip("sound played when shooting")]

    private bool m_wantsToShoot = false;

    public UnityAction onShoot;

    float m_CurrentAmmo;
    float m_LastTimeShot = Mathf.NegativeInfinity;
    public float LastChargeTriggerTimestamp { get; private set; }
    Vector3 m_LastMuzzlePosition;

    public GameObject owner { get; set; }
    public GameObject sourcePrefab { get; set; }
    public bool isCharging { get; private set; }
    public float currentAmmoRatio { get; private set; }
    public bool isSpellActive { get; private set; }
    public bool isCooling { get; private set; }
    public float currentCharge { get; private set; }
    public Vector3 muzzleWorldVelocity { get; private set; }
    public float GetAmmoNeededToShoot() => (shootType != SpellShootType.Charge ? 1f : Mathf.Max(1f, ammoUsedOnStartCharge)) / (maxAmmo * bulletsPerShot);
    private CharacterStatsController m_CharacterStatsController;
    private float m_currentCDR = 1;

    const string k_AnimAttackParameter = "Attack";

    void Awake()
    {
        m_CurrentAmmo = maxAmmo;
        m_LastMuzzlePosition = SpellMuzzle.position;
    }

    void Update()
    {
        m_currentCDR = m_CharacterStatsController.cdr.GetValue();
        UpdateAmmo();
        UpdateCharge();

        if (Time.deltaTime > 0)
        {
            muzzleWorldVelocity = (SpellMuzzle.position - m_LastMuzzlePosition) / Time.deltaTime;
            m_LastMuzzlePosition = SpellMuzzle.position;
        }
    }

    void UpdateAmmo()
    {
        if (m_LastTimeShot + ammoReloadDelay*m_currentCDR < Time.time && m_CurrentAmmo < maxAmmo && !isCharging)
        {
            // reloads Spell over time
            m_CurrentAmmo += ammoReloadRate * Time.deltaTime;

            // limits ammo to max value
            m_CurrentAmmo = Mathf.Clamp(m_CurrentAmmo, 0, maxAmmo);

            isCooling = true;
        }
        else
        {
            isCooling = false;
        }

        if (maxAmmo == Mathf.Infinity)
        {
            currentAmmoRatio = 1f;
        }
        else
        {
            currentAmmoRatio = m_CurrentAmmo / maxAmmo;
        }
    }

    void UpdateCharge()
    {
        if (isCharging)
        {
            if (currentCharge < 1f)
            {
                float chargeLeft = 1f - currentCharge;

                // Calculate how much charge ratio to add this frame
                float chargeAdded = 0f;
                if (maxChargeDuration <= 0f)
                {
                    chargeAdded = chargeLeft;
                }
                else
                {
                    chargeAdded = (1f / maxChargeDuration) * Time.deltaTime;
                }

                chargeAdded = Mathf.Clamp(chargeAdded, 0f, chargeLeft);

                // See if we can actually add this charge
                float ammoThisChargeWouldRequire = chargeAdded * ammoUsageRateWhileCharging;
                if (ammoThisChargeWouldRequire <= m_CurrentAmmo)
                {
                    // Use ammo based on charge added
                    UseAmmo(ammoThisChargeWouldRequire);

                    // set current charge ratio
                    currentCharge = Mathf.Clamp01(currentCharge + chargeAdded);
                }
            }
        }
    }

    public void ShowSpell(bool show)
    {
        SpellRoot.SetActive(show);
        isSpellActive = show;
        m_CharacterStatsController = owner.GetComponent<CharacterStatsController>();
        DebugUtility.HandleErrorIfNullGetComponent<CharacterStatsController, SpellController>(m_CharacterStatsController, this, gameObject);
    }

    public void UseAmmo(float amount)
    {
        m_CurrentAmmo = Mathf.Clamp(m_CurrentAmmo - amount, 0f, maxAmmo);
        m_LastTimeShot = Time.time;
    }

    public bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp)
    {
        m_wantsToShoot = inputDown || inputHeld;
        switch (shootType)
        {
            case SpellShootType.Manual:
                if (inputDown)
                {
                    return TryShoot();
                }
                return false;

            case SpellShootType.Automatic:
                if (inputHeld)
                {
                    return TryShoot();
                }
                return false;

            case SpellShootType.Charge:
                if (inputHeld)
                {
                    TryBeginCharge();
                }
                // Check if we released charge or if the Spell shoot autmatically when it's fully charged
                if (inputUp || (automaticReleaseOnCharged && currentCharge >= 1f))
                {
                    return TryReleaseCharge();
                }
                return false;

            default:
                return false;
        }
    }

    bool TryShoot()
    {
        if (m_CurrentAmmo >= 1f 
            && m_LastTimeShot + delayBetweenShots*m_currentCDR < Time.time)
        {
            HandleShoot();
            m_CurrentAmmo -= 1f;

            return true;
        }

        return false;
    }

    bool TryBeginCharge()
    {
        if (!isCharging
            && m_CurrentAmmo >= ammoUsedOnStartCharge
            && Mathf.FloorToInt((m_CurrentAmmo - ammoUsedOnStartCharge) * bulletsPerShot) > 0
            && m_LastTimeShot + delayBetweenShots*m_currentCDR < Time.time)
        {
            UseAmmo(ammoUsedOnStartCharge);

            LastChargeTriggerTimestamp = Time.time;
            isCharging = true;

            return true;
        }

        return false;
    }

    bool TryReleaseCharge()
    {
        if (isCharging)
        {
            HandleShoot();

            currentCharge = 0f;
            isCharging = false;

            return true;
        }
        return false;
    }

    void HandleShoot()
    {
        int bulletsPerShotFinal = shootType == SpellShootType.Charge ? Mathf.CeilToInt(currentCharge * bulletsPerShot) : bulletsPerShot;
        
        // spawn all bullets with random direction
        for (int i = 0; i < bulletsPerShotFinal; i++)
        {
            Vector3 shotDirection = GetShotDirectionWithinSpread(SpellMuzzle);
            ProjectileBase newProjectile = Instantiate(projectilePrefab, SpellMuzzle.position, Quaternion.LookRotation(shotDirection));
            newProjectile.Shoot(this);
        }

        m_LastTimeShot = Time.time;

        // Trigger attack animation if there is any
        if (SpellAnimator)
        {
            SpellAnimator.SetTrigger(k_AnimAttackParameter);
        }

        // Callback on shoot
        if (onShoot != null)
        {
            onShoot();
        }
    }

    public Vector3 GetShotDirectionWithinSpread(Transform shootTransform)
    {
        float spreadAngleRatio = bulletSpreadAngle / 180f;
        Vector3 spreadWorldDirection = Vector3.Slerp(shootTransform.forward, UnityEngine.Random.insideUnitSphere, spreadAngleRatio);

        return spreadWorldDirection;
    }
}
