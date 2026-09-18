using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WgMod.Common.Players;

namespace WgMod.Content.Buffs;

public class SoulWeightBuff : WgBuffBase
{
    public override string Texture => "WgMod/Content/Buffs/FatBuff";

    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = true;
        Main.buffNoSave[Type] = true;
    }

    public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
    {
        if (!Main.LocalPlayer.TryGetModPlayer(out WgPlayer wg))
            return;

        tip = base.Description.Format(wg.SoulWeight.Mass.Display());
    }

    public override float GetProgress(WgPlayer wg, int buffIndex)
    {
        return wg.SoulWeight.GetStageFactor();
    }

    public override bool RightClick(int buffIndex)
    {
        return false;
    }
}
