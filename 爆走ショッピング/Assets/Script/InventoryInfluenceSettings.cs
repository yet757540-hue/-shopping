using UnityEngine;

[DisallowMultipleComponent]
// 所持品の総重量を、移動性能と衝突性能へ反映する調整役です。
// 役割:
// - PlayerInventory の TotalWeight から各種倍率を計算します。
// - PlayerManager へ加速、減速、旋回加速、旋回減速の倍率を渡します。
// - ImpactSettings へ衝突倍率を渡し、重いほど衝突効果が強くなるようにします。
// 接続:
// - InventoryChanged イベントを購読し、荷物が増減したタイミングで ApplyInfluence を再実行します。
// - 参照が未設定でも PlayerInventory、PlayerManager、ImpactSettings をシーンから探します。
// 読むときの要点:
// - 移動系は重いほど 1 未満へ下がり、衝突系は重いほど 1 より大きくなります。
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

    // 起動時に参照解決、イベント購読、初回倍率反映を行います。
    private void Awake()
    {
        ResolveReferences();
        SubscribeInventory();
        ApplyInfluence();
    }

    // 再有効化時にも参照と購読を張り直します。
    private void OnEnable()
    {
        ResolveReferences();
        SubscribeInventory();
        ApplyInfluence();
    }

    // 無効化時は InventoryChanged の購読を解除し、二重購読や破棄済み参照を避けます。
    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.InventoryChanged -= ApplyInfluence;
        }
    }

    // 必要な参照をシーンから探し、ImpactSettings がなければ追加します。
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

    // 所持品変更イベントを購読します。事前に解除してから追加するので二重登録されません。
    private void SubscribeInventory()
    {
        if (inventory == null)
        {
            return;
        }

        inventory.InventoryChanged -= ApplyInfluence;
        inventory.InventoryChanged += ApplyInfluence;
    }

    // 現在の総重量から移動倍率と衝突倍率を計算し、各システムへ反映します。
    private void ApplyInfluence()
    {
        if (inventory == null || playerManager == null || impactSettings == null)
        {
            ResolveReferences();
            SubscribeInventory();
        }

        float weight = inventory != null ? inventory.TotalWeight : 0f;
        // 重量ペナルティは項目ごとに分け、加速だけ、減速だけなどを個別に調整できるようにしています。
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

    // 重量と係数から、1 以下で minimum を下回らないペナルティ倍率を作ります。
    private float CalculatePenalty(float weight, float factor, float minimum)
    {
        float multiplier = 1f / (1f + Mathf.Max(0f, weight) * Mathf.Max(0f, factor));
        return Mathf.Max(Mathf.Clamp01(minimum), multiplier);
    }

    // Inspector で倍率計算に使う値が不正にならないように補正します。
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
