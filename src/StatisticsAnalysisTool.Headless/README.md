# sat-headless

A console daemon that captures Albion Online traffic with libpcap and writes what happens in game as
JSON lines on **stdout**. Diagnostics (logs) go to **stderr** only, so stdout can be piped straight into another
program. It reuses the Photon parsing and the Linux libpcap provider of this repository and has no GUI.

## Usage

```bash
packaging/linux/build-headless.sh            # -> artifacts/headless/sat-headless
packaging/linux/install-headless.sh          # -> ~/.local/opt/StatisticsAnalysisTool/sat-headless
sudo setcap cap_net_raw,cap_net_admin=eip ~/.local/opt/StatisticsAnalysisTool/sat-headless   # once, after every re-install

sat-headless                     # every up, non-loopback interface
sat-headless --device eth0       # only this interface (repeatable)
sat-headless | jq -c 'select(.type=="loot")'
```

- Needs `CAP_NET_RAW` (or root) and the system libpcap (`libpcap.so.1`).
- Without the capability it prints one `status` line with state `no_privilege` (the message contains the exact
  `setcap` command) and exits with code **2**.
- `SIGTERM` / `SIGINT` stop the capture, print `status` / `stopped` and exit with code 0.
- Exit codes: `0` normal stop, `1` capture could not start, `2` missing privilege, `64` bad arguments.
- Optional game data (item names, cluster names and modes) is read from
  `$XDG_DATA_HOME/StatisticsAnalysisTool/GameFiles` (default `~/.local/share/...`): `IndexedItems.json` and
  `world.json`, the files the desktop app extracts. Without them item names are `null` and clusters are `unknown`.

## Contract v1

One JSON object per line. Common fields on every line: `v` (always `1`), `type`, `ts` (ISO 8601 UTC with
milliseconds, e.g. `2026-01-02T03:04:05.678Z`). Amounts are numbers decoded from the game's fixed-point values.

| type | fields | notes |
| --- | --- | --- |
| `status` | `state`: `capturing` \| `no_privilege` \| `error` \| `stopped`, `message` | lifecycle |
| `player` | `name` | local player, from the Join response; sent again on every join |
| `cluster` | `index`, `name`, `mode`: `black` \| `red` \| `yellow` \| `blue` \| `safe` \| `unknown` | on join and every cluster change |
| `fame` | `amount`, `premium`, `satchel` | fame gained by one event; `amount` includes premium and satchel |
| `silver` | `amount` | silver taken by the local player after tax (party members' shares are not counted) |
| `respec` | `amount` | gained re-spec points (in game: Fame Credits) |
| `might_favor` | `might`, `favor` | Might and Favor received |
| `died` | `killedBy` | only when the local player died |
| `chest` | `rarity`: `common` \| `uncommon` \| `rare` \| `legendary` \| `unknown`, `opened` | chest appeared (`opened:false`) or was opened; repeated updates without a state change are dropped |
| `loot` | `itemIndex`, `uniqueName`, `name`, `quantity`, `source`: `chest` \| `mob` \| `player` \| `unknown`, `estValue` | only items grabbed by the local player; `uniqueName`/`name` are `null` without `IndexedItems.json`; `estValue` is `null` (no price data in the daemon) |

Example:

```json
{"v":1,"type":"status","ts":"2026-01-02T03:04:05.678Z","state":"capturing","message":"Capturing on 1 interface(s)."}
{"v":1,"type":"player","ts":"2026-01-02T03:04:09.120Z","name":"Bob"}
{"v":1,"type":"loot","ts":"2026-01-02T03:05:11.402Z","itemIndex":1841,"uniqueName":"T4_BAG","name":"Adept's Bag","quantity":1,"source":"mob","estValue":null}
```

### Known limits

- `source` comes from the name of the looted body: `@MOB` in the name is `mob`, `CHEST` is `chest`, any other name is
  a `player` body, no name is `unknown`. Loot taken from a chest through inventory events (not the
  `OtherGrabbedLoot` event) is not reported.
- `mode` is derived from the `@type` of the cluster in `world.json` (keywords YELLOW, RED, BLACK, BLUE, SAFEAREA,
  HIDEOUT, PLAYERCITY). Islands and hideouts are `safe`; dungeons and anything not in `world.json` are `unknown`.
- Events that arrive before the first Join response (silver, loot, death) are ignored because the local player is
  not known yet.

## Layout

- `Contract/` message records, JSON serializer and `IEventSink` / `JsonLineSink`.
- `Events/` Photon payload parsers (same parameter indexes as the desktop tool's event classes).
- `Mapping/` `EventMapper` (parsed event to contract line, local-player filters) and the game-data catalogs.
- `Capture/` Photon handler wiring and the small stubs that let the shared libpcap provider compile here.
- The libpcap provider, privilege check and Photon code enums are linked from the other projects, not copied.
- Tests: `src/StatisticsAnalysisTool.Headless.Tests` (xunit): `dotnet test src/StatisticsAnalysisTool.Headless.Tests`.
