using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Rust.Modular;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ModularCar;

namespace Oxide.Plugins
{
    [Info("Spawn Modular Car", "WhiteThunder", "2.0.1")]
    [Description("Allows players to spawn modular cars.")]
    internal class SpawnModularCar : CovalencePlugin
    {
        #region Fields

        private static SpawnModularCar pluginInstance;

        private PluginData pluginData;
        private PluginConfig pluginConfig;

        private const string DefaultPresetName = "default";
        private const int PresetMaxLength = 30;

        private const string PermissionSpawnSockets2 = "spawnmodularcar.spawn.2";
        private const string PermissionSpawnSockets3 = "spawnmodularcar.spawn.3";
        private const string PermissionSpawnSockets4 = "spawnmodularcar.spawn.4";
        
        private const string PermissionEnginePartsTier1 = "spawnmodularcar.engineparts.tier1";
        private const string PermissionEnginePartsTier2 = "spawnmodularcar.engineparts.tier2";
        private const string PermissionEnginePartsTier3 = "spawnmodularcar.engineparts.tier3";

        private const string PermissionFix = "spawnmodularcar.fix";
        private const string PermissionFetch = "spawnmodularcar.fetch";
        private const string PermissionDespawn = "spawnmodularcar.despawn";
        private const string PermissionAutoFuel = "spawnmodularcar.autofuel";
        private const string PermissionAutoCodeLock = "spawnmodularcar.autocodelock";
        private const string PermissionAutoKeyLock = "spawnmodularcar.autokeylock";
        private const string PermissionDriveUnderwater = "spawnmodularcar.underwater";
        private const string PermissionAutoStartEngine = "spawnmodularcar.autostartengine";
        private const string PermissionAutoFillTankers = "spawnmodularcar.autofilltankers";

        private const string PermissionPresets = "spawnmodularcar.presets";
        private const string PermissionPresetLoad = "spawnmodularcar.presets.load";

        private const string PrefabSockets2 = "assets/content/vehicles/modularcar/2module_car_spawned.entity.prefab";
        private const string PrefabSockets3 = "assets/content/vehicles/modularcar/3module_car_spawned.entity.prefab";
        private const string PrefabSockets4 = "assets/content/vehicles/modularcar/4module_car_spawned.entity.prefab";

        private const string ItemDropPrefab = "assets/prefabs/misc/item drop/item_drop.prefab";
        private const string CodeLockPrefab = "assets/prefabs/locks/keypad/lock.code.prefab";

        private const string RepairEffectPrefab = "assets/bundled/prefabs/fx/build/promote_toptier.prefab";
        private const string TankerFilledEffectPrefab = "assets/prefabs/food/water jug/effects/water-jug-fill-container.prefab";
        private const string CodeLockDeployedEffectPrefab = "assets/prefabs/locks/keypad/effects/lock-code-deploy.prefab";

        private readonly Vector3 CodeLockPosition = new Vector3(-0.9f, 0.35f, -0.5f);

        private const int CodeLockItemId = 1159991980;

        private readonly Dictionary<string, PlayerConfig> PlayerConfigsMap = new Dictionary<string, PlayerConfig>();

        private CooldownManager SpawnCarCooldowns;
        private CooldownManager FixCarCooldowns;
        private CooldownManager FetchCarCooldowns;
        private CooldownManager LoadPresetCooldowns;

        #endregion

        #region Hooks

        private void Init()
        {
            pluginInstance = this;

            pluginData = Interface.Oxide.DataFileSystem.ReadObject<PluginData>(Name);
            pluginConfig = Config.ReadObject<PluginConfig>();

            permission.RegisterPermission(PermissionSpawnSockets2, this);
            permission.RegisterPermission(PermissionSpawnSockets3, this);
            permission.RegisterPermission(PermissionSpawnSockets4, this);

            permission.RegisterPermission(PermissionEnginePartsTier1, this);
            permission.RegisterPermission(PermissionEnginePartsTier2, this);
            permission.RegisterPermission(PermissionEnginePartsTier3, this);

            permission.RegisterPermission(PermissionFix, this);
            permission.RegisterPermission(PermissionFetch, this);
            permission.RegisterPermission(PermissionDespawn, this);
            permission.RegisterPermission(PermissionAutoFuel, this);
            permission.RegisterPermission(PermissionAutoCodeLock, this);
            permission.RegisterPermission(PermissionAutoKeyLock, this);
            permission.RegisterPermission(PermissionDriveUnderwater, this);
            permission.RegisterPermission(PermissionAutoStartEngine, this);
            permission.RegisterPermission(PermissionAutoFillTankers, this);

            permission.RegisterPermission(PermissionPresets, this);
            permission.RegisterPermission(PermissionPresetLoad, this);

            SpawnCarCooldowns = new CooldownManager(pluginConfig.Cooldowns.SpawnSeconds);
            FixCarCooldowns = new CooldownManager(pluginConfig.Cooldowns.FixSeconds);
            FetchCarCooldowns = new CooldownManager(pluginConfig.Cooldowns.FetchSeconds);
            LoadPresetCooldowns = new CooldownManager(pluginConfig.Cooldowns.LoadPresetSeconds);
        }

        private void OnServerInitialized()
        {
            if (pluginConfig.DisableSpawnLimitEnforcement)
                DisableSpawnLimitEnforcement();
        }

