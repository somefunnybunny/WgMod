using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WgMod.Common.Configs;
using WgMod.Content.Buffs.Debuffs;
using WgMod.Content.NPCs.UndergroundDesert;

namespace WgMod.Common.Players;

public class MegaBlobPlayer : ModPlayer
{
    const int FoodGraceTime = 60 * 3;
    const int FoodCheckInterval = 30;
    const float NearbyFoodRange = 2400f;

    int _megaBlobTime;
    int _foodCheckTimer;
    bool _megaHitboxScaled;

    public override bool CanUseItem(Item item)
    {
        return !IsMegaBlob();
    }

    public override void PostUpdate()
    {
        UpdateMegaBlobHitbox();

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

    public override void ModifyScreenPosition()
    {
        if (Player.dead || !Player.TryGetModPlayer(out WgPlayer wg))
            return;

        int stage = wg.Weight.GetStage();
        if (stage < WeightStage.Blob)
            return;

        float visualScale = stage >= WeightStage.MegaBlob
            ? 2f
            : float.Lerp(1f, 2f, Math.Clamp(wg.Weight.GetStageFactor(), 0f, 1f));

        if (Player.TryGetModPlayer(out StrainingPlayer straining) && straining.Stacks > 0)
            visualScale *= straining.SizeFactor;

        // Before the physical hitbox has caught up with visual growth, supply exactly the
        // missing vertical camera shift. Once the taller hitbox exists, its center takes over.
        float physicalScale = _megaHitboxScaled
            ? Player.height / (float)Player.defaultHeight
            : 1f;
        float missingScale = MathF.Max(0f, visualScale - physicalScale);
        Main.screenPosition.Y -= Player.defaultHeight * 0.5f * missingScale * Player.gravDir;
    }

    void UpdateMegaBlobHitbox()
    {
        bool shouldScale = IsMegaBlob()
            && !Player.dead
            && !WgServerConfig.Instance.DisableFatHitbox
            && !Player.mount.Active
            && !Player.isLockedToATile;

        if (shouldScale)
        {
            float strainScale = 1f;
            if (Player.TryGetModPlayer(out StrainingPlayer straining) && straining.Stacks > 0)
                strainScale = straining.SizeFactor;

            int baseWidth = WeightValues.GetHitboxWidthInTiles(WeightStage.MegaBlob) * 16 - 12;
            int targetWidth = Math.Max(1, (int)MathF.Round(baseWidth * strainScale));
            int targetHeight = Math.Max(1, (int)MathF.Round(Player.defaultHeight * 2f * strainScale));
            if (ResizeHitbox(targetWidth, targetHeight))
                _megaHitboxScaled = true;
        }
        else if (_megaHitboxScaled)
        {
            int stage = Player.TryGetModPlayer(out WgPlayer wg) ? wg.Weight.GetStage() : WeightStage.Regular;
            int targetWidth = Player.defaultWidth;
            if (!WgServerConfig.Instance.DisableFatHitbox && !Player.mount.Active && !Player.isLockedToATile)
                targetWidth = WeightValues.GetHitboxWidthInTiles(Math.Min(stage, WeightStage.MegaBlob)) * 16 - 12;

            ResizeHitbox(targetWidth, Player.defaultHeight, true);
            _megaHitboxScaled = false;
        }
    }

    bool ResizeHitbox(int targetWidth, int targetHeight, bool forceShrink = false)
    {
        if (Player.width == targetWidth && Player.height == targetHeight)
            return true;

        float centerX = Player.position.X + Player.width * 0.5f;
        float bottomY = Player.position.Y + Player.height;
        Vector2 targetPosition = new(centerX - targetWidth * 0.5f, bottomY - targetHeight);

        bool shrinking = targetWidth <= Player.width && targetHeight <= Player.height;
        if (!forceShrink && !shrinking && Collision.SolidCollision(targetPosition, targetWidth, targetHeight))
            return false;

        Player.position = targetPosition;
        Player.width = targetWidth;
        Player.height = targetHeight;
        return true;
    }

    bool IsMegaBlob()
    {
        return Player.TryGetModPlayer(out WgPlayer wg) && wg.Weight.GetStage() >= WeightStage.MegaBlob;
    }
}
