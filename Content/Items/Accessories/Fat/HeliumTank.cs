using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WgMod.Common.Players;
using WgMod.Content.Buffs.Debuffs;

namespace WgMod.Content.Items.Accessories.Fat;

[Credit(ProjectRole.Programmer, Contributor.jumpsu2)]
[Credit(ProjectRole.Artist, Contributor.jumpsu2)]
public class HeliumTank : ModItem
{
    WgStat _gravity = new(1f, 0.1f);

    public override void SetDefaults()
    {
        Item.width = 24;
        Item.height = 32;

        Item.accessory = true;
        Item.rare = ItemRarityID.LightRed;
        Item.value = Item.buyPrice(gold: 2, silver: 25);
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        if (!player.TryGetModPlayer(out WgPlayer wg))
            return;

        float immobility = wg.Weight.ClampedImmobility;
        _gravity.Lerp(immobility);
        player.gravity *= _gravity;

        if (player.TryGetModPlayer(out HeliumTankPlayer heliumTank))
            heliumTank.Equipped = true;
    }
}

public class HeliumTankPlayer : ModPlayer
{
    const int PumpInterval = 60;
    const int BloatedTimePerPump = 60 * 3;

    int _pumpTimer;

    public bool Equipped { get; set; }

    public override void ResetEffects()
    {
        Equipped = false;
    }

    public override void PostUpdateEquips()
    {
        if (!Equipped || Player.dead)
        {
            _pumpTimer = 0;
            return;
        }

        _pumpTimer++;
        if (_pumpTimer < PumpInterval)
            return;

        _pumpTimer = 0;
        if (Player.TryGetModPlayer(out BloatedPlayer bloated))
            bloated.ApplyBloated(BloatedTimePerPump);
    }
}

public class SellHeliumTank : GlobalNPC
{
    public override void ModifyShop(NPCShop shop)
    {
        if (shop.NpcType == NPCID.PartyGirl)
            shop.Add<HeliumTank>();
    }
}
