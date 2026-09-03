using System;
using OpenConfiguration;
using Vintagestory.API.Common;

namespace RPGOverlay;

public class BaseConfiguration
{
    public int healthTierPerHealth = 5;
    public int damageTierPerDamage = 3;
    public int levelPerDamage = 3;
    public int levelPerHealth = 2;
    public int levelUpGlobalEXPPerLevelBase = 200;
    public float levelUpGlobalEXPMultiplyPerLevel = 1.05f;
    public bool enableLevelUPGlobalLevel = false;
    public bool enableExtendedLogs = false;
}

#pragma warning disable CA2211
public static class Configuration
{
    public static BaseConfiguration Base = new();

    internal static void Load(ICoreAPI api)
    {
        Base = ConfigManager.LoadModConfig<BaseConfiguration>(api, "RPGOverlay", "base", RPGOverlayModSystem.Logger, "rpgoverlay:config/base.json");
    }

    public static int GlobalGetLevelByEXP(ulong exp)
    {
        double baseExp = Base.levelUpGlobalEXPPerLevelBase;
        double multiplier = Base.levelUpGlobalEXPMultiplyPerLevel;

        if (multiplier <= 1.0)
        {
            return (int)(exp / baseExp);
        }

        double expDouble = exp;

        double level = Math.Log((expDouble * (multiplier - 1) / baseExp) + 1) / Math.Log(multiplier);

        return Math.Max(0, (int)Math.Floor(level));
    }

    public static ulong GlobalGetExpByLevel(int level)
    {
        double baseExp = Base.levelUpGlobalEXPPerLevelBase;
        double multiplier = Base.levelUpGlobalEXPMultiplyPerLevel;

        if (multiplier == 1.0)
        {
            return (ulong)(baseExp * level);
        }

        double exp = baseExp * (Math.Pow(multiplier, level) - 1) / (multiplier - 1);
        return (ulong)Math.Floor(exp);
    }
}
