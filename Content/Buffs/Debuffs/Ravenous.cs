using System;
using Microsoft.Xna.Framework;
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
    public const int BiteCycleTicks = 60;
    public const int CrunchTick = BiteCycleTicks / 2;
    public const float FeedPerBite = 5f;

    Mass _remainingFeed;
    int _biteTimer;
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

        bool wasActive = _remainingFeed > 0f;
        _remainingFeed += amount;
        Player.channel = false;
        Player.itemAnimation = 0;
        Player.itemTime = 0;
        if (!wasActive)
            _biteTimer = 0;
        Player.AddBuff(ModContent.BuffType<Ravenous>(), 2);
    }

    public override bool CanUseItem(Item item)
    {
        return !Active;
    }

    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
    {
        if (Main.netMode != NetmodeID.Server || _remainingFeed <= 0f)
            return;

        ModPacket packet = Mod.GetPacket(WgMod.MessageType.RavenousStart);
        packet.Write((byte)Player.whoAmI);
        packet.Write(_remainingFeed.Value);
        packet.Send(toWho, fromWho);
    }

    public override void SetControls()
    {
        if (Active)
            Player.controlUseItem = false;
    }

    public override bool PreItemCheck()
    {
        if (!Active)
            return true;

        EnsureVisualFood();
        Player.lastVisualizedSelectedItem = _visualFood;

        if (Player.itemAnimationMax != BiteCycleTicks)
            Player.ApplyItemAnimation(_visualFood);

        Player.itemAnimationMax = BiteCycleTicks;
        Player.itemAnimation = Math.Max(BiteCycleTicks - _biteTimer, 1);

        Rectangle heldItemFrame = Item.GetDrawHitbox(_visualFood.type, Player);
        Player.ItemCheck_ApplyUseStyle(Player.HeightOffsetHitboxCenter, _visualFood, heldItemFrame);

        // Ravenous owns the item animation while active. Skipping vanilla ItemCheck
        // prevents the actually selected weapon/item from firing off the forced animation.
        return false;
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

        _biteTimer++;

        if (_biteTimer == CrunchTick && Player.whoAmI == Main.myPlayer)
            PlayCrunch();

        if (_biteTimer < BiteCycleTicks)
            return;

        _biteTimer = 0;
        Mass bite = MathF.Min(FeedPerBite, _remainingFeed.Value);
        _remainingFeed -= bite;

        if (Player.whoAmI == Main.myPlayer && Player.TryGetModPlayer(out WgPlayer wg))
        {
            wg.CombatWeightText(bite, false);
            wg.AddStomach(bite);
            SoundEngine.PlaySound(WgSounds.Gulp, Player.Center);
        }

        if (_remainingFeed <= 0f)
            Clear();
    }

    public override void UpdateDead()
    {
        Clear();
    }

    void EnsureVisualFood()
    {
        if (_visualFood != null)
            return;

        _visualFood = new Item(ItemID.ChocolateChipCookie)
        {
            useAnimation = BiteCycleTicks,
            useTime = BiteCycleTicks,
            autoReuse = false
        };
    }

    void PlayCrunch()
    {
        EnsureVisualFood();

        if (_visualFood.UseSound.HasValue)
            SoundEngine.PlaySound(_visualFood.UseSound.Value, Player.Center);

        Vector2 mouth = Player.MouthPosition.Value + new Vector2(Player.direction * 4f, 0f);
        for (int i = 0; i < 6; i++)
        {
            Vector2 velocity = new(Player.direction * Main.rand.NextFloat(0.4f, 1.8f), Main.rand.NextFloat(-1.4f, 0.4f));
            Dust.NewDustPerfect(mouth, DustID.Dirt, velocity, 0, new Color(139, 90, 43), Main.rand.NextFloat(0.7f, 1.05f));
        }
    }

    void Clear()
    {
        _remainingFeed = 0f;
        _biteTimer = 0;
        Player.itemAnimation = 0;
        Player.itemTime = 0;
        Player.ClearBuff(ModContent.BuffType<Ravenous>());
    }
}
