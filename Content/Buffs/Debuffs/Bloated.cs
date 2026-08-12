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
            9 => "Uncontainably Bloated",
            _ => "Megabloated",
        };

        tip = Tier switch
        {
            1 => "I feel swollen all over... I'm one weight stage heavier, and my stomach feels unsettled.",
            2 => "Urf... I'm swelling even more. Two stages heavier now... I keep needing to burp.",
            3 => "Hrrp... my body feels badly distended... three stages heavier, and the pressure won't stop building.",
            4 => "HUUURP... ngh... four stages heavier... I can barely get a sentence out without belching.",
            5 => "BUUURRRP... ugh... five stages heavier... there's so much pressure inside me...",
            6 => "HUUUURRRP... b-buurp... six stages... I'm ballooning faster than I can let any of this out...",
            7 => "BUUUUURRRP... HURRRP... seven... stages...? I can't... *urp*... stop...",
            8 => "HUUUUUURRRRRP... BUUURP... eight... *BURRRP*... too much... can't...",
            9 => "BUUUUUUUURRRRRP... HUUUUURRRP... UUUURP... B-BUUURRRP... HHHHURRRP...",
            _ => "BUUUUUUUUUURRRRRP... H-HUUUUURRRP... m-mega... *BUURRRP*... Blob... can't... stop swelling... can't stop... *HUUUURRRP*...",
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

public class Megabloated : BloatedTierBuff
{
    public override int Tier => 10;
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

        // Additional Bloating only becomes physically dangerous once the new Mega Blob ceiling
        // has actually been reached. Blob itself is deliberately safe from Straining pressure.
        if (wg.Weight.GetStage() >= WeightStage.MegaBlob && Player.TryGetModPlayer(out StrainingPlayer straining))
            straining.AddBloatedTime(timeToAdd);

        int tier = GetActiveTier(out int buffIndex);

        if (tier <= 0)
        {
            // A first application can approach the cap, but escalation requires a further hit.
            Player.AddBuff(GetBuffType(1), Math.Min(timeToAdd, MaxTimer - 1));
            return;
        }

        int totalTime = Player.buffTime[buffIndex] + timeToAdd;
        if (totalTime >= MaxTimer && tier < WeightStage.MegaBlob)
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
            ClearBloatedBuffs();
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

        // Natural movement-based weight loss is disabled for the entire Bloated chain.
        // Explicit changes such as potions, enemy effects, and digestion still work normally.
        wg.WeightLossRate *= 0f;

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

        // Capture legitimate late-frame weight changes such as digestion, then reassert the
        // forced Bloated stage. Natural movement loss is already suppressed in PostUpdateBuffs.
        CaptureUnderlyingWeightChange(wg);
        UpdateMinimumNaturalStage();
        EnforceForcedWeight(wg, tier);
    }

    public override void UpdateDead()
    {
        ClearBloatedBuffs();
        ResetTracking();
    }

    public override void OnRespawn()
    {
        // Explicitly clear every tier here as well. buffNoSave prevents world persistence, but
        // death/respawn is a separate lifecycle and previously allowed the chain to linger.
        ClearBloatedBuffs();
        ResetTracking();
    }

    void BeginTracking(WgPlayer wg)
    {
        _underlyingWeight = wg.Weight;
        _lastForcedWeight = wg.Weight;
        _minimumNaturalStage = Math.Clamp(_underlyingWeight.GetStage(), WeightStage.Regular, WeightStage.MegaBlob);
        _trackingUnderlyingWeight = true;
    }

    void CaptureUnderlyingWeightChange(WgPlayer wg)
    {
        Mass delta = wg.Weight.Mass - _lastForcedWeight.Mass;
        if (MathF.Abs(delta) > 0.0001f)
            _underlyingWeight = Weight.Clamp(_underlyingWeight + delta);
    }

    void UpdateMinimumNaturalStage()
    {
        int naturalStage = Math.Clamp(_underlyingWeight.GetStage(), WeightStage.Regular, WeightStage.MegaBlob);
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
        int naturalStage = Math.Clamp(underlyingWeight.GetStage(), WeightStage.Regular, WeightStage.MegaBlob);
        int baselineStage = Math.Max(naturalStage, minimumNaturalStage);
        int targetStage = Math.Min(baselineStage + tier, WeightStage.MegaBlob);

        if (targetStage <= naturalStage)
            return underlyingWeight;

        float stageProgress = naturalStage >= baselineStage
            ? Math.Clamp(underlyingWeight.GetStageFactor(), 0f, 1f)
            : 0f;

        if (targetStage < WeightStage.MegaBlob)
        {
            float targetStart = Weight.FromStage(targetStage).Mass;
            float targetEnd = Weight.FromStage(targetStage + 1).Mass;
            return new Weight(float.Lerp(targetStart, targetEnd, stageProgress));
        }

        // Mega Blob is now the final stage. Preserve progress inside its clamped 10 kg range.
        return new Weight(Weight.FromStage(WeightStage.MegaBlob).Mass + 10f * stageProgress);
    }

    void ClearBloatedBuffs()
    {
        for (int tier = WeightStage.MegaBlob; tier >= 1; tier--)
        {
            int index = Player.FindBuffIndex(GetBuffType(tier));
            if (index >= 0)
                Player.DelBuff(index);
        }
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
        for (int tier = WeightStage.MegaBlob; tier >= 1; tier--)
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
            9 => ModContent.BuffType<UncontainablyBloated>(),
            _ => ModContent.BuffType<Megabloated>(),
        };
    }
}
