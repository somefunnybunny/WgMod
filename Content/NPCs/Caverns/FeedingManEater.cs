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
    const int NarrativeInterval = 60 * 5;
    const int OpenSpaceSearchRadiusTiles = 22;
    const float CarrySpeed = 9f;
    const float CarryArrivalDistance = 10f;

    int _grabbedPlayer = -1;
    int _releasedPlayer = -1;
    int _feedTimer;
    int _narrativeTimer;
    int _narrativeStep;
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

    void BeginGrab(Player player)
    {
        _grabbedPlayer = player.whoAmI;
        _feedTimer = 0;
        _narrativeTimer = 0;
        _narrativeStep = 0;
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
                    // If the direct path clips terrain, stop at the nearest safe point rather than
                    // dragging the victim through blocks. Feeding begins from here.
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
                    Say(player, "It isn't letting up... every gulp is making me heavier while it keeps me pinned in all this open space.");
                    break;
                case 2:
                    Say(player, "I can feel the weight piling on now. It picked somewhere with enough room that getting bigger isn't going to save me.");
                    break;
                case 3:
                    Say(player, "Another mouthful... and another. There's still empty space around me, and the plant seems determined to fill it with me.");
                    break;
                case 4:
                    Say(player, "It really planned this out... it dragged me somewhere I could keep expanding and now it just keeps feeding me.");
                    break;
                default:
                    Say(player, "Still feeding me... still making me bigger. It isn't going to stop unless I make it stop.");
                    break;
            }
        }
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
            height = player.defaultHeight * 2;
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
        int baseHeight = player.defaultHeight * 2;
        float clearance = 0f;

        // Reward candidates that still have room after the minimum Mega Blob rectangle fits.
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
