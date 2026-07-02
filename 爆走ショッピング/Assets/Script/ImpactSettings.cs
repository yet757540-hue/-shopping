using UnityEngine;

[DisallowMultipleComponent]
// 衝突の「生の速度」をゲーム内で使いやすい衝突強度へ変換する設定です。
// 役割:
// - minImpactSpeed 未満の衝突を無視し、maxImpactSpeed までを 0〜1 の impactRate に正規化します。
// - 荷物重量による衝突倍率 loadImpactMultiplier を適用します。
// 接続:
// - CollisionFeedbackManager はこの値で音・振動・カメラ揺れを決めます。
// - ScoreboardManager はこの値で衝突時に獲得するアイテム数を決めます。
// - InventoryInfluenceSettings が荷物重量に応じて SetLoadImpactMultiplier を呼びます。
// 読むときの要点:
// - LastRawImpactSpeed、LastAdjustedImpactSpeed、LastImpactRate は Inspector で確認するための実行時デバッグ値です。
public class ImpactSettings : MonoBehaviour
{
    [Header("Impact Speed Range")]
    [SerializeField] private float minImpactSpeed = 2f;
    [SerializeField] private float maxImpactSpeed = 20f;

    [Header("Runtime Influence")]
    [SerializeField] private float loadImpactMultiplier = 1f;
    [SerializeField] private float lastRawImpactSpeed = 0f;
    [SerializeField] private float lastAdjustedImpactSpeed = 0f;
    [SerializeField] private float lastImpactRate = 0f;

    public float LoadImpactMultiplier => loadImpactMultiplier;
    public float LastRawImpactSpeed => lastRawImpactSpeed;
    public float LastAdjustedImpactSpeed => lastAdjustedImpactSpeed;
    public float LastImpactRate => lastImpactRate;

    // 所持重量などから計算された衝突倍率を受け取ります。
    public void SetLoadImpactMultiplier(float multiplier)
    {
        loadImpactMultiplier = Mathf.Max(0f, multiplier);
    }

    // 生の衝突速度に倍率をかけ、最後の計算結果として保存します。
    public float GetAdjustedImpactSpeed(float rawImpactSpeed)
    {
        lastRawImpactSpeed = Mathf.Max(0f, rawImpactSpeed);
        lastAdjustedImpactSpeed = lastRawImpactSpeed * loadImpactMultiplier;
        return lastAdjustedImpactSpeed;
    }

    // 補正後の衝突速度が反応しきい値を超えているかを判定します。
    public bool IsStrongEnough(float impactSpeed)
    {
        return impactSpeed >= minImpactSpeed;
    }

    // 補正後の衝突速度を 0〜1 の強度へ変換します。
    public float GetImpactRate(float impactSpeed)
    {
        lastImpactRate = Mathf.InverseLerp(
            minImpactSpeed,
            maxImpactSpeed,
            impactSpeed
        );
        return lastImpactRate;
    }

    // 生の衝突速度から補正と正規化をまとめて行います。
    public float GetImpactRateFromRawSpeed(float rawImpactSpeed)
    {
        float adjustedImpactSpeed = GetAdjustedImpactSpeed(rawImpactSpeed);
        return GetImpactRate(adjustedImpactSpeed);
    }

    // しきい値と倍率がマイナスや逆転状態にならないようにします。
    private void OnValidate()
    {
        minImpactSpeed = Mathf.Max(0f, minImpactSpeed);
        maxImpactSpeed = Mathf.Max(minImpactSpeed + 0.01f, maxImpactSpeed);
        loadImpactMultiplier = Mathf.Max(0f, loadImpactMultiplier);
    }
}
