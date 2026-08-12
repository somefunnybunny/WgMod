using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WgMod.Common.Players;
using WgMod.Content.Buffs.Debuffs;

namespace WgMod.Content.NPCs.BiomeVariants;

public abstract class WeightVariantNPC : ModNPC
{
    protected static int Stage(Player player)
    {
        return player.TryGetModPlayer(out WgPlayer wg) ? wg.Weight.GetStage() : WeightStage.Regular;
    }

    protected static void Feed(Player player, float mass, string cue = null)
    {
        if (player.TryGetModPlayer(out WgPlayer wg))
            wg.AddStomach(mass);

        if (cue != null && player.whoAmI == Main.myPlayer && !Main.dedServ)
            CombatText.NewText(player.Hitbox, Color.Orange, cue);
    }

    protected static void Say(Player player, string text, Color color)
    {
        if (player.whoAmI == Main.myPlayer && !Main.dedServ)
            Main.NewText(text, color);
    }
}

public class StuffingSlime : WeightVariantNPC
{
    public override string Texture => $"Terraria/Images/NPC_{NPCID.BlueSlime}";

    public override void SetStaticDefaults() => Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.BlueSlime];

    public override void SetDefaults()
    {
        NPC.CloneDefaults(NPCID.BlueSlime);
        AIType = NPCID.BlueSlime;
        AnimationType = NPCID.BlueSlime;
        NPC.lifeMax = 80;
        NPC.damage = 12;
        NPC.value = 75f;
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        return spawnInfo.Player.ZoneJungle && !spawnInfo.Player.dead ? 0.08f : 0f;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
    {
        if (target.TryGetModPlayer(out ForceFedPlayer forceFed))
            forceFed.ApplyCustomForceFed(180, 4f);
        target.AddBuff(BuffID.Slow, 120);
        Say(target, "The slime splats over me and starts forcing its syrupy mass down my throat!", Color.YellowGreen);
    }
}

public class GorgeWorm : WeightVariantNPC
{
    public override string Texture => $"Terraria/Images/NPC_{NPCID.EaterofSouls}";

    public override void SetStaticDefaults() => Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.EaterofSouls];

    public override void SetDefaults()
    {
        NPC.CloneDefaults(NPCID.EaterofSouls);
        AIType = NPCID.EaterofSouls;
        AnimationType = NPCID.EaterofSouls;
        NPC.lifeMax = 120;
        NPC.damage = 20;
        NPC.value = 110f;
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        Player p = spawnInfo.Player;
        return (p.ZoneCorrupt || p.ZoneCrimson) && !p.dead ? 0.06f : 0f;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
    {
        target.AddBuff(ModContent.BuffType<DigestiveBloom>(), 60 * 12);
        Say(target, "Something in that bite made every bit of food feel dangerously efficient...", Color.MediumPurple);
    }
}

public class SyrupAntlion : WeightVariantNPC
{
    int _syrupTimer;

    public override string Texture => $"Terraria/Images/NPC_{NPCID.Antlion}";

    public override void SetStaticDefaults() => Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Antlion];

    public override void SetDefaults()
    {
        NPC.CloneDefaults(NPCID.Antlion);
        AIType = NPCID.Antlion;
        AnimationType = NPCID.Antlion;
        NPC.lifeMax = 90;
        NPC.damage = 14;
        NPC.value = 90f;
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        return spawnInfo.Player.ZoneDesert && !spawnInfo.Player.dead ? 0.08f : 0f;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
    {
        Feed(target, 5f, "*sticky gulp*");
        target.AddBuff(BuffID.Slow, 60 * 4);
    }

    public override void AI()
    {
        if (!NPC.HasPlayerTarget)
            return;

        Player player = Main.player[NPC.target];
        if (!player.active || player.dead || Vector2.DistanceSquared(player.Center, NPC.Center) > 220f * 220f)
            return;

        _syrupTimer++;
        if (_syrupTimer >= 45 && player.velocity.Y == 0f)
        {
            _syrupTimer = 0;
            player.velocity.X *= 0.7f;
            Feed(player, 0.8f, "*syrup clings*" );
        }
    }
}

public class BanquetSkeleton : WeightVariantNPC
{
    int _banquetTimer;

    public override string Texture => $"Terraria/Images/NPC_{NPCID.DarkCaster}";

