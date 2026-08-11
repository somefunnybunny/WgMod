using System;
using Terraria;
using Terraria.ModLoader;
using WgMod.Common.Players;

namespace WgMod.Content.Buffs.Debuffs;

public abstract class BloatedTierBuff : ModBuff
{
    public abstract int Tier { get; }

    public override string Texture => "WgMod/Content/Buffs/Debuffs/Bloated";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.pvpBuff[Type] = true;
        Main.buffNoSave[Type] = true;
    }

    public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
    {
        buffName = Tier switch
        {
            1 => "Bloated",
            2 => "Swollen",
            3 => "Distended",
            4 => "Engorged",
            5 => "Overinflated",
            6 => "Ballooned",
            7 => "Overblown",
            8 => "Hyperinflated",
            _ => "Uncontainably Bloated",
        };

        tip = Tier switch
        {
            1 => "Your body is visibly swollen, forcing your weight 1 stage higher. Further exposure can worsen it.",
            2 => "The swelling is getting worse, forcing your weight 2 stages higher.",
            3 => "Severe distension forces your weight 3 stages higher.",
            4 => "Your body is heavily engorged, forcing your weight 4 stages higher.",
            5 => "The pressure keeps building, forcing your weight 5 stages higher.",
            6 => "You've ballooned dramatically, forcing your weight 6 stages higher.",
            7 => "Your bloating is overwhelming, forcing your weight 7 stages higher.",
            8 => "Extreme hyperinflation forces your weight 8 stages higher.",
            _ => "You are bloated beyond control, forcing your weight to Blob status.",
        };
    }
}

[Credit(ProjectRole.Programmer, Contributor.maimaichubs)]
public class Bloated : BloatedTierBuff
{
    public override int Tier => 1;
}

public class Swollen : BloatedTierBuff
{
    public override int Tier => 2;
}

public class Distended : BloatedTierBuff
{
    public override int Tier => 3;
}

public class Engorged : BloatedTierBuff
{
    public override int Tier => 4;
}

public class Overinflated : BloatedTierBuff
{
    public override int Tier => 5;
}

public class Ballooned : BloatedTierBuff
{
    public override int Tier => 6;
}

public class Overblown : BloatedTierBuff
{
    public override int Tier => 7;
}

public class Hyperinflated : BloatedTierBuff
{
    public override int Tier => 8;
}

public class UncontainablyBloated : BloatedTierBuff
{
    public override int Tier => 9;
}

public class BloatedPlayer : ModPlayer
{
    public const int MaxTimer = 60 * 25;

    Weight _underlyingWeight;
    Weight _lastForcedWeight;
    bool _trackingUnderlyingWeight;
    int _previousTier;
    int _minimumNaturalStage = -1;

    public void ApplyBloated(int timeToAdd)
    {
        if (!Player.TryGetModPlayer(out WgPlayer wg) || !wg.OwnsPlayer())
            return;

        timeToAdd = Math.Max(timeToAdd, 1);
        int tier = GetActiveTier(out int buffIndex);

        if (tier <= 0)
        {
            // A first application can approach the cap, but escalation requires a further hit.
            Player.AddBuff(GetBuffType(1), Math.Min(timeToAdd, MaxTimer - 1));
            return;
        }

        int totalTime = Player.buffTime[buffIndex] + timeToAdd;
        if (totalTime >= MaxTimer && tier < WeightStage.Blob)
        {
            int overflow = totalTime - MaxTimer;
            Player.DelBuff(buffIndex);
            Player.AddBuff(GetBuffType(tier + 1), Math.Clamp(overflow, 1, MaxTimer));
            return;
        }

        Player.buffTime[buffIndex] = Math.Min(totalTime, MaxTimer);
    }

