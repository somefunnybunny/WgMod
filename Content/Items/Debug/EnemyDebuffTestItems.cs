using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WgMod.Content.Buffs.Debuffs;

namespace WgMod.Content.Items.Debug;

public abstract class EnemyDebuffTestItem : ModItem
{
    public abstract int BuffType { get; }
    public virtual int Duration => 60 * 10;

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

        int index = player.FindBuffIndex(BuffType);
        if (index >= 0)
            player.DelBuff(index);

        player.AddBuff(BuffType, Duration);
        return true;
    }
}

public class TestSlimed : EnemyDebuffTestItem
{
    public override int BuffType => BuffID.Slimed;
}

public class TestForceFed : EnemyDebuffTestItem
{
    public override int BuffType => ModContent.BuffType<ForceFed>();
}

public class TestPrismaticStuffing : EnemyDebuffTestItem
{
    public override int BuffType => ModContent.BuffType<PrismaticStuffing>();
}
