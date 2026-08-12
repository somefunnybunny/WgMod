using System;
using Terraria;
using Terraria.ModLoader;

namespace WgMod.Content.Buffs.Debuffs;

public abstract class OverindulgenceTierBuff : ModBuff
{
    public abstract int Tier { get; }

    public override string Texture => "WgMod/Content/Buffs/Debuffs/ForceFed";

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
            1 => "Overindulgent",
            2 => "Overstuffed",
            3 => "Gluttonous",
            4 => "Voracious",
            5 => "Insatiable",
            6 => "Bottomless",
            7 => "Ravenous",
            _ => "All-Consuming",
        };

        tip = Tier switch
        {
            1 => "That really wasn't so bad... and I swear my backside feels a little softer already. I wouldn't mind another.",
            2 => "I'm getting pretty full, and my hips are starting to feel heavier... but I still want another one to find me.",
            3 => "I'm stuffed, my rear is getting huge, and I know exactly what more will do to me... but I want it anyway.",
            4 => "Give me more. I want my hips wider and my backside heavier. I don't care how helpless I get if it keeps feeding me there.",
            5 => "More. Keep feeding me. Make my backside enormous. I don't care if it gets so huge I can't move at all. I want all of it.",
            6 => "I don't want anything else finding me now. Just keep sending food until my hips swallow up everything around me.",
            7 => "More food. Nothing but food. I want my backside so absurdly huge that there's barely room for anything else.",
            _ => "Let the whole world be food. Keep piling it into me until my enormous rear is all I can feel and all anything can reach.",
        };
    }
}

public class Overindulgent : OverindulgenceTierBuff
{
    public override int Tier => 1;
}

public class Overstuffed : OverindulgenceTierBuff
{
    public override int Tier => 2;
}

public class Gluttonous : OverindulgenceTierBuff
{
    public override int Tier => 3;
}

public class Voracious : OverindulgenceTierBuff
{
    public override int Tier => 4;
}

public class Insatiable : OverindulgenceTierBuff
{
    public override int Tier => 5;
}

public class Bottomless : OverindulgenceTierBuff
{
    public override int Tier => 6;
}

public class Ravenous : OverindulgenceTierBuff
{
    public override int Tier => 7;
}

public class AllConsuming : OverindulgenceTierBuff
{
    public override int Tier => 8;
}

public static class OverindulgenceChain
{
    public const int Duration = 60 * 60;
    public const int MaxTier = 8;

    public static void Advance(Player player)
    {
        int currentTier = GetTier(player, out int buffIndex);
        int nextTier = Math.Min(currentTier + 1, MaxTier);

        if (buffIndex >= 0)
            player.DelBuff(buffIndex);

        player.AddBuff(GetBuffType(nextTier), Duration);
    }

    public static int GetTier(Player player)
    {
        return GetTier(player, out _);
    }

    public static float GetSpawnMultiplier(Player player)
    {
        return GetTier(player) switch
        {
            1 => 1.25f,
            2 => 1.5f,
            3 => 2f,
            4 => 3f,
            5 => 5f,
            6 => 10f,
            7 => 25f,
            8 => 60f,
            _ => 1f,
        };
    }

    public static float GetOtherSpawnMultiplier(Player player)
    {
        return GetTier(player) switch
        {
            1 => 0.50f,
            2 => 0.25f,
            3 => 0.10f,
            4 => 0.04f,
            5 => 0.015f,
            6 => 0.005f,
            7 => 0.001f,
            8 => 0.0001f,
            _ => 1f,
        };
    }

    static int GetTier(Player player, out int buffIndex)
    {
        for (int tier = MaxTier; tier >= 1; tier--)
        {
            int index = player.FindBuffIndex(GetBuffType(tier));
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
            1 => ModContent.BuffType<Overindulgent>(),
            2 => ModContent.BuffType<Overstuffed>(),
            3 => ModContent.BuffType<Gluttonous>(),
            4 => ModContent.BuffType<Voracious>(),
            5 => ModContent.BuffType<Insatiable>(),
            6 => ModContent.BuffType<Bottomless>(),
            7 => ModContent.BuffType<Ravenous>(),
            _ => ModContent.BuffType<AllConsuming>(),
        };
    }
}
