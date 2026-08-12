using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WgMod.Common.Players;
using WgMod.Common.Systems;

namespace WgMod.Content.Buffs.Debuffs;

public class StrangeFeeling : ModBuff
{
    public override string Texture => "WgMod/Content/Buffs/Debuffs/Bloated";

    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        Main.pvpBuff[Type] = true;
        Main.buffNoSave[Type] = true;
    }

    public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
    {
        buffName = "Strange Feeling";
        tip = "That sting left something warm and busy inside me... like my body has started making food for itself.";
    }

    public override void Update(Player player, ref int buffIndex)
    {
        if (player.TryGetModPlayer(out StrangeFeelingPlayer strangeFeeling))
            strangeFeeling.Activate();
    }
}

public class StrangeFeelingPlayer : ModPlayer
{
    const int FatteningStartTime = 60 * 22;
    const int FatteningInterval = 60;
    const float FatPerPulse = 18f;

    bool _active;
    int _timer;
    int _fatteningTimer;
    int _fatteningPulses;

    public bool Active => _active;

    public void Activate()
    {
        if (_active)
            return;

        _active = true;
        _timer = 0;
        _fatteningTimer = 0;
        _fatteningPulses = 0;
        SoundEngine.PlaySound(SoundID.NPCHit1, Player.Center);
        Say("That sting felt... different. There's something warm churning inside me now. Huh...");
    }

    public override void PostUpdate()
    {
        if (!_active)
            return;

        if (Player.dead)
        {
            ResetSequence();
            return;
        }

        _timer++;
        switch (_timer)
        {
            case 60 * 10:
                SoundEngine.PlaySound(WgSounds.Belly, Player.Center);
                Say("Hrrp... my stomach keeps making noises even though I haven't eaten anything. It almost feels like something in there is making food on its own...");
                break;
            case 60 * 19:
                SoundEngine.PlaySound(WgSounds.Belly, Player.Center);
                Say("BUURP... okay, that's impossible. I can actually feel myself being fed from the inside. Whatever that bee started isn't waiting for me to eat anymore.");
                break;
            case FatteningStartTime:
                SoundEngine.PlaySound(WgSounds.Belly, Player.Center);
                Say("HUUURP... there it goes again. My own body is turning into a food factory... and I don't think it's going to stop feeding me until I'm completely helpless.");
                break;
        }

        if (_timer < FatteningStartTime)
            return;

        if (!Player.TryGetModPlayer(out WgPlayer wg))
            return;

        if (wg.Weight.GetStage() >= WeightStage.MegaBlob)
        {
            SoundEngine.PlaySound(WgSounds.Belly, Player.Center);
            wg.Jiggle(5f);
            Say("BUUUUUURRRP... it finally stopped. I'm beyond Blob, completely helpless... I guess this is what that little factory inside me was trying to make all along.");
            ResetSequence();
            return;
        }

        _fatteningTimer++;
        if (_fatteningTimer < FatteningInterval)
            return;
        _fatteningTimer = 0;
        _fatteningPulses++;

        int oldStage = wg.Weight.GetStage();
        wg.AddWeight(FatPerPulse);
        int newStage = wg.Weight.GetStage();

        SoundEngine.PlaySound(WgSounds.Belly, Player.Center);
        wg.Jiggle(1.8f + System.MathF.Min(_fatteningPulses * 0.08f, 2f));

        if (Player.whoAmI == Main.myPlayer && !Main.dedServ)
        {
            string pulseText = _fatteningPulses switch
            {
                <= 10 => "*urp*",
                <= 24 => "*BUURP*",
                <= 40 => "*HUUURRRP*",
                _ => "*BUUUUUURRRP*",
            };
            CombatText.NewText(Player.Hitbox, Color.MediumPurple, pulseText, dramatic: _fatteningPulses >= 36);
        }

        // Keep the spoken narration sparse. The burps and visible growth carry most pulses.
        switch (_fatteningPulses)
        {
            case 12:
                Say("Hrrp... it just keeps producing more. I can feel every fresh batch settling onto me before the next one is even ready.");
                break;
            case 28:
                Say("BUUURRRP... I'm huge already, and that thing inside me is still working. It doesn't care how much room I have left... it just keeps feeding me.");
                break;
            case 44:
                Say("HUUUUURRRP... I can barely move now. The factory is still churning, still feeding... it's really going to keep going until there's nothing left for me to do but sit here and take it.");
                break;
        }

        if (newStage == WeightStage.Blob && oldStage < WeightStage.Blob)
            Say("BUUUURRRP... Blob already... and it's still making more. So even this isn't where it plans to stop.");
    }

    public override void UpdateDead()
    {
        ResetSequence();
    }

    public override void OnRespawn()
    {
        ResetSequence();
    }

    void Say(string text)
    {
        if (Player.whoAmI == Main.myPlayer && !Main.dedServ)
            Main.NewText(text, Color.MediumPurple);
    }

    void ResetSequence()
    {
        _active = false;
        _timer = 0;
        _fatteningTimer = 0;
        _fatteningPulses = 0;

        int index = Player.FindBuffIndex(ModContent.BuffType<StrangeFeeling>());
        if (index >= 0)
            Player.DelBuff(index);
    }
}
