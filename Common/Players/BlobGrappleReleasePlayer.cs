using Terraria;
using Terraria.ModLoader;

namespace WgMod.Common.Players;

/// <summary>
/// Blob-stage arms are intentionally unusable, so an already-latched grappling hook should not let
/// the player keep a mobility state they could no longer initiate normally.
/// </summary>
public class BlobGrappleReleasePlayer : ModPlayer
{
    bool _wasBlob;

    public override void PostUpdate()
    {
        bool isBlob = Player.Wg().Weight.GetStage() >= WeightStage.Blob;

        if (isBlob && (!_wasBlob || Player.grapCount > 0))
            Player.RemoveAllGrapplingHooks();

        _wasBlob = isBlob;
    }

    public override void UpdateDead()
    {
        _wasBlob = false;
    }
}
