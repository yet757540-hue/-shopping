using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/CandyEffectLibrary", fileName = "CandyEffectLibrary")]
public sealed class CandyEffectLibrary : ScriptableObject
{
    [SerializeField] private CandyEffect[] effects = Array.Empty<CandyEffect>();

    public IReadOnlyList<CandyEffect> Effects => effects;
}
