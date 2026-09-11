using UnityEngine;

/// <summary>衝突速度を重量補正し、0〜1 の強度へ変換します。</summary>
[DisallowMultipleComponent]
public class ImpactSettings : MonoBehaviour
{
    // 衝突を有効とする最小速度と、強度が最大になる速度です。
    [Header("衝突速度の範囲")]
    [SerializeField] private float minImpactSpeed = 2f;
    [SerializeField] private float maxImpactSpeed = 20f;

    // 直近の計算結果は Inspector で調整を確認するために保持します。
    [Header("実行時の補正値")]
    [SerializeField] private float loadImpactMultiplier = 1f;
    [SerializeField] private float lastRawImpactSpeed = 0f;
    [SerializeField] private float lastAdjustedImpactSpeed = 0f;
    [SerializeField] private float lastImpactRate = 0f;

    // 所持重量による衝突速度の補正倍率を返します。
    public float LoadImpactMultiplier => loadImpactMultiplier;
    // 直前に計算した補正前の衝突速度を返します。
    public float LastRawImpactSpeed => lastRawImpactSpeed;
    // 直前に計算した重量補正後の衝突速度を返します。
    public float LastAdjustedImpactSpeed => lastAdjustedImpactSpeed;
    // 直前に計算したゼロから一までの衝突強度を返します。
    public float LastImpactRate => lastImpactRate;

    // 所持重量による衝突倍率を、負にならない範囲で更新します。
    public void SetLoadImpactMultiplier(float multiplier)
    {
        loadImpactMultiplier = Mathf.Max(0f, multiplier);
    }

    // 入力速度をゼロ以上に補正して重量倍率を掛け、デバッグ用の値も保存します。
    public float GetAdjustedImpactSpeed(float rawImpactSpeed)
    {
        lastRawImpactSpeed = Mathf.Max(0f, rawImpactSpeed);
        lastAdjustedImpactSpeed = lastRawImpactSpeed * loadImpactMultiplier;
        return lastAdjustedImpactSpeed;
    }

    // 補正後の速度が、取得やフィードバックに必要な最小速度以上かを返します。
    public bool IsStrongEnough(float impactSpeed)
    {
        return impactSpeed >= minImpactSpeed;
    }

    // 最小速度で 0、最大速度で 1 になる補間値を返します。
    public float GetImpactRate(float impactSpeed)
    {
        lastImpactRate = Mathf.InverseLerp(
            minImpactSpeed,
            maxImpactSpeed,
            impactSpeed
        );
        return lastImpactRate;
    }

    // 生の衝突速度に重量補正を掛けてから、ゼロから一までの強度へ変換します。
    public float GetImpactRateFromRawSpeed(float rawImpactSpeed)
    {
        float adjustedImpactSpeed = GetAdjustedImpactSpeed(rawImpactSpeed);
        return GetImpactRate(adjustedImpactSpeed);
    }

    // Inspector の変更時に、設定値を有効な範囲へ補正します。
    private void OnValidate()
    {
        minImpactSpeed = Mathf.Max(0f, minImpactSpeed);
        maxImpactSpeed = Mathf.Max(minImpactSpeed + 0.01f, maxImpactSpeed);
        loadImpactMultiplier = Mathf.Max(0f, loadImpactMultiplier);
    }
}
