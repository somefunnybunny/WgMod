using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WgMod.Content.Buffs.Debuffs;

namespace WgMod.Content.Items.Debug;

public abstract class BloatedTestItem : ModItem
{
    public abstract int Tier { get; }

    public override string Texture => "WgMod/Content/Items/WeightManipulator";

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
    }

    public override void SetDefaults()
    {
        Item.width = 20;
        Item.height = 20;
        Item.maxStack = 1;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.useTime = 10;
        Item.useAnimation = 10;
        Item.noMelee = true;
        Item.UseSound = SoundID.Item4;
    }

    public override bool? UseItem(Player player)
    {
        if (player.whoAmI != Main.myPlayer)
            return null;

        for (int tier = 1; tier <= WeightStage.Blob; tier++)
        {
            int index = player.FindBuffIndex(GetBuffType(tier));
            if (index >= 0)
                player.DelBuff(index);
        }

        player.AddBuff(GetBuffType(Tier), BloatedPlayer.MaxTimer);
        return true;
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

public class TestBloated : BloatedTestItem
{
    public override int Tier => 1;
}

public class TestSwollen : BloatedTestItem
{
    public override int Tier => 2;
}

public class TestDistended : BloatedTestItem
{
    public override int Tier => 3;
}

public class TestEngorged : BloatedTestItem
{
    public override int Tier => 4;
}

public class TestOverinflated : BloatedTestItem
{
    public override int Tier => 5;
}

public class TestBallooned : BloatedTestItem
{
    public override int Tier => 6;
}

public class TestOverblown : BloatedTestItem
{
    public override int Tier => 7;
}

public class TestHyperinflated : BloatedTestItem
{
    public override int Tier => 8;
}

public class TestUncontainablyBloated : BloatedTestItem
{
    public override int Tier => 9;
}
