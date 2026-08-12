using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using WgMod.Common.Players;
using WgMod.Content.Buffs.Debuffs;

namespace WgMod.Content.NPCs.UndergroundDesert;

[Credit(ProjectRole.Programmer, Contributor.follycake)]
[Credit(ProjectRole.Idea, Contributor.haydumbb)]
public class HomingFood : ModNPC
{
    const int AggressiveTier = 6;
    const float BaseFeedMass = 6f;
    const float FeedMassPerTier = 1f;

    static readonly int[] _items =
    [
        ItemID.ChristmasPudding,
        ItemID.GingerbreadCookie,
        ItemID.RoastedBird,
        ItemID.MonsterLasagna,
        ItemID.BananaSplit,
        ItemID.Fries,
        ItemID.Burger,
        ItemID.Pizza,
        ItemID.IceCream,
        ItemID.Hotdog,
        ItemID.Milkshake
    ];

    int _itemIndex;
    int _itemId;
    bool _fedPlayer;

    public override void SetDefaults()
    {
        NPC.width = 22;
        NPC.height = 22;
        NPC.damage = 0;
        NPC.defense = 14;
        NPC.lifeMax = 30;
        NPC.HitSound = SoundID.NPCHit1;
        NPC.DeathSound = SoundID.NPCDeath1;
        NPC.value = 0f;
        NPC.knockBackResist = 0f;
        NPC.aiStyle = NPCAIStyleID.CursedSkull;
        NPC.noTileCollide = true;
        NPC.noGravity = true;
        NPC.friendly = false;

        AIType = NPCID.CursedSkull;
        _itemIndex = 0;
        _itemId = _items[_itemIndex];
    }

    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
    {
        bestiaryEntry.Info.Add(
            new FlavorTextBestiaryInfoElement(Mod.GetLocalizationKey("Bestiary." + nameof(HomingFood)))
        );
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        return 0.1f * OverindulgenceChain.GetSpawnMultiplier(spawnInfo.Player);
    }

    public override void OnSpawn(IEntitySource source)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;
        _itemIndex = Main.rand.Next(_items.Length);
        _itemId = _items[_itemIndex];
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(_itemIndex);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        int index = reader.ReadInt32();
        if (_itemIndex != index)
        {
            _itemIndex = index;
            _itemId = _items[_itemIndex];
        }
    }

    public override bool PreAI()
    {
        NPC.TargetClosest();
        if (!NPC.HasPlayerTarget)
            return true;

        Player player = Main.player[NPC.target];
        int tier = OverindulgenceChain.GetTier(player);
        if (tier < AggressiveTier)
            return true;

        Vector2 direction = player.Center - NPC.Center;
        if (direction.LengthSquared() > 0.001f)
        {
            direction.Normalize();
            float speed = tier switch
            {
                6 => 8f,
                7 => 10f,
                _ => 12f,
            };
            NPC.velocity = direction * speed;
        }

        return false;
    }

    public override void PostAI()
    {
        if (NPC.HasPlayerTarget)
        {
            Player player = Main.player[NPC.target];
            if (NPC.getRect().Intersects(player.getRect()))
                FeedPlayer(player);
        }
        Lighting.AddLight(NPC.Center, Color.Purple.ToVector3() * 0.78f);
    }

    public override bool CanHitPlayer(Player target, ref int cooldownSlot)
    {
        return false;
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        Main.instance.LoadItem(_itemId);
        Asset<Texture2D> texture = TextureAssets.Item[_itemId];
        Rectangle frame = texture.Frame(1, 3);
        spriteBatch.Draw(texture.Value, NPC.Center - screenPos, frame, Color.White, (float)(Math.Sin(Main.timeForVisualEffects / 30.0) * 0.2), frame.Size() * 0.5f, 0.8f, SpriteEffects.None, 0f);
        return false;
    }

    public override void DrawEffects(ref Color drawColor)
    {
        Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.PinkTorch, NPC.velocity.X, NPC.velocity.Y, 0, Color.White, 1f);
        dust.noGravity = true;
    }

    void FeedPlayer(Player player)
    {
        if (_fedPlayer)
            return;

        _fedPlayer = true;

        int nextOverindulgenceTier = Math.Min(OverindulgenceChain.GetTier(player) + 1, OverindulgenceChain.MaxTier);
        Mass feedMass = BaseFeedMass + FeedMassPerTier * nextOverindulgenceTier;

        if (player.TryGetModPlayer(out WgPlayer wg))
        {
            bool alreadyBlobbed = wg.Weight.GetStage() >= WeightStage.Blob;
            wg.CombatWeightText(wg.AddWeight(feedMass), false);

            // Blob is already weight-clamped, so count the attempted feeding mass directly
            // toward Straining rather than relying on the visible weight change.
            if (alreadyBlobbed && player.TryGetModPlayer(out StrainingPlayer straining))
                straining.AddFedMass(feedMass, StrainingSource.ForceFed);
        }

        player.AddBuff(BuffID.WellFed, 60 * 4);
        OverindulgenceChain.Advance(player);
        SoundEngine.PlaySound(SoundID.Item2, NPC.Center);
        NPC.life = 0;
    }
}
