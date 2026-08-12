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
            ? "The Man Eater coils around me and starts hauling me away... wait, it's looking for somewhere to keep me?"
            : "The Man Eater coils around me and locks me in place... wait, why is it trying to feed me?");
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

        // Mega Blob is the plant's natural endpoint. Check it before CanGrab(), which
        // intentionally rejects Mega Blob players and would otherwise skip this release.
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
                Say(player, "It found an open spot and holds me there... there's way too much room around me for comfort.");
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
                    Say(player, "The vine can't carry me any farther, so it tightens around me here instead...");
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

            // Once the victim has grown past Fat, the plant's constant feeding also starts
            // producing a Honey-like background gain directly in the stomach. This is separate
            // from the discrete Force Fed mouthfuls and ends immediately when the grab ends.
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
            WeightStage.Chubby => "No. Absolutely not. I'm not letting a plant stuff me just because it caught me...",
            WeightStage.Overweight => "It's already making me noticeably bigger. I keep fighting every mouthful, but it just forces the next one in.",
            WeightStage.Fat => "I'm actually fat now... and I'm still trying to turn my head away between every bite it pushes at me.",
            WeightStage.Obese => "Something changed. Even between mouthfuls I can feel my body quietly adding more softness on its own...",
            WeightStage.MorbidlyObese => "I'm getting enormous. I'm still resisting, but the feeding is starting to feel disturbingly easy to fall into.",
            WeightStage.BarelyMobile => "I'm so heavy the vine barely has to restrain me anymore... and I'm spending more time swallowing than struggling.",
            WeightStage.Immobile => "Mmph... another one. I know I should be worried, but right now I'm mostly waiting for it to bring the next mouthful.",
            WeightStage.Encumbered => "I can barely see past what I'm carrying in front of me, and I can feel just as much spreading out behind... but I keep eating anyway.",
            WeightStage.Blob => "There is so much of me sticking out in every direction now... *gulp*... and somehow my attention is still on the food.",
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
                1 => "I keep trying to twist away from it, but the vine just holds me still and pushes another mouthful in.",
                2 => "I'm not cooperating with this. The moment I get an opening, I'm getting away from this thing.",
                _ => "Another forced gulp... no. I'm still fighting this. It hasn't won yet.",
            };
        }
        else if (stage < WeightStage.Immobile)
        {
            text = (step % 3) switch
            {
                1 => "The constant feeding is getting harder to separate from the slow fattening underneath it. I'm growing even between bites now.",
                2 => "I'm still trying to resist, but every mouthful feels a little more automatic than the last.",
                _ => "I should be thinking about escaping. Instead I caught myself swallowing before it even had to force me.",
            };
        }
        else
        {
            text = (step % 3) switch
            {
                1 => "Mmph... *gulp*... what was I worried about again? There's another bite coming.",
                2 => "I can feel how absurdly far my body sticks out in front and behind me... but the next mouthful has my attention right now.",
                _ => "More... *gulp*... I'll think about how helpless I've gotten after I finish this one. And maybe the next one.",
            };
        }

        Say(player, text);
    }

    void SayReleaseMessage(Player player, int stage, bool naturalMegaBlobRelease)
    {
        string text;
        if (naturalMegaBlobRelease || stage >= WeightStage.MegaBlob)
        {
            text = "It finally lets me go... wait, that's it? I let it feed me all the way into a Mega Blob, and now it decides I'm finished?";
        }
        else
        {
            text = stage switch
            {
                WeightStage.Regular => "I'm free. Good. I got away before that thing could start fattening me up.",
                WeightStage.Chubby => "I'm free... finally. A little softer is a lot better than finding out how far that plant wanted to take this.",
                WeightStage.Overweight => "I got away. I'm heavier than I was, but at least I stopped it before this got completely out of hand.",
                WeightStage.Fat => "I'm out. I'm actually fat because of that thing, but I'm still relieved I managed to stop it here.",
                WeightStage.Obese => "It's gone... good. I think. Why does part of me already miss having the next mouthful pushed toward me?",
                WeightStage.MorbidlyObese => "I'm free, but... that's really the end of the feeding? I should probably be happier about that.",
                WeightStage.BarelyMobile => "It stopped. I can barely move after all that, and somehow I'm more disappointed about losing the food than relieved about escaping.",
                WeightStage.Immobile => "No more food...? I know getting free should matter more than that, but right now I'm mostly noticing the empty space in front of my mouth.",
                WeightStage.Encumbered => "It actually stopped feeding me. With this much of me spread out in front and behind, you'd think I'd be relieved... but I wanted another bite.",
                WeightStage.Blob => "It's over...? I'm a huge helpless blob because I kept eating, and the part bothering me most is that the mouthfuls stopped.",
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
