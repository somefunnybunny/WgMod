using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using WgMod.Common.Players;

namespace WgMod.Content.Buffs.Debuffs;

public enum StrainingSource
{
    Generic,
    ForceFed,
    PrismaticStuffing,
    Bloated,
    SugarSpirit
}

public class Straining : ModBuff
{
    public override string Texture => "WgMod/Content/Buffs/Debuffs/Bloated";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.pvpBuff[Type] = true;
        Main.buffNoSave[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
    }

    public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
    {
        int stacks = Main.LocalPlayer.TryGetModPlayer(out StrainingPlayer straining) ? straining.Stacks : 0;
        buffName = stacks > 0 ? $"Straining x{stacks}" : "Straining";
        tip = stacks switch
        {
            1 => "I'm pushing past what my body can comfortably hold...",
            2 => "The pressure is getting harder to ignore. I can feel myself straining.",
            3 => "I'm stretched frighteningly tight now, and every bit more feels dangerous.",
            4 => "I'm under enormous pressure... my body is struggling to keep itself together.",
            5 => "I feel ready to burst. I really shouldn't take much more of this...",
            _ => "I'm at my absolute limit. One more serious strain could make me burst.",
        };
    }

    public override void Update(Player player, ref int buffIndex)
    {
        if (!player.TryGetModPlayer(out StrainingPlayer straining) || straining.Stacks <= 0)
            return;

        float lifeFactor = 1f + 0.15f * straining.Stacks;
        float defenseFactor = MathF.Max(0f, 1f - 0.20f * straining.Stacks);

        player.statLifeMax2 = Math.Max(1, (int)MathF.Floor(player.statLifeMax2 * lifeFactor));
        player.statDefense *= defenseFactor;
        player.buffTime[buffIndex] = 2;
    }
}

public class StrainingPlayer : ModPlayer
{
    public const int MaxStacks = 7;
    public const float FedMassPerStack = 20f;
    public const int BloatedTimePerStack = 60 * 25;
    public const int SugarSpiritsPerStack = 3;
    public const float SizePerStack = 0.05f;

    float _fedMassPressure;
    int _bloatedTimePressure;
    int _sugarSpiritPressure;
    bool _exploded;
    bool _hitboxScaled;
    int _baseHitboxWidth;
    int _baseHitboxHeight;

    public int Stacks { get; private set; }
    public float SizeFactor => 1f + SizePerStack * Stacks;

    public void AddFedMass(float mass, StrainingSource source)
    {
        if (mass <= 0f || !IsBlobbed())
            return;

        _fedMassPressure += mass;
        while (_fedMassPressure >= FedMassPerStack && Stacks < MaxStacks)
        {
            _fedMassPressure -= FedMassPerStack;
            AddStack(source);
            if (_exploded)
                return;
        }
    }

    public void AddBloatedTime(int time)
    {
        if (time <= 0 || !IsBlobbed())
            return;

        _bloatedTimePressure += time;
        while (_bloatedTimePressure >= BloatedTimePerStack && Stacks < MaxStacks)
        {
            _bloatedTimePressure -= BloatedTimePerStack;
            AddStack(StrainingSource.Bloated);
            if (_exploded)
                return;
        }
    }

    public void AddSugarSpiritPossession()
    {
        if (!IsBlobbed())
            return;

        _sugarSpiritPressure++;
        while (_sugarSpiritPressure >= SugarSpiritsPerStack && Stacks < MaxStacks)
        {
            _sugarSpiritPressure -= SugarSpiritsPerStack;
            AddStack(StrainingSource.SugarSpirit);
            if (_exploded)
                return;
        }
    }

    public override void PostUpdateBuffs()
    {
        if (Player.dead)
        {
            ResetAll();
            return;
        }

        if (!IsBlobbed())
        {
            ResetAll();
            return;
        }

        if (Stacks > 0 && !Player.HasBuff(ModContent.BuffType<Straining>()))
            Player.AddBuff(ModContent.BuffType<Straining>(), 2);
    }

    public override void PostUpdate()
    {
        UpdateScaledHitbox();
    }

    void UpdateScaledHitbox()
    {
        if (Stacks > 0 && !Player.mount.Active && !Player.isLockedToATile)
        {
            if (!_hitboxScaled)
            {
                _baseHitboxWidth = Player.width;
                _baseHitboxHeight = Player.height;
                _hitboxScaled = true;
            }

            int targetWidth = Math.Max(1, (int)MathF.Round(_baseHitboxWidth * SizeFactor));
            int targetHeight = Math.Max(1, (int)MathF.Round(_baseHitboxHeight * SizeFactor));
            ResizeHitbox(targetWidth, targetHeight);
        }
        else if (_hitboxScaled)
        {
            ResizeHitbox(_baseHitboxWidth, _baseHitboxHeight, true);
            _hitboxScaled = false;
        }
    }

    void ResizeHitbox(int targetWidth, int targetHeight, bool forceShrink = false)
    {
        if (Player.width == targetWidth && Player.height == targetHeight)
            return;

        float centerX = Player.position.X + Player.width * 0.5f;
        float bottomY = Player.position.Y + Player.height;
        Vector2 targetPosition = new(centerX - targetWidth * 0.5f, bottomY - targetHeight);

        bool shrinking = targetWidth <= Player.width && targetHeight <= Player.height;
        if (forceShrink || shrinking || !Collision.SolidCollision(targetPosition, targetWidth, targetHeight))
        {
            Player.position = targetPosition;
            Player.width = targetWidth;
            Player.height = targetHeight;
        }
    }

    void AddStack(StrainingSource source)
    {
        Stacks++;
        if (Stacks >= MaxStacks)
        {
            Explode(source);
            return;
        }

        Player.AddBuff(ModContent.BuffType<Straining>(), 2);
    }

    bool IsBlobbed()
    {
        return Player.TryGetModPlayer(out WgPlayer wg) && wg.Weight.GetStage() >= WeightStage.Blob;
    }

    void Explode(StrainingSource source)
    {
        if (_exploded || Player.dead)
            return;

        _exploded = true;
        SoundEngine.PlaySound(SoundID.Item14, Player.Center);

        for (int i = 0; i < 120; i++)
        {
            Dust dust = Dust.NewDustDirect(
                Player.Center - new Vector2(96f),
                192,
                192,
                i % 3 == 0 ? DustID.Torch : DustID.Smoke,
                Main.rand.NextFloat(-10f, 10f),
                Main.rand.NextFloat(-10f, 10f),
                100,
                default,
                Main.rand.NextFloat(1.8f, 4f)
            );
            dust.noGravity = Main.rand.NextBool();
        }

        string reason = source switch
        {
            StrainingSource.ForceFed => Player.name + " was force-fed far past their breaking point.",
            StrainingSource.PrismaticStuffing => Player.name + " burst in a spectacular shower of prismatic stuffing.",
            StrainingSource.Bloated => Player.name + " couldn't contain the pressure any longer.",
            StrainingSource.SugarSpirit => Player.name + " was possessed by one Sugar Spirit too many.",
            _ => Player.name + " finally burst from the strain."
        };

        Player.KillMe(
            PlayerDeathReason.ByCustomReason(reason),
            99999d,
            0
        );
    }

    void ResetAll()
    {
        Stacks = 0;
        _fedMassPressure = 0f;
        _bloatedTimePressure = 0;
        _sugarSpiritPressure = 0;
        _exploded = false;
    }
}