        private void Unload()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, pluginData);
            pluginInstance = null;
        }

        private void OnNewSave(string filename)
        {
            pluginData.playerCars.Clear();
            Interface.Oxide.DataFileSystem.WriteObject(Name, pluginData);
        }

        private void OnEntityKill(ModularCar car)
        {
            if (!IsPlayerCar(car)) return;
            
            string userID = pluginData.playerCars.FirstOrDefault(x => x.Value == car.net.ID).Key;
            BasePlayer player = BasePlayer.Find(userID);

            if (player != null)
                ChatMessage(player, "Generic.Info.CarDestroyed");

            pluginData.playerCars.Remove(userID);
        }

        private void OnEntityKill(VehicleModuleSeating seatingModule)
        {
            var car = seatingModule.Vehicle as ModularCar;
            if (car == null) return;

            var codeLock = seatingModule.GetComponentInChildren<CodeLock>();
            if (codeLock == null) return;
            
            codeLock.SetParent(null);
            NextTick(() =>
            {
                var driverModule = car != null ? FindFirstDriverModule(car) : null;
                if (driverModule != null)
                    codeLock.SetParent(driverModule);
                else
                    codeLock.Kill();
            });
        }

        private void OnEntityMounted(BaseMountable mountable, BasePlayer player)
        {
            var car = (mountable as BaseVehicleMountPoint)?.GetVehicleParent() as ModularCar;
            if (car == null || !pluginData.playerCars.ContainsValue(car.net.ID)) return;

            if (car.waterSample.transform.parent != null &&
                car.OwnerID != 0 &&
                permission.UserHasPermission(car.OwnerID.ToString(), PermissionDriveUnderwater))
            {
                // Water sample needs to be updated to enable underwater driving
                // This is necessary because sometimes the water sample is altered, such as on server restart
                EnableCarUnderwater(car);
            }

            if (car.OwnerID == player.userID && car.CanRunEngines())
                car.FinishStartingEngine();
        }

        object CanMountEntity(BasePlayer player, BaseVehicleMountPoint entity)
        {
            var car = entity?.GetVehicleParent() as ModularCar;
            return CanPlayerInteractWithCar(player, car);
        }

        object CanLootEntity(BasePlayer player, StorageContainer container)
        {
            var parent = container.GetParentEntity();
            var car = parent as ModularCar ?? (parent as VehicleModuleStorage)?.Vehicle as ModularCar;
            return CanPlayerInteractWithCar(player, car);
        }

        object CanLootEntity(BasePlayer player, LiquidContainer container)
        {
            var car = (container.GetParentEntity() as VehicleModuleStorage)?.Vehicle as ModularCar;
            return CanPlayerInteractWithCar(player, car);
        }

        object CanLootEntity(BasePlayer player, ModularCarGarage carLift)
        {
            if (!pluginConfig.PreventEditingWhileCodeLockedOut || !carLift.PlatformIsOccupied) return null;
            return CanPlayerInteractWithCar(player, carLift.carOccupant);
        }

        private object CanPlayerInteractWithCar(BasePlayer player, ModularCar car)
        {
            if (car == null || !IsPlayerCar(car)) return null;

            var codeLock = GetCarCodeLock(car);
            if (codeLock == null || IsPlayerAuthorizedToCodeLock(player, codeLock)) return null;

            PlayCodeLockDeniedEffect(codeLock);
            player.ChatMessage(GetMessage(player.IPlayer, "Generic.Error.CarLocked"));

            return false;
        }

        #endregion

        #region Commands

        [Command("mycar")]
        private void MyCarCommand(IPlayer player, string cmd, string[] args)
        {
            if (player.IsServer) return;

            if (args.Length == 0)
            {
                SubCommand_SpawnCar(player, args);
                return;
            }

            switch (args[0].ToLower())
            {
                case "help":
                    SubCommand_Help(player, args.Skip(1).ToArray());
                    return;

                case "list":
                    SubCommand_ListPresets(player, args.Skip(1).ToArray());
                    return;

                case "save":
                    SubCommand_SavePreset(player, args.Skip(1).ToArray());
                    return;

                case "update":
                    SubCommand_UpdatePreset(player, args.Skip(1).ToArray());
                    return;

                case "load":
                    SubCommand_LoadPreset(player, args.Skip(1).ToArray());
                    return;

                case "rename":
                    SubCommand_RenamePreset(player, args.Skip(1).ToArray());
                    return;

                case "delete":
                    SubCommand_DeletePreset(player, args.Skip(1).ToArray());
                    return;

                case "fix":
                    SubCommand_FixCar(player, args.Skip(1).ToArray());
                    return;

                case "fetch":
                    SubCommand_FetchCar(player, args.Skip(1).ToArray());
                    return;

                case "destroy":
                    SubCommand_DestroyCar(player, args.Skip(1).ToArray());
                    return;

                case "autocodelock":
                    SubCommand_ToggleAutoCodeLock(player, args.Skip(1).ToArray());
                    return;

                case "autokeylock":
                    SubCommand_ToggleAutoKeyLock(player, args.Skip(1).ToArray());
                    return;

                case "autofilltankers":
                    SubCommand_ToggleAutoFillTankers(player, args.Skip(1).ToArray());
                    return;

                default:
                    SubCommand_SpawnCar(player, args);
                    return;
            }
        }

        private void SubCommand_Help(IPlayer player, string[] args)
        {
            ushort maxAllowedSockets = GetPlayerMaxAllowedCarSockets(player.Id);
            if (maxAllowedSockets == 0)
            {
                ReplyToPlayer(player, "Generic.Error.NoPermission");
                return;
            }

            var messages = new List<String> { GetMessage(player, "Command.Help") };

            if (permission.UserHasPermission(player.Id, PermissionPresets))
                messages.Add(GetMessage(player, "Command.Help.Spawn.Basic.PresetsAllowed"));
            else
                messages.Add(GetMessage(player, "Command.Help.Spawn.Basic"));

            messages.Add(GetMessage(player, "Command.Help.Spawn.Sockets"));

            if (permission.UserHasPermission(player.Id, PermissionPresets))
                messages.Add(GetMessage(player, "Command.Help.Spawn.Preset"));

            if (permission.UserHasPermission(player.Id, PermissionFix))
                messages.Add(GetMessage(player, "Command.Help.Fix"));

            if (permission.UserHasPermission(player.Id, PermissionFetch))
                messages.Add(GetMessage(player, "Command.Help.Fetch"));

            if (permission.UserHasPermission(player.Id, PermissionDespawn))
                messages.Add(GetMessage(player, "Command.Help.Destroy"));

            if (permission.UserHasPermission(player.Id, PermissionPresets))
            {
                messages.Add(GetMessage(player, "Command.Help.ListPresets"));
                messages.Add(GetMessage(player, "Command.Help.SavePreset"));
                messages.Add(GetMessage(player, "Command.Help.UpdatePreset"));

                if (permission.UserHasPermission(player.Id, PermissionPresetLoad))
                    messages.Add(GetMessage(player, "Command.Help.LoadPreset"));

                messages.Add(GetMessage(player, "Command.Help.RenamePreset"));
                messages.Add(GetMessage(player, "Command.Help.DeletePreset"));
            }

            if (permission.UserHasPermission(player.Id, PermissionAutoCodeLock))
                messages.Add(GetMessage(player, "Command.Help.ToggleAutoCodeLock", BooleanToLocalizedString(player, GetPlayerConfig(player).Settings.AutoCodeLock)));

            if (permission.UserHasPermission(player.Id, PermissionAutoKeyLock))
                messages.Add(GetMessage(player, "Command.Help.ToggleAutoKeyLock", BooleanToLocalizedString(player, GetPlayerConfig(player).Settings.AutoKeyLock)));

            if (permission.UserHasPermission(player.Id, PermissionAutoFillTankers))
                messages.Add(GetMessage(player, "Command.Help.ToggleAutoFillTankers", BooleanToLocalizedString(player, GetPlayerConfig(player).Settings.AutoFillTankers)));

            player.Reply(string.Join("\n", messages));
        }

        private void SubCommand_SpawnCar(IPlayer player, string[] args)
        {
            ushort maxAllowedSockets = GetPlayerMaxAllowedCarSockets(player.Id);
            if (maxAllowedSockets == 0)
            {
                ReplyToPlayer(player, "Generic.Error.NoPermission");
                return;
            }

            var car = FindPlayerCar(player);
            if (car != null)
            {
                var messages = new List<String> { GetMessage(player, "Command.Spawn.Error.CarAlreadyExists") };
                if (permission.UserHasPermission(player.Id, PermissionFetch))
                    messages.Add(GetMessage(player, "Command.Spawn.Error.CarAlreadyExists.Help"));

                player.Reply(string.Join(" ", messages));
                return;
            }

            if (!VerifyOffCooldown(SpawnCarCooldowns, player)) return;
            if (!pluginConfig.CanSpawnBuildingBlocked && !VerifyNotBuildingBlocked(player)) return;

            // Key binds automatically pass the "True" argument
            var wasPassedArgument = args.Length > 0 && args[0] != "True";

            if (wasPassedArgument)
            {
                int desiredSockets;
                if (int.TryParse(args[0], out desiredSockets))
                {
                    if (desiredSockets < 2 || desiredSockets > 4)
                    {
                        ReplyToPlayer(player, "Command.Spawn.Error.SocketSyntax");
                        return;
                    }

                    if (desiredSockets > maxAllowedSockets)
                    {
                        ReplyToPlayer(player, "Generic.Error.NoPermission");
                        return;
                    }

                    SpawnRandomCarForPlayer(player, desiredSockets);
                    return;
                }

                if (!VerifyPermissionAny(player, PermissionPresets)) return;

                var presetName = args[0];

                PlayerCarPreset preset;
                if (!VerifyOnlyOneMatchingPreset(player, presetName, out preset)) return;

                SpawnPresetCarForPlayer(player, preset);
            }
            else
            {
                if (permission.UserHasPermission(player.Id, PermissionPresets))
                {
                    var preset = GetPlayerConfig(player).FindPreset(DefaultPresetName);
                    if (preset != null)
                    {
                        SpawnPresetCarForPlayer(player, preset);
                        return;
                    }
                }

                SpawnRandomCarForPlayer(player, maxAllowedSockets);
            }
        }
        
        private void SubCommand_FixCar(IPlayer player, string[] args)
        {
            if (!VerifyPermissionAny(player, PermissionFix)) return;

            ModularCar car;
            if (!VerifyHasCar(player, out car)) return;
            if (!VerifyCarIsNotDead(player, car)) return;
            if (!VerifyOffCooldown(FixCarCooldowns, player)) return;

            FixCar(car, GetPlayerAllowedFuel(player.Id), GetPlayerEnginePartsTier(player.Id));

            if (ShouldTryFillTankersForPlayer(player))
                TryFillTankerModules(car);
            
            FixCarCooldowns.UpdateLastUsedForPlayer(player.Id);

            MaybePlayCarRepairEffects(car);
            ReplyToPlayer(player, "Command.Fix.Success");
        }

        private void SubCommand_FetchCar(IPlayer player, string[] args)
        {
            if (!VerifyPermissionAny(player, PermissionFetch)) return;
            
            ModularCar car;
            if (!VerifyHasCar(player, out car)) return;
            if (!pluginConfig.CanFetchOccupied && !VerifyCarNotOccupied(player, car)) return;
            if (!VerifyOffCooldown(FetchCarCooldowns, player)) return;
            if (!pluginConfig.CanFetchBuildingBlocked && !VerifyNotBuildingBlocked(player)) return;

            // This is a hacky way to determine that the car is on a lift
            if (car.rigidBody.isKinematic && !TryReleaseCarFromLift(car))
            {
                var messages = new List<String> { GetMessage(player, "Command.Fetch.Error.StuckOnLift") };
                if (permission.UserHasPermission(player.Id, PermissionDespawn))
                    messages.Add(GetMessage(player, "Command.Fetch.Error.StuckOnLift.Help"));

                player.Reply(string.Join(" ", messages));
                return;
            }

            if (pluginConfig.DismountPlayersOnFetch)
                DismountAllPlayersFromCar(car);

            var basePlayer = player.Object as BasePlayer;
            car.transform.SetPositionAndRotation(GetIdealCarPosition(basePlayer), GetIdealCarRotation(basePlayer));
            car.SetVelocity(Vector3.zero);

            FetchCarCooldowns.UpdateLastUsedForPlayer(player.Id);
            ReplyToPlayer(player, "Command.Fetch.Success");
        }

        private void SubCommand_DestroyCar(IPlayer player, string[] args)
        {
            if (!VerifyPermissionAny(player, PermissionDespawn)) return;

            ModularCar car;
            if (!VerifyHasCar(player, out car)) return;
            if (!pluginConfig.CanDespawnOccupied && !VerifyCarNotOccupied(player, car)) return;

            var basePlayer = player.Object as BasePlayer;
            MaybeRemoveMatchingKeysFromPlayer(basePlayer, car);

            var extractedEngineParts = ExtractEnginePartsAboveTierAndDeleteRest(car, GetPlayerEnginePartsTier(player.Id));
            
            car.Kill();

            if (extractedEngineParts.Count > 0)
            {
                GiveItemsToPlayerOrDrop(basePlayer, extractedEngineParts);
                ReplyToPlayer(player, "Generic.Info.PartsRecovered");
            }
        }

        private void SubCommand_ListPresets(IPlayer player, string[] args)
        {
            if (!VerifyPermissionAny(player, PermissionPresets)) return;

            PlayerConfig config = GetPlayerConfig(player);
            if (config.Presets.Count == 0)
            {
                ReplyToPlayer(player, "Generic.Error.NoPresets");
                return;
            }

            var chatMessages = new List<string> { GetMessage(player, "Command.List") };

            // Copy preset list then sort, with "default" at the beginning
            var presetList = config.Presets.Select(p => p).ToList();
            presetList.Sort(SortPresetNames);

            foreach (var preset in presetList)
                chatMessages.Add(GetMessage(player, "Command.List.Item", preset.Name, preset.NumSockets));

            player.Reply(string.Join("\n", chatMessages));
        }

        private void SubCommand_SavePreset(IPlayer player, string[] args)
        {
            if (!VerifyPermissionAny(player, PermissionPresets)) return;

            ModularCar car;
            if (!VerifyHasCar(player, out car)) return;

            var presetName = args.Length == 0 ? DefaultPresetName : args[0];

            PlayerConfig config = GetPlayerConfig(player);
            var existingPreset = config.FindPreset(presetName);
            if (existingPreset != null)
            {
                ReplyToPlayer(player, "Command.SavePreset.Error.PresetAlreadyExists", existingPreset.Name);
                return;
            }

            if (config.Presets.Count >= pluginConfig.MaxPresetsPerPlayer)
            {
                ReplyToPlayer(player, "Command.SavePreset.Error.TooManyPresets", pluginConfig.MaxPresetsPerPlayer);
                return;
            }

            if (presetName.Length > PresetMaxLength)
            {
                ReplyToPlayer(player, "Generic.Error.PresetNameLength", PresetMaxLength);
                return;
            }

            config.SavePreset(PlayerCarPreset.FromCar(car, presetName));
            ReplyToPlayer(player, "Command.SavePreset.Success", presetName);    
        }

        private void SubCommand_UpdatePreset(IPlayer player, string[] args)
        {
            if (!VerifyPermissionAny(player, PermissionPresets)) return;

            ModularCar car;
            if (!VerifyHasCar(player, out car)) return;

            var presetNameArg = args.Length == 0 ? DefaultPresetName : args[0];

            var config = GetPlayerConfig(player);
            var preset = config.FindPreset(presetNameArg);
            if (preset == null)
            {
                ReplyToPlayer(player, "Generic.Error.PresetNotFound", presetNameArg);
                return;
            }

            config.UpdatePreset(PlayerCarPreset.FromCar(car, preset.Name));
            ReplyToPlayer(player, "Command.UpdatePreset.Success", preset.Name);
        }

        private void SubCommand_LoadPreset(IPlayer player, string[] args)
        {
            if (!VerifyPermissionAny(player, PermissionPresetLoad)) return;

            ModularCar car;
            if (!VerifyHasCar(player, out car)) return;
            if (!VerifyCarIsNotDead(player, car)) return;
            if (!VerifyCarNotOccupied(player, car)) return;
            if (!VerifyOffCooldown(LoadPresetCooldowns, player)) return;

            var presetNameArg = args.Length == 0 ? DefaultPresetName : args[0];

            PlayerCarPreset preset;
            if (!VerifyOnlyOneMatchingPreset(player, presetNameArg, out preset)) return;

            var presetNumSockets = preset.NumSockets;
            if (presetNumSockets > GetPlayerMaxAllowedCarSockets(player.Id))
            {
                ReplyToPlayer(player, "Generic.Error.NoPermissionToPresetSocketCount");
                return;
            }

            if (presetNumSockets != car.TotalSockets)
            {
                ReplyToPlayer(player, "Command.LoadPreset.Error.SocketCount", preset.Name, presetNumSockets, car.TotalSockets);
                return;
            }

            var wasEngineOn = car.IsOn();
            var enginePartsTier = GetPlayerEnginePartsTier(player.Id);
            var extractedEngineParts = ExtractEnginePartsAboveTierAndDeleteRest(car, enginePartsTier);
            UpdateCarModules(car, preset.ModuleIDs);
            LoadPresetCooldowns.UpdateLastUsedForPlayer(player.Id);

            var basePlayer = player.Object as BasePlayer;

            NextTick(() => {
                var wereExtraParts = false;

                if (extractedEngineParts.Count > 0)
                {
                    var remainingEngineParts = AddEngineItemsAndReturnRemaining(car, extractedEngineParts);
                    if (remainingEngineParts.Count > 0)
                    {
                        wereExtraParts = true;
                        GiveItemsToPlayerOrDrop(basePlayer, remainingEngineParts);
                    }
                }

                FixCar(car, GetPlayerAllowedFuel(player.Id), enginePartsTier);

                // Restart the engine if it turned off during the brief moment it had no engine or no parts
                if (wasEngineOn && !car.IsOn() && car.CanRunEngines()) car.FinishStartingEngine();

                if (ShouldTryFillTankersForPlayer(player))
                    TryFillTankerModules(car);

                if (car.carLock.HasALock && !car.carLock.CanHaveALock())
                {
                    MaybeRemoveMatchingKeysFromPlayer(basePlayer, car);
                    car.RemoveLock();
                }

                MaybePlayCarRepairEffects(car);

                var chatMessages = new List<String>() { GetMessage(player, "Command.LoadPreset.Success", preset.Name) };
                if (wereExtraParts)
                    chatMessages.Add(GetMessage(player, "Generic.Info.PartsRecovered"));

                player.Reply(String.Join(" ", chatMessages));
            });
        }

        private void SubCommand_RenamePreset(IPlayer player, string[] args)
        {
            if (!VerifyPermissionAny(player, PermissionPresets)) return;

            if (args.Length < 2)
            {
                ReplyToPlayer(player, "Command.RenamePreset.Error.Syntax");
                return;
            }

            var oldName = args[0];
            var newName = args[1];

            PlayerCarPreset preset;
            if (!VerifyHasPreset(player, oldName, out preset)) return;

            // Cache actual old preset name since matching is case-insensitive
            var actualOldPresetName = preset.Name;

            var config = GetPlayerConfig(player);
            PlayerCarPreset existingPresetWithNewName = config.FindPreset(newName);

            if (newName.Length > PresetMaxLength)
            {
                ReplyToPlayer(player, "Generic.Error.PresetNameLength", PresetMaxLength);
                return;
            }

            // Allow renaming if just changing case
            if (existingPresetWithNewName != null && preset != existingPresetWithNewName)
            {
                ReplyToPlayer(player, "Generic.Error.PresetAlreadyTaken", existingPresetWithNewName.Name);
                return;
            }

            config.RenamePreset(preset, newName);
            ReplyToPlayer(player, "Command.RenamePreset.Success", actualOldPresetName, newName);
        }

        private void SubCommand_DeletePreset(IPlayer player, string[] args)
        {
            if (!VerifyPermissionAny(player, PermissionPresets)) return;

            var presetName = args.Length == 0 ? DefaultPresetName : args[0];

            PlayerCarPreset preset;
            if (!VerifyHasPreset(player, presetName, out preset)) return;

            GetPlayerConfig(player).DeletePreset(preset);
            ReplyToPlayer(player, "Command.DeletePreset.Success", preset.Name);
        }

        private void SubCommand_ToggleAutoCodeLock(IPlayer player, string[] args)
        {
            if (!VerifyPermissionAny(player, PermissionAutoCodeLock)) return;

            var config = GetPlayerConfig(player);
            config.Settings.AutoCodeLock = !config.Settings.AutoCodeLock;
            config.SaveData();
            ReplyToPlayer(player, "Command.ToggleAutoCodeLock.Success", BooleanToLocalizedString(player, config.Settings.AutoCodeLock));
        }

        private void SubCommand_ToggleAutoKeyLock(IPlayer player, string[] args)
        {
            if (!VerifyPermissionAny(player, PermissionAutoKeyLock)) return;

            var config = GetPlayerConfig(player);
            config.Settings.AutoKeyLock = !config.Settings.AutoKeyLock;
            config.SaveData();
            ReplyToPlayer(player, "Command.ToggleAutoKeyLock.Success", BooleanToLocalizedString(player, config.Settings.AutoKeyLock));
        }

        private void SubCommand_ToggleAutoFillTankers(IPlayer player, string[] args)
        {
            if (!VerifyPermissionAny(player, PermissionAutoFillTankers)) return;

            var config = GetPlayerConfig(player);
            config.Settings.AutoFillTankers = !config.Settings.AutoFillTankers;
            config.SaveData();
            ReplyToPlayer(player, "Command.ToggleAutoFillTankers.Success", BooleanToLocalizedString(player, config.Settings.AutoFillTankers));
        }

        #endregion

        #region Helper Methods - Command Checks

        private bool VerifyPermissionAny(IPlayer player, params string[] permissionNames)
        {
            foreach (var perm in permissionNames)
            {
                if (!permission.UserHasPermission(player.Id, perm))
                {
                    ReplyToPlayer(player, "Generic.Error.NoPermission");
                    return false;
                }
            }
            return true;
        }

        private bool VerifyNotBuildingBlocked(IPlayer player)
        {
            if ((player.Object as BasePlayer).IsBuildingBlocked())
            {
                ReplyToPlayer(player, "Generic.Error.BuildingBlocked");
                return false;
            }
            return true;
        }

        private bool VerifyHasPreset(IPlayer player, string presetName, out PlayerCarPreset preset)
        {
            preset = GetPlayerConfig(player).FindPreset(presetName);
            if (preset == null)
            {
                ReplyToPlayer(player, "Generic.Error.PresetNotFound", presetName);
                return false;
            }
            return true;
        }

        private bool VerifyHasCar(IPlayer player, out ModularCar car)
        {
            car = FindPlayerCar(player);
            if (car == null)
            {
                ReplyToPlayer(player, "Generic.Error.CarNotFound");
                return false;
            }
            return true;
        }

        private bool VerifyCarNotOccupied(IPlayer player, ModularCar car)
        {
            // Players can either be mounted in seats, or standing on flatbed modules
            if (car.AnyMounted() || car.AttachedModuleEntities.Any(module => module.children.Any(child => child is BasePlayer)))
            {
                ReplyToPlayer(player, "Generic.Error.CarOccupied");
                return false;
            }
            return true;
        }

        private bool VerifyCarIsNotDead(IPlayer player, ModularCar car)
        {
            if (car.IsDead())
            {
                ReplyToPlayer(player, "Generic.Error.CarDead");
                return false;
            }
            return true;
        }

        private bool VerifyOffCooldown(CooldownManager cooldownManager, IPlayer player)
        {
            var secondsRemaining = cooldownManager.GetSecondsRemaining(player.Id);
            if (secondsRemaining > 0)
            {
                ReplyToPlayer(player, "Generic.Error.Cooldown", Math.Ceiling(secondsRemaining));
                return false;
            }
            return true;
        }

        private bool VerifyOnlyOneMatchingPreset(IPlayer player, string presetName, out PlayerCarPreset preset)
        {
            var config = GetPlayerConfig(player.Id);
            preset = config.FindPreset(presetName);
            if (preset != null) return true;

            var matchingPresets = config.FindMatchingPresets(presetName);
            var matchCount = matchingPresets.Count;

            if (matchCount == 0)
            {
                ReplyToPlayer(player, "Generic.Error.PresetNotFound", presetName);
                return false;
            }
            else if (matchCount > 1)
            {
                ReplyToPlayer(player, "Generic.Error.PresetMultipleMatches", presetName);
                return false;
            }

            preset = matchingPresets.First();
            return true;
        }

        #endregion

        #region Helper Methods - Cars

        private bool IsPlayerCar(ModularCar car) =>
            pluginData.playerCars.ContainsValue(car.net.ID);

        private int SortPresetNames(PlayerCarPreset a, PlayerCarPreset b) =>
            a.Name.ToLower() == DefaultPresetName ? -1 :
            b.Name.ToLower() == DefaultPresetName ? 1 :
            a.Name.CompareTo(b.Name);

        private ModularCar FindPlayerCar(IPlayer player)
        {
            if (!pluginData.playerCars.ContainsKey(player.Id))
                return null;

            var car = BaseNetworkable.serverEntities.Find(pluginData.playerCars[player.Id]) as ModularCar;

            // Just in case the car was removed and that somehow wasn't detected sooner
            // This could happen if the data file somehow got out of sync for instance
            if (car == null)
                pluginData.playerCars.Remove(player.Id);

            return car;
        }

        private int[] GetCarModuleIDs(ModularCar car)
        {
            var moduleIDs = new List<int>();

            for (int socketIndex = 0; socketIndex < car.TotalSockets; socketIndex++)
            {
                BaseVehicleModule module;
                if (car.TryGetModuleAt(socketIndex, out module) && module.FirstSocketIndex == socketIndex)
                    moduleIDs.Add(module.AssociatedItemDef.itemid);
                else
                    // Empty socket, use 0 to represent this
                    moduleIDs.Add(0);
            }

            return moduleIDs.ToArray();
        }

        private Vector3 GetIdealCarPosition(BasePlayer player)
        {
            Vector3 forward = player.GetNetworkRotation() * Vector3.forward;
            Vector3 position = player.transform.position + forward.normalized * 3f;
            position.y = player.transform.position.y + 1f;
            return position;
        }

        private Quaternion GetIdealCarRotation(BasePlayer player) =>
            Quaternion.Euler(0, player.GetNetworkRotation().eulerAngles.y - 90, 0);

        private int GetPlayerAllowedFuel(string userID) =>
            permission.UserHasPermission(userID, PermissionAutoFuel) ? pluginConfig.FuelAmount : 0;

        private int GetPlayerEnginePartsTier(string userID)
        {
            if (permission.UserHasPermission(userID, PermissionEnginePartsTier3))
                return 3;
            else if (permission.UserHasPermission(userID, PermissionEnginePartsTier2))
                return 2;
            else if (permission.UserHasPermission(userID, PermissionEnginePartsTier1))
                return 1;
            else
                return 0;
        }

        private ushort GetPlayerMaxAllowedCarSockets(string userID)
        {
            if (permission.UserHasPermission(userID, PermissionSpawnSockets4))
                return 4;
            else if (permission.UserHasPermission(userID, PermissionSpawnSockets3))
                return 3;
            else if (permission.UserHasPermission(userID, PermissionSpawnSockets2))
                return 2;
            else
                return 0;
        }

        private void SpawnRandomCarForPlayer(IPlayer player, int desiredSockets)
        {
            SpawnCarForPlayer(player.Object as BasePlayer, desiredSockets, null, shouldTrackCar: true, onReady: car =>
            {
                var chatMessages = new List<string> { GetMessage(player, "Command.Spawn.Success") };

                if (car.carLock.HasALock)
                    chatMessages.Add(GetMessage(player, "Command.Spawn.Success.Locked"));

                player.Reply(string.Join(" ", chatMessages));
            });
        }

        private void SpawnPresetCarForPlayer(IPlayer player, PlayerCarPreset preset)
        {
            if (preset.NumSockets > GetPlayerMaxAllowedCarSockets(player.Id))
            {
                ReplyToPlayer(player, "Generic.Error.NoPermissionToPresetSocketCount", preset.Name, preset.NumSockets);
                return;
            }

            SpawnCarForPlayer(player.Object as BasePlayer, preset.NumSockets, preset, shouldTrackCar: true, onReady: car =>
            {
                var chatMessages = new List<string> { GetMessage(player, "Command.Spawn.Success.Preset", preset.Name) };

                if (car.carLock.HasALock)
                    chatMessages.Add(GetMessage(player, "Command.Spawn.Success.Locked"));

                ReplyToPlayer(player, string.Join(" ", chatMessages));

                if (preset != null)
                    MaybePlayCarRepairEffects(car);
            });
        }

        private void SpawnCarForPlayer(BasePlayer player, int desiredSockets, PlayerCarPreset preset = null, bool shouldTrackCar = false, Action<ModularCar> onReady = null)
        {
            string prefabName;
            if (desiredSockets == 4) prefabName = PrefabSockets4;
            else if (desiredSockets == 3) prefabName = PrefabSockets3;
            else if (desiredSockets == 2) prefabName = PrefabSockets2;
            else return;

            var position = GetIdealCarPosition(player);
            if (position == null) return;

            ModularCar car = (ModularCar)GameManager.server.CreateEntity(prefabName, position, GetIdealCarRotation(player));
            if (car == null) return;

            if (preset != null)
                car.spawnSettings = MakeSpawnSettings(preset.ModuleIDs);

            car.OwnerID = player.userID;
            car.Spawn();

            if (shouldTrackCar)
            {
                if (permission.UserHasPermission(player.UserIDString, PermissionDriveUnderwater))
                    EnableCarUnderwater(car);

                pluginData.playerCars.Add(player.UserIDString, car.net.ID);
                SpawnCarCooldowns.UpdateLastUsedForPlayer(player.UserIDString);
            }

            NextTick(() =>
            {
                FixCar(car, GetPlayerAllowedFuel(player.UserIDString), GetPlayerEnginePartsTier(player.UserIDString));
                
                if (ShouldTryFillTankersForPlayer(player.IPlayer))
                    TryFillTankerModules(car);

                if (ShouldTryAddCodeLockForPlayer(player.IPlayer))
                    TryAddCodeLockForPlayer(car, player);

                if (ShouldTryAddKeyLockForPlayer(player.IPlayer))
                    TryAddKeyLockCarForPlayer(car, player);

                onReady?.Invoke(car);
            });
        }

        private CodeLock GetCarCodeLock(ModularCar car) =>
            car.GetSlot(BaseEntity.Slot.Lock) as CodeLock;

        private bool IsPlayerAuthorizedToCodeLock(BasePlayer player, CodeLock codeLock) =>
            !codeLock.IsLocked() || 
            codeLock.whitelistPlayers.Contains(player.userID) || 
            codeLock.guestPlayers.Contains(player.userID);

        private void PlayCodeLockDeniedEffect(CodeLock codeLock) =>
            Effect.server.Run(codeLock.effectDenied.resourcePath, codeLock, 0, Vector3.zero, Vector3.forward);
        
        private VehicleModuleSeating FindFirstDriverModule(ModularCar car)
        {
            for (int socketIndex = 0; socketIndex < car.TotalSockets; socketIndex++)
            {
                BaseVehicleModule module;
                if (car.TryGetModuleAt(socketIndex, out module))
                {
                    var seatingModule = module as VehicleModuleSeating;
                    if (seatingModule != null && seatingModule.HasADriverSeat())
                        return seatingModule;
                }
            }
            return null;
        }

        private void EnableCarUnderwater(ModularCar car)
        {
            car.waterSample.transform.SetParent(null);
            car.waterSample.transform.SetPositionAndRotation(Vector3.up * 1000, new Quaternion());
        }

        private void UpdateCarModules(ModularCar car, int[] moduleIDs)
        {
            // Phase 1: Remove all modules that don't match the desired preset
            // This is done first since some modules take up two sockets
            for (int socketIndex = 0; socketIndex < car.TotalSockets; socketIndex++)
            {
                var desiredItemID = moduleIDs[socketIndex];
                var existingItem = car.Inventory.ModuleContainer.GetSlot(socketIndex);
                
                if (existingItem != null && existingItem.info.itemid != desiredItemID)
                {
                    existingItem.RemoveFromContainer();
                    existingItem.Remove();
                }
            }

            // Phase 2: Add the modules that are missing
            for (int socketIndex = 0; socketIndex < car.TotalSockets; socketIndex++)
            {
                var desiredItemID = moduleIDs[socketIndex];
                var existingItem = car.Inventory.ModuleContainer.GetSlot(socketIndex);

                // We are using 0 to represent an empty socket which we skip
                if (existingItem == null && desiredItemID != 0)
                {
                    var moduleItem = ItemManager.CreateByItemID(desiredItemID);
                    if (moduleItem != null)
                        car.TryAddModule(moduleItem, socketIndex);
                }
            }
        }

        private List<Item> AddEngineItemsAndReturnRemaining(ModularCar car, List<Item> engineItems)
        {
            var itemsByType = engineItems
                .GroupBy(item => item.info.GetComponent<ItemModEngineItem>().engineItemType)
                .ToDictionary(
                    grouping => grouping.Key,
                    grouping => grouping.OrderByDescending(item => item.info.GetComponent<ItemModEngineItem>().tier).ToList()
                );

            foreach (var module in car.AttachedModuleEntities)
            {
                var engineStorage = (module as VehicleModuleEngine)?.GetContainer() as EngineStorage;
                if (engineStorage == null) continue;

                for (var slotIndex = 0; slotIndex < engineStorage.inventory.capacity; slotIndex++)
                {
                    var engineItemType = engineStorage.slotTypes[slotIndex];
                    if (!itemsByType.ContainsKey(engineItemType)) continue;

                    var itemsOfType = itemsByType[engineItemType];
                    var existingItem = engineStorage.inventory.GetSlot(slotIndex);
                    if (existingItem != null || itemsOfType.Count == 0) continue;

                    itemsOfType[0].MoveToContainer(engineStorage.inventory, slotIndex, allowStack: false);
                    itemsOfType.RemoveAt(0);
                }
            }

            return itemsByType.Values.SelectMany(x => x).ToList();
        }

        private void AddUpgradeOrRepairEngineParts(EngineStorage engineStorage, int desiredTier)
        {
            if (engineStorage.inventory == null) return;
            
            var inventory = engineStorage.inventory;
            for (var i = 0; i < inventory.capacity; i++)
            {
                var item = inventory.GetSlot(i);
                if (item != null)
                {
                    var component = item.info.GetComponent<ItemModEngineItem>();
                    if (component != null && component.tier < desiredTier)
                    {
                        item.RemoveFromContainer();
                        item.Remove();
                        TryAddEngineItem(engineStorage, i, desiredTier);
                    }
                    else
                        item.condition = item.maxCondition;
                }
                else if (desiredTier > 0)
                    TryAddEngineItem(engineStorage, i, desiredTier);
            }
        }

        private bool TryAddEngineItem(EngineStorage engineStorage, int slot, int tier)
        {
            ItemModEngineItem output;
            if (!engineStorage.allEngineItems.TryGetItem(tier, engineStorage.slotTypes[slot], out output)) return false;

            var component = output.GetComponent<ItemDefinition>();
            var item = ItemManager.Create(component);
            if (item == null) return false;

            item.condition = component.condition.max;
            item.MoveToContainer(engineStorage.inventory, slot, allowStack: false);

            return true;
        }

        private List<Item> ExtractEnginePartsAboveTierAndDeleteRest(ModularCar car, int tier)
        {
            var extractedEngineParts = new List<Item>();

            foreach (var module in car.AttachedModuleEntities)
            {
                var engineStorage = (module as VehicleModuleEngine)?.GetContainer() as EngineStorage;
                if (engineStorage == null) continue;

                for (var i = 0; i < engineStorage.inventory.capacity; i++)
                {
                    var item = engineStorage.inventory.GetSlot(i);
                    if (item == null) continue;

                    var component = item.info.GetComponent<ItemModEngineItem>();
                    if (component == null) continue;

                    item.RemoveFromContainer();

                    if (component.tier > tier)
                        extractedEngineParts.Add(item);
                    else
                        item.Remove();
                }
            }

            return extractedEngineParts;
        }

        private void GiveItemsToPlayerOrDrop(BasePlayer player, List<Item> itemList)
        {
            var itemsToDrop = new List<Item>();

            foreach (var item in itemList)
                if (!player.inventory.GiveItem(item))
                    itemsToDrop.Add(item);

            if (itemsToDrop.Count > 0)
                DropEngineParts(player, itemsToDrop);
        }

        private void DropEngineParts(BasePlayer player, List<Item> itemList)
        {
            if (itemList.Count == 0) return;

            var position = player.GetDropPosition();
            if (itemList.Count == 1)
            {
                itemList[0].Drop(position, player.GetDropVelocity());
                return;
            }

            var container = GameManager.server.CreateEntity(ItemDropPrefab, position, player.GetNetworkRotation()) as DroppedItemContainer;
            if (container == null) return;

            container.playerName = $"{player.displayName}'s Engine Parts";

            // 4 large engines * 8 parts (each damaged) = 32 max engine parts
            // This fits within the standard max size of 36
            var capacity = Math.Min(itemList.Count, container.maxItemCount);

            container.inventory = new ItemContainer();
            container.inventory.ServerInitialize(null, capacity);
            container.inventory.GiveUID();
            container.inventory.entityOwner = container;
            container.inventory.SetFlag(ItemContainer.Flag.NoItemInput, true);

            foreach (var item in itemList)
                if (!item.MoveToContainer(container.inventory))
                    item.DropAndTossUpwards(position);

            container.ResetRemovalTime();
            container.SetVelocity(player.GetDropVelocity());
            container.Spawn();
        }

        private SpawnSettings MakeSpawnSettings(int[] moduleIDs)
        {
            var presetConfig = ScriptableObject.CreateInstance<ModularCarPresetConfig>();
            presetConfig.socketItemDefs = moduleIDs.Select(id =>
            {
                // We are using 0 to represent an empty socket
                if (id == 0) return null;
                return ItemManager.FindItemDefinition(id)?.GetComponent<ItemModVehicleModule>();
            }).ToArray();

            return new SpawnSettings
            {
                useSpawnSettings = true,
                minStartHealthPercent = 100,
                maxStartHealthPercent = 100,
                configurationOptions = new ModularCarPresetConfig[] { presetConfig }
            };
        }

        private void FixCar(ModularCar car, int fuelAmount, int enginePartsTier)
        {
            car.SetHealth(car.MaxHealth());
            car.SendNetworkUpdate();
            AddOrRestoreFuel(car, fuelAmount);

            foreach (var module in car.AttachedModuleEntities)
            {
                module.SetHealth(module.MaxHealth());
                module.SendNetworkUpdate();

                var engineModule = module as VehicleModuleEngine;
                if (engineModule != null)
                {
                    var engineStorage = engineModule.GetContainer() as EngineStorage;
                    AddUpgradeOrRepairEngineParts(engineStorage, enginePartsTier);
                    engineModule.RefreshPerformanceStats(engineStorage);
                }
            }
        }

        private void AddOrRestoreFuel(ModularCar car, int specifiedFuelAmount)
        {
            var fuelContainer = car.fuelSystem.GetFuelContainer();
            var targetFuelAmount = specifiedFuelAmount == -1 ? fuelContainer.allowedItem.stackable : specifiedFuelAmount;
            if (targetFuelAmount == 0) return;

            var fuelItem = fuelContainer.inventory.FindItemByItemID(fuelContainer.allowedItem.itemid);
            if (fuelItem == null)
            {
                fuelContainer.inventory.AddItem(fuelContainer.allowedItem, targetFuelAmount);
            }
            else if (fuelItem.amount < targetFuelAmount)
            {
                fuelItem.amount = targetFuelAmount;
                fuelItem.MarkDirty();
            }
        }

        private bool TryReleaseCarFromLift(ModularCar car)
        {
            RaycastHit hitInfo;
            // This isn't perfect as it can hit other deployables such as rugs
            if (!Physics.SphereCast(car.transform.position + Vector3.up, 1f, Vector3.down, out hitInfo, 1f)) return false;

            var lift = RaycastHitEx.GetEntity(hitInfo) as ModularCarGarage;
            if (lift == null || lift.carOccupant != car) return false;

            // Sometimes the lift grabs the car back after it's released, so we check and re-release a few times
            // This avoids an infinite loop where the car is fetched back onto the same lift
            Timer timerCheckOccupied = null;
            timerCheckOccupied = timer.Repeat(0.1f, 5, () =>
            {
                if (lift != null && car != null && lift.carOccupant == car)
                    lift.ReleaseOccupant();
                else
                    timerCheckOccupied.Destroy();
            });

            return true;
        }

        private void DismountAllPlayersFromCar(ModularCar car)
        {
            // Dismount seated players
            if (car.AnyMounted())
                car.DismountAllPlayers();

            // Dismount players standing on flatbed modules
            foreach (var module in car.AttachedModuleEntities)
                foreach (var child in module.children.ToList())
                    if (child is BasePlayer)
                        (child as BasePlayer).SetParent(null, worldPositionStays: true);
        }

        private bool ShouldTryAddCodeLockForPlayer(IPlayer player) =>
            player.HasPermission(PermissionAutoCodeLock) && GetPlayerConfig(player).Settings.AutoCodeLock;

        private bool TryAddCodeLockForPlayer(ModularCar car, BasePlayer player)
        {
            if (!car.carLock.CanHaveALock()) return false;

            var driverModule = FindFirstDriverModule(car);
            if (driverModule == null) return false;

            var codeLock = GameManager.server.CreateEntity(CodeLockPrefab, CodeLockPosition, Quaternion.identity) as CodeLock;
            if (codeLock == null) return false;

            codeLock.OwnerID = player.userID;
            codeLock.SetParent(driverModule);
            codeLock.Spawn();
            car.SetSlot(BaseEntity.Slot.Lock, codeLock);

            Effect.server.Run(CodeLockDeployedEffectPrefab, codeLock.transform.position);

            // Allow other plugins to detect the lock being deployed (e.g., auto lock)
            var codeLockItem = player.inventory.FindItemID(CodeLockItemId);
            if (codeLockItem != null)
            {
                Interface.CallHook("OnItemDeployed", codeLockItem.GetHeldEntity(), car);
            }
            else
            {
                // Temporarily increase the player inventory capacity to ensure there is enough space
                player.inventory.containerMain.capacity++;
                var temporaryLockItem = ItemManager.CreateByItemID(CodeLockItemId);
                if (player.inventory.GiveItem(temporaryLockItem))
                {
                    Interface.CallHook("OnItemDeployed", temporaryLockItem.GetHeldEntity(), car);
                    temporaryLockItem.RemoveFromContainer();
                }
                temporaryLockItem.Remove();
                player.inventory.containerMain.capacity--;
            }

            return true;
        }

        private bool ShouldTryAddKeyLockForPlayer(IPlayer player) =>
            player.HasPermission(PermissionAutoKeyLock) && GetPlayerConfig(player).Settings.AutoKeyLock;

        private bool TryAddKeyLockCarForPlayer(ModularCar car, BasePlayer player)
        {
            if (car.carLock.HasALock || !car.carLock.CanHaveALock()) return false;
            car.carLock.AddALock();
            car.carLock.TryCraftAKey(player, free: true);
            return true;
        }

        private void MaybeRemoveMatchingKeysFromPlayer(BasePlayer player, ModularCar car)
        {
            if (pluginConfig.DeleteKeyOnDespawn && car.carLock.HasALock)
            {
                var matchingKeys = player.inventory.FindItemIDs(car.carKeyDefinition.itemid)
                    .Where(key => key.instanceData != null && key.instanceData.dataInt == car.carLock.LockID);

                foreach (var key in matchingKeys)
                    key.Remove();
            }
        }

        private bool ShouldTryFillTankersForPlayer(IPlayer player) =>
            player.HasPermission(PermissionAutoFillTankers) && GetPlayerConfig(player).Settings.AutoFillTankers;

        private void TryFillTankerModules(ModularCar car)
        {
            foreach (var module in car.AttachedModuleEntities)
            {
                if (module is VehicleModuleStorage)
                {
                    var container = (module as VehicleModuleStorage).GetContainer();
                    if (container is LiquidContainer)
                    {
                        var liquidContainer = (container as LiquidContainer);

                        // Remove existing liquid such as salt water
                        var liquidItem = liquidContainer.GetLiquidItem();
                        if (liquidItem != null)
                        {
                            liquidItem.RemoveFromContainer();
                            liquidItem.Remove();
                        }

                        liquidContainer.inventory.AddItem(liquidContainer.defaultLiquid, liquidContainer.maxStackSize);

                        if (pluginConfig.EnableEffects)
                            Effect.server.Run(TankerFilledEffectPrefab, module.transform.position);
                    }
                }
            }
        }

        private void MaybePlayCarRepairEffects(ModularCar car)
        {
            if (!pluginConfig.EnableEffects) return;

            if (car.AttachedModuleEntities.Count > 0)
                foreach (var module in car.AttachedModuleEntities)
                    Effect.server.Run(RepairEffectPrefab, module.transform.position);
            else
                Effect.server.Run(RepairEffectPrefab, car.transform.position);
        }

        private void DisableSpawnLimitEnforcement()
        {
            var spawnHandler = SingletonComponent<SpawnHandler>.Instance;
            foreach (var population in spawnHandler.AllSpawnPopulations)
            {
                if (population.name == "ModularCar.Population")
                {
                    if (population.EnforcePopulationLimits)
                    {
                        population.EnforcePopulationLimits = false;
                        Puts("Disabled spawn limit enforcement for: {0}", population.name);
                    }
                    else
                        Puts("Spawn limit enforcement already disabled for: {0}", population.name);
                    break;
                }
            }
        }

        #endregion

        #region Helper Classes

        internal class CooldownManager
        {
            private readonly Dictionary<string, float> CooldownMap = new Dictionary<string, float>();
            private readonly float CooldownDuration;

            public CooldownManager(float duration)
            {
                CooldownDuration = duration;
            }

            public void UpdateLastUsedForPlayer(string userID)
            {
                if (CooldownMap.ContainsKey(userID))
                    CooldownMap[userID] = Time.realtimeSinceStartup;
                else
                    CooldownMap.Add(userID, Time.realtimeSinceStartup);
            }

            public float GetSecondsRemaining(string userID)
            {
                if (!CooldownMap.ContainsKey(userID)) return 0;
                return CooldownMap[userID] + CooldownDuration - Time.realtimeSinceStartup;
            }
        }

        #endregion

        #region Localization

        private string BooleanToLocalizedString(IPlayer player, bool value) =>
            value ? GetMessage(player, "Generic.Setting.On") : GetMessage(player, "Generic.Setting.Off");

        private void ReplyToPlayer(IPlayer player, string messageName, params object[] args) =>
            player.Reply(string.Format(GetMessage(player, messageName), args));

        private void ChatMessage(BasePlayer player, string messageName, params object[] args) =>
            player.ChatMessage(string.Format(GetMessage(player.IPlayer, messageName), args));

        private string GetMessage(IPlayer player, string messageName, params object[] args)
        {
            var message = lang.GetMessage(messageName, this, player.Id);
            return args.Length > 0 ? string.Format(message, args) : message;
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Generic.Setting.On"] = "<color=yellow>ON</color>",
                ["Generic.Setting.Off"] = "<color=#bbb>OFF</color>",
                ["Generic.Error.NoPermission"] = "You don't have permission to use this command.",
                ["Generic.Error.BuildingBlocked"] = "Error: Cannot do that while building blocked.",
                ["Generic.Error.NoPresets"] = "You don't have any saved presets.",
                ["Generic.Error.CarNotFound"] = "Error: You need a car to do that.",
                ["Generic.Error.CarOccupied"] = "Error: Cannot do that while your car is occupied.",
                ["Generic.Error.CarDead"] = "Error: Your car is dead.",
                ["Generic.Error.Cooldown"] = "Please wait <color=yellow>{0}s</color> and try again.",
                ["Generic.Error.NoPermissionToPresetSocketCount"] = "Error: You don't have permission to use preset <color=yellow>{0}</color> because it requires <color=yellow>{1}</color> sockets.",
                ["Generic.Error.PresetNotFound"] = "Error: Preset <color=yellow>{0}</color> not found.",
                ["Generic.Error.PresetMultipleMatches"] = "Error: Multiple presets found matching <color=yellow>{0}</color>. Use <color=yellow>mycar list</color> to view your presets.",
                ["Generic.Error.PresetAlreadyTaken"] = "Error: Preset <color=yellow>{0}</color> is already taken.",
                ["Generic.Error.PresetNameLength"] = "Error: Preset name may not be longer than {0} characters.",
                ["Generic.Error.CarLocked"] = "That vehicle is locked.",
                ["Generic.Info.CarDestroyed"] = "Your modular car was destroyed.",
                ["Generic.Info.PartsRecovered"] = "Recovered engine components were added to your inventory or dropped in front of you.",
                ["Command.Spawn.Error.SocketSyntax"] = "Syntax: <color=yellow>mycar <2|3|4></color>",
                ["Command.Spawn.Error.CarAlreadyExists"] = "Error: You already have a car.",
                ["Command.Spawn.Error.CarAlreadyExists.Help"] = "Try <color=yellow>mycar fetch</color> or <color=yellow>mycar help</color>.",
                ["Command.Spawn.Success"] = "Here is your modular car.",
                ["Command.Spawn.Success.Locked"] = "A matching key was added to your inventory.",
                ["Command.Spawn.Success.Preset"] = "Here is your modular car from preset <color=yellow>{0}</color>.",
                ["Command.Fetch.Error.StuckOnLift"] = "Error: Unable to fetch your car from its lift.",
                ["Command.Fetch.Error.StuckOnLift.Help"] = "You can use <color=yellow>mycar destroy</color> to destroy it.",
                ["Command.Fetch.Success"] = "Here is your modular car.",
                ["Command.Fix.Success"] = "Your car was fixed.",
                ["Command.SavePreset.Error.TooManyPresets"] = "Error: You may not have more than <color=yellow>{0}</color> presets. Please delete another preset and try again. See <color=yellow>mycar help</color>.",
                ["Command.SavePreset.Error.PresetAlreadyExists"] = "Error: Preset <color=yellow>{0}</color> already exists. Use <color=yellow>mycar update {0}</color> to update it.",
                ["Command.SavePreset.Success"] = "Saved car as <color=yellow>{0}</color> preset.",
                ["Command.UpdatePreset.Success"] = "Updated <color=yellow>{0}</color> preset with current module configuration.",
                ["Command.LoadPreset.Error.SocketCount"] = "Error: Unable to load <color=yellow>{0}</color> preset ({1} sockets) because your car has <color=yellow>{2}</color> sockets.",
                ["Command.LoadPreset.Success"] = "Loaded <color=yellow>{0}</color> preset onto your car.",
                ["Command.DeletePreset.Success"] = "Deleted <color=yellow>{0}</color> preset.",
                ["Command.RenamePreset.Error.Syntax"] = "Syntax: <color=yellow>mycar rename <name> <new_name></color>",
                ["Command.RenamePreset.Success"] = "Renamed <color=yellow>{0}</color> preset to <color=yellow>{1}</color>",
                ["Command.List"] = "Your saved modular car presets:",
                ["Command.List.Item"] = "<color=yellow>{0}</color> ({1} sockets)",
                ["Command.ToggleAutoCodeLock.Success"] = "<color=yellow>AutoCodeLock</color> set to {0}",
                ["Command.ToggleAutoKeyLock.Success"] = "<color=yellow>AutoKeyLock</color> set to {0}",
                ["Command.ToggleAutoFillTankers.Success"] = "<color=yellow>AutoFillTankers</color> set to {0}",
                ["Command.Help"] = "<color=orange>SpawnModularCar Command Usages</color>",
                ["Command.Help.Spawn.Basic"] = "<color=yellow>mycar</color> - Spawn a random car with max allowed sockets",
                ["Command.Help.Spawn.Basic.PresetsAllowed"] = "<color=yellow>mycar</color> - Spawn a car using your <color=yellow>default</color> preset if saved, else spawn a random car with max allowed sockets",
                ["Command.Help.Spawn.Sockets"] = "<color=yellow>mycar <2|3|4></color> - Spawn a random car with the specified number of sockets",
                ["Command.Help.Spawn.Preset"] = "<color=yellow>mycar <name></color> - Spawn a car from a saved preset",
                ["Command.Help.Fetch"] = "<color=yellow>mycar fetch</color> - Fetch your car",
                ["Command.Help.Fix"] = "<color=yellow>mycar fix</color> - Fix your car",
                ["Command.Help.Destroy"] = "<color=yellow>mycar destroy</color> - Destroy your car",
                ["Command.Help.ListPresets"] = "<color=yellow>mycar list</color> - List your saved presets",
                ["Command.Help.SavePreset"] = "<color=yellow>mycar save <name></color> - Save your car as a preset",
                ["Command.Help.UpdatePreset"] = "<color=yellow>mycar update <name></color> - Overwrite an existing preset",
                ["Command.Help.LoadPreset"] = "<color=yellow>mycar load <name></color> - Load a preset onto your car",
                ["Command.Help.RenamePreset"] = "<color=yellow>mycar rename <name> <new_name></color> - Rename a preset",
                ["Command.Help.DeletePreset"] = "<color=yellow>mycar delete <name></color> - Delete a preset",
                ["Command.Help.ToggleAutoCodeLock"] = "<color=yellow>mycar autocodelock</color> - Toggle AutoCodeLock: {0}",
                ["Command.Help.ToggleAutoKeyLock"] = "<color=yellow>mycar autokeylock</color> - Toggle AutoKeyLock: {0}",
                ["Command.Help.ToggleAutoFillTankers"] = "<color=yellow>mycar autofilltankers</color> - Toggle automatic filling of tankers with fresh water: {0}",
            }, this);
        }

        #endregion

        #region Configuration

        internal class PluginData
        {
            public Dictionary<string, uint> playerCars = new Dictionary<string, uint>();
        }

        internal class CooldownConfig
        {
            [JsonProperty("SpawnCarSeconds")]
            public float SpawnSeconds = 10;

            [JsonProperty("FetchCarSeconds")]
            public float FetchSeconds = 10;

            [JsonProperty("LoadPresetSeconds")]
            public float LoadPresetSeconds = 10;

            [JsonProperty("FixCarSeconds")]
            public float FixSeconds = 60;
        }

        internal class PluginConfig
        {
            [JsonProperty("CanSpawnWhileBuildingBlocked")]
            public bool CanSpawnBuildingBlocked = false;

            [JsonProperty("CanFetchWhileBuildingBlocked")]
            public bool CanFetchBuildingBlocked = false;

            [JsonProperty("CanFetchWhileOccupied")]
            public bool CanFetchOccupied = false;

            [JsonProperty("CanDespawnWhileOccupied")]
            public bool CanDespawnOccupied = false;

            [JsonProperty("Cooldowns")]
            public CooldownConfig Cooldowns = new CooldownConfig();

            [JsonProperty("DeleteMatchingKeysFromPlayerInventoryOnDespawn")]
            public bool DeleteKeyOnDespawn = true;

            [JsonProperty("DismountPlayersOnFetch")]
            public bool DismountPlayersOnFetch = true;

            [JsonProperty("EnableEffects")]
            public bool EnableEffects = true;

            [JsonProperty("FuelAmount")]
            public int FuelAmount = -1;

            [JsonProperty("MaxPresetsPerPlayer")]
            public int MaxPresetsPerPlayer = 10;

            [JsonProperty("DisableSpawnLimitEnforcement")]
            public bool DisableSpawnLimitEnforcement = false;

            [JsonProperty("PreventEditingWhileCodeLockedOut")]
            public bool PreventEditingWhileCodeLockedOut = false;
        }

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig();
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }

        internal class PlayerCarPreset
        {
            public static PlayerCarPreset FromCar(ModularCar car, string presetName)
            {
                return new PlayerCarPreset
                {
                    Name = presetName,
                    ModuleIDs = pluginInstance.GetCarModuleIDs(car)
                };
            }

            [JsonProperty("Name")]
            public string Name;

            [JsonProperty("ModuleIDs")]
            public int[] ModuleIDs;

            [JsonIgnore]
            public int NumSockets
            {
                get { return ModuleIDs.Length; }
            }
        }

        private PlayerConfig GetPlayerConfig(IPlayer player) =>
            GetPlayerConfig(player.Id);

        private PlayerConfig GetPlayerConfig(string userID)
        {
            if (PlayerConfigsMap.ContainsKey(userID))
                return PlayerConfigsMap[userID];

            PlayerConfig config = PlayerConfig.Get(Name, userID);
            PlayerConfigsMap.Add(userID, config);
            return config;
        }

        internal class PlayerConfig
        {
            public static PlayerConfig Get(string dirPath, string ownerID)
            {
                var filepath = $"{dirPath}/{ownerID}";

                var config = Interface.Oxide.DataFileSystem.ExistsDatafile(filepath) ?
                    Interface.Oxide.DataFileSystem.ReadObject<PlayerConfig>(filepath) :
                    new PlayerConfig(ownerID);

                config.Filepath = filepath;
                return config;
            }

            public static Func<PlayerCarPreset, bool> MatchPresetName(string presetName) =>
                new Func<PlayerCarPreset, bool>(preset => preset.Name.Equals(presetName, StringComparison.CurrentCultureIgnoreCase));

            private string Filepath;

            [JsonProperty("OwnerID")]
            public string OwnerID { get; private set; }

            [JsonProperty("Settings")]
            public PlayerSettings Settings = new PlayerSettings();

            [JsonProperty("Presets")]
            public List<PlayerCarPreset> Presets = new List<PlayerCarPreset>();

            public PlayerCarPreset FindPreset(string presetName) => 
                Presets.FirstOrDefault(MatchPresetName(presetName));

            public List<PlayerCarPreset> FindMatchingPresets(string presetName) =>
                Presets.Where(preset => preset.Name.IndexOf(presetName, StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();

            public bool HasPreset(string presetName) => 
                Presets.Any(MatchPresetName(presetName));

            public void SavePreset(PlayerCarPreset newPreset)
            {
                Presets.Add(newPreset);
                SaveData();
            }

            public void UpdatePreset(PlayerCarPreset newPreset)
            {
                var presetIndex = Presets.FindIndex(new Predicate<PlayerCarPreset>(MatchPresetName(newPreset.Name)));
                if (presetIndex == -1) return;
                Presets[presetIndex] = newPreset;
                SaveData();
            }

            public void RenamePreset(PlayerCarPreset preset, string newName)
            {
                preset.Name = newName;
                SaveData();
            }

            public void DeletePreset(PlayerCarPreset preset)
            {
                Presets.Remove(preset);
                SaveData();
            }

            public PlayerConfig(string ownerID)
            {
                OwnerID = ownerID;
            }

            public void SaveData()
            {
                Interface.Oxide.DataFileSystem.WriteObject(Filepath, this);
            }
        }

        internal class PlayerSettings
        {
            [JsonProperty("AutoCodeLock")]
            public bool AutoCodeLock = false;

            [JsonProperty("AutoKeyLock")]
            public bool AutoKeyLock = false;

            [JsonProperty("AutoFillTankers")]
            public bool AutoFillTankers = false;
        }

        #endregion
    }
}
