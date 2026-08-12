using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using WgMod.Common.Players;

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

    public bool Active => _active;

    public void Activate()
    {
        if (_active)
            return;

        _active = true;
        _timer = 0;
        _bloatingTimer = 0;
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
                Say("Urf... that warmth is turning into pressure. I keep needing to burp, but... it actually feels kind of nice.");
                break;
            case 60 * 10:
                Say("Hrrp... okay, I'm definitely swelling. If all this has to go somewhere... I wouldn't mind if most of it settled into my hips and backside.");
                break;
            case 60 * 15:
                Say("HUUURP... ngh... I'm getting bigger fast. My stomach is so gassy, but I can't stop thinking about how much wider and heavier my rear could get.");
                break;
            case 60 * 19:
                Say("BUUURRRP... I don't think I can stop this anymore... and I don't think I want to. If I'm going to balloon up, then let as much of it as possible pile onto my hips and backside.");
                break;
            case BloatingStartTime:
                Say("HUUUUURRRP... th-there it is... I'm swelling for real now. Keep going... *burp*... lower... wider... bigger...");
                break;
        }

        if (_timer < BloatingStartTime)
            return;

        if (Player.TryGetModPlayer(out WgPlayer wg) && wg.Weight.GetStage() >= WeightStage.MegaBlob)
        {
            Say("BUUUUUUUURRRRRP... I-I'm enormous... *HUUURP*... my hips, my backside... there's so much of me now... and that strange feeling finally feels satisfied.");
            ResetSequence();
            return;
        }

        _bloatingTimer++;
        if (_bloatingTimer < BloatingInterval)
            return;
        _bloatingTimer = 0;

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

        int index = Player.FindBuffIndex(ModContent.BuffType<StrangeFeeling>());
        if (index >= 0)
            Player.DelBuff(index);
    }
}
