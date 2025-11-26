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
