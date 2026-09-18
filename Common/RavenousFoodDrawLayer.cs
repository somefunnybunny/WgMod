using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using WgMod.Content.Buffs.Debuffs;

namespace WgMod.Common;

public class RavenousFoodDrawLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition()
    {
        return new AfterParent(PlayerDrawLayers.HeldItem);
    }

    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => true;

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;
        RavenousPlayer ravenous = player.GetModPlayer<RavenousPlayer>();
        if (!ravenous.Active || drawInfo.ShouldHidePlayer())
            return;

        Texture2D texture = TextureAssets.Item[ItemID.ChocolateChipCookie].Value;
        Vector2 handPosition = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, ravenous.EatingArmRotation);
        handPosition.Y += player.gfxOffY;

        DrawData data = new(
            texture,
            handPosition - Main.screenPosition,
            null,
            Color.White,
            0f,
            new Vector2(texture.Width, texture.Height) * 0.5f,
            1f,
            player.direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
            0
        );

        drawInfo.DrawDataCache.Add(data);
    }
}
