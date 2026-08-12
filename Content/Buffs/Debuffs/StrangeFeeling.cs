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
        tip = "That sting left a strange warmth behind... I can't tell if I should be worried or curious.";
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

    public void Activate()
    {
        if (_active)
            return;

        _active = true;
        _timer = 0;
        _bloatingTimer = 0;
        Say("That sting felt... different. Not bad, exactly. Just strangely warm.");
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
                Say("That warmth is spreading through me now. It's almost comfortable... which somehow makes it more suspicious.");
                break;
            case 60 * 10:
                Say("I feel pressure building somewhere under my skin. I should hate this feeling... but I'm getting curious instead.");
                break;
            case 60 * 15:
                Say("Something is definitely about to happen to me. The thought of swelling up should be terrifying... so why does part of me want to see it?");
                break;
            case 60 * 19:
                Say("It's getting stronger. I don't think I could stop whatever that bee started now... and I'm not sure I want to.");
                break;
            case BloatingStartTime:
                Say("There it is... I'm starting to swell. This is going to get completely out of hand, isn't it?");
                break;
        }

        if (_timer < BloatingStartTime)
            return;

        if (Player.TryGetModPlayer(out WgPlayer wg) && wg.Weight.GetStage() >= WeightStage.MegaBlob)
        {
            Say("I'm huge... way beyond where I should have stopped. And somehow that strange feeling finally seems satisfied.");
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
