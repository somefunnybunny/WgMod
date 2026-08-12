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
        tip = "That sting left a warm heaviness inside me... and somehow I know it isn't going to stop until there's far more of me.";
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
        Say("That sting felt... different. There's this warm little heaviness inside me now. Huh...");
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
            case 60 * 5:
                SoundEngine.PlaySound(SoundID.Item2, Player.Center);
                Say("That warmth is spreading. It feels less like swelling and more like... weight waiting to happen.");
                break;
            case 60 * 10:
                SoundEngine.PlaySound(WgSounds.Belly, Player.Center);
                Say("My clothes already feel a little tighter. If this is really going to make me bigger... I hope plenty of it settles into my hips and backside.");
                break;
            case 60 * 15:
                SoundEngine.PlaySound(WgSounds.Belly, Player.Center);
                Say("I'm definitely getting heavier. I can feel it pulling lower every minute... and some part of me is starting to look forward to the next surge.");
                break;
            case 60 * 19:
                SoundEngine.PlaySound(WgSounds.Belly, Player.Center);
                Say("I don't think this is going to wear off on its own. Whatever that bee started has already passed the point where I can stop it.");
                break;
            case FatteningStartTime:
                SoundEngine.PlaySound(WgSounds.Belly, Player.Center);
                Say("There it is... another heavy push. This isn't bloat at all. It's just making me fatter... and it isn't stopping.");
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
            Say("I'm enormous... far beyond Blob, completely stuck, and that strange feeling finally went quiet. It really wasn't going to stop until it got me here.");
            ResetSequence();
            return;
        }

        _fatteningTimer++;
        if (_fatteningTimer < FatteningInterval)
            return;
        _fatteningTimer = 0;
        _fatteningPulses++;

        int oldStage = wg.Weight.GetStage();
        Mass added = wg.AddWeight(FatPerPulse);
        int newStage = wg.Weight.GetStage();

        SoundEngine.PlaySound(WgSounds.Belly, Player.Center);
        wg.Jiggle(1.8f + System.MathF.Min(_fatteningPulses * 0.08f, 2f));

        if (Player.whoAmI == Main.myPlayer && !Main.dedServ)
        {
            string pulseText = _fatteningPulses switch
            {
                <= 8 => "*heavier...*",
                <= 20 => "*fatter... heavier...*",
                <= 36 => "*HEAVIER...*",
                _ => "*MUCH HEAVIER...*",
            };
            CombatText.NewText(Player.Hitbox, Color.MediumPurple, pulseText, dramatic: _fatteningPulses >= 30);
        }

        if (newStage > oldStage)
            StageMessage(newStage);
        else
        {
            switch (_fatteningPulses)
            {
                case 6:
                    Say("It keeps coming in steady waves. I'm not just puffing up... every bit of this is staying on me.");
                    break;
                case 14:
                    Say("I'm getting wider every minute. My hips feel so much heavier now... and there's still no sign of it slowing down.");
                    break;
                case 24:
                    Say("This has gone way past something I could shrug off later. I'm carrying all of it now, and another surge is already building.");
                    break;
                case 36:
                    Say("I'm so huge already... but it still wants more. I can feel exactly where this ends now, and there's nothing left to do but let it take me there.");
                    break;
            }
        }

        if (added <= 0f && wg.Weight.GetStage() < WeightStage.MegaBlob && _fatteningPulses % 10 == 0)
            Say("The pressure keeps trying to add more weight, even if something is fighting it. It doesn't feel willing to give up.");
    }

    void StageMessage(int stage)
    {
        string text = stage switch
        {
            WeightStage.Overweight => "Another stage already... I'm getting properly heavy now.",
            WeightStage.Fat => "I'm undeniably fat now... and it still keeps adding more.",
            WeightStage.Obese => "I'm huge. Every surge makes my hips and backside feel heavier than the last.",
            WeightStage.MorbidlyObese => "This is getting absurd... there's so much of me now, and the next wave is already starting.",
            WeightStage.BarelyMobile => "Moving is getting difficult. That should scare me more than it does.",
            WeightStage.Immobile => "I can barely move anymore... but the strange feeling still isn't satisfied.",
            WeightStage.Encumbered => "I'm completely overwhelmed by my own weight now. It still wants to make me bigger.",
            WeightStage.Blob => "I've reached Blob... and it still didn't stop. So that wasn't the destination after all.",
            WeightStage.MegaBlob => "I'm enormous... far beyond Blob. That was the point it was dragging me toward all along.",
            _ => "I'm getting heavier again...",
        };

        Say(text);
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
