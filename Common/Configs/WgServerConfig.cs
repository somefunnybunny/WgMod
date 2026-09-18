using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace WgMod.Common.Configs;

public class WgServerConfig : ModConfig
{
    public static WgServerConfig Instance => ModContent.GetInstance<WgServerConfig>();
    public override ConfigScope Mode => ConfigScope.ServerSide;

    [Header("General")]
    [DefaultValue(false)]
    public bool DisableFatBuffs;

    [DefaultValue(false)]
    public bool DisableFatHitbox;

    [DefaultValue(false)]
    public bool DisablePlayerPushing;

    [DefaultValue(false)]
    public bool AlwaysTriggerOnHitEffects;

    [Slider, DrawTicks, Increment(5), Range(0, 100), DefaultValue(20)]
    public int DeathWeightLossPercent;

    [Header("SoulWeight")]
    [DefaultValue(false)]
    public bool EnableSoulWeight;

    [Slider, DrawTicks, Range(WeightStage.Regular, WeightStage.Max), DefaultValue(WeightStage.Regular)]
    public int SoulWeightMinimumStage;

    [Slider, DrawTicks, Range(WeightStage.Regular, WeightStage.Max), DefaultValue(WeightStage.BarelyMobile)]
    public int SoulWeightMaximumStage;
}
