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
  "DefaultMaxRange": 500.0,
  "ProfilesRequiringPermission": [
    {
      "PermissionSuffix": "short",
      "MaxRange": 250.0
    },
    {
      "PermissionSuffix": "medium",
      "MaxRange": 500.0
    },
    {
      "PermissionSuffix": "long",
      "MaxRange": 1000.0
    },
    {
      "PermissionSuffix": "unlimited",
      "MaxRange": 0.0
    }
  ]
}
```

- `DefaultMaxRange` -- Max range for players who do not have permission to any profiles in `ProfilesRequiringPermission`.
- `ProfilesRequiringPermission` -- Each profile in this list generates a permission like `limiteddronerange.<suffix>`. Granting a profile to a player determines how far they can pilot drones away from the host computer station, overriding `DefaultMaxRange`.
  - `PermissionSuffix` -- Determines the generated permission of format `limiteddronerange.<suffix>`.
  - `MaxRange` -- Determines the max range for players with this profile.
