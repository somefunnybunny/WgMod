using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WgMod.Common.Players;
using WgMod.Content.Buffs.Debuffs;

namespace WgMod.Content.NPCs.Caverns;

public class FeedingManEater : ModNPC
{
    const int FeedRefreshInterval = 15;
    const int FeedDuration = 35;
    const int NarrativeInterval = 60 * 5;

    int _grabbedPlayer = -1;
    int _releasedPlayer = -1;
    int _feedTimer;
    int _narrativeTimer;
    int _narrativeStep;

    public override string Texture => $"Terraria/Images/NPC_{NPCID.ManEater}";

    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.ManEater];
    }

    public override void SetDefaults()
    {
        NPC.CloneDefaults(NPCID.ManEater);
        AIType = NPCID.ManEater;
        AnimationType = NPCID.ManEater;
        NPC.damage = 0;
        NPC.value = 80f;
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        Player player = spawnInfo.Player;
        if (!player.ZoneJungle || player.dead)
            return 0f;

        if (player.TryGetModPlayer(out WgPlayer wg) && wg.Weight.GetStage() >= WeightStage.MegaBlob)
            return 0f;

        // Intentionally common across the whole Jungle, including the surface, to mirror
        // the broad habitat of ordinary Man Eaters while remaining easy enough to encounter.
        return 0.18f;
    }

    public override bool CanHitPlayer(Player target, ref int cooldownSlot)
    {
        return false;
    }

    public override void AI()
    {
        if (_grabbedPlayer >= 0)
        {
            UpdateGrab();
            return;
        }

        if (_releasedPlayer >= 0)
        {
            NPC.target = 255;
            NPC.velocity *= 0.85f;
            return;
        }

        if (!NPC.HasPlayerTarget)
            return;

        Player player = Main.player[NPC.target];
        if (!CanGrab(player))
            return;

        Rectangle grabBox = NPC.Hitbox;
        grabBox.Inflate(10, 10);
        if (grabBox.Intersects(player.Hitbox))
            BeginGrab(player);
    }

    void BeginGrab(Player player)
    {
        _grabbedPlayer = player.whoAmI;
        _feedTimer = 0;
        _narrativeTimer = 0;
        _narrativeStep = 0;
        NPC.netUpdate = true;

        Say(player, "The Man Eater coils around me and locks me in place... wait, why is it trying to feed me?");
        Cue(player, "*caught!*", Color.YellowGreen);
        SoundEngine.PlaySound(SoundID.Item7, player.Center);
    }

    void UpdateGrab()
    {
        if (_grabbedPlayer < 0 || _grabbedPlayer >= Main.maxPlayers)
        {
            Release();
            return;
        }

        Player player = Main.player[_grabbedPlayer];
        if (!CanGrab(player))
        {
            Release();
            return;
        }

        if (player.TryGetModPlayer(out WgPlayer wg) && wg.Weight.GetStage() >= WeightStage.MegaBlob)
        {
            Say(player, "It finally lets go. I guess even this thing thinks Mega Blob is enough...");
            Cue(player, "*released*", Color.YellowGreen);
            _releasedPlayer = player.whoAmI;
            Release();
            return;
        }

        player.Center = NPC.Center;
        player.velocity = Vector2.Zero;
        player.controlLeft = false;
        player.controlRight = false;
        player.controlUp = false;
        player.controlDown = false;
        player.controlJump = false;
        player.controlHook = false;
        player.jump = 0;
        player.fallStart = (int)(player.position.Y / 16f);

        _feedTimer++;
        if (_feedTimer >= FeedRefreshInterval)
        {
            _feedTimer = 0;
            if (player.TryGetModPlayer(out ForceFedPlayer forceFed))
                forceFed.ApplyCustomForceFed(FeedDuration, ForceFed.FatPerCycle);

            Cue(player, "*gulp*", Color.Orange);
        }

        _narrativeTimer++;
        if (_narrativeTimer >= NarrativeInterval)
        {
            _narrativeTimer = 0;
            _narrativeStep++;
            switch (_narrativeStep)
            {
                case 1:
                    Say(player, "It isn't letting up... every gulp is making me heavier while it keeps me pinned right here.");
                    break;
                case 2:
                    Say(player, "I can feel the weight piling on now. If I don't kill this thing, it's going to keep feeding me until I can't move at all.");
                    break;
                case 3:
                    Say(player, "Another mouthful... and another. My body is getting too heavy to fight the vine as easily as before.");
                    break;
                case 4:
                    Say(player, "It really means to keep going until I'm enormous. I can barely tell where the feeding ends and all this new weight begins.");
                    break;
                default:
                    Say(player, "Still feeding me... still making me heavier. It isn't going to stop unless I make it stop.");
                    break;
            }
        }
    }

    bool CanGrab(Player player)
    {
        if (!player.active || player.dead)
            return false;

        return !player.TryGetModPlayer(out WgPlayer wg) || wg.Weight.GetStage() < WeightStage.MegaBlob;
    }

    void Release()
    {
        _grabbedPlayer = -1;
        _feedTimer = 0;
        _narrativeTimer = 0;
        _narrativeStep = 0;
        NPC.target = 255;
        NPC.netUpdate = true;
    }

    static void Say(Player player, string text)
    {
        if (player.whoAmI == Main.myPlayer && !Main.dedServ)
            Main.NewText(text, Color.YellowGreen);
    }

    static void Cue(Player player, string text, Color color)
    {
        if (player.whoAmI == Main.myPlayer && !Main.dedServ)
            CombatText.NewText(player.Hitbox, color, text);
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(_grabbedPlayer);
        writer.Write(_releasedPlayer);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        _grabbedPlayer = reader.ReadInt32();
        _releasedPlayer = reader.ReadInt32();
    }
}
