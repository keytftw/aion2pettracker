# Aion 2 Pet Tracker — 1.0.0

A Windows x64 pet-soul tracker for Aion 2 Global, developed by Keyt and licensed under GPL-3.0.

[Download the Windows Setup](https://github.com/keytftw/aion2pettracker/releases/latest/download/A2PetTracker-win-Setup.exe) · [Website and full guide](https://aion2-pet-tracker.lovelymacaw4.chatgpt.site/) · [Support on Patreon](https://www.patreon.com/cw/keytftw)

## Install and start

1. Install [Npcap](https://npcap.com/#download) separately. It is not bundled.
2. Run `A2PetTracker-win-Setup.exe`. Choose a language, accept the terms, select an installation folder and optionally create a desktop shortcut. The .NET runtime is included.
3. Open the installed tracker. Startup checks stable GitHub releases and shows download progress for available updates.
4. Return to Aion 2 Character Select and log in again. The overlay opens after a valid fresh character and collection snapshot.

Language and terms are configured during installation. Preferences persist; cached counters are reset at every launch and require a fresh game snapshot. Users of earlier portable distributions must run the new Setup once. This release provides Setup only; it does not produce a portable ZIP.

## Tracker controls

- Pet portraits, three level stars, within-level soul progress and remaining goals. Lv 0/1/2 goals are 5/25/75; total 105 reaches Lv 3 MAX.
- Real soul events increment by numeric Pet ID. Attacking a non-MAX target can display its card without increasing souls. Already-MAX pets require manual addition.
- Searchable Add Pet menu with incomplete/maxed tabs. Pinning fixes a card; unpinning removes only its pin and keeps the card, including after restarting. Delete/Clear removes the card and pin without deleting progress. A later real soul gain can reactivate it.
- Up/down arrows reorder cards with animation. Pins, manually retained cards and order are scoped to character/server and persist independently of counters.
- Fixed width; 1–7 visible cards. New cards automatically grow the full window to seven slots, after which scrolling settles at card boundaries. Position lock is separate from height.
- 30–100% opacity, Always on top, a movable compact box and a separately movable Settings window. Opening the desktop shortcut again restores the running instance rather than starting a new session.
- Ctrl+P hides/restores; Ctrl+U compacts/expands. Settings can change both shortcuts to Ctrl/Alt/Shift plus a letter, digit or F1–F24. Held keys do not repeatedly toggle; conflicting combinations are rejected.
- New MAX transitions play a subtle sound. MAX cards remain visible; hover and click CLEAR to remove one.
- Ten languages: Turkish, English, German, Russian, Spanish, French, Portuguese, Arabic, Hindi and Simplified Chinese. Changes apply immediately and are saved.

## Interactive Pet Atlas

Use the gold globe beside Add Pet to open a separate map window, or the globe on a card to focus that pet's locations. Six regions, farmable/maxed filters, search, a draggable carousel, animated focus, map dragging, a zoom slider and mouse-wheel zoom are included. Locations appear as red dots with black outlines. Click a marker to focus it.

Terrain and data are bundled; there is no WebView2 or online map dependency. The source snapshot contains six maps, 200 catalog IDs and coordinates for 160 pets. Missing locations are shown explicitly. These are recorded guides, not live spawn information or player-position tracking. The current bitmap is disposed on region changes; map region, size and position persist.

## Data integrity and capture

Full validated collection snapshots, soul events, session activity and UI preferences are separate. New snapshots reconcile counts without losing stable preferences. A gap or reconnect keeps last-known values and marks them uncertain until resynchronization; uncertain values are never displayed as exact. Identical legitimate gains are not deduplicated merely by ID and amount. Shard/crystal messages are not counted as attributable souls.

Npcap capture is receive-only and server-to-client. There is no packet injection, replay, game-memory access, DLL injection, game-client modification or gameplay automation. The passive-capture review required no capture-layer optimization. Review the setup terms and game rules; this independent community tool is not publisher approval or a zero-risk guarantee. Terms revision: 2026-10-13 / 1.1.

## Development and verification

.NET 10 SDK and Windows x64:

```powershell
dotnet build PetTracker.slnx -c Release -m:1
dotnet test PetTracker.slnx -c Release --no-build --no-restore -m:1
dotnet tool install vpk --version 1.2.161 --tool-path .tools/vpk
./tools/Build-Setup.ps1
```

Offline tests cover state reconciliation, identity isolation, pin/delete retention, saved order, installer consent/location, localization, map data/camera and elapsed-time animation easing. Source-based UI tests render demo forms without live capture, check clipping and compact sizing, and measure native timer cadence and seven-card rendering. They never launch the packaged tracker or installer. Current verification: 361 logic tests plus 6 UI tests passed.

Rendering caches final-size text and portraits. Active animation uses an 8 ms UI timer and elapsed-time easing; idle/hidden windows use lower timer rates. Settings changes apply immediately and are saved after a short debounce. In the local 96-DPI benchmark, seven-card paint median decreased from 10.31 ms to 4.19 ms and allocations/frame from 354,984 to 36,216 bytes. These are offline measurements, not guaranteed in-game FPS.

Manual checks still include gaming-load fluidity, real installation/update/restart behavior and a no-auto-loot session after a data gap. Source instructions prohibit executing installers or packaged application binaries during local development.

## Release packaging

Actions → **Build and publish updater release** → **Run workflow** builds/tests on Windows and packages the installed Velopack feed. Increment Version in both App and Setup projects before release.

`tools/Build-Setup.ps1` publishes the app, verifies required data/portraits/maps/licenses, disables portable generation and skips Velopack's app-execution probe. Our localized setup wizard embeds the matching installer and a SHA-256 integrity check. It runs installation only after the user explicitly accepts terms and clicks Install. Only successful installation writes consent and preferences.

A release includes the public Setup, necessary updater packages/feed and corresponding source ZIP. GitHub updates require no token. If GitHub is unavailable, Retry or Continue current version remains available. User preferences remain under `%LOCALAPPDATA%/Aion2PetTracker`; fresh collection data is required after updating. Code signing remains a separate distribution task.

## Attribution

Only required capture/protocol/catalog components were reused with GPL attribution; this application has its own tracker state and UI. Portrait, font, map and server-data sources and licenses are documented in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md), [data/PET-SOURCES.md](data/PET-SOURCES.md) and `licenses/`.
