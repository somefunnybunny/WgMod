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
        tip = "That sting left a warm pressure in my stomach... and for some reason I keep wondering where all that swelling is going to end up.";
    }

    public override void Update(Player player, ref int buffIndex)
    {
        if (player.TryGetModPlayer(out StrangeFeelingPlayer strangeFeeling))
            strangeFeeling.Activate();
    }
}

public class StrangeFeelingPlayer : ModPlayer
{
    const int BloatingStartTime = 60 * 22;
    const int BloatingInterval = 45;

    bool _active;
    int _timer;
    int _bloatingTimer;
    int _bloatingPulses;

    public bool Active => _active;

    public void Activate()
    {
        if (_active)
            return;

        _active = true;
        _timer = 0;
        _bloatingTimer = 0;
        _bloatingPulses = 0;
        SoundEngine.PlaySound(SoundID.NPCHit1, Player.Center);
        Say("That sting felt... different. My stomach feels warm. Full, almost. Huh...");
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
                Say("Urf... that warmth is turning into pressure. I keep needing to burp, but... it actually feels kind of nice.");
                break;
            case 60 * 10:
                SoundEngine.PlaySound(WgSounds.Belly, Player.Center);
                Say("Hrrp... okay, I'm definitely swelling. If all this has to go somewhere... I wouldn't mind if most of it settled into my hips and backside.");
                break;
            case 60 * 15:
                SoundEngine.PlaySound(WgSounds.Belly, Player.Center);
                Say("HUUURP... ngh... I'm getting bigger fast. My stomach is so gassy, but I can't stop thinking about how much wider and heavier my rear could get.");
                break;
            case 60 * 19:
                SoundEngine.PlaySound(WgSounds.Belly, Player.Center);
                Say("BUUURRRP... I don't think I can stop this anymore... and I don't think I want to. If I'm going to balloon up, then let as much of it as possible pile onto my hips and backside.");
                break;
            case BloatingStartTime:
                SoundEngine.PlaySound(WgSounds.Belly, Player.Center);
                Say("HUUUUURRRP... th-there it is... I'm swelling for real now. Keep going... *burp*... lower... wider... bigger...");
                break;
        }

        if (_timer < BloatingStartTime)
            return;

        if (Player.TryGetModPlayer(out WgPlayer wg) && wg.Weight.GetStage() >= WeightStage.MegaBlob)
        {
            SoundEngine.PlaySound(WgSounds.Belly, Player.Center);
            wg.Jiggle(5f);
            Say("BUUUUUUUURRRRRP... I-I'm enormous... *HUUURP*... my hips, my backside... there's so much of me now... and that strange feeling finally feels satisfied.");
            ResetSequence();
            return;
        }

        _bloatingTimer++;
        if (_bloatingTimer < BloatingInterval)
            return;
        _bloatingTimer = 0;
        _bloatingPulses++;

        SoundEngine.PlaySound(WgSounds.Belly, Player.Center);
        if (Player.TryGetModPlayer(out WgPlayer pulseWg))
            pulseWg.Jiggle(2.4f + _bloatingPulses * 0.15f);

        if (Player.whoAmI == Main.myPlayer && !Main.dedServ)
        {
            string pulseText = _bloatingPulses switch
            {
                <= 2 => "*hrrp... swell*",
                <= 5 => "*BUURP... swell*",
                <= 8 => "*HUUURP... SWELL*",
                _ => "*BUUUURRRP... SWELL*",
            };
            CombatText.NewText(Player.Hitbox, Color.MediumPurple, pulseText, dramatic: _bloatingPulses >= 7);
        }

        switch (_bloatingPulses)
        {
            case 2:
                Say("Hrrp... there goes another one. I can actually feel myself rounding out between each burp now...");
                break;
            case 4:
                Say("BUURP... again...! My hips feel heavier every time that pressure surges through me. More there... please...");
                break;
            case 6:
                Say("HUUURRRP... I'm ballooning so fast now... my backside feels enormous, and every pulse is making it harder to think about anything else.");
                break;
            case 8:
                Say("BUUUURRRP... m-more... I want the next one... and the next... keep swelling me until my hips and rear are all I can feel...");
                break;
            case 10:
                Say("HUUUUUURRRP... I can barely move... but it's still building... one more huge push and I don't think Blob is going to be my limit anymore...");
                break;
        }

        if (Player.TryGetModPlayer(out BloatedPlayer bloated))
            bloated.ApplyBloated(BloatedPlayer.MaxTimer);
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
        _bloatingTimer = 0;
        _bloatingPulses = 0;

        int index = Player.FindBuffIndex(ModContent.BuffType<StrangeFeeling>());
        if (index >= 0)
            Player.DelBuff(index);
    }
}
