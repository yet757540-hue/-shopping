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
    [SerializeField] private float turnAccelerationWeightFactor = 0.04f;
    [SerializeField] private float turnDecelerationWeightFactor = 0.04f;
    [SerializeField] private float collisionWeightFactor = 0.03f;

    [Header("Minimum Movement Multipliers")]
    [SerializeField] private float minAccelerationMultiplier = 0.35f;
    [SerializeField] private float minDecelerationMultiplier = 0.45f;
    [SerializeField] private float minTurnAccelerationMultiplier = 0.35f;
    [SerializeField] private float minTurnDecelerationMultiplier = 0.35f;

    [Header("Runtime Debug")]
    [SerializeField] private float currentTotalWeight = 0f;
    [SerializeField] private float currentAccelerationMultiplier = 1f;
    [SerializeField] private float currentDecelerationMultiplier = 1f;
    [SerializeField] private float currentTurnAccelerationMultiplier = 1f;
    [SerializeField] private float currentTurnDecelerationMultiplier = 1f;
    [SerializeField] private float currentCollisionMultiplier = 1f;

    public float CurrentTotalWeight => currentTotalWeight;
    public float CurrentAccelerationMultiplier => currentAccelerationMultiplier;
    public float CurrentDecelerationMultiplier => currentDecelerationMultiplier;
    public float CurrentTurnAccelerationMultiplier => currentTurnAccelerationMultiplier;
    public float CurrentTurnDecelerationMultiplier => currentTurnDecelerationMultiplier;
    public float CurrentCollisionMultiplier => currentCollisionMultiplier;

    public void MultiplyAllInfluenceFactors(float multiplier)
    {
        float clampedMultiplier = Mathf.Max(0f, multiplier);
        accelerationWeightFactor *= clampedMultiplier;
        decelerationWeightFactor *= clampedMultiplier;
        turnAccelerationWeightFactor *= clampedMultiplier;
        turnDecelerationWeightFactor *= clampedMultiplier;
        collisionWeightFactor *= clampedMultiplier;
        ApplyInfluence();
    }

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
        float turnAccelerationMultiplier = CalculatePenalty(
            weight,
            turnAccelerationWeightFactor,
            minTurnAccelerationMultiplier
        );
        float turnDecelerationMultiplier = CalculatePenalty(
            weight,
            turnDecelerationWeightFactor,
            minTurnDecelerationMultiplier
        );
        float collisionMultiplier = 1f + Mathf.Max(0f, weight) * Mathf.Max(0f, collisionWeightFactor);

        currentTotalWeight = weight;
        currentAccelerationMultiplier = accelerationMultiplier;
        currentDecelerationMultiplier = decelerationMultiplier;
        currentTurnAccelerationMultiplier = turnAccelerationMultiplier;
        currentTurnDecelerationMultiplier = turnDecelerationMultiplier;
        currentCollisionMultiplier = collisionMultiplier;

        if (playerManager != null)
        {
            playerManager.SetLoadInfluence(
                accelerationMultiplier,
                decelerationMultiplier,
                turnAccelerationMultiplier,
                turnDecelerationMultiplier
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
        turnAccelerationWeightFactor = Mathf.Max(0f, turnAccelerationWeightFactor);
        turnDecelerationWeightFactor = Mathf.Max(0f, turnDecelerationWeightFactor);
        collisionWeightFactor = Mathf.Max(0f, collisionWeightFactor);
        minAccelerationMultiplier = Mathf.Clamp01(minAccelerationMultiplier);
        minDecelerationMultiplier = Mathf.Clamp01(minDecelerationMultiplier);
        minTurnAccelerationMultiplier = Mathf.Clamp01(minTurnAccelerationMultiplier);
        minTurnDecelerationMultiplier = Mathf.Clamp01(minTurnDecelerationMultiplier);
    }
}
