using System;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Oxide.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rust.Modular;
using static ModularCar;

namespace Oxide.Plugins
{
    [Info("Spawn Modular Car", "WhiteThunder", "1.3.2")]
    [Description("Allows players to spawn modular cars.")]
    internal class SpawnModularCar : RustPlugin
    {
        #region Fields

        private static SpawnModularCar pluginInstance;

        private PluginData pluginData;
        private PluginConfig pluginConfig;

        private const string DefaultPresetName = "default";

        private const string PermissionSpawnSockets2 = "spawnmodularcar.spawn.2";
        private const string PermissionSpawnSockets3 = "spawnmodularcar.spawn.3";
        private const string PermissionSpawnSockets4 = "spawnmodularcar.spawn.4";
        
        private const string PermissionEnginePartsTier1 = "spawnmodularcar.engineparts.tier1";
        private const string PermissionEnginePartsTier2 = "spawnmodularcar.engineparts.tier2";
        private const string PermissionEnginePartsTier3 = "spawnmodularcar.engineparts.tier3";
        private const string PermissionEnginePartsCleanup = "spawnmodularcar.engineparts.cleanup";

        private const string PermissionFix = "spawnmodularcar.fix";
        private const string PermissionFetch = "spawnmodularcar.fetch";
        private const string PermissionDespawn = "spawnmodularcar.despawn";
        private const string PermissionAutoKeyLock = "spawnmodularcar.autokeylock";

        private const string PermissionPresets = "spawnmodularcar.presets";

        private const string PrefabSockets2 = "assets/content/vehicles/modularcar/2module_car_spawned.entity.prefab";
        private const string PrefabSockets3 = "assets/content/vehicles/modularcar/3module_car_spawned.entity.prefab";
        private const string PrefabSockets4 = "assets/content/vehicles/modularcar/4module_car_spawned.entity.prefab";

        private const string RepairEffectPrefab = "assets/bundled/prefabs/fx/build/promote_toptier.prefab";

        private readonly Dictionary<ulong, PlayerConfig> PlayerConfigsMap = new Dictionary<ulong, PlayerConfig>();

        private CooldownManager SpawnCarCooldowns;
        private CooldownManager FixCarCooldowns;
        private CooldownManager FetchCarCooldowns;
        private CooldownManager LoadPresetCooldowns;

        #endregion

        #region Hooks

        private void Loaded()
        {
            pluginInstance = this;

            pluginData = Interface.Oxide.DataFileSystem.ReadObject<PluginData>("SpawnModularCar");
            pluginConfig = Config.ReadObject<PluginConfig>();

            permission.RegisterPermission(PermissionSpawnSockets2, this);
            permission.RegisterPermission(PermissionSpawnSockets3, this);
            permission.RegisterPermission(PermissionSpawnSockets4, this);

            permission.RegisterPermission(PermissionEnginePartsTier1, this);
            permission.RegisterPermission(PermissionEnginePartsTier2, this);
            permission.RegisterPermission(PermissionEnginePartsTier3, this);
            permission.RegisterPermission(PermissionEnginePartsCleanup, this);

            permission.RegisterPermission(PermissionFix, this);
            permission.RegisterPermission(PermissionFetch, this);
            permission.RegisterPermission(PermissionDespawn, this);
            permission.RegisterPermission(PermissionAutoKeyLock, this);

            permission.RegisterPermission(PermissionPresets, this);

            SpawnCarCooldowns = new CooldownManager(pluginConfig.Cooldowns.SpawnSeconds);
            FixCarCooldowns = new CooldownManager(pluginConfig.Cooldowns.FixSeconds);
            FetchCarCooldowns = new CooldownManager(pluginConfig.Cooldowns.FetchSeconds);
            LoadPresetCooldowns = new CooldownManager(pluginConfig.Cooldowns.LoadPresetSeconds);
        }

        private void Unload()
        {
            Interface.Oxide.DataFileSystem.WriteObject("SpawnModularCar", pluginData);
        }

        private void OnNewSave(string filename)
        {
            pluginData.playerCars.Clear();
            Interface.Oxide.DataFileSystem.WriteObject("SpawnModularCar", pluginData);
        }

        private void OnEntityKill(ModularCar car)
        {
            if (pluginData.playerCars.ContainsValue(car.net.ID))
            {
                string userID = pluginData.playerCars.FirstOrDefault(x => x.Value == car.net.ID).Key;
                
                ulong result;
                if (!ulong.TryParse(userID, out result)) return;

                BasePlayer player = BasePlayer.FindByID(result);

                if (player != null)
                    ChatMessage(player, "Generic.Info.CarDestroyed");

                pluginData.playerCars.Remove(userID);
            }
        }

        #endregion

        #region Commands

