**SpawnModularCar** allows players to spawn modular cars. Each player can have only one car at a time.

## Commands

### Spawn commands
- `mycar` -- Spawn a modular car using your "default" preset if saved, else spawn a random car with the maximum number of allowed sockets based on your permissions.
- `mycar <2|3|4>` -- Spawn a random modular car with the specified number of sockets.
- `mycar <name>` -- Spawn a modular car from the specified preset. Partial name matching supported. See presets section for more details.

When a modular car is spawned, it will be at full health. Additionally, depending on your granted permissions and personal settings:
- It may be fueled.
- It may automatically have a code lock.
- It may automatically have a key lock, with a matching key added to your inventory.
- All tanker modules may be automatically filled with fresh water.
- All engine modules may be automatically filled with your highest allowed quality of engine components.

### Other commands
- `mycar fix` -- Fix your car. This restores it to original condition as though you had just spawned it, with the exception that it will not add or remove a lock, regardless of your `AutoCodeLock` or `AutoKeyLock` setting. Engine components will also be repaired. If you were granted any of the `spawnmodularcar.engineparts.*` permissions, missing engine components are added, and lower quality components are replaced with the maximum quality you are allowed. Note: This command cannot restore your car if it is "dead" (0 health on every module).
- `mycar fetch` -- Teleport your car to you in an upright position.
- `mycar destroy` -- Destroy your car, allowing you to spawn a new one.
  - Non-empty storage modules will drop a bag with their items next to where the car was located.
  - If the car's engine modules contained components that are of higher quality than are allowed by your `spawnmodularcar.engineparts.*` permissions, those will be added to your inventory if there is space, else dropped to the ground in front of you.
- `mycar autocodelock` -- Toggle AutoCodeLock. While ON, spawning your car will automatically create a code lock and add it to the front-most cockpit module if it has one.
- `mycar autokeylock` -- Toggle AutoKeyLock. While ON, spawning your car will automatically create a key lock and add a matching key to your inventory. Note: This only happens if the car has at least one cockpit module.
- `mycar autofilltankers` -- Toggle AutoFillTankers. While ON, spawning your car, fixing it, or loading a preset will automatically fill any tanker modules to maximum capacity with fresh water, replacing any salt water already in them.
- `mycar help` -- Print a list of available commands and their usage. Only shows commands allowed by your permissions.

### Preset-related commands

Players can save custom module configurations, allowing them to spawn a car or update a car in-place with a saved preset.

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

## Permissions

The following permissions control the maximum number of module sockets the player is allowed to have on their car. If none of these are granted, the player cannot spawn a car.
- `spawnmodularcar.spawn.2` -- Allows spawning a car with 2 sockets.
- `spawnmodularcar.spawn.3` -- Allows spawning a car with 2-3 sockets.
- `spawnmodularcar.spawn.4` -- Allows spawning a car with 2-4 sockets.

Granting the following permissions will cause the player's car to automatically spawn with engine components of the corresponding quality in each engine module. If none of these are granted, the car spawns without any engine components, so it cannot be driven until a player adds them.
- `spawnmodularcar.engineparts.tier1` -- Spawn your car with low quality engine components.
- `spawnmodularcar.engineparts.tier2` -- Spawn your car with medium quality engine components.
- `spawnmodularcar.engineparts.tier3` -- Spawn your car with high quality engine components.

Misc:
- `spawnmodularcar.fix` -- Required to use `mycar fix`.
- `spawnmodularcar.fetch` -- Required to use `mycar fetch`.
- `spawnmodularcar.despawn` -- Required to use `mycar destroy`.
- `spawnmodularcar.autofuel` -- Fuels your car automatically.
- `spawnmodularcar.autocodelock` -- Required to use the automatic code lock feature (i.e., `mycar autocodelock`).
- `spawnmodularcar.autokeylock` -- Required to use the automatic key lock feature (i.e., `mycar autokeylock`).
- `spawnmodularcar.autofilltankers` - Required to use automatic filling of tanker modules (i.e. `mycar autofilltankers`).
- `spawnmodularcar.underwater` -- Allows your car to be driven underwater (scuba gear is recommended). Note: Underwater driving is noticeably slower than on land.
- `spawnmodularcar.autostartengine` -- Instantly start your car's engine when you get in.
- `spawnmodularcar.presets` -- Allows you to spawn your car from a preset. Also enables the `save`, `update`, `rename` and `delete` preset commands.
- `spawnmodularcar.presets.load` -- Required to use `mycar load`.

