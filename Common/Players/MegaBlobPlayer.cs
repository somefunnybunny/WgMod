using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WgMod.Content.NPCs.UndergroundDesert;

namespace WgMod.Common.Players;

public class MegaBlobPlayer : ModPlayer
{
    const int FoodGraceTime = 60 * 3;
    const int FoodCheckInterval = 30;
    const float NearbyFoodRange = 2400f;

    int _megaBlobTime;
    int _foodCheckTimer;

    public override bool CanUseItem(Item item)
    {
        return !IsMegaBlob();
    }

    public override void PostUpdate()
    {
        if (!IsMegaBlob() || Player.dead)
        {
            _megaBlobTime = 0;
            _foodCheckTimer = 0;
            return;
        }

        // Stop any use animation/channel that began before the player crossed into Mega Blob.
        Player.itemAnimation = 0;
        Player.itemTime = 0;
        Player.reuseDelay = 0;
        Player.channel = false;

        _megaBlobTime++;
        if (_megaBlobTime < FoodGraceTime || Main.netMode == NetmodeID.MultiplayerClient)
            return;

        _foodCheckTimer++;
        if (_foodCheckTimer < FoodCheckInterval)
            return;
        _foodCheckTimer = 0;

        int foodType = ModContent.NPCType<HomingFood>();
        float rangeSq = NearbyFoodRange * NearbyFoodRange;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.type != foodType || Vector2.DistanceSquared(npc.Center, Player.Center) > rangeSq)
                continue;

            // An existing Food satisfies the safeguard, but make sure it is actually pursuing
            // this trapped player instead of somebody else nearby.
            if (npc.target != Player.whoAmI)
            {
                npc.target = Player.whoAmI;
                npc.netUpdate = true;
            }
            return;
        }

        float side = Main.rand.NextBool() ? 1f : -1f;
        Vector2 spawnPosition = Player.Center + new Vector2(
            side * Main.rand.NextFloat(420f, 620f),
            Main.rand.NextFloat(-260f, -120f)
        );

        NPC.NewNPC(
            Player.GetSource_Misc("MegaBlobHauntedFood"),
            (int)spawnPosition.X,
            (int)spawnPosition.Y,
            foodType,
            Target: Player.whoAmI
        );
    }

    bool IsMegaBlob()
    {
        return Player.TryGetModPlayer(out WgPlayer wg) && wg.Weight.GetStage() >= WeightStage.MegaBlob;
    }
}
