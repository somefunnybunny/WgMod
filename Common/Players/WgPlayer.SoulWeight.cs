using System;
using WgMod.Common.Configs;

namespace WgMod.Common.Players;

public partial class WgPlayer
{
    /// <summary>
    /// Permanent weight floor used by the optional Soul Weight system.
    /// No gameplay source increases this yet; future mechanics should use AddSoulWeight.
    /// </summary>
    public Weight SoulWeight { get; private set; } = Weight.Base;

    Weight ClampSoulWeightToConfig(Weight weight)
    {
        weight = Weight.Clamp(weight);
        if (!WgServerConfig.Instance.EnableSoulWeight)
            return weight;

        int minStage = Math.Clamp(WgServerConfig.Instance.SoulWeightMinimumStage, WeightStage.Regular, WeightStage.Max);
        int maxStage = Math.Clamp(WgServerConfig.Instance.SoulWeightMaximumStage, minStage, WeightStage.Max);

        Weight lowerBound = Weight.FromStage(minStage);
        weight = Weight.Clamp(weight, maxStage);
        return new Weight(MathF.Max(weight.Mass, lowerBound.Mass));
    }

    internal Weight ApplySoulWeightFloor(Weight weight)
    {
        if (!WgServerConfig.Instance.EnableSoulWeight)
            return weight;

        SoulWeight = ClampSoulWeightToConfig(SoulWeight);
        return new Weight(MathF.Max(weight.Mass, SoulWeight.Mass));
    }

    public void SetSoulWeight(Weight weight, bool effects = true)
    {
        if (!OwnsPlayer() || !WgServerConfig.Instance.EnableSoulWeight)
            return;

        SetSoulWeightForced(weight, effects);
    }

    public Mass AddSoulWeight(Mass mass, bool effects = true)
    {
        if (!OwnsPlayer() || !WgServerConfig.Instance.EnableSoulWeight)
            return 0f;

        Weight start = SoulWeight;
        SetSoulWeightForced(SoulWeight + mass, effects);
        return SoulWeight.Mass - start.Mass;
    }

    internal void SetSoulWeightForced(Weight weight, bool effects = true)
    {
        SoulWeight = ClampSoulWeightToConfig(weight);
        if (WgServerConfig.Instance.EnableSoulWeight && Weight.Mass < SoulWeight.Mass)
            SetWeightForced(SoulWeight, effects);
    }

    internal void UpdateSoulWeightFloor()
    {
        if (!WgServerConfig.Instance.EnableSoulWeight)
            return;

        SoulWeight = ClampSoulWeightToConfig(SoulWeight);
        if (Weight.Mass < SoulWeight.Mass)
            SetWeightForced(SoulWeight, false);
    }
}