    public override void SetStaticDefaults() => Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.DarkCaster];

    public override void SetDefaults()
    {
        NPC.CloneDefaults(NPCID.DarkCaster);
        AIType = NPCID.DarkCaster;
        AnimationType = NPCID.DarkCaster;
        NPC.lifeMax = 150;
        NPC.damage = 18;
        NPC.value = 140f;
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        return spawnInfo.Player.ZoneDungeon && !spawnInfo.Player.dead ? 0.05f : 0f;
    }

    public override void AI()
    {
        if (!NPC.HasPlayerTarget)
            return;

        Player player = Main.player[NPC.target];
        if (!player.active || player.dead || Vector2.DistanceSquared(player.Center, NPC.Center) > 480f * 480f)
            return;

        _banquetTimer++;
        if (_banquetTimer >= 120)
        {
            _banquetTimer = 0;
            Feed(player, 3f, "*spectral snack*" );
            Say(player, "A ghostly course simply appears in my mouth before I can refuse it.", Color.LightSkyBlue);
        }
    }
}

public class HoneyHornetQueen : WeightVariantNPC
{
    int _honeyTimer;

    public override string Texture => $"Terraria/Images/NPC_{NPCID.Hornet}";

    public override void SetStaticDefaults() => Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Hornet];

    public override void SetDefaults()
    {
        NPC.CloneDefaults(NPCID.Hornet);
        AIType = NPCID.Hornet;
        AnimationType = NPCID.Hornet;
        NPC.scale = 1.35f;
        NPC.lifeMax = 180;
        NPC.damage = 22;
        NPC.value = 170f;
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        return spawnInfo.Player.ZoneJungle && !spawnInfo.Player.dead ? 0.035f : 0f;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
    {
        target.AddBuff(ModContent.BuffType<RoyalJelly>(), 60 * 15);
        Feed(target, 3f, "*royal jelly*" );
    }

    public override void AI()
    {
        if (!NPC.HasPlayerTarget)
            return;

        Player player = Main.player[NPC.target];
        if (!player.active || player.dead || !player.honeyWet || Vector2.DistanceSquared(player.Center, NPC.Center) > 520f * 520f)
            return;

        _honeyTimer++;
        if (_honeyTimer >= 60)
        {
            _honeyTimer = 0;
            Feed(player, 1.5f, "*honey thickens*" );
        }
    }
}

public class Bloatfish : WeightVariantNPC
{
    public override string Texture => $"Terraria/Images/NPC_{NPCID.Shark}";

    public override void SetStaticDefaults() => Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Shark];

    public override void SetDefaults()
    {
        NPC.CloneDefaults(NPCID.Shark);
        AIType = NPCID.Shark;
        AnimationType = NPCID.Shark;
        NPC.lifeMax = 140;
        NPC.damage = 18;
        NPC.value = 120f;
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        return spawnInfo.Player.ZoneBeach && !spawnInfo.Player.dead ? 0.055f : 0f;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
    {
        target.AddBuff(ModContent.BuffType<BuoyantBloat>(), 60 * 8);
        Feed(target, 4f, "*bloop!*" );
        if (target.velocity.Y > -2f)
            target.velocity.Y = -2f;
    }
}

public class CreamSlushie : WeightVariantNPC
{
    public override string Texture => $"Terraria/Images/NPC_{NPCID.IceSlime}";

    public override void SetStaticDefaults() => Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.IceSlime];

    public override void SetDefaults()
    {
        NPC.CloneDefaults(NPCID.IceSlime);
        AIType = NPCID.IceSlime;
        AnimationType = NPCID.IceSlime;
        NPC.lifeMax = 100;
        NPC.damage = 15;
        NPC.value = 85f;
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        return spawnInfo.Player.ZoneSnow && !spawnInfo.Player.dead ? 0.075f : 0f;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
    {
        target.AddBuff(BuffID.Chilled, 60 * 4);
        target.AddBuff(ModContent.BuffType<Thickened>(), 60 * 10);
        Feed(target, 3f, "*cream splat*" );
    }
}

public class FurnaceImp : WeightVariantNPC
{
    public override string Texture => $"Terraria/Images/NPC_{NPCID.FireImp}";

    public override void SetStaticDefaults() => Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.FireImp];

    public override void SetDefaults()
    {
        NPC.CloneDefaults(NPCID.FireImp);
        AIType = NPCID.FireImp;
        AnimationType = NPCID.FireImp;
        NPC.lifeMax = 160;
        NPC.damage = 22;
        NPC.value = 150f;
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        return spawnInfo.Player.ZoneUnderworldHeight && !spawnInfo.Player.dead ? 0.05f : 0f;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
    {
        target.AddBuff(ModContent.BuffType<DemonDough>(), 60 * 12);
        Feed(target, 6f, "*hot dough*" );
    }
}

public class PossessedBuffet : WeightVariantNPC
{
    int _servingTimer;

    public override string Texture => $"Terraria/Images/NPC_{NPCID.Mimic}";

