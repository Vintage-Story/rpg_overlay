using System;
using System.Threading.Tasks;
using OpenConfiguration;
using Vintagestory.API.Client;
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

    private IServerNetworkChannel _serverChannel;
    private HudRegionNotification _regionHud;

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        Logger = new ModLogger(api.Logger, "RPGOverlay");
        Logger.Log($"Running on Version: {Mod.Info.Version}");

        overwriter.OverwriteNativeFunctions();
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        Logger.Log("[RegionHUD] StartClientSide called");
        _regionHud = new HudRegionNotification(api);
        api.Network.RegisterChannel("rpgoverlay-region")
            .RegisterMessageType<RegionNotificationPacket>()
            .SetMessageHandler<RegionNotificationPacket>(packet =>
            {
                string name = ResolveRegionName(api, packet);
                string display = Lang.Get("rpgoverlay:region-title", name, packet.Level);
                Logger.LogDebug($"[RegionHUD] Packet received on client: zone={packet.Zone} name='{name}' level={packet.Level}");
                _regionHud.Show(display);
            });
        Logger.Log("[RegionHUD] Client channel registered");
    }

    private static string ResolveRegionName(ICoreClientAPI api, RegionNotificationPacket packet)
    {
        // For "sand-claystone", "gravel-granite" etc., try specific key first then base zone key.
        string zone = packet.Zone ?? "";
        int dash = zone.IndexOf('-');
        string baseZone = dash >= 0 ? zone[..dash] : zone;

        var names = new System.Collections.Generic.List<string>();
        foreach (string candidate in new[] { zone, baseZone })
        {
            for (int i = 0; ; i++)
            {
                string key = $"rpgoverlay:region-zone-{candidate}-{i}";
                string val = Lang.Get(key);
                if (val == key) break;
                names.Add(val);
            }
            if (names.Count > 0) break;
        }

        if (names.Count == 0) return zone;

        int seed = (int)(api.World.Seed ^ ((long)packet.RegionX * 1234567L) ^ ((long)packet.RegionZ * 7654321L));
        return names[Math.Abs(seed) % names.Count];
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);

        Logger.Log("[RegionHUD] StartServerSide called");
        _serverChannel = api.Network.RegisterChannel("rpgoverlay-region")
            .RegisterMessageType<RegionNotificationPacket>();
        Logger.Log("[RegionHUD] Server channel registered");

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

        bool hasDifficulty = api.ModLoader.IsModEnabled("rpgdifficulty");
        Logger.Log($"[RegionHUD] rpgdifficulty installed: {hasDifficulty}");
        if (hasDifficulty)
        {
            // Task is necessary so it will not cry for missing assembly when rpgdifficulty is not present
            Task.Run(() =>
            {
                Logger.Log("[RegionHUD] Subscribing to RegionAPI.OnPlayerEnterRegion");
                RPGDifficulty.RegionAPI.OnPlayerEnterRegion += (player, oldRegion, newRegion) =>
                {
                    Logger.Log($"[RegionHUD] Region changed for {player.PlayerName}: ({newRegion.RegionX},{newRegion.RegionZ}) zone={newRegion.Zone} Level {newRegion.Level}");
                    _serverChannel.SendPacket(new RegionNotificationPacket
                    {
                        Zone = newRegion.Zone,
                        RegionX = newRegion.RegionX,
                        RegionZ = newRegion.RegionZ,
                        Level = newRegion.Level
                    }, player);
                };
                Logger.Log("[RegionHUD] Subscribed to OnPlayerEnterRegion");
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