    public override void PostUpdateBuffs()
    {
        if (!Player.TryGetModPlayer(out WgPlayer wg) || !wg.OwnsPlayer())
            return;

        if (Player.dead)
        {
            ResetTracking();
            return;
        }

        int tier = GetActiveTier(out _);

        // When a tier naturally expires, decay by exactly one tier rather than clearing entirely.
        if (tier == 0 && _previousTier > 1)
        {
            tier = _previousTier - 1;
            Player.AddBuff(GetBuffType(tier), MaxTimer);
        }

        if (!_trackingUnderlyingWeight)
        {
            if (tier <= 0)
            {
                _previousTier = 0;
                return;
            }

            BeginTracking(wg);
        }
        else
        {
            CaptureUnderlyingWeightChange(wg);
        }

        if (tier <= 0)
        {
            // Restore the true underlying weight once the final Bloated tier ends.
            wg.SetWeight(_underlyingWeight, false);
            ResetTracking();
            return;
        }

        int naturalStage = Math.Clamp(_underlyingWeight.GetStage(), WeightStage.Regular, WeightStage.Blob);

        // The floor can rise with real weight gain, but it cannot fall while the chain is active.
        // Weight loss is still recorded in _underlyingWeight and becomes visible when Bloated ends.
        _minimumNaturalStage = Math.Max(_minimumNaturalStage, naturalStage);

        Weight forcedWeight = GetForcedWeight(_underlyingWeight, tier, _minimumNaturalStage);
        wg.SetWeight(forcedWeight, false);
        _lastForcedWeight = wg.Weight;
        _previousTier = tier;
    }

    void BeginTracking(WgPlayer wg)
    {
        _underlyingWeight = wg.Weight;
        _lastForcedWeight = wg.Weight;
        _minimumNaturalStage = Math.Clamp(_underlyingWeight.GetStage(), WeightStage.Regular, WeightStage.Blob);
        _trackingUnderlyingWeight = true;
    }

    void CaptureUnderlyingWeightChange(WgPlayer wg)
    {
        // Anything that changed the displayed weight after the previous Bloated enforcement
        // belongs to the real underlying weight. This includes potions, enemy gain, digestion,
        // and movement-based weight loss. Crucially, we never subtract an old temporary mass.
        Mass delta = wg.Weight.Mass - _lastForcedWeight.Mass;
        if (MathF.Abs(delta) > 0.0001f)
            _underlyingWeight = Weight.Clamp(_underlyingWeight + delta);
    }

    static Weight GetForcedWeight(Weight underlyingWeight, int tier, int minimumNaturalStage)
    {
        int naturalStage = Math.Clamp(underlyingWeight.GetStage(), WeightStage.Regular, WeightStage.Blob);
        int baselineStage = Math.Max(naturalStage, minimumNaturalStage);
        int targetStage = Math.Min(baselineStage + tier, WeightStage.Blob);

        if (targetStage <= naturalStage)
            return underlyingWeight;

        // Preserve progress within a stage when the underlying weight is at the active floor.
        // If weight loss has pushed the hidden weight below that floor, hold at the start of
        // the forced stage instead of allowing that loss to partially cancel Bloated.
        float stageProgress = naturalStage >= baselineStage
            ? Math.Clamp(underlyingWeight.GetStageFactor(), 0f, 1f)
            : 0f;

        if (targetStage < WeightStage.Blob)
        {
            float targetStart = Weight.FromStage(targetStage).Mass;
            float targetEnd = Weight.FromStage(targetStage + 1).Mass;
            return new Weight(float.Lerp(targetStart, targetEnd, stageProgress));
        }

        // Blob is the final stage, so preserve progress within its clamped 10 kg range.
        return new Weight(Weight.FromStage(WeightStage.Blob).Mass + 10f * stageProgress);
    }

    void ResetTracking()
    {
        _trackingUnderlyingWeight = false;
        _previousTier = 0;
        _minimumNaturalStage = -1;
        _underlyingWeight = default;
        _lastForcedWeight = default;
    }

    int GetActiveTier(out int buffIndex)
    {
        for (int tier = WeightStage.Blob; tier >= 1; tier--)
        {
            int index = Player.FindBuffIndex(GetBuffType(tier));
            if (index >= 0)
            {
                buffIndex = index;
                return tier;
            }
        }

        buffIndex = -1;
        return 0;
    }

    static int GetBuffType(int tier)
    {
        return tier switch
        {
            1 => ModContent.BuffType<Bloated>(),
            2 => ModContent.BuffType<Swollen>(),
            3 => ModContent.BuffType<Distended>(),
            4 => ModContent.BuffType<Engorged>(),
            5 => ModContent.BuffType<Overinflated>(),
            6 => ModContent.BuffType<Ballooned>(),
            7 => ModContent.BuffType<Overblown>(),
            8 => ModContent.BuffType<Hyperinflated>(),
            _ => ModContent.BuffType<UncontainablyBloated>(),
        };
    }
}
