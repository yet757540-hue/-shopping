using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SettlementArea : MonoBehaviour
{
    [Header("Trigger Rules")]
    [SerializeField] private string playerTag = "Player";

    private int lastEnterFrame = -1;
    private int lastExitFrame = -1;
    private readonly HashSet<Collider> playerCollidersInside = new HashSet<Collider>();

    public event Action PlayerEntered;
    public event Action PlayerExited;
    public bool HasPlayerInside => playerCollidersInside.Count > 0;

    private void Awake()
    {
        Collider zoneCollider = GetComponent<Collider>();

        if (zoneCollider != null && !zoneCollider.isTrigger)
        {
            Debug.LogWarning("[SettlementArea] Collider should be marked as Trigger.", this);
        }
    }

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

    public void HandleEnter()
    {
        if (lastEnterFrame == Time.frameCount)
        {
            return;
        }

        lastEnterFrame = Time.frameCount;
        PlayerEntered?.Invoke();
    }

    public void HandleExit()
    {
        if (lastExitFrame == Time.frameCount)
        {
            return;
        }

        lastExitFrame = Time.frameCount;
        PlayerExited?.Invoke();
    }

    private bool IsPlayer(Collider other)
    {
        if (string.IsNullOrEmpty(playerTag))
        {
            return true;
        }

        return other.CompareTag(playerTag);
    }
}
