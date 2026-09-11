using UnityEngine;

/// 所持品の総重量を移動性能と衝突判定への補正値に変換します。
[DisallowMultipleComponent]
public class InventoryInfluenceSettings : MonoBehaviour
{
    // 他の機能や表示部品への参照です。設定方法は Initialize または初期化処理を参照してください。
    [Header("参照")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private ImpactSettings impactSettings;

    // 重量 1 あたりの性能低下量です。値が大きいほど荷物の影響が強くなります。
    [Header("重量による影響")]
    [SerializeField] private float accelerationWeightFactor = 0.04f;
    [SerializeField] private float decelerationWeightFactor = 0.03f;
    [SerializeField] private float turnAccelerationWeightFactor = 0.04f;
    [SerializeField] private float turnDecelerationWeightFactor = 0.04f;
    [SerializeField] private float collisionWeightFactor = 0.03f;

    // 重量が増えても性能がこれ以上低下しないよう、各倍率の下限を指定します。
    [Header("移動性能の下限倍率")]
    [SerializeField] private float minAccelerationMultiplier = 0.35f;
    [SerializeField] private float minDecelerationMultiplier = 0.45f;
    [SerializeField] private float minTurnAccelerationMultiplier = 0.35f;
    [SerializeField] private float minTurnDecelerationMultiplier = 0.35f;

    // 実行中の計算結果です。調整確認用であり、ゲーム設定としては変更しません。
    [Header("実行時デバッグ")]
    [SerializeField] private float currentTotalWeight = 0f;
    [SerializeField] private float currentAccelerationMultiplier = 1f;
    [SerializeField] private float currentDecelerationMultiplier = 1f;
    [SerializeField] private float currentTurnAccelerationMultiplier = 1f;
    [SerializeField] private float currentTurnDecelerationMultiplier = 1f;
    [SerializeField] private float currentCollisionMultiplier = 1f;

    // 直前の性能補正に使用した所持重量を返します。
    public float CurrentTotalWeight => currentTotalWeight;
    // 現在の重量による前進・後退の加速倍率を返します。
    public float CurrentAccelerationMultiplier => currentAccelerationMultiplier;
    // 現在の重量による減速倍率を返します。
    public float CurrentDecelerationMultiplier => currentDecelerationMultiplier;
    // 現在の重量による旋回加速倍率を返します。
    public float CurrentTurnAccelerationMultiplier => currentTurnAccelerationMultiplier;
    // 現在の重量による旋回減衰倍率を返します。
    public float CurrentTurnDecelerationMultiplier => currentTurnDecelerationMultiplier;
    // 現在の重量による衝突速度の補正倍率を返します。
    public float CurrentCollisionMultiplier => currentCollisionMultiplier;

    // 所持品・車両・衝突設定を受け取り、変更通知の購読と重量補正を行います。
    public void Initialize(PlayerInventory configuredInventory, PlayerManager configuredPlayerManager, ImpactSettings configuredImpactSettings)
    {
        inventory = configuredInventory;
        playerManager = configuredPlayerManager;
        impactSettings = configuredImpactSettings;
        SubscribeInventory();
        ApplyInfluence();
    }

    // 所持品通知を購読し、初期重量による性能補正を適用します。
    private void Awake()
    {
        SubscribeInventory();
        ApplyInfluence();
    }

    // 再有効化時に所持品通知を購読し、現在重量の補正を適用します。
    private void OnEnable()
    {
        SubscribeInventory();
        ApplyInfluence();
    }

    // 無効化時に、所持品の変更通知を解除します。
    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.InventoryChanged -= ApplyInfluence;
        }
    }

    // 同じ所持品イベントが重複しないよう、解除後に登録し直します。
    private void SubscribeInventory()
    {
        if (inventory == null)
        {
            return;
        }

        inventory.InventoryChanged -= ApplyInfluence;
        inventory.InventoryChanged += ApplyInfluence;
    }

    // 所持品が変わるたびに、重量から各コンポーネント用の倍率を再計算します。
    private void ApplyInfluence()
    {
        if (inventory == null || playerManager == null || impactSettings == null)
        {
            SubscribeInventory();
        }

        // 移動性能は重量で低下しますが、衝突倍率は重量とともに増加します。
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

        // HUD と Inspector で確認する値を、実際に適用する値とそろえます。
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

    // 重量と係数から性能低下倍率を計算し、設定した下限より低くならないようにします。
    private float CalculatePenalty(float weight, float factor, float minimum)
    {
        float multiplier = 1f / (1f + Mathf.Max(0f, weight) * Mathf.Max(0f, factor));
        return Mathf.Max(Mathf.Clamp01(minimum), multiplier);
    }

    // Inspector の変更時に、設定値を有効な範囲へ補正します。
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
