using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace WgMod.Common.Players;

public class FatDeathResetPlayer : ModPlayer
{
    bool _resetFatOnDeath;

    public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
    {
        _resetFatOnDeath = Player.TryGetModPlayer(out WgPlayer wg) && wg.Weight.GetStage() > WeightStage.Regular;
        return true;
    }

    public override void UpdateDead()
    {
        if (!_resetFatOnDeath || !Player.TryGetModPlayer(out WgPlayer wg))
            return;

        wg.SetWeightForced(Weight.Base, false);
        wg.SetStomachForced(0f, false);
        _resetFatOnDeath = false;
    }
}
