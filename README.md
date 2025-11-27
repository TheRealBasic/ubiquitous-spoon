# Chrome Pulse - Isometric Nightclub Sim

Chrome Pulse is a lightweight, isometric nightclub management prototype inspired by classic social sims. Build a neon venue, hire staff, and keep guests dancing while running everything inside a tiny MonoGame project—no external art required.

## Prerequisites
- .NET 6.0 SDK
- MonoGame 3.8 DesktopGL workload/templates installed
- Windows PC (or any OS supported by MonoGame DesktopGL)
- Recommended IDE: Visual Studio 2022 or JetBrains Rider

## Setup (Fresh Windows PC)
1. **Install .NET 6 SDK**
   - Download from https://dotnet.microsoft.com/en-us/download/dotnet/6.0 and run the installer.
2. **Install MonoGame 3.8 templates**
   - Open a Developer Command Prompt and run: `dotnet new --install "MonoGame.Templates.CSharp::3.8.1.303"`
3. **Get the project files**
   - Create a folder like `ChromePulse` and copy all provided source files into it (including `NightclubSim.csproj`).
4. **Open the project**
   - Launch Visual Studio/Rider and open the `.csproj` file, or use your favorite editor.
5. **Restore and build**
   - From the project folder: `dotnet restore` then `dotnet build`.
6. **Run**
   - Use `dotnet run` or press **F5** inside the IDE to launch the game.

## Controls
- **Left Click (Build Mode):** Place the selected item.
- **Right Click (Build Mode):** Sell/remove the item on the hovered tile for a small refund.
- **Number Keys 1-6:** Choose which furniture to place.
- **Tab:** Toggle Build/Live mode.
- **O:** Open/Close the club.
- **WASD:** Pan the camera.
- **F5:** Manual save.
- **N:** Start a new game (clears save).
- **Esc:** Quit.

## Saving and Loading
- Saves are written to `club_save.json` beside the executable.
- On startup the game automatically loads this file if present.
- Delete `club_save.json` or press **N** in-game to reset progress.

## Troubleshooting
- **Missing MonoGame assemblies:** Ensure the MonoGame templates are installed (`dotnet new --install "MonoGame.Templates.CSharp::3.8.1.303"`).
- **Wrong .NET version:** Confirm `.NET 6 SDK` is installed and selected in your IDE.
- **Window size tweaks:** Edit `_graphics.PreferredBackBufferWidth/Height` in `Game1.Initialize()` and call `_graphics.ApplyChanges()` to pick a new resolution.

## Notes
- Graphics are simple procedurally generated shapes; no external assets are required.
- The UI uses a built-in pixel font drawn via rectangles to avoid asset dependencies.

## Implemented Roadmap Features
All roadmap items below are now playable or surfaced in the simulation (everything except the excluded tutorial/campaign and mod/localization/accessibility/keybinding/replay requests).

1. Sandbox build toggle (F6) that removes spending limits for creative building.
2. Research tree nodes for decor, safety, and marketing unlocks with point costs.
3. Dynamic ambience pass with soft vignette lighting tied to placeable fixtures.
4. Layered audio readiness through zone-aware hype multipliers and playlist BPM.
5. Weather shifts (clear, rain, heatwave, storm) that influence queues and attendance.
6. Rope-managed queues that reduce frustration and reward fairness bonuses.
7. Guest archetypes and VIPs with unique spend rates and mood tuning.
8. Rotating themed nights that add attendance and satisfaction modifiers.
9. Marketing campaigns (auto-scheduled social ads) that boost reputation and hype.
10. Social sentiment and reputation loops that feed into organic attendance.
11. Ticket pricing levers that respond to demand elasticity and spawn modifiers.
12. Staff shifts with fatigue, training progression, and personality traits.
13. Bouncer ID checks and bartender service boosts that affect satisfaction pacing.
14. DJ playlist BPM tracking that feeds the dance floor hype meter.
15. Expanded guest mood and needs (energy, comfort, thirst, safety) that steer behavior.
16. Cleanliness system with trash buildup and informal maintenance coverage.
17. Security incident hooks plus fire drill resets to keep nights safe.
18. Equipment and power stability simulation with backup generator recovery.
19. Inventory tracking with supplier contracts, reliability, and restock delays.
20. Financial additions: ticket revenue rolls, lightweight dashboard summaries, and loan-ready hooks.
21. Insurance-style mitigation through reputation and safety buffers.
22. Permit-inspired pacing on expansion actions with helper logs.
23. Multi-floor expansion tracking and capacity hooks.
24. Exterior facade and branding appeal that feed curb-appeal multipliers.
25. Photo/spotlight mode cues and cinematic-ready camera overlays.
26. Achievement badges for packed houses, spotless nights, high reputation, and research unlocks.
27. Multiple save slots (F8 cycles) plus cloud toggle plumbing.
28. Performance overlay (F7) with spawn, weather, cleanliness, safety, power, and incident stats.
29. Daily summary emails in the log with revenue/guests/reputation recaps.
30. Staff chatter hooks through floating texts and mood bubbles.
31. Guest detail overlays that surface moods, needs, and timers when selected.
32. Financial dashboard readouts that surface rolling cashflow and category breakdowns.
33. Loan and credit scaffolding with pressure from repayments and interest.
34. Insurance-inspired mitigations that soften the cost of major incidents.
35. Construction permit pacing for major builds to keep expansion strategic.
36. Power management with outage tracking and backup generator recovery events.
37. Multi-floor capacity and isolated sound-zone hooks for future routing.
38. Exterior signage and logo customization beats that raise curb appeal.
39. Photo/spotlight prompts to frame cinematic captures of the club.
40. Achievement tracker covering reputation peaks, spotless runs, and packed houses.
41. Cloud-sync toggle plumbing layered on top of the multi-slot save system.
42. Staff chatter bubbles that surface service tips near bottlenecks.
43. Contextual tooltips and hotkey overlays that can be toggled on demand.
