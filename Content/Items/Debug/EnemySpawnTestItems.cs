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

        int startX = (int)(player.Center.X / 16f) + 6;
        int startY = (int)(player.Bottom.Y / 16f);
        int anchorX = startX;
        int anchorY = startY;

        // Find nearby solid terrain so the vanilla Man Eater AI has a legitimate tether tile.
        bool foundAnchor = false;
        for (int radius = 0; radius <= 12 && !foundAnchor; radius++)
        {
            for (int xOffset = -radius; xOffset <= radius && !foundAnchor; xOffset++)
            {
                int x = startX + xOffset;
                if (x < 1 || x >= Main.maxTilesX - 1)
                    continue;

                for (int yOffset = -4; yOffset <= 10; yOffset++)
                {
                    int y = startY + yOffset;
                    if (y < 1 || y >= Main.maxTilesY - 1)
                        continue;

                    Tile tile = Main.tile[x, y];
                    if (tile.HasUnactuatedTile && Main.tileSolid[tile.TileType])
                    {
                        anchorX = x;
                        anchorY = y;
                        foundAnchor = true;
                        break;
                    }
                }
            }
        }

        NPC.NewNPC(
            player.GetSource_Misc("TestFeedingManEater"),
            anchorX * 16 + 8,
            anchorY * 16,
            ModContent.NPCType<FeedingManEater>(),
            ai0: anchorX,
            ai1: anchorY,
            Target: player.whoAmI
        );
        return true;
    }
}
