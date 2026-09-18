using Terraria.ModLoader.Utilities;
using WgMod.Common.Players;

namespace WgMod.Content.NPCs.Caverns;

/// <summary>
/// Debug-focused Sugar Specter variant with an intentionally enormous Ravenous payload.
/// It never spawns naturally, but can be spawned through NPC debug/cheat tools.
/// </summary>
public class BeltBustingBoogeywoman : SugarSpecter
{
    public override string Texture => "WgMod/Content/NPCs/Caverns/SugarSpecter";

    public override float SpawnChance(NPCSpawnInfo spawnInfo)
    {
        return 0f;
    }

    public override Mass GetRavenousMass(WgPlayer wg)
    {
        // Guarantee that a Normal player with an empty stomach reaches at least Obese:
        // first fill the stomach, then overflow enough mass to reach the start of Obese.
        Mass weightToObese = Weight.FromStage(WeightStage.Obese).Mass - Weight.Base.Mass;
        return WgPlayer.StomachCapacity + weightToObese;
    }
}
