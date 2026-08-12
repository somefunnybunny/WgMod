using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using WgMod.Content.NPCs.Caverns;
using WgMod.Content.NPCs.UndergroundDesert;

namespace WgMod.Content.Buffs.Debuffs;

public class EnemyDebuffSpawnControl : GlobalNPC
{
    public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
    {
        Player player = spawnInfo.Player;
        int foodTier = OverindulgenceChain.GetTier(player);
        int spiritTier = PossessionChain.GetTier(player);

        float otherMultiplier = Math.Min(
            OverindulgenceChain.GetOtherSpawnMultiplier(player),
            PossessionChain.GetOtherSpawnMultiplier(player)
        );

        if (otherMultiplier >= 1f)
            return;

        int foodType = ModContent.NPCType<HomingFood>();
        int spiritType = ModContent.NPCType<SweetSpirit>();
        bool preserveFood = foodTier > 0;
        bool preserveSpirit = spiritTier > 0;

        int[] npcTypes = new int[pool.Count];
        pool.Keys.CopyTo(npcTypes, 0);
        foreach (int npcType in npcTypes)
        {
            if ((preserveFood && npcType == foodType) || (preserveSpirit && npcType == spiritType))
                continue;

            pool[npcType] *= otherMultiplier;
        }
    }

    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
    {
        int attractorTier = Math.Max(
            OverindulgenceChain.GetTier(player),
            PossessionChain.GetTier(player)
        );

        switch (attractorTier)
        {
            case 1:
                maxSpawns += 2;
                break;
            case 2:
                maxSpawns += 3;
                break;
            case 3:
                spawnRate = Math.Max(1, spawnRate / 2);
                maxSpawns += 4;
                break;
            case 4:
                spawnRate = Math.Max(1, spawnRate / 2);
                maxSpawns += 6;
                break;
            case 5:
                spawnRate = Math.Max(1, spawnRate / 3);
                maxSpawns += 8;
                break;
            case 6:
                spawnRate = Math.Max(1, spawnRate / 4);
                maxSpawns += 10;
                break;
            case 7:
                spawnRate = Math.Max(1, spawnRate / 5);
                maxSpawns += 12;
                break;
            case >= 8:
                spawnRate = Math.Max(1, spawnRate / 6);
                maxSpawns += 15;
                break;
        }
    }
}
