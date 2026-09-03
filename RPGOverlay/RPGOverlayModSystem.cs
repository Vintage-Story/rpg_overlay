using System;
using System.Threading.Tasks;
using OpenConfiguration;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
namespace RPGOverlay;

public class RPGOverlayModSystem : ModSystem
{
    private readonly Overwrite overwriter = new();
    internal static ModLogger Logger = ModLogger.None;

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        Logger = new ModLogger(api.Logger, "RPGOverlay");
        Logger.Log($"Running on Version: {Mod.Info.Version}");

        overwriter.OverwriteNativeFunctions();
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);
        if (Configuration.Base.enableLevelUPGlobalLevel && api.ModLoader.IsModEnabled("levelup"))
        {
            // Task is necessary so it will not cry for missing assembly when levelup is not present
            Task.Run(() =>
            {
                EntityOverlay.ShouldEnablePlayerLevel = true;
                foreach (string playerClass in LevelUP.Configuration.ClassExperience.Keys)
                    LevelUP.Configuration.RegisterNewClassLevel(playerClass, "classGlobalLevelMultiply", 1.0f);
                LevelUP.Configuration.RegisterNewLevel("Global", true);
                LevelUP.Configuration.RegisterNewLevelTypeEXP("Global", Configuration.GlobalGetLevelByEXP);
                LevelUP.Configuration.RegisterNewEXPLevelType("Global", Configuration.GlobalGetExpByLevel);
                LevelUP.Configuration.RegisterNewMaxLevelByLevelTypeEXP("Global", 999);
                LevelUP.Server.ExperienceEvents.OnExperienceIncrease += LevelUPOnPlayerExperienceIncrease;
            });
        }
    }

    private static void LevelUPOnPlayerExperienceIncrease(IPlayer player, string type, ref ulong amount)
    {
        if (type != "Global")
        {
            // Increase global levels
            LevelUP.Server.Experience.IncreaseExperience(player, "Global", amount);

            // Check if player level up
            int previousLevel = player.Entity.WatchedAttributes.GetInt("LevelUP_Level_Global");
            ulong exp = LevelUP.Server.Experience.GetExperience(player, "Global");
            int nextLevel = Configuration.GlobalGetLevelByEXP(exp);

            // Update global for level up
            player.Entity.WatchedAttributes.SetLong("LevelUP_Global", (long)exp);
            player.Entity.WatchedAttributes.SetInt("LevelUP_Level_Global", nextLevel);
            // Update global for rpg overlay
            player.Entity.WatchedAttributes.SetInt("RPGOverlayEntityLevel", nextLevel);
        }
    }

    public override void AssetsLoaded(ICoreAPI api)
    {
        base.AssetsLoaded(api);
        Configuration.Load(api);
        Logger.ExtendedLoggingEnabled = Configuration.Base.enableExtendedLogs;
        Logger.Log("Configuration set");
    }

    public static void SetInfoTexts(Entity entity)
    {
        Logger.LogDebug($"Setting Info text for {entity.Code}");
        // Adding the health tier
        if (entity.WatchedAttributes.HasAttribute("extraInfoText"))
        {
            ITreeAttribute entityLookInfoText = entity.WatchedAttributes.GetTreeAttribute("extraInfoText");
            entityLookInfoText.SetString("dmgTier", Lang.Get("Damage tier: {0}", entity.WatchedAttributes.GetInt("RPGOverlayEntityDamageTier")));
            entityLookInfoText.SetString("hpTier", Lang.Get("rpgoverlay:health-tier", entity.WatchedAttributes.GetInt("RPGOverlayEntityHealthTier")));
        }
    }

    public static int CalculateEntityLevel(Entity entity)
    {
        Logger.LogDebug($"Calculating entity {entity.Code} level");

        // Getting level by damage
        int level = (int)Math.Round(entity.WatchedAttributes.GetFloat("RPGOverlayEntityDamage") / Configuration.Base.levelPerDamage);

        Logger.LogDebug($"Level by damage {level}");

        // Getting level by health
        level += (int)Math.Round(entity.WatchedAttributes.GetFloat("RPGOverlayEntityHealth") / Configuration.Base.levelPerHealth);

        Logger.LogDebug($"Level by health and damage {level}");

        return level;
    }

    public override void Dispose()
    {
        base.Dispose();
        overwriter.instance?.UnpatchAll();
    }

    public override double ExecuteOrder()
    {
        return 1.1;
    }
}
