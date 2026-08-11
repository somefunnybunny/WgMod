using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace WgMod.Common.Players;

public class HeavyFootstepPlayer : ModPlayer
{
    float _stepDistance;
    int _screenShakeTime;
    int _screenShakeMagnitude;

    public override void PostUpdate()
    {
        if (!Player.TryGetModPlayer(out WgPlayer wg) || !wg.OwnsPlayer() || Player.dead)
        {
            ResetStepTracking();
            return;
        }

        int stage = wg.Weight.GetStage();
        if (stage < WeightStage.Obese || Player.mount.Active || Player.velocity.Y != 0f || Player.wet)
        {
            ResetStepTracking();
            return;
        }

        float horizontalSpeed = MathF.Abs(Player.velocity.X);
        if (horizontalSpeed < 0.05f)
        {
            _stepDistance = 0f;
            return;
        }

        _stepDistance += horizontalSpeed;

        // Heavier stages take shorter, more forceful steps, so impacts happen slightly more often.
        float strideDistance = MathF.Max(24f, 44f - (stage - WeightStage.Obese) * 4f);
        if (_stepDistance < strideDistance)
            return;

        _stepDistance %= strideDistance;
        DoHeavyStep(stage);
    }

    void DoHeavyStep(int stage)
    {
        int stageOffset = Math.Max(0, stage - WeightStage.Obese);
        float volume = MathF.Min(1.35f, 0.7f + stageOffset * 0.1f);
        SoundEngine.PlaySound(WgSounds.Stomp.WithVolumeScale(volume), Player.Bottom);

        int dustCount = 5 + stageOffset * 3;
        float dustWidth = MathF.Max(Player.width, 28f + stageOffset * 12f);
        Vector2 dustOrigin = new(Player.Center.X - dustWidth * 0.5f, Player.Bottom.Y - 4f);

        for (int i = 0; i < dustCount; i++)
        {
            float x = Main.rand.NextFloat(dustWidth);
            Vector2 position = dustOrigin + new Vector2(x, 0f);
            int dust = Dust.NewDust(position, 4, 4, DustID.Smoke, Player.velocity.X * 0.15f, -0.8f - stageOffset * 0.08f, 100);
            Main.dust[dust].scale = 0.8f + stageOffset * 0.12f;
            Main.dust[dust].velocity.X += Main.rand.NextFloat(-0.8f, 0.8f);
        }

        // Obese has audiovisual weight, while stages above it begin shaking the camera.
        if (stage > WeightStage.Obese)
        {
            _screenShakeTime = Math.Max(_screenShakeTime, 4 + stageOffset);
            _screenShakeMagnitude = Math.Max(_screenShakeMagnitude, 1 + stageOffset);
        }
    }

    public override void ModifyScreenPosition()
    {
        if (_screenShakeTime <= 0 || Player.whoAmI != Main.myPlayer)
            return;

        _screenShakeTime--;
        Main.screenPosition += new Vector2(
            Main.rand.Next(-_screenShakeMagnitude, _screenShakeMagnitude + 1),
            Main.rand.Next(-_screenShakeMagnitude, _screenShakeMagnitude + 1));

        if (_screenShakeTime <= 0)
            _screenShakeMagnitude = 0;
    }

    void ResetStepTracking()
    {
        _stepDistance = 0f;
    }
}
