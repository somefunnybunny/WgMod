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

    Mass _mass;
    int _previousTier;

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
            RemoveTemporaryMass(wg);
            _previousTier = 0;
            return;
        }

        int tier = GetActiveTier(out _);

        // When a tier naturally expires, decay by exactly one tier rather than clearing entirely.
        if (tier == 0 && _previousTier > 1)
        {
            tier = _previousTier - 1;
            Player.AddBuff(GetBuffType(tier), MaxTimer);
        }

        if (tier != _previousTier)
        {
            RemoveTemporaryMass(wg);
            if (tier > 0)
            {
                Mass targetMass = Weight.FromStage(tier).Mass - Weight.Base.Mass;
                _mass = wg.AddWeight(targetMass);
            }
            _previousTier = tier;
        }
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

    void RemoveTemporaryMass(WgPlayer wg)
    {
        if (_mass <= 0f)
            return;

        wg.AddWeight(-_mass);
        _mass = 0f;
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
