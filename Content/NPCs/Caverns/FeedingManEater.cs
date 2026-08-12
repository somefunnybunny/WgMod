using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using WgMod.Common.Players;
using WgMod.Content.Buffs.Debuffs;

namespace WgMod.Content.NPCs.Caverns;

public class FeedingManEater : ModNPC
{
    const int FeedRefreshInterval = 15;
    const int FeedDuration = 35;
    const int NarrativeInterval = 60 * 9;
    const float PassiveFatteningPerFeed = 0.35f;
    const int OpenSpaceSearchRadiusTiles = 22;
    const float CarrySpeed = 9f;
    const float CarryArrivalDistance = 10f;

    int _grabbedPlayer = -1;
    int _releasedPlayer = -1;
    int _feedTimer;
    int _narrativeTimer;
    int _narrativeStep;
    int _lastNarrativeStage = -1;
    bool _carrying;
    Vector2 _feedingPosition;

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

        return 0.18f;
    }

    public override void OnSpawn(IEntitySource source)
    {
        if (NPC.ai[0] != 0f || NPC.ai[1] != 0f)
            return;

        int startX = (int)(NPC.Center.X / 16f);
        int startY = (int)((NPC.position.Y + NPC.height) / 16f);
        if (TryFindAnchor(startX, startY, out int anchorX, out int anchorY))
        {
            NPC.ai[0] = anchorX;
            NPC.ai[1] = anchorY;
        }
        else
        {
            NPC.ai[0] = startX;
            NPC.ai[1] = startY;
        }

        NPC.netUpdate = true;
    }

    static bool TryFindAnchor(int startX, int startY, out int anchorX, out int anchorY)
    {
        for (int radius = 0; radius <= 10; radius++)
        {
            for (int xOffset = -radius; xOffset <= radius; xOffset++)
            {
                int x = startX + xOffset;
                if (x < 1 || x >= Main.maxTilesX - 1)
                    continue;

                for (int yOffset = -4; yOffset <= 12; yOffset++)
                {
                    int y = startY + yOffset;
                    if (y < 1 || y >= Main.maxTilesY - 1)
                        continue;

                    Tile tile = Main.tile[x, y];
                    if (tile.HasUnactuatedTile && Main.tileSolid[tile.TileType])
                    {
                        anchorX = x;
                        anchorY = y;
                        return true;
                    }
                }
            }
        }

        anchorX = startX;
        anchorY = startY;
        return false;
    }

    public override bool CanHitPlayer(Player target, ref int cooldownSlot)
    {
        return false;
    }

    public override bool PreAI()
    {
        if (_grabbedPlayer < 0)
            return true;

        UpdateGrab();
        return false;
    }

    public override void AI()
    {
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

    public override void OnKill()
    {
        if (_grabbedPlayer < 0 || _grabbedPlayer >= Main.maxPlayers)
            return;

        Player player = Main.player[_grabbedPlayer];
        if (!player.active || player.dead)
            return;

        int stage = player.TryGetModPlayer(out WgPlayer wg)
            ? wg.Weight.GetStage()
            : WeightStage.Regular;

        SayReleaseMessage(player, stage, false);
        Cue(player, "*escaped!*", Color.YellowGreen);
        _grabbedPlayer = -1;
    }

    void BeginGrab(Player player)
    {
        _grabbedPlayer = player.whoAmI;
        _feedTimer = 0;
        _narrativeTimer = 0;
        _narrativeStep = 0;
        _lastNarrativeStage = -1;
        _feedingPosition = FindOpenFeedingPosition(player);
        _carrying = Vector2.DistanceSquared(NPC.Center, _feedingPosition) > CarryArrivalDistance * CarryArrivalDistance;
        NPC.netUpdate = true;

        Say(player, _carrying
            ? "The Man Eater coils around me and starts hauling me away... no. Wherever it's taking me, I'm not cooperating."
            : "The Man Eater coils around me and locks me in place... no. I'm not letting a plant feed me.");
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
        if (!player.active || player.dead)
        {
            Release();
            return;
        }

        WgPlayer wg = null;
        int stage = WeightStage.Regular;
        if (player.TryGetModPlayer(out WgPlayer foundWg))
        {
            wg = foundWg;
            stage = wg.Weight.GetStage();
        }

        if (stage >= WeightStage.MegaBlob)
        {
            SayReleaseMessage(player, stage, true);
            Cue(player, "*finally released*", Color.YellowGreen);
            _releasedPlayer = player.whoAmI;
            Release();
            return;
        }

        if (!CanGrab(player))
        {
            Release();
            return;
        }

        RestrainPlayer(player);

        if (_carrying)
        {
            Vector2 delta = _feedingPosition - NPC.Center;
            if (delta.LengthSquared() <= CarryArrivalDistance * CarryArrivalDistance)
            {
                NPC.Center = _feedingPosition;
                NPC.velocity = Vector2.Zero;
                _carrying = false;
                Say(player, "It settles me into the open and keeps shifting me around just enough to leave my changing shape in plain view... that's disturbingly deliberate.");
                Cue(player, "*held in place*", Color.YellowGreen);
                NPC.netUpdate = true;
            }
            else
            {
                Vector2 step = delta;
                if (step.Length() > CarrySpeed)
                {
                    step.Normalize();
                    step *= CarrySpeed;
                }

                Vector2 nextCenter = NPC.Center + step;
                if (CanOccupyFeedingSpace(nextCenter, player, false))
                    NPC.Center = nextCenter;
                else
                {
                    _feedingPosition = NPC.Center;
                    _carrying = false;
                    Say(player, "The vine can't carry me any farther, so it tightens around me here instead... and angles me where I can still see what it's doing.");
                    NPC.netUpdate = true;
                }
            }

            player.Center = NPC.Center;
            player.velocity = Vector2.Zero;
            return;
        }

        player.Center = NPC.Center;
        player.velocity = Vector2.Zero;

        if (stage != _lastNarrativeStage)
        {
            _lastNarrativeStage = stage;
            SayStageMessage(player, stage);
            _narrativeTimer = 0;
            _narrativeStep = 0;
        }

        _feedTimer++;
        if (_feedTimer >= FeedRefreshInterval)
        {
            _feedTimer = 0;
            if (player.TryGetModPlayer(out ForceFedPlayer forceFed))
                forceFed.ApplyCustomForceFed(FeedDuration, ForceFed.FatPerCycle);

            if (stage >= WeightStage.Obese && wg != null)
                wg.AddStomach(PassiveFatteningPerFeed, false);

            Cue(player, stage >= WeightStage.Immobile ? "*gulp... munch*" : "*gulp*", Color.Orange);
        }

        _narrativeTimer++;
        if (_narrativeTimer >= NarrativeInterval)
        {
            _narrativeTimer = 0;
            _narrativeStep++;
            SayPhaseMessage(player, stage, _narrativeStep);
        }
    }

    void SayStageMessage(Player player, int stage)
    {
        string text = stage switch
        {
            WeightStage.Chubby => "No. Absolutely not. I'm not letting some vine stuff me just because it caught me.",
            WeightStage.Overweight => "It's already changing me... I can actually see myself getting wider every time it feeds me. That needs to stop.",
            WeightStage.Fat => "I'm actually fat now. And why does it keep shifting me around like it wants me to get a better look at myself?",
            WeightStage.Obese => "It moved me again... and now I can't stop noticing how much softer and broader I've gotten. That's not what I should be focusing on.",
            WeightStage.MorbidlyObese => "I'm enormous... and it keeps holding me where I can see every new inch settle onto me. I should be fighting harder than this.",
            WeightStage.BarelyMobile => "I'm getting so heavy that struggling barely moves me anymore... but I keep catching myself watching what each mouthful does instead.",
            WeightStage.Immobile => "Wait... when did I stop being able to move normally? I was too busy watching myself spread out to even notice.",
            WeightStage.Encumbered => "I can't really move at all now... but look at how far I'm sticking out in front and behind. It just keeps adding more.",
            WeightStage.Blob => "I'm a huge helpless blob now... and I'm still watching myself get bigger like that's somehow the important part.",
            _ => "I need to get out of this before it starts making me bigger.",
        };
        Say(player, text);
    }

    void SayPhaseMessage(Player player, int stage, int step)
    {
        string text;
        if (stage <= WeightStage.Fat)
        {
            text = (step % 3) switch
            {
                1 => "I keep twisting away, but the vine just pulls me back into place and forces another mouthful in.",
                2 => "No. I'm not cooperating with this. I don't care what it's trying to do to me, I'm getting out.",
                _ => "Another forced gulp... and another little change. I can see it happening, but that doesn't mean I'm going to let it continue.",
            };
        }
        else if (stage < WeightStage.Immobile)
        {
            text = (step % 3) switch
            {
                1 => "It keeps nudging me into just the right position to see myself. Every time I look, there's noticeably more of me.",
                2 => "I know I should be planning an escape, but I keep checking how much wider I've gotten instead.",
                _ => "Another mouthful, another shift... I hate that I'm starting to wait for the part where I get to see what changed.",
            };
        }
        else
        {
            text = (step % 3) switch
            {
                1 => "Mmph... *gulp*... hold on, move me a little. I want to see how much that one added.",
                2 => "I can barely tell where normal movement stopped being possible. I was too busy watching myself keep spreading.",
                _ => "More... *gulp*... I should probably care that I'm completely helpless now, but I really want to see what the next mouthful does.",
            };
        }

        Say(player, text);
    }

    void SayReleaseMessage(Player player, int stage, bool naturalMegaBlobRelease)
    {
        string text;
        if (naturalMegaBlobRelease || stage >= WeightStage.MegaBlob)
        {
            text = "It finally lets me go... wait, that's it? I let it feed me all the way into a Mega Blob while I was busy watching myself grow, and now it decides I'm finished?";
        }
        else
        {
            text = stage switch
            {
                WeightStage.Regular => "I'm free. Good. I got away before that thing could start changing me.",
                WeightStage.Chubby => "I'm free... finally. A little softer is a lot better than finding out how far that plant wanted to take this.",
                WeightStage.Overweight => "I got away. I'm definitely heavier, but at least I stopped it before staring at the changes got distracting.",
                WeightStage.Fat => "I'm out. I'm actually fat because of that thing... and I hate that part of me wanted one more look before it stopped.",
                WeightStage.Obese => "It's gone... good. I think. Why am I still looking myself over like I'm expecting another change?",
                WeightStage.MorbidlyObese => "I'm free, but... that's really it? I should be relieved, not disappointed that I don't get to watch myself grow any more.",
                WeightStage.BarelyMobile => "It stopped. I can barely move after all that, and somehow I'm more disappointed about losing the next change than relieved about escaping.",
                WeightStage.Immobile => "No more...? I can't even move normally anymore, and somehow the first thing I notice is that there's nothing new to watch.",
                WeightStage.Encumbered => "It actually stopped. I'm completely stuck with all this mass in front and behind me... and I still wanted to see what one more mouthful would do.",
                WeightStage.Blob => "It's over...? I'm a huge helpless blob, and the part bothering me most is that I don't get to watch myself get any bigger.",
                _ => "I'm free... and I should probably put some distance between me and any more of those plants.",
            };
        }

        Say(player, text);
    }

    void RestrainPlayer(Player player)
    {
        player.controlLeft = false;
        player.controlRight = false;
        player.controlUp = false;
        player.controlDown = false;
        player.controlJump = false;
        player.controlHook = false;
        player.jump = 0;
        player.fallStart = (int)(player.position.Y / 16f);
    }

    Vector2 FindOpenFeedingPosition(Player player)
    {
        Vector2 caughtAt = player.Center;
        Vector2 best = NPC.Center;
        float bestScore = float.NegativeInfinity;

        for (int radius = 2; radius <= OpenSpaceSearchRadiusTiles; radius += 2)
        {
            for (int x = -radius; x <= radius; x += 2)
            {
                TryCandidate(caughtAt + new Vector2(x * 16f, -radius * 16f));
                TryCandidate(caughtAt + new Vector2(x * 16f, radius * 16f));
            }

            for (int y = -radius + 2; y <= radius - 2; y += 2)
            {
                TryCandidate(caughtAt + new Vector2(-radius * 16f, y * 16f));
                TryCandidate(caughtAt + new Vector2(radius * 16f, y * 16f));
            }
        }

        return best;

        void TryCandidate(Vector2 candidate)
        {
            if (!CanOccupyFeedingSpace(candidate, player, true))
                return;

            float distancePenalty = Vector2.DistanceSquared(candidate, caughtAt) * 0.001f;
            float clearance = MeasureClearance(candidate, player);
            float score = clearance * 100f - distancePenalty;
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }
    }

    bool CanOccupyFeedingSpace(Vector2 center, Player player, bool requireMegaBlobClearance)
    {
        int width;
        int height;

        if (requireMegaBlobClearance)
        {
            width = WeightValues.GetHitboxWidthInTiles(WeightStage.MegaBlob) * 16 - 12;
            height = Player.defaultHeight * 2;
        }
        else
        {
            width = Math.Max(player.width, NPC.width);
            height = Math.Max(player.height, NPC.height);
        }

        Vector2 topLeft = center - new Vector2(width * 0.5f, height * 0.5f);
        return !Collision.SolidCollision(topLeft, width, height);
    }

    float MeasureClearance(Vector2 center, Player player)
    {
        int baseWidth = WeightValues.GetHitboxWidthInTiles(WeightStage.MegaBlob) * 16 - 12;
        int baseHeight = Player.defaultHeight * 2;
        float clearance = 0f;

        for (int padding = 16; padding <= 96; padding += 16)
        {
            int width = baseWidth + padding * 2;
            int height = baseHeight + padding * 2;
            Vector2 topLeft = center - new Vector2(width * 0.5f, height * 0.5f);
            if (Collision.SolidCollision(topLeft, width, height))
                break;
            clearance += 1f;
        }

        return clearance;
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
        _lastNarrativeStage = -1;
        _carrying = false;
        _feedingPosition = Vector2.Zero;
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
        writer.Write(_carrying);
        writer.Write(_feedingPosition.X);
        writer.Write(_feedingPosition.Y);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        _grabbedPlayer = reader.ReadInt32();
        _releasedPlayer = reader.ReadInt32();
        _carrying = reader.ReadBoolean();
        _feedingPosition = new Vector2(reader.ReadSingle(), reader.ReadSingle());
    }
}
