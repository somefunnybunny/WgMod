using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace WgMod.Common.Players;

public class HeavyFootstepPlayer : ModPlayer
{
    int _lastLegFrame = -1;
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
        if (stage < WeightStage.Obese || Player.mount.Active || Player.velocity.Y != 0f || Player.wet || MathF.Abs(Player.velocity.X) < 0.05f)
        {
            ResetStepTracking();
            return;
        }

        if (Player.legFrame.Height <= 0)
        {
            ResetStepTracking();
            return;
        }

        int legFrame = Player.legFrame.Y / Player.legFrame.Height;
        if (legFrame == _lastLegFrame)
            return;

        _lastLegFrame = legFrame;

        // Vanilla grounded walking cycles through the leg animation frames. These two frames
        // correspond to the alternating planted-foot portions of the normal walk cycle, so the
        // impact is driven by the visible animation rather than accumulated movement distance.
        if (legFrame == 10 || legFrame == 17)
            DoHeavyStep(stage);
    }

    void DoHeavyStep(int stage)
    {
        int stageOffset = Math.Clamp(stage - WeightStage.Obese, 0, WeightStage.Blob - WeightStage.Obese);

        // Keep the existing stomp samples, but make the lower stages read as heavy footsteps
        // rather than full impacts. The sound gradually becomes deeper and louder with size.
        float[] volumes = { 0.25f, 0.35f, 0.5f, 0.75f, 0.95f, 1.1f };
        float[] pitches = { 0.35f, 0.25f, 0.12f, 0f, -0.08f, -0.15f };
        SoundEngine.PlaySound(
            WgSounds.Stomp.WithVolumeScale(volumes[stageOffset]).WithPitchOffset(pitches[stageOffset]),
            Player.Bottom);

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
        _lastLegFrame = -1;
    }
}
