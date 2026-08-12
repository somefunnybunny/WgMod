using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WgMod.Content.NPCs.Caverns;

namespace WgMod.Content.Items.Debug;

public class TestFeedingManEater : ModItem
{
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
        if (player.whoAmI != Main.myPlayer || Main.netMode == NetmodeID.MultiplayerClient)
            return null;

        NPC.NewNPC(
            player.GetSource_Misc("TestFeedingManEater"),
            (int)player.Center.X + 96,
            (int)player.Center.Y,
            ModContent.NPCType<FeedingManEater>(),
            Target: player.whoAmI
        );
        return true;
    }
}
