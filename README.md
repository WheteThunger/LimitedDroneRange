## Features

- Allows limiting how far RC drones can be controlled from computer stations

## Permissions

Default permissions:

- `limiteddronerange.shortrange` -- 250m
- `limiteddronerange.mediumrange` -- 500m
- `limiteddronerange.longrange` -- 1000m
- `limiteddronerange.unlimitedrange` -- No limit

## Configuration

Default configuration:

```json
{
  "DefaultMaxRange": 500.0,
  "ProfilesRequiringPermission": [
    {
      "PermissionSuffix": "shortrange",
      "MaxRange": 250.0
    },
    {
      "PermissionSuffix": "mediumrange",
      "MaxRange": 500.0
    },
    {
      "PermissionSuffix": "longrange",
      "MaxRange": 1000.0
    },
    {
      "PermissionSuffix": "unlimitedrange",
      "MaxRange": 0.0
    }
  ]
}
```
