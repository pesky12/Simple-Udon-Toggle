<div align="center">

# Simple Udon Toggle

*A flexible toggle component for VRChat worlds.*

[![VPM](https://img.shields.io/badge/VPM-8B5CF6?style=flat-square&logo=unity&logoColor=white)](https://pesky12.github.io/PeskyBox/index.json)
[![License](https://img.shields.io/badge/License-MIT-EC4899?style=flat-square)](LICENSE)

</div>

---

Supports GameObjects, Components, UI toggles, Udon events, and network sync with minimal setup.

## Features

- **Multiple Target Types** — Control GameObjects, Components, Sync UI Toggles, and send Udon events
- **Auto-Assignment** — Use `SimpleToggleMarker` components to auto-connect targets by name, never worry about broken references again (finally..)
- **Network Sync** — Three modes: None, Synced and OwnerOnly (advanced use cases)
- **VRC Persistence** — Optional state persistence using VRChat's PlayerData API
- **Trigger Support** — Toggle on player trigger enter/exit

## Installation

### Via VPM (Recommended)

```
https://pesky12.github.io/PeskyBox/index.json
```

Copy the URL above and add it to your [VRChat Creator Companion](https://vcc.docs.vrchat.com/) package listings.

### Manual

1. Download the latest release from the [GitHub releases page]
2. Extract the `com.pesky.box.simpleToggle` folder into your Unity project's `Assets/Packages` directory
3. Open Unity and let it compile
4. Have a snack!

## Quick Start

1. Add `SimpleUdonToggle` component to a GameObject
2. Set a unique `Toggle Name` (used for auto-assignment)
3. Configure your targets in the Inspector
4. Optionally use `SimpleToggleMarker` on target objects for auto-assignment

## Network Modes

| Mode | Description |
|------|-------------|
| **None** | Local only, no network sync |
| **OwnerOnly** | Only the owner can toggle, notifies owner via network event |
| **Synced** | Full sync with late-joiner support using `UdonSynced` variable |

## API

```csharp
// Toggle current state
toggle.Toggle();

// Set specific state
toggle.SetToggle(true);

// Check current state
bool current = toggle.syncedIsOn;
```

## Dependencies

- VRChat Worlds SDK 3.5.x (includes UdonSharp)

## License

MIT License — Free to use in any project, commercial or otherwise.

**Restriction:** Redistribution or resale as a standalone asset package is prohibited. You may include this in your own projects, games, and worlds, but you may not sell or redistribute it as a standalone Unity package or asset store product.

---

<div align="center">

Made with 💜 by [Pesky12](https://github.com/pesky12)


</div>