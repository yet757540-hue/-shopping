using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>結算エリアへの最初の入場と最後の退出だけを通知します。</summary>
[RequireComponent(typeof(Collider))]
public class SettlementArea : MonoBehaviour
{
    // 結算エリアがプレイヤーとして受け付けるタグを指定します。
    [Header("トリガー条件")]
    [SerializeField] private string playerTag = "Player";

    private int lastEnterFrame = -1;
    private int lastExitFrame = -1;
    private readonly HashSet<Collider> playerCollidersInside = new HashSet<Collider>();

    public event Action PlayerEntered;
    public event Action PlayerExited;
    // プレイヤーの Collider が一つ以上エリア内に登録されているかを返します。
    public bool HasPlayerInside => playerCollidersInside.Count > 0;

    // エリアの Collider を確認し、トリガー設定が無効なら警告します。
    private void Awake()
    {
        Collider zoneCollider = GetComponent<Collider>();

        if (zoneCollider != null && !zoneCollider.isTrigger)
        {
            Debug.LogWarning("[SettlementArea] Collider should be marked as Trigger.", this);
        }
    }

    // プレイヤーが複数 Collider を持っても、イベントは一度だけ発生させます。
    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        bool wasEmpty = playerCollidersInside.Count == 0;
        playerCollidersInside.Add(other);

        if (wasEmpty)
        {
            HandleEnter();
        }
    }

    // 内部のプレイヤー Collider を取り除き、最後の退出時だけ通知します。
    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        if (!playerCollidersInside.Remove(other))
        {
            return;
        }

        if (playerCollidersInside.Count == 0)
        {
            HandleExit();
        }
    }

    // 同じフレームの重複を防ぎ、入場イベントを通知します。
    public void HandleEnter()
    {
        if (lastEnterFrame == Time.frameCount)
        {
            return;
        }

        lastEnterFrame = Time.frameCount;
        PlayerEntered?.Invoke();
    }

    // 同じフレームの重複を防ぎ、退出イベントを通知します。
    public void HandleExit()
    {
        if (lastExitFrame == Time.frameCount)
        {
            return;
        }

        lastExitFrame = Time.frameCount;
        PlayerExited?.Invoke();
    }

    // 指定タグとの一致を確認します。タグが空なら全 Collider を受け付けます。
    private bool IsPlayer(Collider other)
    {
        if (string.IsNullOrEmpty(playerTag))
        {
            return true;
        }

        return other.CompareTag(playerTag);
    }
}
