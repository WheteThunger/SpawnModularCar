**SpawnModularCar** allows players to spawn modular cars.

- Each player can have one car at a time using `mycar`.
- Multiple types of presets are supported, including personal presets.
- API and an admin/server command allow spawning unlimited cars with a variety of options.

## Upcoming backwards-incompatible changes

##### 5.0.0
- Planning to remove compatibility with [Modular Car Code Locks](https://umod.org/plugins/modular-car-code-locks). This means that deploying code locks to spawned cars will no longer work unless you have [Vehicle Deployed Locks](https://umod.org/plugins/vehicle-deployed-locks) installed, so please migrate to that if you use the code lock feature. The plugins are very similar so migrating should be easy.

## Recommended related plugins
- [Vehicle Deployed Locks](https://umod.org/plugins/vehicle-deployed-locks) - Deploy code locks or key locks to vehicles. Integrates with this plugin to allow automatically deploying code locks to cars spawned by privileged players
- [Engine Parts Durability](https://umod.org/plugins/engine-parts-durability) - Reduce or disable engine part durability (so they no longer take damage when the car does)
- [No Engine Parts](https://umod.org/plugins/no-engine-parts) - Allow car engines to work without engine parts. Supports multiple use cases, including super speed
- [Larger Car Storage](https://umod.org/plugins/larger-car-storage) - Increase capacity of storage modules on cars owned by privileged players
- [Vehicle Decay Protection](https://umod.org/plugins/vehicle-decay-protection) - Reduce or disable vehicle decay in various situations
- [Car Spawn Settings](https://umod.org/plugins/car-spawn-settings) - Configure modules, health, fuel, and engine parts that random cars spawn with. Module presets defined in that plugin will also apply when players spawn random cars with this plugin
- [Craft Car Chassis](https://umod.org/plugins/craft-car-chassis) - Allow players to craft a blank chassis at a car lift

## Commands

### Spawn commands
- `mycar` -- Spawn a modular car using your "default" preset if saved, else spawn a random car with the maximum number of allowed sockets based on your permissions.
- `mycar <2|3|4>` -- Spawn a random modular car with the specified number of sockets.
- `mycar <name>` -- Spawn a modular car from the specified personal preset. See the personal presets section for more details.
- `mycar common <name>` -- Spawn a modular car from a common preset managed by privileged adminstrators. See the common presets section for more details.
- `givecar <player> <preset>` -- Spawn a car for the target player using the specified server-wide preset. See the server presets configuration section for details.

When a modular car is spawned, it will be at full health. Additionally, depending on your granted permissions and personal settings:
- It may be fueled (amount is configurable).
- It may automatically have a code lock.
- It may automatically have a key lock, with a matching key added to your inventory.
- All tanker modules may be automatically filled with fresh water (amount is configurable).
- All engine modules may be automatically filled with your highest allowed quality of engine components.

### Other commands

- `mycar fix` -- Fix your car. This restores it to original condition as though you had just spawned it, with the exception that it will not add or remove a lock, regardless of your `AutoCodeLock` or `AutoKeyLock` setting. Engine components will also be repaired. If you were granted any of the `spawnmodularcar.engineparts.*` permissions, missing engine components are added, and lower quality components are replaced with the maximum quality you are allowed.
- `mycar fetch` -- Teleport your car to you in an upright position.
- `mycar destroy` -- Destroy your car, allowing you to spawn a new one.
  - Non-empty storage modules will drop a bag with their items next to where the car was located.
  - If the car's engine modules contained components that are of higher quality than are allowed by your `spawnmodularcar.engineparts.*` permissions, those will be added to your inventory if there is space, else dropped to the ground in front of you.
- `mycar help` -- Print a list of available commands and their usage. Only shows commands allowed by your permissions.

### Personal presets

Players with the `spawnmodularcar.presets` permission can save custom module configurations, allowing them to spawn their car or change their car in-place with a saved preset.

- `mycar list` -- List your saved module configuration presets.
- `mycar save <name>` -- Save your car's current module configuration under the specified preset name.
- `mycar update <name>` -- Overwrite an existing preset with your car's current module configuration.
- `mycar load <name>` -- Load the specified preset (partial name matching supported) onto your existing car, replacing any modules that don't match the preset. Loading a preset is not allowed if your current car is occupied or if it has a different number of sockets than the preset.
  - The car will be fixed according to the same rules as `mycar fix`.
  - The car's locks will be removed if the car no longer has at least one cockpit module.
  - Non-empty storage modules that were replaced will drop a bag with their items next to the car.
  - Any engine components of higher quality than are allowed by your `spawnmodularcar.engineparts.*` permissions will be redistributed throughout the engine modules in the loaded preset, with the highest quality parts towards the front-most engine modules. If any engine components are left over, those will be added to your inventory if there is space, else dropped to the ground in front of you.
- `mycar rename <name> <new name>` -- Rename a preset.
- `mycar delete <name>` -- Delete the specified preset.

Note: The `save`, `update`, `load` and `delete` commands will use the "default" preset if the preset name is not specified.

### Common presets

Players with the `spawnmodularcar.presets.common.manage` permission can optionally save their cars as common presets for the rest of the server to use. Any player with the `spawnmodularcar.presets.common` permission can list common presets and spawn a car from a common preset, as long as the player's `spawnmodularcar.spawn.*` permission allows for the number of sockets in the preset.

- `mycar common list` -- List all common presets that you have permission to spawn, based on your `spawnmodularcar.spawn.*` permission.
- `mycar common load <name>` -- Load the specified common preset onto your existing car. See the personal preset section for details on how loading works.
- `mycar common save <name>` -- Save your car's current module configuration under the specified common preset name.
- `mycar common update <name>` -- Overwrite an existing common preset with your car's current module configuration.
- `mycar common rename <name> <new name>` -- Rename a common preset.
- `mycar delete <name>` -- Delete the specified common preset.

### Personal settings

- `mycar autocodelock` -- Toggle AutoCodeLock. While ON, spawning your car will automatically create a code lock and add it to the front-most cockpit module if it has one. Requires `spawnmodularcar.autocodelock` permission.
- `mycar autokeylock` -- Toggle AutoKeyLock. While ON, spawning your car will automatically create a key lock and add a matching key to your inventory, as long as the car has at least one cockpit module. Requires `spawnmodularcar.autokeylock` permission.
- `mycar autofilltankers` -- Toggle AutoFillTankers. While ON, spawning your car, fixing it, or loading a preset will automatically fill any tanker modules to allowed capacity (configurable) with fresh water, replacing any salt water already in them. Requires `spawnmodularcar.autofilltankers` permission.

## Permissions

The following permissions control the maximum number of module sockets the player is allowed to have on their car. If none of these are granted, the player cannot spawn a car, even from a preset.
- `spawnmodularcar.spawn.2` -- Allows spawning a car with 2 sockets.
- `spawnmodularcar.spawn.3` -- Allows spawning a car with 2-3 sockets.
- `spawnmodularcar.spawn.4` -- Allows spawning a car with 2-4 sockets.

Granting the following permissions will cause the player's car to automatically spawn with engine components of the corresponding quality in each engine module. If none of these are granted, the car spawns without any engine components, so it cannot be driven until a player adds them.
- `spawnmodularcar.engineparts.tier1` -- Spawn your car with low quality engine components.
- `spawnmodularcar.engineparts.tier2` -- Spawn your car with medium quality engine components.
- `spawnmodularcar.engineparts.tier3` -- Spawn your car with high quality engine components.

Presets:
- `spawnmodularcar.presets` -- Allows you to spawn your car from a personal preset. Also enables the `save`, `update`, `rename` and `delete` preset commands.
- `spawnmodularcar.presets.load` -- Required to use `mycar load <preset>` and `mycar common load <preset>`.
- `spawnmodularcar.presets.common` -- Required to use `mycar common <preset>` and `mycar common load <preset>`.
- `spawnmodularcar.presets.common.manage` -- Required to manage common presets.
- `spawnmodularcar.givecar` -- Required to use the `givecar` command.

Misc:
- `spawnmodularcar.fix` -- Required to use `mycar fix`.
- `spawnmodularcar.fetch` -- Required to use `mycar fetch`.
- `spawnmodularcar.despawn` -- Required to use `mycar destroy`.
- `spawnmodularcar.autofuel` -- Fuels your car automatically.
- `spawnmodularcar.autocodelock` -- Required to use the automatic code lock feature (i.e., `mycar autocodelock`).
- `spawnmodularcar.autokeylock` -- Required to use the automatic key lock feature (i.e., `mycar autokeylock`).
- `spawnmodularcar.autofilltankers` - Required to use automatic filling of tanker modules (i.e. `mycar autofilltankers`).
- `spawnmodularcar.autostartengine` -- Start your car's engine instantly.

## Configuration

```json
{
  "CanDespawnWhileOccupied": false,
  "CanFetchWhileBuildingBlocked": false,
  "CanFetchWhileOccupied": false,
  "CanSpawnWhileBuildingBlocked": false,
  "Cooldowns": {
    "FetchCarSeconds": 10.0,
    "FixCarSeconds": 60.0,
    "LoadPresetSeconds": 10.0,
    "SpawnCarSeconds": 10.0
  },
  "DeleteMatchingKeysFromPlayerInventoryOnDespawn": true,
  "DismountPlayersOnFetch": true,
  "EnableEffects": true,
  "FreshWaterAmount": -1,
  "FuelAmount": -1,
  "MaxPresetsPerPlayer": 10,
  "Presets": []
}
```

- `CanDespawnWhileOccupied` (`true` or `false`) -- Whether to allow players to use `mycar destroy` while their car is occupied. If `true`, despawning a car will safely eject players from seating and flatbed modules.
- `CanFetchWhileOccupied` (`true` or `false`) -- Whether to allow players to fetch their car while it's occupied. If `true`, fetchinig a car will safely eject players from seating and flatbed modules if `DismountPlayersOnFetch` is `true`, else those players will be teleported with the car.
- `CanFetchWhileBuildingBlocked` (`true` or `false`) -- Whether to allow players to fetch their car while they are building blocked. Recommended to be set to `false` to avoid exploits where people use the car to get through a wall.
- `CanSpawnWhileBuildingBlocked` (`true` or `false`) -- Whether to allow players to spawn their car while they are building blocked. Recommended to be set to `false` to avoid exploits where people use the car to get through a wall.
- `Cooldowns` -- Various cooldowns for balancing. These were primarily implemented to prevent spamming, so they are not currently tracked across plugin reloads or server restarts, so setting them very high (e.g., hours or days) may not always work as intended.
- `DeleteMatchingKeysFromPlayerInventoryOnDespawn` (`true` or `false`) -- Whether to delete all matching keys from the owner player's inventory when they use `mycar destroy`. Also applies to when they use `mycar load` and the lock is removed because the preset contains no cockpit modules. I recommend this be set to `true`, especially if you are allowing players to use the automatic key lock feature since that spawns keys which may otherwise clutter the player's inventory.
- `DismountPlayersOnFetch` (`true` or `false`) -- Whether to dismount all players from a car when it is fetched. Has no effect unless `CanFetchWhileOccupied` is also `true`.
- `EnableEffects` (`true` or `false`) -- Enable audio and visual effects when spawning a car from a preset, using `mycar fix` or `mycar load`.
- `FreshWaterAmount` -- The amount of fresh water to add to each of the car's tanker modules when spawning. Only applies if the player has the `spawnmodularcar.autofilltankers` permission and has the AutoFillTankers setting on. Defaults to `-1` which represents maximum stack size. Note: If for some reason the car has multiple tankers (i.e., no other modules), this amount will be added to each one.
- `FuelAmount` -- The amount of low grade fuel to add to the fuel tank when spawning. Only applies if the player has the `spawnmodularcar.autofuel` permission. When fixing the car or loading a preset, only the amount missing will be added to reach this minimum target value. Defaults to `-1` which represents maximum stack size.
- `MaxPresetsPerPlayer` -- The maximum number of module configuration presets each player is allowed to save.

### Server presets

Server presets must be manually defined in the plugin's configuration file. A car can be spawned for a player using a server preset with the `givecar <player> <preset>` command. Cars spawned this way will use the settings that are provided in the preset, instead of using the general plugin configuration, player permissions and player settings. These cars do not count towards the player limit of one car, nor can they be spawned or interacted with using `mycar`.

Here is an example list of presets to get you started.

```json
"Presets": [
  {
    "Name": "LongChassis",
    "Options": {
      "Modules": [0, 0, 0, 0]
    }
  },
  {
    "Name": "TourBus",
    "Options": {
      "Modules": [
        "vehicle.1mod.cockpit.with.engine",
        "vehicle.2mod.passengers",
        "vehicle.1mod.rear.seats"
      ],
      "EnginePartsTier": 2,
      "FuelAmount": 100,
      "CodeLock": true
    }
  },
  {
    "Name": "ArmoredCar",
    "Options": {
      "Modules": [
        "vehicle.1mod.engine",
        "vehicle.1mod.cockpit.armored",
        "vehicle.1mod.cockpit.armored"
      ],
      "EnginePartsTier": 3,
      "FuelAmount": 500,
      "KeyLock": true
    }
  },
  {
    "Name": "DoubleTanker",
    "Options": {
      "Modules": [
        "vehicle.2mod.fuel.tank",
        "vehicle.2mod.fuel.tank"
      ],
      "FreshWaterAmount": 50000
    }
  }
],
```

Here are all of the available options you can define per preset. The only required option is `Modules`. The rest default to `0` or `false`.

- `Modules` -- List of module item ids or short names that will be added to the car automatically. The number `0` represents an empty socket. Item names and ids can be found on the [uMod item list page](https://umod.org/documentation/games/rust/definitions).
  - Previously this field was named `ModuleIDs`. That name still works for backwards compatibility but it only accepts ids.
- `CodeLock` (`true` or `false`) -- Whether to deploy a code lock to the car.
- `KeyLock` (`true` or `false`) -- Whether to create a key lock on the car and add a matching key to the player's inventory.
- `EnginePartsTier` (`0` - `3`) -- The quality of engine components to automatically add to all engine modules (`0` for no engine components).
- `FuelAmount` -- The amount of fuel to put in the fuel tank (`-1` for max).
- `FreshWaterAmount` -- The amount of fresh water to add to each tanker module if applicable (`-1` for max).

## Localization

```json
{
  "Generic.Setting.On": "<color=yellow>ON</color>",
  "Generic.Setting.Off": "<color=#bbb>OFF</color>",
  "Generic.Error.NoPermission": "You don't have permission to use this command.",
  "Generic.Error.LocationRestricted": "Error: Cannot do that here.",
  "Generic.Error.BuildingBlocked": "Error: Cannot do that while building blocked.",
  "Generic.Error.NoPresets": "You don't have any saved presets.",
  "Generic.Error.NoCommonPresets": "There are no common presets.",
  "Generic.Error.CarNotFound": "Error: You need a car to do that.",
  "Generic.Error.CarOccupied": "Error: Cannot do that while your car is occupied.",
  "Generic.Error.Cooldown": "Please wait <color=yellow>{0}s</color> and try again.",
  "Generic.Error.NoPermissionToPresetSocketCount": "Error: You don't have permission to use preset <color=yellow>{0}</color> because it requires <color=yellow>{1}</color> sockets.",
  "Generic.Error.PresetNotFound": "Error: Preset <color=yellow>{0}</color> not found.",
  "Generic.Error.PresetMultipleMatches": "Error: Multiple presets found matching <color=yellow>{0}</color>. Use <color=yellow>mycar list</color> to view your presets.",
  "Generic.Error.PresetAlreadyTaken": "Error: Preset <color=yellow>{0}</color> is already taken.",
  "Generic.Error.PresetNameLength": "Error: Preset name may not be longer than {0} characters.",
  "Generic.Info.CarDestroyed": "Your modular car was destroyed.",
  "Generic.Info.PartsRecovered": "Recovered engine components were added to your inventory or dropped in front of you.",
  "Command.Spawn.Error.SocketSyntax": "Syntax: <color=yellow>mycar <2|3|4></color>",
  "Command.Spawn.Error.CarAlreadyExists": "Error: You already have a car.",
  "Command.Spawn.Error.CarAlreadyExists.Help": "Try <color=yellow>mycar fetch</color> or <color=yellow>mycar help</color>.",
  "Command.Spawn.Success": "Here is your modular car.",
  "Command.Spawn.Success.Locked": "A matching key was added to your inventory.",
  "Command.Spawn.Success.Preset": "Here is your modular car from preset <color=yellow>{0}</color>.",
  "Command.Fix.Success": "Your car was fixed.",
  "Command.Fetch.Error.StuckOnLift": "Error: Unable to fetch your car from its lift.",
  "Command.Fetch.Error.StuckOnLift.Help": "You can use <color=yellow>mycar destroy</color> to destroy it.",
  "Command.Fetch.Success": "Here is your modular car.",
  "Command.SavePreset.Error.TooManyPresets": "Error: You may not have more than <color=yellow>{0}</color> presets. You may delete another preset and try again. See <color=yellow>mycar help</color>.",
  "Command.SavePreset.Error.PresetAlreadyExists": "Error: Preset <color=yellow>{0}</color> already exists. Use <color=yellow>mycar update {0}</color> to update it.",
  "Command.SavePreset.Success": "Saved car as <color=yellow>{0}</color> preset.",
  "Command.UpdatePreset.Success": "Updated <color=yellow>{0}</color> preset with current module configuration.",
  "Command.LoadPreset.Error.SocketCount": "Error: Unable to load <color=yellow>{0}</color> preset (<color=yellow>{1}</color> sockets) because your car has <color=yellow>{2}</color> sockets.",
  "Command.LoadPreset.Success": "Loaded <color=yellow>{0}</color> preset onto your car.",
  "Command.DeletePreset.Success": "Deleted <color=yellow>{0}</color> preset.",
  "Command.RenamePreset.Error.Syntax": "Syntax: <color=yellow>mycar rename <name> <new_name></color>",
  "Command.RenamePreset.Success": "Renamed <color=yellow>{0}</color> preset to <color=yellow>{1}</color>",
  "Command.List": "Your saved modular car presets:",
  "Command.List.Item": "<color=yellow>{0}</color> ({1} sockets)",
  "Command.Common.List": "Common modular car presets:",
  "Command.Common.Error.Syntax": "Try <color=yellow>mycar help</color>",
  "Command.Common.LoadPreset.Error.Syntax": "Syntax: <color=yellow>mycar common load <name></color>",
  "Command.Common.SavePreset.Error.Syntax": "Syntax: <color=yellow>mycar common save <name></color>",
  "Command.Common.SavePreset.Error.PresetAlreadyExists": "Error: Common preset <color=yellow>{0}</color> already exists. Use <color=yellow>mycar common update {0}</color> to update it.",
  "Command.Common.UpdatePreset.Error.Syntax": "Syntax: <color=yellow>mycar common update <name></color>",
  "Command.Common.RenamePreset.Error.Syntax": "Syntax: <color=yellow>mycar common rename <name> <new_name></color>",
  "Command.Common.DeletePreset.Error.Syntax": "Syntax: <color=yellow>mycar common delete <name></color>",
  "Command.ToggleAutoCodeLock.Success": "<color=yellow>AutoCodeLock</color> set to {0}",
  "Command.ToggleAutoKeyLock.Success": "<color=yellow>AutoKeyLock</color> set to {0}",
  "Command.ToggleAutoFillTankers.Success": "<color=yellow>AutoFillTankers</color> set to {0}",
  "Command.Give.Error.Syntax": "Syntax: <color=yellow>givecar <player> <preset></color>",
  "Command.Give.Error.PlayerNotFound": "Error: Player <color=yellow>{0}</color> not found.",
  "Command.Give.Error.PresetTooFewModules": "Error: Preset <color=yellow>{0}</color> has too few modules ({1}).",
  "Command.Give.Error.PresetTooManyModules": "Error: Preset <color=yellow>{0}</color> has too many modules ({1}).",
  "Command.Give.Success": "Modular car given to <color=yellow>{0}</color> from preset <color=yellow>{1}</color>.",
  "Command.Help": "<color=orange>SpawnModularCar Command Usages</color>",
  "Command.Help.Spawn.Basic": "<color=yellow>mycar</color> - Spawn a random car with max allowed sockets",
  "Command.Help.Spawn.Basic.PresetsAllowed": "<color=yellow>mycar</color> - Spawn a car using your <color=yellow>default</color> preset if saved, else spawn a random car with max allowed sockets",
  "Command.Help.Spawn.Sockets": "<color=yellow>mycar <2|3|4></color> - Spawn a random car of desired length",
  "Command.Help.Fetch": "<color=yellow>mycar fetch</color> - Fetch your car",
  "Command.Help.Fix": "<color=yellow>mycar fix</color> - Fix your car",
  "Command.Help.Destroy": "<color=yellow>mycar destroy</color> - Destroy your car",
  "Command.Help.Section.PersonalPresets": "<color=orange>--- Personal presets ---</color>",
  "Command.Help.ListPresets": "<color=yellow>mycar list</color> - List your saved presets",
  "Command.Help.Spawn.Preset": "<color=yellow>mycar <name></color> - Spawn a car from a saved preset",
  "Command.Help.LoadPreset": "<color=yellow>mycar load <name></color> - Load a preset onto your car",
  "Command.Help.SavePreset": "<color=yellow>mycar save <name></color> - Save your car as a preset",
  "Command.Help.UpdatePreset": "<color=yellow>mycar update <name></color> - Overwrite a preset",
  "Command.Help.RenamePreset": "<color=yellow>mycar rename <name> <new_name></color> - Rename a preset",
  "Command.Help.DeletePreset": "<color=yellow>mycar delete <name></color> - Delete a preset",
  "Command.Help.Section.CommonPresets": "<color=orange>--- Common presets ---</color>",
  "Command.Help.Common.ListPresets": "<color=yellow>mycar common list</color> - List common presets",
  "Command.Help.Common.Spawn": "<color=yellow>mycar common <name></color> - Spawn a car from a common preset",
  "Command.Help.Common.LoadPreset": "<color=yellow>mycar common load <name></color> - Load a common preset onto your car",
  "Command.Help.Common.SavePreset": "<color=yellow>mycar common save <name></color> - Save your car as a common preset",
  "Command.Help.Common.UpdatePreset": "<color=yellow>mycar common update <name></color> - Overwrite a common preset",
  "Command.Help.Common.RenamePreset": "<color=yellow>mycar common rename <name> <new_name></color> - Rename a common preset",
  "Command.Help.Common.DeletePreset": "<color=yellow>mycar common delete <name></color> - Delete a common preset",
  "Command.Help.Section.PersonalSettings": "<color=orange>--- Personal settings ---</color>",
  "Command.Help.ToggleAutoCodeLock": "<color=yellow>mycar autocodelock</color> - Toggle AutoCodeLock: {0}",
  "Command.Help.ToggleAutoKeyLock": "<color=yellow>mycar autokeylock</color> - Toggle AutoKeyLock: {0}",
  "Command.Help.ToggleAutoFillTankers": "<color=yellow>mycar autofilltankers</color> - Toggle automatic filling of tankers with fresh water: {0}",
  "Command.Help.Section.OtherCommands": "<color=orange>--- Other commands ---</color>",
  "Command.Help.Give": "<color=yellow>givecar <player> <preset></color> - Spawn a car for the target player from the specified server preset"
}
```

## Developer API

#### API_SpawnPresetCar

Plugins can call this API to spawn a modular car for a player with various options. Cars spawned this way are independent of `mycar`.

```csharp
ModularCar API_SpawnPresetCar(BasePlayer, Dictionary<string, object> options, Action<ModularCar> onReady)
```

Below is an example with all options provided, plus the optional callback:

```csharp
SpawnModularCar.Call("API_SpawnPresetCar", player,
    new Dictionary<string, object>
    {
        ["CodeLock"] = true,
        ["KeyLock"] = false,
        ["EnginePartsTier"] = 3,
        ["FreshWaterAmount"] = 50000,
        ["FuelAmount"] = 500,
        ["Modules"] = new object[] {
            "vehicle.1mod.cockpit.with.engine",
            "vehicle.2mod.fuel.tank"
        },
    },
    new Action<ModularCar>(car =>
    {
        // Example: Lock all engine containers
        foreach (var module in car.AttachedModuleEntities)
        {
            var engineContainer = (module as VehicleModuleEngine)?.GetContainer();
            if (engineContainer != null)
                engineContainer.inventory.SetLocked(true);
        }
    }
));
```

The available options (e.g., locks, fuel, water) are the same as for server presets. See that section for more details.

The return value will be the `ModularCar` instance that was spawned, or `null` if it was unable to be spawned for some reason (such as a plugin blocking it with a hook).

**Note:** If your plugin needs to interact with features of the car after spawning it, you should use the optional `onReady` callback to ensure the car's modules are fully spawned and the other features you requested are applied (e.g., locks, fuel, water). The `ModularCar` instance is only returned directly by the API in case you need to synchronously register the car for cooldowns or other purposes.

## Hooks

#### CanSpawnModularCar

- Called when a player or a plugin tries to spawn a modular car.
- Returning `false` will prevent the default behavior.
- Returning `null` will result in the default behavior.

```csharp
object CanSpawnModularCar(BasePlayer player)
```

#### CanSpawnMyCar

- Called when a player tries to spawn their car with any of the various `mycar` commands.
- Returning `false` will prevent the default behavior.
- Returning `null` will result in the default behavior.

```csharp
object CanSpawnMyCar(BasePlayer player)
```

#### CanFetchMyCar

- Called when a player tries to fetch their car with `mycar fetch`.
- Returning `false` will prevent the default behavior.
- Returning `null` will result in the default behavior.

```csharp
object CanFetchMyCar(BasePlayer player, ModularCar car)
```

#### CanFixMyCar

- Called when a player tries to fix their car with `mycar fix`.
- Returning `false` will prevent the default behavior.
- Returning `null` will result in the default behavior.

```csharp
object CanFixMyCar(BasePlayer player, ModularCar car)
```

#### CanLoadMyCarPreset

- Called when a player tries to load a preset onto their existing car with any of the various `mycar` commands.
- Returning `false` will prevent the default behavior.
- Returning `null` will result in the default behavior.

```csharp
object CanLoadMyCarPreset(BasePlayer player, ModularCar car)
```

#### CanDestroyMyCar

- Called when a player tries to despawn their car with `mycar destroy`.
- Returning `false` will prevent the default behavior.
- Returning `null` will result in the default behavior.

```csharp
object CanDestroyMyCar(BasePlayer player, ModularCar car)
```
