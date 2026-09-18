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

    void PlayCrunch()
    {
        Item cookie = new(ItemID.ChocolateChipCookie);
        if (cookie.UseSound.HasValue)
            SoundEngine.PlaySound(cookie.UseSound.Value, Player.Center);

        Vector2 mouth = Player.MouthPosition.Value + new Vector2(Player.direction * 4f, 0f);
        for (int i = 0; i < 6; i++)
        {
            Vector2 velocity = new(Player.direction * Main.rand.NextFloat(0.4f, 1.8f), Main.rand.NextFloat(-1.4f, 0.4f));
            Dust.NewDustPerfect(mouth, DustID.Dirt, velocity, 0, new Color(139, 90, 43), Main.rand.NextFloat(0.7f, 1.05f));
        }
    }

    public float BiteProgress => Math.Clamp(_biteTimer / (float)BiteCycleTicks, 0f, 1f);

    public float EatingArmRotation
    {
        get
        {
            float raise = MathF.Sin(BiteProgress * MathF.PI);
            return -Player.direction * float.Lerp(0.15f, 1.15f, raise);
        }
    }

    public override void PostUpdate()
    {
        if (Active)
            Player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, EatingArmRotation);
    }

    void Clear()
    {
        _remainingFeed = 0f;
        _biteTimer = 0;
        Player.ClearBuff(ModContent.BuffType<Ravenous>());
    }
}
