using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WgMod.Content.Buffs.Debuffs;

namespace WgMod.Content.NPCs.Caverns;

public class StrangeBee : ModNPC
{
    public override string Texture => $"Terraria/Images/NPC_{NPCID.BeeSmall}";

    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.BeeSmall];
    }

    public override void SetDefaults()
    {
        NPC.CloneDefaults(NPCID.BeeSmall);
        AIType = NPCID.BeeSmall;
        AnimationType = NPCID.BeeSmall;
        NPC.value = 0f;
    }

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        Player player = spawnInfo.Player;
        if (!player.ZoneJungle || player.dead || player.HasBuff(ModContent.BuffType<StrangeFeeling>()))
            return 0f;

        if (player.TryGetModPlayer(out StrangeFeelingPlayer strangeFeeling) && strangeFeeling.Active)
            return 0f;

        // Rare enough to read as an ordinary small bee most of the time it is encountered.
        return 0.002f;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
    {
        target.AddBuff(ModContent.BuffType<StrangeFeeling>(), 60 * 30);
        if (target.TryGetModPlayer(out StrangeFeelingPlayer strangeFeeling))
            strangeFeeling.Activate();

        // The bee's only job is to deliver the strange sting. Removing it immediately also
        // prevents repeated contact from restarting or visually revealing the gimmick.
        NPC.active = false;
        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
    }
}
