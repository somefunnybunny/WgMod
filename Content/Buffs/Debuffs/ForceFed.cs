using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using WgMod.Common.Players;

namespace WgMod.Content.Buffs.Debuffs;

[Credit(ProjectRole.Programmer, Contributor.maimaichubs)]
[Credit(ProjectRole.Artist, Contributor._d_u_m_m_y_)]
public class ForceFed : ModBuff
{
    public const int TicksPerCycle = 30;
    public const float FatPerCycle = 2f;
    int _cooldown;

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.pvpBuff[Type] = true;
        Main.buffNoSave[Type] = true;
    }

    public override bool ReApply(Player player, int time, int buffIndex)
    {
        _cooldown += TicksPerCycle / 2;
        return false;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        if (!player.TryGetModPlayer(out WgPlayer wg) || !player.TryGetModPlayer(out ForceFedPlayer forceFed))
            return;

        if (_cooldown < TicksPerCycle)
            _cooldown++;
        else
        {
            _cooldown = 0;
            float fatPerCycle = forceFed.GetFatPerCycle();
            wg.CombatWeightText(fatPerCycle, false);
            wg.AddStomach(fatPerCycle);
            SoundEngine.PlaySound(WgSounds.Gulp, player.Center);
        }
    }
}

public class ForceFedPlayer : ModPlayer
{
    float _customFatPerCycle;

    public void AddStackingForceFed(int duration)
    {
        duration = Math.Max(duration, 1);
        int buffType = ModContent.BuffType<ForceFed>();
        int buffIndex = Player.FindBuffIndex(buffType);

        if (buffIndex >= 0)
            Player.buffTime[buffIndex] += duration;
        else
            Player.AddBuff(buffType, duration);
    }

    public void ApplyCustomForceFed(int duration, float fatPerCycle)
    {
        duration = Math.Max(duration, 1);
        fatPerCycle = MathF.Max(0f, fatPerCycle);
        int buffType = ModContent.BuffType<ForceFed>();
        int buffIndex = Player.FindBuffIndex(buffType);

        // Never allow a weaker slime to overwrite a stronger active feeding effect.
        _customFatPerCycle = MathF.Max(_customFatPerCycle, fatPerCycle);

        if (buffIndex >= 0)
        {
            // Slime applications do not stack their full durations, but a new application can
            // refresh a shorter remaining timer to at least the new slime's duration.
            Player.buffTime[buffIndex] = Math.Max(Player.buffTime[buffIndex], duration);
        }
        else
        {
            Player.AddBuff(buffType, duration);
        }
    }

    public float GetFatPerCycle()
    {
        return _customFatPerCycle > 0f ? _customFatPerCycle : ForceFed.FatPerCycle;
    }

    public override void PostUpdateBuffs()
    {
        if (!Player.HasBuff(ModContent.BuffType<ForceFed>()))
            _customFatPerCycle = 0f;
    }
}
