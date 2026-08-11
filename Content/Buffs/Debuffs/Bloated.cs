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
            1 => "I feel swollen all over... I'm being forced one weight stage heavier. More exposure could make this worse.",
            2 => "I'm swelling even more... I've been pushed two weight stages above my normal size.",
            3 => "My body feels badly distended... I've been forced three weight stages heavier.",
            4 => "I can feel myself getting heavier by the second... I'm four weight stages above normal now.",
            5 => "There's so much pressure building inside me... I've been forced five weight stages heavier.",
            6 => "I'm ballooning out of control... I've been pushed six weight stages above my normal size.",
            7 => "I'm getting far too big to move properly... I've been forced seven weight stages heavier.",
            8 => "I can barely do anything at this size... I've been pushed eight weight stages above normal.",
            _ => "I can't contain any more... I've been forced all the way into Blob status.",
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

        UpdateMinimumNaturalStage();
        EnforceForcedWeight(wg, tier);
        _previousTier = tier;
    }

    public override void PostUpdate()
    {
        if (!Player.TryGetModPlayer(out WgPlayer wg) || !wg.OwnsPlayer() || !_trackingUnderlyingWeight || Player.dead)
            return;

        int tier = GetActiveTier(out _);
        if (tier <= 0)
            return;

        // Movement-based weight loss happens after PostUpdateBuffs. Capture that real change here,
        // then restore the forced Bloated stage before the frame finishes. This prevents grappling
        // hooks and other sharp movement changes from making the displayed weight oscillate.
        CaptureUnderlyingWeightChange(wg);
        UpdateMinimumNaturalStage();
        EnforceForcedWeight(wg, tier);
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
        // and movement-based weight loss. Bloated itself never subtracts temporary mass.
        Mass delta = wg.Weight.Mass - _lastForcedWeight.Mass;
        if (MathF.Abs(delta) > 0.0001f)
            _underlyingWeight = Weight.Clamp(_underlyingWeight + delta);
    }

    void UpdateMinimumNaturalStage()
    {
        int naturalStage = Math.Clamp(_underlyingWeight.GetStage(), WeightStage.Regular, WeightStage.Blob);

        // The floor can rise with real weight gain, but it cannot fall while the chain is active.
        // Weight loss is still recorded in _underlyingWeight and becomes visible when Bloated ends.
        _minimumNaturalStage = Math.Max(_minimumNaturalStage, naturalStage);
    }

    void EnforceForcedWeight(WgPlayer wg, int tier)
    {
        Weight forcedWeight = GetForcedWeight(_underlyingWeight, tier, _minimumNaturalStage);
        wg.SetWeight(forcedWeight, false);
        _lastForcedWeight = wg.Weight;
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
