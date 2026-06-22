using UnityEngine;

[DisallowMultipleComponent]
public class InventoryInfluenceSettings : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private ImpactSettings impactSettings;

    [Header("Weight Influence")]
    [SerializeField] private float accelerationWeightFactor = 0.04f;
    [SerializeField] private float decelerationWeightFactor = 0.03f;
    [SerializeField] private float turnWeightFactor = 0.04f;
    [SerializeField] private float collisionWeightFactor = 0.03f;

    [Header("Minimum Movement Multipliers")]
    [SerializeField] private float minAccelerationMultiplier = 0.35f;
    [SerializeField] private float minDecelerationMultiplier = 0.45f;
    [SerializeField] private float minTurnMultiplier = 0.35f;

    [Header("Runtime Debug")]
    [SerializeField] private float currentTotalWeight = 0f;
    [SerializeField] private float currentAccelerationMultiplier = 1f;
    [SerializeField] private float currentDecelerationMultiplier = 1f;
    [SerializeField] private float currentTurnMultiplier = 1f;
    [SerializeField] private float currentCollisionMultiplier = 1f;

    public float CurrentTotalWeight => currentTotalWeight;
    public float CurrentAccelerationMultiplier => currentAccelerationMultiplier;
    public float CurrentDecelerationMultiplier => currentDecelerationMultiplier;
    public float CurrentTurnMultiplier => currentTurnMultiplier;
    public float CurrentCollisionMultiplier => currentCollisionMultiplier;

    private void Awake()
    {
        ResolveReferences();
        SubscribeInventory();
        ApplyInfluence();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeInventory();
        ApplyInfluence();
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.InventoryChanged -= ApplyInfluence;
        }
    }

    private void ResolveReferences()
    {
        if (inventory == null)
        {
            inventory = FindAnyObjectByType<PlayerInventory>();
        }

        if (playerManager == null)
        {
            playerManager = FindAnyObjectByType<PlayerManager>();
        }

        if (impactSettings == null)
        {
            impactSettings = FindAnyObjectByType<ImpactSettings>();
        }

        if (impactSettings == null)
        {
            impactSettings = gameObject.AddComponent<ImpactSettings>();
        }
    }

    private void SubscribeInventory()
    {
        if (inventory == null)
        {
            return;
        }

        inventory.InventoryChanged -= ApplyInfluence;
        inventory.InventoryChanged += ApplyInfluence;
    }

    private void ApplyInfluence()
    {
        if (inventory == null || playerManager == null || impactSettings == null)
        {
            ResolveReferences();
            SubscribeInventory();
        }

        float weight = inventory != null ? inventory.TotalWeight : 0f;
        float accelerationMultiplier = CalculatePenalty(weight, accelerationWeightFactor, minAccelerationMultiplier);
        float decelerationMultiplier = CalculatePenalty(weight, decelerationWeightFactor, minDecelerationMultiplier);
        float turnMultiplier = CalculatePenalty(weight, turnWeightFactor, minTurnMultiplier);
        float collisionMultiplier = 1f + Mathf.Max(0f, weight) * Mathf.Max(0f, collisionWeightFactor);

        currentTotalWeight = weight;
        currentAccelerationMultiplier = accelerationMultiplier;
        currentDecelerationMultiplier = decelerationMultiplier;
        currentTurnMultiplier = turnMultiplier;
        currentCollisionMultiplier = collisionMultiplier;

        if (playerManager != null)
        {
            playerManager.SetLoadInfluence(
                accelerationMultiplier,
                decelerationMultiplier,
                turnMultiplier
            );
        }

        if (impactSettings != null)
        {
            impactSettings.SetLoadImpactMultiplier(collisionMultiplier);
        }
    }

    private float CalculatePenalty(float weight, float factor, float minimum)
    {
        float multiplier = 1f / (1f + Mathf.Max(0f, weight) * Mathf.Max(0f, factor));
        return Mathf.Max(Mathf.Clamp01(minimum), multiplier);
    }

    private void OnValidate()
    {
        accelerationWeightFactor = Mathf.Max(0f, accelerationWeightFactor);
        decelerationWeightFactor = Mathf.Max(0f, decelerationWeightFactor);
        turnWeightFactor = Mathf.Max(0f, turnWeightFactor);
        collisionWeightFactor = Mathf.Max(0f, collisionWeightFactor);
        minAccelerationMultiplier = Mathf.Clamp01(minAccelerationMultiplier);
        minDecelerationMultiplier = Mathf.Clamp01(minDecelerationMultiplier);
        minTurnMultiplier = Mathf.Clamp01(minTurnMultiplier);
    }
}
