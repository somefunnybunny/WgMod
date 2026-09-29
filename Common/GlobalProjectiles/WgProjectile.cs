using Terraria;
using Terraria.ModLoader;
using WgMod.Common.Configs;
using WgMod.Common.Players;

namespace WgMod.Common.GlobalProjectiles;

public class WgProjectile : GlobalProjectile
{
    public override bool? CanUseGrapple(int type, Player player)
    {
        if (!player.TryGetModPlayer(out WgPlayer wg))
            return null;
        if (WgServerConfig.Instance.DisableFatBuffs || wg.Weight.GetStage() < WeightStage.Blob)
            return null;

        wg.TriggerBlobArmSwing();
        return false;
    }
}
