using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WgMod.Content.Buffs.Debuffs;

namespace WgMod.Common.Players;

public partial class BuffHitPlayer : ModPlayer
{
    void AddNPCs(HashSet<int> table, string mod, params string[] npcs)
    {
        if (!ModLoader.TryGetMod(mod, out Mod foundMod))
            return;
        foreach (string npc in npcs)
        {
            if (!foundMod.TryFind(npc, out ModNPC foundNpc))
            {
                Mod.Logger.Warn($"Couldn't find buff '{npc}' for mod '{mod}'");
                continue;
            }
            table.Add(foundNpc.Type);
        }
    }

    public override void Load()
    {
        AddModNPCs();
    }

    void AddBuff(int type, int timeToAdd, Mass weightGain)
    {
        if (!Player.TryGetModPlayer(out WgPlayer wg))
            return;
        Player.AddBuff(type, timeToAdd);
        weightGain = wg.AddWeight(weightGain);
        SoundEngine.PlaySound(WgSounds.Gulp, Player.Center);
        if (weightGain > 0f)
            wg.CombatWeightText(weightGain, true);
    }

    void AddBloated(int timeToAdd, Mass weightGain)
    {
        if (!Player.TryGetModPlayer(out WgPlayer wg) || !Player.TryGetModPlayer(out BloatedPlayer bloated))
            return;

        bloated.ApplyBloated(timeToAdd);
        weightGain = wg.AddWeight(weightGain);
        SoundEngine.PlaySound(WgSounds.Gulp, Player.Center);
        if (weightGain > 0f)
            wg.CombatWeightText(weightGain, true);
    }

    void ConsumeSlime(NPC npc)
    {
        // Scale Force Fed duration from the slime's actual hitbox size. Geometric mean keeps
        // unusually wide or tall slimes from producing absurd durations from one dimension alone.
        float size = MathF.Sqrt(npc.width * npc.height);
        int durationSeconds = Math.Clamp((int)MathF.Round(size / 8f), 3, 60);

        // Green Slime is the 1 kg/cycle reference. Square-root HP scaling keeps huge boss HP
        // meaningful without making every 30-tick feeding cycle explode into four-digit gains.
        int greenSlimeLife = ContentSamples.NpcsByNetId[NPCID.GreenSlime].lifeMax;
        float fatPerCycle = MathF.Sqrt(MathF.Max(1f, npc.lifeMax) / MathF.Max(1f, greenSlimeLife));

        if (Player.TryGetModPlayer(out ForceFedPlayer forceFed))
            forceFed.ApplyCustomForceFed(durationSeconds * 60, fatPerCycle);
        else
            Player.AddBuff(_feedersBuff, durationSeconds * 60);

        SoundEngine.PlaySound(WgSounds.Gulp, Player.Center);

        // Remove the slime without killing it, so this behaves like consumption rather than a kill:
        // no death effects, loot, boss drops, or split-spawns from Mother/Corrupt-type slimes.
        npc.active = false;
        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
    }

    public override void OnHitByNPC(NPC npc, Player.HurtInfo hurtInfo)
    {
        if (_slimes.Contains(npc.type))
        {
            ConsumeSlime(npc);
            return;
        }

        if (_bees.Contains(npc.type))
            AddBloated(60 * 10 + (20 * hurtInfo.Damage), hurtInfo.Damage / 8);

        if (_feeders.Contains(npc.type))
            AddBuff(_feedersBuff, 60 * 3 + (20 * hurtInfo.Damage), hurtInfo.Damage / 6);

        if (npc.type == NPCID.HallowBoss && hurtInfo.Damage < 1250)
            AddBuff(_empressBuff, 4 * hurtInfo.Damage, hurtInfo.Damage / 6);
    }

    public override void OnHitByProjectile(Projectile proj, Player.HurtInfo hurtInfo)
    {
        if (_slimeProjectiles.Contains(proj.type))
            AddBuff(_slimesBuff, 60 * 6 + (10 * hurtInfo.Damage), hurtInfo.Damage / 10);

        if (_bloaters.Contains(proj.type))
            AddBloated(60 * 10 + (10 * hurtInfo.Damage), hurtInfo.Damage / 8);

        if (_feederProjectiles.Contains(proj.type))
            AddBuff(_feedersBuff, 60 * 3 + (10 * hurtInfo.Damage), hurtInfo.Damage / 6);

        if (_empressOfLight.Contains(proj.type) && hurtInfo.Damage < 1250)
            AddBuff(_empressBuff, 3 * hurtInfo.Damage, hurtInfo.Damage / 7);
    }
}
