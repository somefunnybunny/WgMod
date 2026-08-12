using Terraria;
using Terraria.ModLoader;
using WgMod.Common.Players;

namespace WgMod.Content.Buffs.Debuffs;

public class DigestiveBloom : ModBuff
{
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.pvpBuff[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        if (player.TryGetModPlayer(out WgPlayer wg))
        {
            wg.FoodAbsorption *= 1.75f;
            wg.WeightGainRate *= 1.25f;
        }
    }
}

public class RoyalJelly : ModBuff
{
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.pvpBuff[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        if (player.TryGetModPlayer(out WgPlayer wg))
        {
            wg.FoodAbsorption *= 1.5f;
            wg.WeightLossRate *= 0.6f;
        }
    }
}

public class BuoyantBloat : ModBuff
{
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.pvpBuff[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        player.gravity *= 0.55f;
        player.maxFallSpeed *= 0.6f;
        if (player.velocity.Y > 1f)
            player.velocity.Y *= 0.85f;
    }
}

public class Thickened : ModBuff
{
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.pvpBuff[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        if (player.TryGetModPlayer(out WgPlayer wg))
        {
            wg.FoodAbsorption *= 1.35f;
            wg.WeightGainRate *= 1.2f;
        }
        player.moveSpeed *= 0.9f;
    }
}

public class DemonDough : ModBuff
{
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.pvpBuff[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        if (player.TryGetModPlayer(out WgPlayer wg))
            wg.WeightGainRate *= 1.65f;
    }
}