        [ChatCommand("mycar")]
        private void MyCarChatCommand(BasePlayer player, string cmd, string[] args)
        {
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

                case "autolock":
                    SubCommand_ToggleAutoLock(player, args.Skip(1).ToArray());
                    return;

                default:
                    SubCommand_SpawnCar(player, args);
                    return;
            }
        }

        private void SubCommand_Help(BasePlayer player, string[] args)
        {
            ushort maxAllowedSockets = GetPlayerMaxAllowedCarSockets(player);
            if (maxAllowedSockets == 0)
            {
                ChatMessage(player, "Generic.Error.NoPermission");
                return;
            }

            var messages = new List<String> { GetMessage(player, "Command.Help") };

            if (permission.UserHasPermission(player.UserIDString, PermissionPresets))
                messages.Add(GetMessage(player, "Command.Help.Spawn.Basic.PresetsAllowed"));
            else
                messages.Add(GetMessage(player, "Command.Help.Spawn.Basic"));

            messages.Add(GetMessage(player, "Command.Help.Spawn.Sockets"));

            if (permission.UserHasPermission(player.UserIDString, PermissionPresets))
                messages.Add(GetMessage(player, "Command.Help.Spawn.Preset"));

            if (permission.UserHasPermission(player.UserIDString, PermissionFix))
                messages.Add(GetMessage(player, "Command.Help.Fix"));

            if (permission.UserHasPermission(player.UserIDString, PermissionFetch))
                messages.Add(GetMessage(player, "Command.Help.Fetch"));

            if (permission.UserHasPermission(player.UserIDString, PermissionDespawn))
                messages.Add(GetMessage(player, "Command.Help.Destroy"));

            if (permission.UserHasPermission(player.UserIDString, PermissionPresets))
            {
                messages.Add(GetMessage(player, "Command.Help.ListPresets"));
                messages.Add(GetMessage(player, "Command.Help.SavePreset"));
                messages.Add(GetMessage(player, "Command.Help.UpdatePreset"));
                messages.Add(GetMessage(player, "Command.Help.LoadPreset"));
                messages.Add(GetMessage(player, "Command.Help.RenamePreset"));
                messages.Add(GetMessage(player, "Command.Help.DeletePreset"));
            }

            if (permission.UserHasPermission(player.UserIDString, PermissionAutoKeyLock))
            {
                var config = GetPlayerConfig(player);
                messages.Add(GetMessage(player, "Command.Help.ToggleAutoLock", BooleanToLocalizedString(player, config.Settings.AutoKeyLock)));
            }

            PrintToChat(player, string.Join("\n", messages));
        }

