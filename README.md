<a name="readme-top"></a>

![GitHub tag (with filter)](https://img.shields.io/github/v/tag/K4ryuu/K4-Missions-SwiftlyS2?style=for-the-badge&label=Version)
![GitHub Repo stars](https://img.shields.io/github/stars/K4ryuu/K4-Missions-SwiftlyS2?style=for-the-badge)
![GitHub issues](https://img.shields.io/github/issues/K4ryuu/K4-Missions-SwiftlyS2?style=for-the-badge)
![GitHub](https://img.shields.io/github/license/K4ryuu/K4-Missions-SwiftlyS2?style=for-the-badge)
![GitHub all releases](https://img.shields.io/github/downloads/K4ryuu/K4-Missions-SwiftlyS2/total?style=for-the-badge)
[![Discord](https://img.shields.io/badge/Discord-Join%20Server-5865F2?style=for-the-badge&logo=discord&logoColor=white)](https://dsc.gg/k4-fanbase)

<!-- PROJECT LOGO -->
<br />
<div align="center">
  <h1 align="center">KitsuneLab©</h1>
  <h3 align="center">K4-Missions</h3>
  <a align="center">A dynamic mission system for Counter-Strike 2 using SwiftlyS2 framework. Create custom missions with configurable events, rewards, and reset modes.</a>

  <p align="center">
    <br />
    <a href="https://github.com/K4ryuu/K4-Missions-SwiftlyS2/releases/latest">Download</a>
    ·
    <a href="https://github.com/K4ryuu/K4-Missions-SwiftlyS2/issues/new?assignees=K4ryuu&labels=bug&projects=&template=bug_report.md&title=%5BBUG%5D">Report Bug</a>
    ·
    <a href="https://github.com/K4ryuu/K4-Missions-SwiftlyS2/issues/new?assignees=K4ryuu&labels=enhancement&projects=&template=feature_request.md&title=%5BREQ%5D">Request Feature</a>
  </p>
</div>

### Support My Work

I create free, open-source projects for the community. While not required, donations help me dedicate more time to development and support. Thank you!

<p align="center">
  <a href="https://paypal.me/k4ryuu"><img src="https://img.shields.io/badge/PayPal-00457C?style=for-the-badge&logo=paypal&logoColor=white" /></a>
  <a href="https://revolut.me/k4ryuu"><img src="https://img.shields.io/badge/Revolut-0075EB?style=for-the-badge&logo=revolut&logoColor=white" /></a>
</p>

### Dependencies

To use this server addon, you'll need the following dependencies installed:

- [**SwiftlyS2**](https://github.com/swiftly-solution/swiftlys2): SwiftlyS2 is a server plugin framework for Counter-Strike 2

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- INSTALLATION -->

## Installation

1. Install [SwiftlyS2](https://github.com/swiftly-solution/swiftlys2) on your server
2. [Download the latest release](https://github.com/K4ryuu/K4-Missions-SwiftlyS2/releases/latest)
3. Extract to your server's `swiftlys2/plugins/` directory
4. Configure `config.json` and `missions.json` in the plugin folder

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- FEATURES -->

## Features

- **Dynamic Mission System**: Create unlimited custom missions via `missions.json`
- **Event-Based Progress**: Track kills, assists, MVP awards, bomb plants/defuses, hostage rescues, round wins, and playtime
- **Event Property Filters**: Filter missions by weapon, headshot, map, and more
- **VIP Support**: Configure different mission counts for VIP and regular players
- **Multiple Reset Modes**: Daily, Weekly, Monthly, Per-Map, or Instant mission resets
- **Reward Commands**: Execute any server command as mission reward
- **Discord Webhooks**: Send notifications on mission completions
- **Minimum Player Requirement**: Prevent mission farming on empty servers
- **Warmup Protection**: Optionally disable progress during warmup

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- CONFIGURATION -->

## Configuration

### config.json

```json
{
  "K4Missions": {
    "DatabaseConnection": "host",
    "MissionCommands": ["mission", "missions"],
    "VipNameDomain": null,
    "MinimumPlayers": 4,
    "MissionAmountNormal": 1,
    "MissionAmountVip": 3,
    "VipFlags": ["any.vip.flag"],
    "EventDebugLogs": false,
    "AllowProgressDuringWarmup": false,
    "ResetMode": "Daily",
    "WebhookUrl": ""
  }
}
```

### missions.json Example

```json
[
  {
    "Event": "EventPlayerDeath",
    "Target": "Attacker",
    "Amount": 10,
    "RewardCommands": ["sw_givecredits {steamid64} 30"],
    "RewardPhrase": "30 Credits",
    "Phrase": "Kill 10 players"
  },
  {
    "Event": "EventPlayerDeath",
    "EventProperties": {
      "Weapon": "ak47",
      "Headshot": true
    },
    "Target": "Attacker",
    "Amount": 5,
    "RewardCommands": ["sw_givecredits {steamid64} 50"],
    "RewardPhrase": "50 Credits",
    "Phrase": "Get 5 AK47 headshot kills"
  },
  {
    "Event": "EventRoundMvp",
    "Target": "Userid",
    "Amount": 3,
    "RewardCommands": ["sw_givecredits {steamid64} 50"],
    "RewardPhrase": "50 Credits",
    "Phrase": "Get 3 MVP awards"
  },
  {
    "Event": "EventRoundEnd",
    "Target": "winner",
    "Amount": 5,
    "RewardCommands": ["sw_givecredits {steamid64} 25"],
    "RewardPhrase": "25 Credits",
    "Phrase": "Win 5 rounds"
  },
  {
    "Event": "PlayTime",
    "Target": "Userid",
    "Amount": 30,
    "RewardCommands": ["sw_givecredits {steamid64} 60"],
    "RewardPhrase": "60 Credits",
    "Phrase": "Play 30 minutes on the server"
  }
]
```

### Available Events

| Event                 | Target                 | Description          |
| --------------------- | ---------------------- | -------------------- |
| `EventPlayerDeath`    | `Attacker`, `Assister` | Player kills/assists |
| `EventRoundMvp`       | `Userid`               | MVP awards           |
| `EventBombPlanted`    | `Userid`               | Bomb plants          |
| `EventBombDefused`    | `Userid`               | Bomb defuses         |
| `EventHostageRescued` | `Userid`               | Hostage rescues      |
| `EventGrenadeThrown`  | `Userid`               | Grenade throws       |
| `EventRoundEnd`       | `winner`, `loser`      | Round wins/losses    |
| `PlayTime`            | `Userid`               | Minutes played       |

### Reset Modes

| Mode      | Description                                 |
| --------- | ------------------------------------------- |
| `Daily`   | Missions reset at midnight                  |
| `Weekly`  | Missions reset every Sunday                 |
| `Monthly` | Missions reset at end of month              |
| `PerMap`  | Missions reset on map change                |
| `Instant` | Completed missions are immediately replaced |

### Reward Placeholders

| Placeholder   | Description          |
| ------------- | -------------------- |
| `{steamid64}` | Player's Steam ID 64 |
| `{steamid}`   | Same as steamid64    |
| `{name}`      | Player's name        |
| `{userid}`    | Player's user ID     |
| `{slot}`      | Player's slot        |

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- LICENSE -->

## License

Distributed under the GPL-3.0 License. See [`LICENSE.md`](LICENSE.md) for more information.

<p align="right">(<a href="#readme-top">back to top</a>)</p>
