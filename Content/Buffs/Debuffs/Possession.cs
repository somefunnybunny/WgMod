using System;
using Terraria;
using Terraria.ModLoader;

namespace WgMod.Content.Buffs.Debuffs;

public abstract class PossessionTierBuff : ModBuff
{
    public abstract int Tier { get; }

    public override string Texture => "WgMod/Content/Buffs/Debuffs/PrismaticStuffing";

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
            1 => "Spirit-Touched",
            2 => "Haunted",
            3 => "Possessed",
            4 => "Overtaken",
            _ => "Spiritbound",
        };

        tip = Tier switch
        {
            1 => "That wasn't so bad... having one inside me was strange, but I wouldn't mind another.",
            2 => "I can feel the spirits gathering again. Good. I want to know what happens if I let more of them in.",
            3 => "They're making me heavier every time, and I know where this is going... but I want them to keep coming.",
            4 => "Let them in. I don't care if they make me too heavy to move; I want to feel myself fill up with them again.",
            _ => "More spirits. All of them. I don't care if they leave me completely helpless. I want them inside me until I can't take another step.",
        };
    }
}

public class SpiritTouched : PossessionTierBuff
{
    public override int Tier => 1;
}

public class Haunted : PossessionTierBuff
{
    public override int Tier => 2;
}

public class Possessed : PossessionTierBuff
{
    public override int Tier => 3;
}

public class Overtaken : PossessionTierBuff
{
    public override int Tier => 4;
}

public class Spiritbound : PossessionTierBuff
{
    public override int Tier => 5;
}

public static class PossessionChain
{
    public const int Duration = 60 * 60;
    public const int MaxTier = 5;

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
            1 => ModContent.BuffType<SpiritTouched>(),
            2 => ModContent.BuffType<Haunted>(),
            3 => ModContent.BuffType<Possessed>(),
            4 => ModContent.BuffType<Overtaken>(),
            _ => ModContent.BuffType<Spiritbound>(),
        };
    }
}