        private void SubCommand_SpawnCar(BasePlayer player, string[] args)
        {
            ushort maxAllowedSockets = GetPlayerMaxAllowedCarSockets(player);
            if (maxAllowedSockets == 0)
            {
                ChatMessage(player, "Generic.Error.NoPermission");
                return;
            }

            var car = FindPlayerCar(player);
            if (car != null)
            {
                var messages = new List<String> { GetMessage(player, "Command.Spawn.Error.CarAlreadyExists") };
                if (permission.UserHasPermission(player.UserIDString, PermissionFetch))
                    messages.Add(GetMessage(player, "Command.Spawn.Error.CarAlreadyExists.Help"));

                PrintToChat(player, string.Join(" ", messages));
                return;
            }

            if (!VerifyOffCooldown(SpawnCarCooldowns, player)) return;
            if (!pluginConfig.CanSpawnBuildingBlocked && !VerifyNotBuildingBlocked(player)) return;

            if (args.Length == 0)
            {
                if (permission.UserHasPermission(player.UserIDString, PermissionPresets))
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
            else
            {
                int desiredSockets;
                if (int.TryParse(args[0], out desiredSockets))
                {
                    if (desiredSockets < 2 || desiredSockets > 4)
                    {
                        ChatMessage(player, "Command.Spawn.Error.SocketSyntax");
                        return;
                    }

                    if (desiredSockets > maxAllowedSockets)
                    {
                        ChatMessage(player, "Generic.Error.NoPermission");
                        return;
                    }

                    SpawnRandomCarForPlayer(player, desiredSockets);
                    return;
                }

                if (!VerifyPermissionAny(player, PermissionPresets)) return;

                var presetName = args[0];

                CarPreset preset;
                if (!VerifyOnlyOneMatchingPreset(player, presetName, out preset)) return;

                SpawnPresetCarForPlayer(player, preset);
            }
        }
        
        private void SubCommand_FixCar(BasePlayer player, string[] args)
        {
            if (!VerifyPermissionAny(player, PermissionFix)) return;

            ModularCar car;
            if (!VerifyHasCar(player, out car)) return;
            if (!VerifyOffCooldown(FixCarCooldowns, player)) return;

            if (car.carLock.HasALock)
            {
                MaybeRemoveMatchingKeysFromPlayer(player, car);
                car.RemoveLock();
            }

            var shouldCleanupEngineParts = permission.UserHasPermission(player.UserIDString, PermissionEnginePartsCleanup);
            UpdateCarModules(car, GetCarModuleIDs(car), shouldCleanupEngineParts);
            car.AdminFixUp(GetPlayerEnginePartsTier(player));
            FixCarCooldowns.UpdateLastUsedForPlayer(player);

            var chatMessages = new List<string> { GetMessage(player, "Command.Fix.Success") };
            if (MaybeAutoLockCarForPlayer(car, player))
                chatMessages.Add(GetMessage(player, "Command.LoadPreset.Success.Locked"));

            PrintToChat(player, string.Join(" ", chatMessages));
            MaybePlayCarRepairEffects(car);
        }

        private void SubCommand_FetchCar(BasePlayer player, string[] args)
        {
            if (!VerifyPermissionAny(player, PermissionFetch)) return;
            
            ModularCar car;
            if (!VerifyHasCar(player, out car)) return;
            if (!pluginConfig.CanFetchOccupied && !VerifyCarNotOccupied(player, car)) return;

            // This is a hacky way to determine that the car is on a lift
            // Moving a car that is on a lift results in buggy behavior
            // Ideally we could remove it from the lift instead of preventing the fetch
            if (car.rigidBody.isKinematic)
            {
                var messages = new List<String> { lang.GetMessage("Command.Fetch.Error.CarOnLift", this, player.UserIDString) };
                if (permission.UserHasPermission(player.UserIDString, PermissionDespawn))
                    messages.Add(lang.GetMessage("Command.Fetch.Error.CarOnLift.Help", this, player.UserIDString));

                PrintToChat(player, string.Join(" ", messages));
                return;
            }

            if (!VerifyOffCooldown(FetchCarCooldowns, player)) return;
            if (!pluginConfig.CanFetchBuildingBlocked && !VerifyNotBuildingBlocked(player)) return;

            if (pluginConfig.DismountPlayersOnFetch)
                DismountAllPlayersFromCar(car);

            car.transform.SetPositionAndRotation(GetIdealCarPosition(player), GetIdealCarRotation(player));
            car.SetVelocity(Vector3.zero);

            FetchCarCooldowns.UpdateLastUsedForPlayer(player);
            ChatMessage(player, "Command.Fetch.Success");
        }

        private void SubCommand_DestroyCar(BasePlayer player, string[] args)
        {
            if (!VerifyPermissionAny(player, PermissionDespawn)) return;

            ModularCar car;
            if (!VerifyHasCar(player, out car)) return;
            if (!pluginConfig.CanDespawnOccupied && !VerifyCarNotOccupied(player, car)) return;
            
            MaybeRemoveMatchingKeysFromPlayer(player, car);

            // Parts will be dropped automatically if not deleted
            if (permission.UserHasPermission(player.UserIDString, PermissionEnginePartsCleanup))
                foreach (var moduleEntity in car.AttachedModuleEntities)
                    if (moduleEntity != null && moduleEntity is VehicleModuleEngine)
                        (moduleEntity as VehicleModuleEngine).GetContainer().inventory.Kill();

            car.Kill();
        }

        private void SubCommand_ListPresets(BasePlayer player, string[] args)
        {
            if (!VerifyPermissionAny(player, PermissionPresets)) return;

            PlayerConfig config = GetPlayerConfig(player);
            if (config.Presets.Count == 0)
            {
                ChatMessage(player, "Generic.Error.NoPresets");
                return;
            }

            var chatMessages = new List<string> { lang.GetMessage("Command.List", this, player.UserIDString) };

            // Copy preset list then sort, with "default" at the beginning
            var presetList = config.Presets.Select(p => p).ToList();
            presetList.Sort((a, b) => (a.Name == "default") ? -1 : a.Name.CompareTo(b.Name));

            foreach (var preset in presetList)
                chatMessages.Add(GetMessage(player, "Command.List.Item", preset.Name, preset.NumSockets));

            PrintToChat(player, string.Join("\n", chatMessages));
        }

        private void SubCommand_SavePreset(BasePlayer player, string[] args)
        {
            if (!VerifyPermissionAny(player, PermissionPresets)) return;

            ModularCar car;
            if (!VerifyHasCar(player, out car)) return;

            var presetName = args.Length == 0 ? DefaultPresetName : args[0];

            PlayerConfig config = GetPlayerConfig(player);
            var existingPreset = config.FindPreset(presetName);
            if (existingPreset != null)
            {
                ChatMessage(player, "Command.SavePreset.Error.PresetAlreadyExists", existingPreset.Name);
                return;
            }

            if (config.Presets.Count >= pluginConfig.MaxPresetsPerPlayer)
            {
                ChatMessage(player, "Command.SavePreset.Error.TooManyPresets", pluginConfig.MaxPresetsPerPlayer);
                return;
            }

            config.SavePreset(CarPreset.FromCar(car, presetName));
            ChatMessage(player, "Command.SavePreset.Success", presetName);    
        }

        private void SubCommand_UpdatePreset(BasePlayer player, string[] args)
        {
            if (!VerifyPermissionAny(player, PermissionPresets)) return;

            ModularCar car;
            if (!VerifyHasCar(player, out car)) return;

            var presetNameArg = args.Length == 0 ? DefaultPresetName : args[0];

            var config = GetPlayerConfig(player);
            var preset = config.FindPreset(presetNameArg);
            if (preset == null)
            {
                ChatMessage(player, "Generic.Error.PresetNotFound", presetNameArg);
                return;
            }

            config.UpdatePreset(CarPreset.FromCar(car, preset.Name));
            ChatMessage(player, "Command.UpdatePreset.Success", preset.Name);
        }

        private void SubCommand_LoadPreset(BasePlayer player, string[] args)
        {
            if (!VerifyPermissionAny(player, PermissionPresets)) return;

            ModularCar car;
            if (!VerifyHasCar(player, out car)) return;
            if (!VerifyCarNotOccupied(player, car)) return;
            if (!VerifyOffCooldown(LoadPresetCooldowns, player)) return;

            var presetNameArg = args.Length == 0 ? DefaultPresetName : args[0];

            CarPreset preset;
            if (!VerifyOnlyOneMatchingPreset(player, presetNameArg, out preset)) return;

            var presetNumSockets = preset.NumSockets;
            if (presetNumSockets > GetPlayerMaxAllowedCarSockets(player))
            {
                ChatMessage(player, "Generic.Error.NoPermissionToPresetSocketCount");
                return;
            }

            if (presetNumSockets != car.TotalSockets)
            {
                ChatMessage(player, "Command.LoadPreset.Error.SocketCount", preset.Name, presetNumSockets, car.TotalSockets);
                return;
            }

            if (car.carLock.HasALock)
            {
                MaybeRemoveMatchingKeysFromPlayer(player, car);
                car.RemoveLock();
            }

            var shouldCleanupEngineParts = permission.UserHasPermission(player.UserIDString, PermissionEnginePartsCleanup);
            UpdateCarModules(car, preset.ModuleIDs, shouldCleanupEngineParts);

            NextTick(() => {
                car.AdminFixUp(GetPlayerEnginePartsTier(player));

                var chatMessages = new List<string> { GetMessage(player, "Command.LoadPreset.Success", preset.Name) };
                if (MaybeAutoLockCarForPlayer(car, player))
                    chatMessages.Add(GetMessage(player, "Command.LoadPreset.Success.Locked"));

                PrintToChat(player, string.Join(" ", chatMessages));
                MaybePlayCarRepairEffects(car);
            });

            LoadPresetCooldowns.UpdateLastUsedForPlayer(player);
        }

        private void SubCommand_RenamePreset(BasePlayer player, string[] args)
        {
            if (!VerifyPermissionAny(player, PermissionPresets)) return;

            if (args.Length < 2)
            {
                ChatMessage(player, "Command.RenamePreset.Error.Syntax");
                return;
            }

            var oldName = args[0];
            var newName = args[1];

            CarPreset preset;
            if (!VerifyHasPreset(player, oldName, out preset)) return;

            // Cache actual old preset name since matching is case-insensitive
            var actualOldPresetName = preset.Name;

            var config = GetPlayerConfig(player);
            CarPreset existingPresetWithNewName = config.FindPreset(newName);
            
            // Allow renaming if just changing case
            if (existingPresetWithNewName != null && preset != existingPresetWithNewName)
            {
                ChatMessage(player, "Generic.Error.PresetAlreadyTaken", existingPresetWithNewName.Name);
                return;
            }

            config.RenamePreset(preset, newName);
            ChatMessage(player, "Command.RenamePreset.Success", actualOldPresetName, newName);
        }

        private void SubCommand_DeletePreset(BasePlayer player, string[] args)
        {
            if (!VerifyPermissionAny(player, PermissionPresets)) return;

            var presetName = args.Length == 0 ? DefaultPresetName : args[0];

            CarPreset preset;
            if (!VerifyHasPreset(player, presetName, out preset)) return;

            GetPlayerConfig(player).DeletePreset(preset);
            ChatMessage(player, "Command.DeletePreset.Success", preset.Name);
        }

        private void SubCommand_ToggleAutoLock(BasePlayer player, string[] args)
        {
            if (!VerifyPermissionAny(player, PermissionAutoKeyLock)) return;

            var config = GetPlayerConfig(player);
            config.Settings.AutoKeyLock = !config.Settings.AutoKeyLock;
            config.SaveData();
            ChatMessage(player, "Command.AutoKeyLock.Success", BooleanToLocalizedString(player, config.Settings.AutoKeyLock));
        }

        #endregion

        #region Helper Methods and Classes

        private bool VerifyPermissionAny(BasePlayer player, params string[] permissionNames)
        {
            foreach (var perm in permissionNames)
            {
                if (!permission.UserHasPermission(player.UserIDString, perm))
                {
                    ChatMessage(player, "Generic.Error.NoPermission");
                    return false;
                }
            }
            return true;
        }

        private bool VerifyNotBuildingBlocked(BasePlayer player)
        {
            if (player.IsBuildingBlocked())
            {
                ChatMessage(player, "Generic.Error.BuildingBlocked");
                return false;
            }
            return true;
        }

        private bool VerifyHasPreset(BasePlayer player, string presetName, out CarPreset preset)
        {
            preset = GetPlayerConfig(player).FindPreset(presetName);
            if (preset == null)
            {
                ChatMessage(player, "Generic.Error.PresetNotFound", presetName);
                return false;
            }
            return true;
        }

        private bool VerifyHasCar(BasePlayer player, out ModularCar car)
        {
            car = FindPlayerCar(player);
            if (car == null)
            {
                ChatMessage(player, "Generic.Error.CarNotFound");
                return false;
            }
            return true;
        }

        private bool VerifyCarNotOccupied(BasePlayer player, ModularCar car)
        {
            // Players can either be mounted in seats, or standing on flatbed modules
            if (car.AnyMounted() || car.AttachedModuleEntities.Any(module => module.children.Any(child => child is BasePlayer)))
            {
                ChatMessage(player, "Generic.Error.CarOccupied");
                return false;
            }
            return true;
        }

        private bool VerifyOffCooldown(CooldownManager cooldownManager, BasePlayer player)
        {
            var secondsRemaining = cooldownManager.GetSecondsRemaining(player);
            if (secondsRemaining > 0)
            {
                ChatMessage(player, "Generic.Error.Cooldown", Math.Ceiling(secondsRemaining));
                return false;
            }
            return true;
        }

        private bool VerifyOnlyOneMatchingPreset(BasePlayer player, string presetName, out CarPreset preset)
        {
            var config = GetPlayerConfig(player);
            preset = config.FindPreset(presetName);
            if (preset != null) return true;

            var matchingPresets = config.FindMatchingPresets(presetName);
            var matchCount = matchingPresets.Count;

            if (matchCount == 0)
            {
                ChatMessage(player, "Generic.Error.PresetNotFound", presetName);
                return false;
            }
            else if (matchCount > 1)
            {
                ChatMessage(player, "Generic.Error.PresetMultipleMatches", presetName);
                return false;
            }

            preset = matchingPresets.First();
            return true;
        }

        private string BooleanToLocalizedString(BasePlayer player, bool value) =>
            value ? GetMessage(player, "Generic.Setting.On") : GetMessage(player, "Generic.Setting.Off");

        private void ChatMessage(BasePlayer player, string messageName, params object[] args) =>
            PrintToChat(player, GetMessage(player, messageName), args);

        private string GetMessage(BasePlayer player, string messageName, params object[] args)
        {
            var message = lang.GetMessage(messageName, this, player.UserIDString);
            return args.Length > 0 ? string.Format(message, args) : message;
        }

        private ModularCar FindPlayerCar(BasePlayer player)
        {
            if (!pluginData.playerCars.ContainsKey(player.UserIDString))
                return null;

            var car = BaseNetworkable.serverEntities.Find(pluginData.playerCars[player.UserIDString]) as ModularCar;

            // Just in case the car was removed and that somehow wasn't detected sooner
            // This could happen if the data file somehow got out of sync for instance
            if (car == null)
                pluginData.playerCars.Remove(player.UserIDString);

            return car;
        }

        private int GetPlayerEnginePartsTier(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, PermissionEnginePartsTier3))
                return 3;
            else if (permission.UserHasPermission(player.UserIDString, PermissionEnginePartsTier2))
                return 2;
            else if (permission.UserHasPermission(player.UserIDString, PermissionEnginePartsTier1))
                return 1;
            else
                return 0;
        }

        private ushort GetPlayerMaxAllowedCarSockets(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, PermissionSpawnSockets4))
                return 4;
            else if (permission.UserHasPermission(player.UserIDString, PermissionSpawnSockets3))
                return 3;
            else if (permission.UserHasPermission(player.UserIDString, PermissionSpawnSockets2))
                return 2;
            else
                return 0;
        }

        private void SpawnRandomCarForPlayer(BasePlayer player, int desiredSockets)
        {
            SpawnCarForPlayer(player, desiredSockets, null, car =>
            {
                var chatMessages = new List<string> { GetMessage(player, "Command.Spawn.Success") };

                if (car.carLock.HasALock)
                    chatMessages.Add(GetMessage(player, "Command.Spawn.Success.Locked"));

                PrintToChat(player, string.Join(" ", chatMessages));
            });
        }

        private void SpawnPresetCarForPlayer(BasePlayer player, CarPreset preset)
        {
            if (preset.NumSockets > GetPlayerMaxAllowedCarSockets(player))
            {
                ChatMessage(player, "Generic.Error.NoPermissionToPresetSocketCount", preset.Name, preset.NumSockets);
                return;
            }

            SpawnCarForPlayer(player, preset.NumSockets, preset, car =>
            {
                var chatMessages = new List<string> { GetMessage(player, "Command.Spawn.Success.Preset", preset.Name) };

                if (car.carLock.HasALock)
                    chatMessages.Add(GetMessage(player, "Command.Spawn.Success.Locked"));

                PrintToChat(player, string.Join(" ", chatMessages));

                if (preset != null)
                    MaybePlayCarRepairEffects(car);
            });
        }

        private void SpawnCarForPlayer(BasePlayer player, int desiredSockets, CarPreset preset = null, Action<ModularCar> onReady = null)
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

            car.Spawn();

            car.OwnerID = player.userID;
            pluginData.playerCars.Add(player.UserIDString, car.net.ID);

            SpawnCarCooldowns.UpdateLastUsedForPlayer(player);

            NextTick(() =>
            {
                car.AdminFixUp(GetPlayerEnginePartsTier(player));
                MaybeAutoLockCarForPlayer(car, player);
                onReady?.Invoke(car);
            });
        }

        private SpawnSettings MakeSpawnSettings(List<int> moduleIDs)
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

        private void MaybePlayCarRepairEffects(ModularCar car)
        {
            if (!pluginConfig.EnableEffects) return;

            if (car.AttachedModuleEntities.Count > 0)
                foreach (var module in car.AttachedModuleEntities)
                    Effect.server.Run(RepairEffectPrefab, module.transform.position);
            else
                Effect.server.Run(RepairEffectPrefab, car.transform.position);
        }

        private bool MaybeAutoLockCarForPlayer(ModularCar car, BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, PermissionAutoKeyLock) && 
                GetPlayerConfig(player).Settings.AutoKeyLock && 
                car.carLock.CanHaveALock())
            {
                car.carLock.AddALock();
                car.carLock.TryCraftAKey(player, free: true);
                return true;
            }
            return false;
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

        private List<int> GetCarModuleIDs(ModularCar car)
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

            return moduleIDs;
        }

        private void UpdateCarModules(ModularCar car, List<int> moduleIDs, bool shouldCleanupEngineParts = false)
        {
            // Phase 1: Remove all modules that don't match the desired preset
            // This is done first since some modules take up two sockets
            for (int socketIndex = 0; socketIndex < car.TotalSockets; socketIndex++)
            {
                var desiredItemID = moduleIDs[socketIndex];
                var existingItem = car.Inventory.ModuleContainer.GetSlot(socketIndex);
                if (existingItem == null) continue;

                var moduleEntity = car.GetModuleForItem(existingItem);
                var isEngineModule = moduleEntity != null && moduleEntity is VehicleModuleEngine;

                if (isEngineModule && !shouldCleanupEngineParts)
                    (moduleEntity as VehicleModuleEngine).GetContainer().DropItems();

                if (existingItem.info.itemid != desiredItemID)
                {
                    // Only have to do this when removing the module since it's done automatically otherwise
                    if (isEngineModule && shouldCleanupEngineParts)
                        (moduleEntity as VehicleModuleEngine).GetContainer().inventory.Kill();

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
                if (existingItem != null || desiredItemID == 0) continue;

                var moduleItem = ItemManager.CreateByItemID(desiredItemID);
                if (moduleItem == null) continue;

                car.TryAddModule(moduleItem, socketIndex);
            }
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

        private PlayerConfig GetPlayerConfig(BasePlayer player)
        {
            if (PlayerConfigsMap.ContainsKey(player.userID))
                return PlayerConfigsMap[player.userID];

            PlayerConfig config = PlayerConfig.Get(Name, player.userID);
            PlayerConfigsMap.Add(player.userID, config);
            return config;
        }

        private Vector3 GetIdealCarPosition(BasePlayer player)
        {
            Quaternion playerRotation = player.GetNetworkRotation();
            Vector3 forward = playerRotation * Vector3.forward;
            Vector3 straight = Vector3.Cross(Vector3.Cross(Vector3.up, forward), Vector3.up).normalized;
            Vector3 position = player.transform.position + straight * 3f;
            position.y = player.transform.position.y + 1f;
            return position;
        }

        private Quaternion GetIdealCarRotation(BasePlayer player) =>
            Quaternion.Euler(0, player.GetNetworkRotation().eulerAngles.y - 90, 0);

        internal class CooldownManager
        {
            private readonly Dictionary<ulong, float> CooldownMap = new Dictionary<ulong, float>();
            private readonly float CooldownDuration;

            public CooldownManager(float duration)
            {
                CooldownDuration = duration;
            }

            public void UpdateLastUsedForPlayer(BasePlayer player)
            {
                if (CooldownMap.ContainsKey(player.userID))
                    CooldownMap[player.userID] = Time.realtimeSinceStartup;
                else
                    CooldownMap.Add(player.userID, Time.realtimeSinceStartup);
            }

            public float GetSecondsRemaining(BasePlayer player)
            {
                if (!CooldownMap.ContainsKey(player.userID)) return 0;
                return CooldownMap[player.userID] + CooldownDuration - Time.realtimeSinceStartup;
            }
        }

        #endregion

        #region Localization

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
                ["Generic.Error.Cooldown"] = "Please wait <color=yellow>{0}s</color> and try again.",
                ["Generic.Error.NoPermissionToPresetSocketCount"] = "Error: You don't have permission to use preset <color=yellow>{0}</color> because it requires <color=yellow>{1}</color> sockets.",
                ["Generic.Error.PresetNotFound"] = "Error: Preset <color=yellow>{0}</color> not found.",
                ["Generic.Error.PresetMultipleMatches"] = "Error: Multiple presets found matching <color=yellow>{0}</color>. Use <color=yellow>/mycar list</color> to view your presets.",
                ["Generic.Error.PresetAlreadyTaken"] = "Error: Preset <color=yellow>{0}</color> is already taken.",
                ["Generic.Info.CarDestroyed"] = "Your modular car was destroyed.",
                ["Command.Spawn.Error.SocketSyntax"] = "Syntax: <color=yellow>/mycar <2|3|4></color>",
                ["Command.Spawn.Error.CarAlreadyExists"] = "Error: You already have a car.",
                ["Command.Spawn.Error.CarAlreadyExists.Help"] = "Try <color=yellow>/mycar fetch</color> or <color=yellow>/mycar help</color>.",
                ["Command.Spawn.Success"] = "Here is your modular car.",
                ["Command.Spawn.Success.Locked"] = "A matching key was added to your inventory.",
                ["Command.Spawn.Success.Preset"] = "Here is your modular car from preset <color=yellow>{0}</color>.",
                ["Command.Fetch.Error.CarOnLift"] = "Error: Cannot fetch your car while it's on a lift.",
                ["Command.Fetch.Error.CarOnLift.Help"] = "You can use <color=yellow>/mycar destroy</color> to destroy it.",
                ["Command.Fetch.Success"] = "Here is your modular car.",
                ["Command.Fix.Success"] = "Your car was fixed.",
                ["Command.SavePreset.Error.TooManyPresets"] = "Error: You may not have more than <color=yellow>{0}</color> presets. Please delete another preset and try again. See <color=yellow>/mycar help</color>.",
                ["Command.SavePreset.Error.PresetAlreadyExists"] = "Error: Preset <color=yellow>{0}</color> already exists. Use <color=yellow>/mycar update {0}</color> to update it.",
                ["Command.SavePreset.Success"] = "Saved car as <color=yellow>{0}</color> preset.",
                ["Command.UpdatePreset.Success"] = "Updated <color=yellow>{0}</color> preset with current module configuration.",
                ["Command.LoadPreset.Error.SocketCount"] = "Error: Unable to load <color=yellow>{0}</color> preset ({1} sockets) because your car has <color=yellow>{2}</color> sockets.",
                ["Command.LoadPreset.Success"] = "Loaded <color=yellow>{0}</color> preset onto your car.",
                ["Command.LoadPreset.Success.Locked"] = "Locks were replaced and a key was added to your inventory.",
                ["Command.DeletePreset.Success"] = "Deleted <color=yellow>{0}</color> preset.",
                ["Command.RenamePreset.Error.Syntax"] = "Syntax: <color=yellow>/mycar rename <name> <new_name></color>",
                ["Command.RenamePreset.Success"] = "Renamed <color=yellow>{0}</color> preset to <color=yellow>{1}</color>",
                ["Command.List"] = "Your saved modular car presets:",
                ["Command.List.Item"] = "<color=yellow>{0}</color> ({1} sockets)",
                ["Command.AutoKeyLock.Success"] = "<color=yellow>AutoLock</color> set to {0}",
                ["Command.Help"] = "<color=orange>SpawnModularCar Command Usages</color>",
                ["Command.Help.Spawn.Basic"] = "<color=yellow>/mycar</color> - Spawn a random car with max allowed sockets",
                ["Command.Help.Spawn.Basic.PresetsAllowed"] = "<color=yellow>/mycar</color> - Spawn a car using your <color=yellow>default</color> preset if saved, else spawn a random car with max allowed sockets",
                ["Command.Help.Spawn.Sockets"] = "<color=yellow>/mycar <2|3|4></color> - Spawn a random car with the specified number of sockets",
                ["Command.Help.Spawn.Preset"] = "<color=yellow>/mycar <name></color> - Spawn a car from a saved preset",
                ["Command.Help.Fetch"] = "<color=yellow>/mycar fetch</color> - Fetch your car",
                ["Command.Help.Fix"] = "<color=yellow>/mycar fix</color> - Fixes your car",
                ["Command.Help.Destroy"] = "<color=yellow>/mycar destroy</color> - Destroy your car",
                ["Command.Help.ListPresets"] = "<color=yellow>/mycar list</color> - List your saved module configuration presets",
                ["Command.Help.SavePreset"] = "<color=yellow>/mycar save <name></color> - Save your car as a preset",
                ["Command.Help.UpdatePreset"] = "<color=yellow>/mycar update <name></color> - Overwrite an existing preset",
                ["Command.Help.LoadPreset"] = "<color=yellow>/mycar load <name></color> - Load a preset onto your car",
                ["Command.Help.RenamePreset"] = "<color=yellow>/mycar rename <name> <new_name></color> - Rename a preset",
                ["Command.Help.DeletePreset"] = "<color=yellow>/mycar delete <name></color> - Delete a preset",
                ["Command.Help.ToggleAutoLock"] = "<color=yellow>/mycar autolock</color> - Toggle auto lock: {0}",
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

            [JsonProperty("MaxPresetsPerPlayer")]
            public int MaxPresetsPerPlayer = 10;
        }

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig();
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }

        internal class CarPreset
        {
            public static CarPreset FromCar(ModularCar car, string presetName)
            {
                return new CarPreset
                {
                    Name = presetName,
                    ModuleIDs = pluginInstance.GetCarModuleIDs(car)
                };
            }

            [JsonProperty("Name")]
            public string Name { get; set; }

            [JsonProperty("ModuleIDs")]
            public List<int> ModuleIDs { get; set; }

            [JsonIgnore]
            public int NumSockets
            {
                get { return ModuleIDs.Count; }
            }
        }

        internal class PlayerSettings
        {
            [JsonProperty("AutoKeyLock")]
            public bool AutoKeyLock = false;
        }

        internal class PlayerConfig
        {
            public static PlayerConfig Get(string dirPath, ulong ownerID)
            {
                var filepath = $"{dirPath}/{ownerID}";

                var config = Interface.Oxide.DataFileSystem.ExistsDatafile(filepath) ?
                    Interface.Oxide.DataFileSystem.ReadObject<PlayerConfig>(filepath) :
                    new PlayerConfig(ownerID);

                config.Filepath = filepath;
                return config;
            }

            public static Func<CarPreset, bool> MatchPresetName(string presetName) =>
                new Func<CarPreset, bool>(preset => preset.Name.Equals(presetName, StringComparison.CurrentCultureIgnoreCase));

            private string Filepath;

            [JsonProperty("OwnerID")]
            public ulong OwnerID { get; private set; }

            [JsonProperty("Settings")]
            public PlayerSettings Settings = new PlayerSettings();

            [JsonProperty("Presets")]
            public List<CarPreset> Presets = new List<CarPreset>();

            public CarPreset FindPreset(string presetName) => 
                Presets.FirstOrDefault(MatchPresetName(presetName));

            public List<CarPreset> FindMatchingPresets(string presetName) =>
                Presets.Where(preset => preset.Name.IndexOf(presetName, StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();

            public bool HasPreset(string presetName) => 
                Presets.Any(MatchPresetName(presetName));

            public void SavePreset(CarPreset newPreset)
            {
                Presets.Add(newPreset);
                SaveData();
            }

            public void UpdatePreset(CarPreset newPreset)
            {
                var presetIndex = Presets.FindIndex(new Predicate<CarPreset>(MatchPresetName(newPreset.Name)));
                if (presetIndex == -1) return;
                Presets[presetIndex] = newPreset;
                SaveData();
            }

            public void RenamePreset(CarPreset preset, string newName)
            {
                preset.Name = newName;
                SaveData();
            }

            public void DeletePreset(CarPreset preset)
            {
                Presets.Remove(preset);
                SaveData();
            }

            public PlayerConfig( ulong ownerID)
            {
                OwnerID = ownerID;
            }

            public void SaveData()
            {
                Interface.Oxide.DataFileSystem.WriteObject(Filepath, this);
            }
        }

        #endregion
    }
}
