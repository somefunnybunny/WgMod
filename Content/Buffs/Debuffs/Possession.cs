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
            1 => "Something brushed against my thoughts... and now I can feel more of them looking for me.",
            2 => "There's a presence following me. No... more than one. They know where to find me now.",
            3 => "I can feel one of them inside me, leaning against my thoughts and making room for the next.",
            4 => "My thoughts keep slipping. I don't know which ones are mine anymore, and the spirits keep coming closer.",
            _ => "We can feel them coming. We want them closer. There is still room in us for more... there is always room.",
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
