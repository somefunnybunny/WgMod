using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WgMod.Content.NPCs.BiomeVariants;
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

public class TestBiomeVariantEnemy : ModItem
{
    int _nextEnemy;

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

        int[] roster =
        {
            ModContent.NPCType<StuffingSlime>(),
            ModContent.NPCType<GorgeWorm>(),
            ModContent.NPCType<SyrupAntlion>(),
            ModContent.NPCType<BanquetSkeleton>(),
            ModContent.NPCType<HoneyHornetQueen>(),
            ModContent.NPCType<Bloatfish>(),
            ModContent.NPCType<CreamSlushie>(),
            ModContent.NPCType<FurnaceImp>(),
            ModContent.NPCType<PossessedBuffet>(),
            ModContent.NPCType<BlobLeech>(),
            ModContent.NPCType<MirrorNymph>(),
        };

        int type = roster[_nextEnemy % roster.Length];
        _nextEnemy = (_nextEnemy + 1) % roster.Length;

        int index = NPC.NewNPC(
            player.GetSource_Misc("TestBiomeVariantEnemy"),
            (int)player.Center.X + player.direction * 180,
            (int)player.Center.Y - 32,
            type,
            Target: player.whoAmI
        );

        if (index >= 0 && index < Main.maxNPCs)
            Main.NewText($"Spawned {Main.npc[index].FullName} ({_nextEnemy}/{roster.Length} next in cycle).", 180, 220, 255);

        return true;
    }
}
