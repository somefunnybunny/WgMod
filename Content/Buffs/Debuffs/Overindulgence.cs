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
            _ => "Insatiable",
        };

        tip = Tier switch
        {
            1 => "That really wasn't so bad... honestly, I wouldn't mind if another one found me.",
            2 => "I'm getting pretty full, but I still want another. If more food comes to me, I'm not stopping it.",
            3 => "I'm stuffed and getting heavier, and I know exactly what more will do to me... but I want it anyway.",
            4 => "Give me more. I don't care how huge or helpless I end up; I want every last bite that finds me.",
            _ => "More. Keep feeding me. I don't care if I get so big I can't move at all. I want all of it.",
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

public static class OverindulgenceChain
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
            1 => ModContent.BuffType<Overindulgent>(),
            2 => ModContent.BuffType<Overstuffed>(),
            3 => ModContent.BuffType<Gluttonous>(),
            4 => ModContent.BuffType<Voracious>(),
            _ => ModContent.BuffType<Insatiable>(),
        };
    }
}