# Configuration

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
  "DisableSpawnLimitEnforcement": false,
  "DismountPlayersOnFetch": true,
  "EnableEffects": true,
  "FuelAmount": -1,
  "MaxPresetsPerPlayer": 10,
  "PreventEditingWhileCodeLockedOut": false
}
```

- `CanDespawnWhileOccupied` (`true` or `false`) -- Whether to allow players to use `mycar destroy` while their car is occupied. If `true`, despawning a car will safely eject players from seating and flatbed modules.
- `CanFetchWhileOccupied` (`true` or `false`) -- Whether to allow players to fetch their car while it's occupied. If `true`, fetchinig a car will safely eject players from seating and flatbed modules if `DismountPlayersOnFetch` is `true`, else those players will be teleported with the car.
- `CanFetchWhileBuildingBlocked` (`true` or `false`) -- Whether to allow players to fetch their car while they are building blocked. Recommended to be set to `false` to avoid exploits where people use the car to get through a wall.
- `CanSpawnWhileBuildingBlocked` (`true` or `false`) -- Whether to allow players to spawn their car while they are building blocked. Recommended to be set to `false` to avoid exploits where people use the car to get through a wall.
- `Cooldowns` -- Various cooldowns for balancing. These were primarily implemented to prevent spamming, so they are not currently tracked across plugin reloads or server restarts, so setting them very high (e.g., hours or days) may not always work as intended.
- `DeleteMatchingKeysFromPlayerInventoryOnDespawn` (`true` or `false`) -- Whether to delete all matching keys from the owner player's inventory when they use `mycar destroy`. Also applies to when they use `mycar load` and the lock is removed because the preset contains no cockpit modules. I recommend this be set to `true`, especially if you are allowing players to use the automatic key lock feature since that spawns keys which may otherwise clutter the player's inventory.
- `DisableSpawnLimitEnforcement` (`true` or `false`) -- Set to `true` to keep all modular cars between server restarts. Otherwise, the game will delete extra cars beyond the server's configured modular car population, which *may* delete player cars depending on how recently they were spawned.
- `DismountPlayersOnFetch` (`true` or `false`) -- Whether to dismount all players from a car when it is fetched. Has no effect unless `CanFetchWhileOccupied` is also `true`.
- `EnableEffects` (`true` or `false`) -- Enable audio and visual effects when spawning a car from a preset, using `mycar fix` or `mycar load`.
- `FuelAmount` -- The amount of low grade fuel to add to the fuel tank when spawning. Only applies if the player has the `spawnmodularcar.autofuel` permission. When fixing the car or loading a preset, only the amount missing will be added to reach this minimum target value. Defaults to `-1` which represents maximum stack size.
- `MaxPresetsPerPlayer` -- The maximum number of module configuration presets each player is allowed to save.
- `PreventEditingWhileCodeLockedOut` (`true` or `false`) -- Whether to prevent players from editing the vehicle on a lift when they are not authorized to the car's code lock. Authorized players can still edit the vehicle regardless.

## Localization

```json
{
  "Generic.Setting.On": "<color=yellow>ON</color>",
  "Generic.Setting.Off": "<color=#bbb>OFF</color>",
  "Generic.Error.NoPermission": "You don't have permission to use this command.",
  "Generic.Error.BuildingBlocked": "Error: Cannot do that while building blocked.",
  "Generic.Error.NoPresets": "You don't have any saved presets.",
  "Generic.Error.CarNotFound": "Error: You need a car to do that.",
  "Generic.Error.CarOccupied": "Error: Cannot do that while your car is occupied.",
  "Generic.Error.CarDead": "Error: Your car is dead.",
  "Generic.Error.Cooldown": "Please wait <color=yellow>{0}s</color> and try again.",
  "Generic.Error.NoPermissionToPresetSocketCount": "Error: You don't have permission to use preset <color=yellow>{0}</color> because it requires <color=yellow>{1}</color> sockets.",
  "Generic.Error.PresetNotFound": "Error: Preset <color=yellow>{0}</color> not found.",
  "Generic.Error.PresetMultipleMatches": "Error: Multiple presets found matching <color=yellow>{0}</color>. Use <color=yellow>mycar list</color> to view your presets.",
  "Generic.Error.PresetAlreadyTaken": "Error: Preset <color=yellow>{0}</color> is already taken.",
  "Generic.Error.PresetNameLength": "Error: Preset name may not be longer than {0} characters.",
  "Generic.Error.CarLocked": "That vehicle is locked.",
  "Generic.Info.CarDestroyed": "Your modular car was destroyed.",
  "Generic.Info.PartsRecovered": "Recovered engine components were added to your inventory or dropped in front of you.",
  "Command.Spawn.Error.SocketSyntax": "Syntax: <color=yellow>mycar <2|3|4></color>",
  "Command.Spawn.Error.CarAlreadyExists": "Error: You already have a car.",
  "Command.Spawn.Error.CarAlreadyExists.Help": "Try <color=yellow>mycar fetch</color> or <color=yellow>mycar help</color>.",
  "Command.Spawn.Success": "Here is your modular car.",
  "Command.Spawn.Success.Locked": "A matching key was added to your inventory.",
  "Command.Spawn.Success.Preset": "Here is your modular car from preset <color=yellow>{0}</color>.",
  "Command.Fetch.Error.StuckOnLift": "Error: Unable to fetch your car from its lift.",
  "Command.Fetch.Error.StuckOnLift.Help": "You can use <color=yellow>mycar destroy</color> to destroy it.",
  "Command.Fetch.Success": "Here is your modular car.",
  "Command.Fix.Success": "Your car was fixed.",
  "Command.SavePreset.Error.TooManyPresets": "Error: You may not have more than <color=yellow>{0}</color> presets. Please delete another preset and try again. See <color=yellow>mycar help</color>.",
  "Command.SavePreset.Error.PresetAlreadyExists": "Error: Preset <color=yellow>{0}</color> already exists. Use <color=yellow>mycar update {0}</color> to update it.",
  "Command.SavePreset.Success": "Saved car as <color=yellow>{0}</color> preset.",
  "Command.UpdatePreset.Success": "Updated <color=yellow>{0}</color> preset with current module configuration.",
  "Command.LoadPreset.Error.SocketCount": "Error: Unable to load <color=yellow>{0}</color> preset ({1} sockets) because your car has <color=yellow>{2}</color> sockets.",
  "Command.LoadPreset.Success": "Loaded <color=yellow>{0}</color> preset onto your car.",
  "Command.DeletePreset.Success": "Deleted <color=yellow>{0}</color> preset.",
  "Command.RenamePreset.Error.Syntax": "Syntax: <color=yellow>mycar rename <name> <new_name></color>",
  "Command.RenamePreset.Success": "Renamed <color=yellow>{0}</color> preset to <color=yellow>{1}</color>",
  "Command.List": "Your saved modular car presets:",
  "Command.List.Item": "<color=yellow>{0}</color> ({1} sockets)",
  "Command.ToggleAutoCodeLock.Success": "<color=yellow>AutoCodeLock</color> set to {0}",
  "Command.ToggleAutoKeyLock.Success": "<color=yellow>AutoKeyLock</color> set to {0}",
  "Command.ToggleAutoFillTankers.Success": "<color=yellow>AutoFillTankers</color> set to {0}",
  "Command.Help": "<color=orange>SpawnModularCar Command Usages</color>",
  "Command.Help.Spawn.Basic": "<color=yellow>mycar</color> - Spawn a random car with max allowed sockets",
  "Command.Help.Spawn.Basic.PresetsAllowed": "<color=yellow>mycar</color> - Spawn a car using your <color=yellow>default</color> preset if saved, else spawn a random car with max allowed sockets",
  "Command.Help.Spawn.Sockets": "<color=yellow>mycar <2|3|4></color> - Spawn a random car with the specified number of sockets",
  "Command.Help.Spawn.Preset": "<color=yellow>mycar <name></color> - Spawn a car from a saved preset",
  "Command.Help.Fetch": "<color=yellow>mycar fetch</color> - Fetch your car",
  "Command.Help.Fix": "<color=yellow>mycar fix</color> - Fix your car",
  "Command.Help.Destroy": "<color=yellow>mycar destroy</color> - Destroy your car",
  "Command.Help.ListPresets": "<color=yellow>mycar list</color> - List your saved presets",
  "Command.Help.SavePreset": "<color=yellow>mycar save <name></color> - Save your car as a preset",
  "Command.Help.UpdatePreset": "<color=yellow>mycar update <name></color> - Overwrite an existing preset",
  "Command.Help.LoadPreset": "<color=yellow>mycar load <name></color> - Load a preset onto your car",
  "Command.Help.RenamePreset": "<color=yellow>mycar rename <name> <new_name></color> - Rename a preset",
  "Command.Help.DeletePreset": "<color=yellow>mycar delete <name></color> - Delete a preset",
  "Command.Help.ToggleAutoCodeLock": "<color=yellow>mycar autocodelock</color> - Toggle AutoCodeLock: {0}",
  "Command.Help.ToggleAutoKeyLock": "<color=yellow>mycar autokeylock</color> - Toggle AutoKeyLock: {0}",
  "Command.Help.ToggleAutoFillTankers": "<color=yellow>mycar autofilltankers</color> - Toggle automatic filling of tankers with fresh water: {0}"
}
```
