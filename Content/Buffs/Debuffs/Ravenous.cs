using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WgMod.Common.Players;

namespace WgMod.Content.Buffs.Debuffs;

public class Ravenous : ModBuff
{
    public override string Texture => "WgMod/Content/Buffs/Debuffs/ForceFed";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.pvpBuff[Type] = true;
        Main.buffNoSave[Type] = true;
        BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        BuffID.Sets.TimeLeftDoesNotDecrease[Type] = true;
    }
}

public class RavenousPlayer : ModPlayer
{
    public const int PulseTicks = 15;
    public const float FeedPerPulse = 2f;

    Mass _remainingFeed;
    int _pulseTimer;
    Item _visualFood;

    public bool Active => _remainingFeed > 0f && Player.HasBuff(ModContent.BuffType<Ravenous>());

    public static void Start(Player player, Mass amount)
    {
        if (amount <= 0f)
            return;

        player.GetModPlayer<RavenousPlayer>().AddFeed(amount);
    }

    public void AddFeed(Mass amount)
    {
        if (amount <= 0f)
            return;

        _remainingFeed += amount;
        Player.AddBuff(ModContent.BuffType<Ravenous>(), 2);
    }

    public override bool CanUseItem(Item item)
    {
        return !Active;
    }

    public override bool PreItemCheck()
    {
        if (!Active)
            return true;

        _visualFood ??= new Item(ItemID.Apple);

        if (Player.itemAnimation <= 1)
        {
            Player.lastVisualizedSelectedItem = _visualFood;
            Player.ApplyItemAnimation(_visualFood);
        }

        return true;
    }

    public override void PostUpdateBuffs()
    {
        if (Player.dead)
        {
            Clear();
            return;
        }

        if (_remainingFeed <= 0f)
        {
            Clear();
            return;
        }

        Player.AddBuff(ModContent.BuffType<Ravenous>(), 2);

        _pulseTimer++;
        if (_pulseTimer < PulseTicks)
            return;

        _pulseTimer = 0;
        Mass pulse = MathF.Min(FeedPerPulse, _remainingFeed.Value);
        _remainingFeed -= pulse;

        if (Player.whoAmI == Main.myPlayer && Player.TryGetModPlayer(out WgPlayer wg))
        {
            wg.CombatWeightText(pulse, false);
            wg.AddStomach(pulse);
            SoundEngine.PlaySound(WgSounds.Gulp, Player.Center);
        }

        if (_remainingFeed <= 0f)
            Clear();
    }

    public override void UpdateDead()
    {
        Clear();
    }

    void Clear()
    {
        _remainingFeed = 0f;
        _pulseTimer = 0;
        Player.ClearBuff(ModContent.BuffType<Ravenous>());
    }
}