    public override void SetStaticDefaults() => Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Mimic];

    public override void SetDefaults()
    {
        NPC.CloneDefaults(NPCID.Mimic);
        AIType = NPCID.Mimic;
        AnimationType = NPCID.Mimic;
        NPC.lifeMax = 450;
        NPC.damage = 20;
        NPC.value = 300f;
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        Player p = spawnInfo.Player;
        bool underground = p.ZoneDirtLayerHeight || p.ZoneRockLayerHeight;
        return Main.hardMode && underground && !p.dead ? 0.02f : 0f;
    }

    public override void AI()
    {
        if (!NPC.HasPlayerTarget)
            return;

        Player player = Main.player[NPC.target];
        if (!player.active || player.dead || Vector2.DistanceSquared(player.Center, NPC.Center) > 560f * 560f)
            return;

        _servingTimer++;
        if (_servingTimer >= 90)
        {
            _servingTimer = 0;
            Feed(player, 2.5f, "*the buffet serves itself*" );
        }
    }
}

public class BlobLeech : WeightVariantNPC
{
    public override string Texture => $"Terraria/Images/NPC_{NPCID.FaceMonster}";

    public override void SetStaticDefaults() => Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.FaceMonster];

    public override void SetDefaults()
    {
        NPC.CloneDefaults(NPCID.FaceMonster);
        AIType = NPCID.FaceMonster;
        AnimationType = NPCID.FaceMonster;
        NPC.lifeMax = 180;
        NPC.damage = 16;
        NPC.value = 130f;
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        int stage = Stage(spawnInfo.Player);
        return stage >= WeightStage.Obese && stage < WeightStage.MegaBlob && !spawnInfo.Player.dead ? 0.035f : 0f;
    }

    public override void AI()
    {
        if (!NPC.HasPlayerTarget)
            return;

        Player player = Main.player[NPC.target];
        int stage = Stage(player);
        NPC.defense = 8 + stage * 3;
        NPC.lifeRegen = stage >= WeightStage.Immobile ? 6 : stage >= WeightStage.Obese ? 2 : 0;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
    {
        int stage = Stage(target);
        Feed(target, 1.5f + stage * 0.5f, "*leeched heavier*" );
    }
}

public class MirrorNymph : WeightVariantNPC
{
    int _gazeTimer;
    int _gazeStep;

    public override string Texture => $"Terraria/Images/NPC_{NPCID.Nymph}";

    public override void SetStaticDefaults() => Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Nymph];

    public override void SetDefaults()
    {
        NPC.CloneDefaults(NPCID.Nymph);
        AIType = NPCID.Nymph;
        AnimationType = NPCID.Nymph;
        NPC.lifeMax = 260;
        NPC.damage = 12;
        NPC.value = 220f;
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        Player p = spawnInfo.Player;
        bool cavern = p.ZoneRockLayerHeight;
        return cavern && !p.dead ? 0.018f : 0f;
    }

    public override void AI()
    {
        if (!NPC.HasPlayerTarget)
            return;

        Player player = Main.player[NPC.target];
        if (!player.active || player.dead || Vector2.DistanceSquared(player.Center, NPC.Center) > 460f * 460f)
        {
            _gazeTimer = 0;
            return;
        }

        bool facingMirror = (NPC.Center.X >= player.Center.X && player.direction == 1) ||
                            (NPC.Center.X < player.Center.X && player.direction == -1);
        if (!facingMirror)
        {
            _gazeTimer = 0;
            return;
        }

        _gazeTimer++;
        if (_gazeTimer < 120)
            return;

        _gazeTimer = 0;
        _gazeStep++;
        int stage = Stage(player);
        float gain = stage < WeightStage.Fat ? 1.5f : stage < WeightStage.Immobile ? 2.5f : 4f;
        Feed(player, gain, "*reflection softens*" );
        player.AddBuff(BuffID.Regeneration, 180);

        string text = stage switch
        {
            <= WeightStage.Overweight => "The reflection looks a little softer than I remember... I should stop staring at it.",
            <= WeightStage.MorbidlyObese => "Every time I look back, the reflection is broader. I know that's a warning, so why am I still checking?",
            <= WeightStage.BarelyMobile => "It keeps showing me just how much I've changed... and I'm spending more time looking than trying to leave.",
            <= WeightStage.Blob => "I can barely move anymore, but the reflection keeps changing, and somehow I still want to see the next version.",
            _ => "The mirror has nothing left to show me but exactly how far I let this go.",
        };

        if (_gazeStep == 1 || _gazeStep % 3 == 0)
            Say(player, text, Color.MediumPurple);
    }
}
