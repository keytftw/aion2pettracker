# Aion 2 Pet Tracker — 0.7.1 / GitHub Updates

Windows x64 pet soul tracker, licensed under GPL-3.0. Public update feed: https://github.com/keytftw/aion2pettracker

Startup sequence: Checking Updates → download an available update (with percentage progress) and restart automatically → 3.5-second introduction → choose from 10 languages → accept the terms → return to Character Select and log in again to retrieve the current collection. If no update is available, normal startup continues. Internet, GitHub, or package errors are not presented as "no updates"; choose Retry or Continue Current Version. Game traffic is not read before the terms are accepted.

Automatic updates work only with the new Velopack portable ZIP / Setup distribution from Releases. Previous ordinary ZIP packages and the standalone EXE in the root folder do not contain Update.exe/sq.version; users must switch to the new package once. GitHub updates require no token and follow stable releases only. The SDK checks package integrity; downloaded executables are not launched directly, but handled through the Velopack update process. The .NET runtime is included. Users must install Npcap separately.

## Publishing a New Release

Open this repository's Actions → Build and publish updater release → Run workflow.

The workflow builds and runs offline tests on Windows, produces a self-contained application, uses vpk 1.2.161 to create the portable ZIP/Setup and update feed, and publishes them to GitHub Releases. A source ZIP is also attached. Ordinary code pushes do not trigger publication. Before a new release, increment the Version value in src/PetTracker.App/PetTracker.App.csproj. Do not publish the same version twice.

No installer or packaged binary was executed locally. If the first GitHub release has not been created yet, automatic updating is not live; release packaging and an update/restart test between two consecutive versions on another computer are required. If GitHub becomes unavailable, the current version can still be used. User records are preserved under %LOCALAPPDATA%/Aion2PetTracker; a fresh collection is still required after an update. Code signing and publisher identification should be considered separately before wider distribution.

## Support

The startup credits display “Support on Patreon.” A separate button in Settings opens https://www.patreon.com/cw/keytftw in your default browser. The main tracker view does not contain a support link. Support is optional and does not change tracking functionality.

## Development

.NET 10 SDK, Windows x64:

```powershell
dotnet build PetTracker.slnx -c Release
dotnet test PetTracker.slnx -c Release --no-build
dotnet publish src/PetTracker.App/PetTracker.App.csproj -p:PublishProfile=Portable
```

PublishProfile only produces build output; an updater-enabled distribution is created through vpk packaging in CI. Demo and maintenance options do not access GitHub or start capture. Source, portrait, and server-data attribution is provided in THIRD_PARTY_NOTICES.md, data/PET-SOURCES.md, and licenses/. Further details about state, protocol handling, and usage follow below.

## Data and Logic

- `AuthoritativeCollectionSnapshot`: the last validated full collection; subsequent gains do not modify this object.
- `LastKnownTotals`: the last known totals derived from the snapshot and reliable gains.
- `SessionActivity` / `SoulEvent`: new session gains and active pets. The session list starts empty when the application is reopened.
- `UIPreferences`: pinned and dismissed IDs are stored per character; a snapshot does not erase them.
- `DataQuality`: waiting for a snapshot / exact / uncertain. Reopened records are not considered exact without a fresh snapshot.

Lv0 covers total souls 0–4; Lv1 5–29; Lv2 30–104; Lv3 105+ MAX. Within-stage goals are 5/25/75. A soul event observed after MAX requires verification. Shard messages do not count; attacks only affect card visibility, and there is no kill counter.

Events carry a character/server scope, stream ID, and monotonically increasing frame ID. The same frame is not applied twice; two different frames containing the same Pet ID and amount both count. Stale frames are logged. An ordered stream is expected; gaps in frame numbers alone do not prove lost souls because other messages may occur between them. Actual loss is reported through a separate Gap signal. Live mode creates frame identities after TCP reassembly.

IDs absent from generic, cached, or incomplete snapshots are not assumed to be zero. Global's validated full collection is sparse: a pet absent from the level/soul lists starts at zero (the reference PetTracker.cs uses the same rule). This rule is applied only to a known catalog Pet ID that gains a real soul in the same uninterrupted stream. The 207 catalog entries are not all populated with zero counters. Unknown IDs, packet loss, or reconnection do not create a zero baseline. `AuthoritativeCollectionSnapshot.OmittedEntriesAreZero` is enabled only for this validated full protocol snapshot; old records and generic snapshots default to disabled.

The catalog contains 207 unique entries, including 7 Special entries; there is no validated mapping to the game's 200-entry list. All catalog names are retained for selection. IDs receiving new souls, activated by the player's own attacks, or pinned by the user appear in the list.

A pinned regular catalog pet absent from both lists of a complete, sparse collection is shown as Lv0 0/5 (5 remaining). This is a read-only interpretation of the snapshot: pinning does not write to counters or session soul events. After data loss or reopening, the same value is only "Last known." The 7 Special entries and IDs outside the catalog that have not yet been observed in a game snapshot or soul event are not automatically treated as zero; they remain unknown.

Demo records: `%LOCALAPPDATA%\Aion2PetTracker\Demo\state.json`, backup `state.json.bak`, and a readable `tracker.log`. State is saved after each operation and on shutdown using an atomic temporary-file-and-replace operation; live events are processed and saved in batches at 250 ms intervals. Corrupt records are not silently reset or overwritten; the application displays an explanatory error. Back up and rename the file to start a clean demo. Demo and live profiles are stored in separate folders.

## Files

`src/PetTracker.Core`: independent logic and JSON persistence. `src/PetTracker.Capture`: passive live connections and the pet protocol. `src/PetTracker.App`: Turkish live/demo WinForms UI. `tests/PetTracker.Tests`: automated offline tests. `data`: pet name catalog and source notes. `LICENSE`, `THIRD_PARTY_NOTICES.md`, and `licenses/`: GPL-3.0 and source attribution.

The reference source was not modified. The live phase was authorized by the user. Only the required TCP/frame/wire components and the pet/identity reader subset were adapted with their licenses; no DPS, Alerts, or boss logic was added. Details are provided in THIRD_PARTY_NOTICES.md.

## Validation and Limitations

`dotnet build` and `dotnet test` are validated with deterministic synthetic streams. Tests cover TCP retransmission, out-of-order delivery and loss; split and LZ4 frames; malformed collections; ignoring shards and ensuring attacks do not count as souls; buffering before identity; character/server isolation; pinning/dismissal; and JSON persistence. UI startup is also checked with `PetTracker.App.exe --smoke-check`, which creates live and demo forms. This check does not start capture and writes test files to the `smoke-check` folder beside the EXE.

Validation against real Global game traffic has not yet been performed. Correct operation therefore needs to be checked in-game by observing a full collection and a real soul gain. If "Waiting for identity/collection" persists, share the relevant log message; do not transfer old or demo counters into live mode merely to make them appear exact.

## Collection Diagnostics

`Collection-Tani.cmd` is a separate, manually started diagnostic tool created only to investigate collection-format errors. It reads passively for 120 seconds; log into your character again during that time. Up to 8 message bodies containing only `0x0090` collection data, together with rejection reasons, are saved under `workspace/diagnostics/collection-local/`. Full network traffic, identity information, and game inputs are not recorded; counter/profile files are unchanged. This recording is disabled in the main application. Collection data contains your pet progress and is kept locally; you do not need to share it. Diagnostic output is personal and must not be committed to version control.

Rebuild the diagnostic tool:

```powershell
dotnet build tools\PetTracker.Diagnostics\PetTracker.Diagnostics.csproj -c Release -p:BaseOutputPath=bin/diagnostics/
```