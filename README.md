## Features

- Limits how far RC drones can be controlled from computer stations
- Allows configuring range limts per player using permissions
- Displays a UI while piloting a drone, showing current and max range, and which changes color near max range

## Permissions

The following permissions come with the plugin's **default configuration**. Granting one to a player determines how far they can pilot drones away from computer stations, overriding the default. Granting multiple profiles to a player will cause only the last one to apply, based on the order in the config.

- `limiteddronerange.short` -- 250m
- `limiteddronerange.medium` -- 500m
- `limiteddronerange.long` -- 1000m
- `limiteddronerange.unlimited` -- No limit

You can add more profiles in the plugin configuration (`ProfilesRequiringPermission`), and the plugin will automatically generate permissions of the format `limiteddronerange.<suffix>` when reloaded.

## Configuration

Default configuration:

```json
{
  "DefaultMaxRange": 500,
  "ProfilesRequiringPermission": [
    {
      "PermissionSuffix": "short",
      "MaxRange": 250
    },
    {
      "PermissionSuffix": "medium",
      "MaxRange": 500
    },
    {
      "PermissionSuffix": "long",
      "MaxRange": 1000
    },
    {
      "PermissionSuffix": "unlimited",
      "MaxRange": 0
    }
  ],
  "UISettings": {
    "AnchorMin": "0.5 0",
    "AnchorMax": "0.5 0",
    "OffsetMin": "0 75",
    "OffsetMax": "0 75",
    "TextSize": 24,
    "DefaultColor": "0.75 0.75 0.75 1",
    "OutOfRangeColor": "1 0.2 0.2 1",
    "DynamicColors": [
      {
        "DistanceRemaining": 100,
        "Color": "1 0.5 0 1"
      },
      {
        "DistanceRemaining": 50,
        "Color": "1 0.2 0.2 1"
      }
    ],
    "SecondsBetweenUpdates": 0.5
  }
}
```

- `DefaultMaxRange` -- Max range for players who do not have permission to any profiles in `ProfilesRequiringPermission`.
- `ProfilesRequiringPermission` -- Each profile in this list generates a permission like `limiteddronerange.<suffix>`. Granting a profile to a player determines how far they can pilot drones away from the host computer station, overriding `DefaultMaxRange`.
  - `PermissionSuffix` -- Determines the generated permission of format `limiteddronerange.<suffix>`.
  - `MaxRange` -- Determines the max range for players with this profile.
- `UISettings` -- Options to control the display of the UI.

## Localization

```json
{
  "UI.Distance": "{0}m / {1}m",
  "UI.OutOfRange": "OUT OF RANGE"
}
```
## FAQ

#### How do I get a drone?

As of this writing, RC drones are a deployable item named `drone`, but they do not appear naturally in any loot table, nor are they craftable. However, since they are simply an item, you can use plugins to add them to loot tables, kits, GUI shops, etc. Admins can also get them with the command `inventory.give drone 1`, or spawn one in directly with `spawn drone.deployed`.

#### How do I remote-control a drone?

If a player has building privilege, they can pull out a hammer and set the ID of the drone. They can then enter that ID at a computer station and select it to start controlling the drone. Controls are `W`/`A`/`S`/`D` to move, `shift` (sprint) to go up, `ctrl` (duck) to go down, and mouse to steer.

Note: If you are unable to steer the drone, that is likely because you have a plugin drawing a UI that is grabbing the mouse cursor. For example, the Movable CCTV plugin previously caused this and was patched in March 2021.

## Recommended compatible plugins

Drone balance:
- [Drone Settings](https://umod.org/plugins/drone-settings) -- Allows changing speed, toughness and other properties of RC drones.
- [Targetable Drones](https://umod.org/plugins/targetable-drones) -- Allows RC drones to be targeted by Auto Turrets and SAM Sites.
- [Limited Drone Range](https://umod.org/plugins/limited-drone-range) (This plugin) -- Limits how far RC drones can be controlled from computer stations.

Drone fixes and improvements:
- [Drone Effects](https://umod.org/plugins/drone-effects) -- Adds collision effects and propeller animations to RC drones.
- [Better Drone Collision](https://umod.org/plugins/better-drone-collision) -- Overhauls RC drone collision damage so it's more intuitive.
- [RC Identifier Fix](https://umod.org/plugins/rc-identifier-fix) -- Auto updates RC identifiers saved in computer stations to refer to the correct entity.
- [Auto Flip Drones](https://umod.org/plugins/auto-flip-drones) -- Auto flips upside-down RC drones when a player takes control.
- [Drone Hover](https://umod.org/plugins/drone-hover) -- Allows RC drones to hover in place while not being controlled.

Drone attachments:
- [Drone Lights](https://umod.org/plugins/drone-lights) -- Adds controllable search lights to RC drones.
- [Drone Turrets](https://umod.org/plugins/drone-turrets) -- Allows players to deploy auto turrets to RC drones.
- [Drone Storage](https://umod.org/plugins/drone-storage) -- Allows players to deploy a small stash to RC drones.
- [Ridable Drones](https://umod.org/plugins/ridable-drones) -- Allows players to ride RC drones by standing on them or mounting a chair.

## Developer Hooks

#### OnDroneRangeLimit

```csharp
bool? OnDroneRangeLimit(Drone drone, ComputerStation station, BasePlayer player)
```

- Called after a player has started controlling a drone, when this plugin is about to start limiting its max range
- Returning `false` will prevent this plugin from limiting the drone's max range or showing a UI to the player
- Returning `null` will result in the default behavior
