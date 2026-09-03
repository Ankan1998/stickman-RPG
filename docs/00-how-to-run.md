# 0. How to run this project

The complete command reference. If you just want the three commands, read the
next section and stop.

---

## The short version

Open a terminal in the project folder (the one containing `StickmanRpg.sln`).

```bash
dotnet test
```

Runs the whole game's rules and checks them. **~1 second. Does not need Godot.**

```bash
godot --path game
```

Plays the game.

```bash
godot --editor --path game
```

Opens the project in the Godot editor, where you can press **F5** to play.

That is everything. The rest of this page is detail.

---

## What is already installed on this machine

This was set up on **2026-09-01** and verified working:

| Software | Version | Where |
|---|---|---|
| **.NET SDK** | 8.0.424 | `C:\Program Files\dotnet\` |
| **.NET Runtime** | 8.0.30 | (bundled with the SDK) |
| **Godot (.NET/Mono build)** | 4.7.2 | `%LOCALAPPDATA%\Microsoft\WinGet\Packages\GodotEngine...\` |
| **Git** | 2.54.0 | `C:\Program Files\Git\` |
| **VS Code** | 1.135.0 | `%LOCALAPPDATA%\Programs\Microsoft VS Code\` |
| **VS Code — C#** | ms-dotnettools.csharp 2.140.9 | extension |
| **VS Code — C# Dev Kit** | ms-dotnettools.csdevkit 3.20.199 | extension |
| **VS Code — Godot Tools** | geequlim.godot-tools 2.7.1 | extension |

`dotnet`, `git`, `code`, `godot` and `godotc` all work from any terminal.

> **About the `godot` command.** Godot ships as a single `.exe` with a long
> version-stamped name, and winget could not create the usual short alias
> (that needs administrator rights). So there are two small shim files next to
> the Godot executable — `godot.cmd` and `godotc.cmd` — and that folder is on
> your PATH. See [If `godot` stops working](#if-godot-stops-working) below.

| Command | Use it for |
|---|---|
| `godot` | Normal use. Opens the editor or plays the game. |
| `godotc` | Command-line use. Same engine, but prints its output to the terminal — needed for `--headless`. |

### "Godot is installed, so why can't I find it?"

Because **Godot does not install like normal Windows software.** It ships as a
portable `.zip` containing a single `.exe` — no installer, no shortcuts. That is
deliberate: it lets you keep several Godot versions side by side and run it
without administrator rights.

So after installing it you get:

| | |
|---|---|
| Registered with winget | yes |
| In Add/Remove Programs | yes, as "Godot Engine (Mono)" |
| **Start Menu shortcut** | **no** |
| **Desktop shortcut** | **no** |

Typing "Godot" into the Start Menu finds nothing, and the file is not even called
`Godot.exe` — it is `Godot_v4.7.2-stable_mono_win64.exe`.

**To find it in File Explorer**, paste this into the address bar:

```
%LOCALAPPDATA%\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7.2-stable_mono_win64
```

From there, right-click the `.exe` to **Pin to Start** or **Pin to taskbar**, or
send a shortcut to your Desktop.

**Or just use the terminal**, which is usually quicker anyway:

```bash
godot --editor --path game
```

---

## Running the tests

**This is the thing to run most often.** It exercises the entire game - damage,
poison, stun, cooldowns, turn order, monster AI, the three-wave run and its
statistics, plus 1000 simulated battles and 400 simulated runs - and it needs no
Godot, no window, and no waiting.

```bash
dotnet test
```

```
Passed!  - Failed: 0, Passed: 39, Skipped: 0, Total: 39, Duration: 583 ms
```

### See the balance numbers

```bash
dotnet test --logger "console;verbosity=detailed"
```

Scroll up through the output and you will find:

```
Battles simulated : 1000
Hero wins         : 749 (74.9%)
Monster wins      : 251
Draws (hit round limit) : 0
Average length    : 6.7 rounds
```

### Run just one test

```bash
dotnet test --filter "FullyQualifiedName~PoisonDamages"
```

`~` means "name contains". Useful when you are iterating on one rule.

### Run one test class

```bash
dotnet test --filter "FullyQualifiedName~CombatRulesTests"
```

---

## Running the game

### Option A — from the terminal (fastest)

```bash
godot --path game
```

The game window opens straight away. Close it to stop.

> `--path game` tells Godot "the project lives in the `game` subfolder". Without
> it, Godot looks in the current directory, finds no `project.godot`, and shows
> its project-picker instead.

### Option B — through the Godot editor (what you want while developing)

```bash
godot --editor --path game
```

Or, the fully manual route the first time:

1. Launch Godot with no arguments. You get the **Project Manager**.
2. Click **Import**.
3. Select **`game/project.godot`** — the file inside the `game` folder, *not* the
   repository root.
4. Click **Import & Edit**.
5. Press **F5**, or the ▶ button at the top right.

Godot remembers the project, so subsequent launches just need a double-click in
the Project Manager list.

The editor is where you will want to be once you start changing the UI, because
it shows you the live scene tree (the **Remote** tab of the Scene dock while the
game is running) and the **Output** panel with any errors.

### Option C — from VS Code

Press **Ctrl+Shift+P**, type **Tasks: Run Task**, and pick one:

| Task | Does |
|---|---|
| `build` | Compiles everything (also on **Ctrl+Shift+B**) |
| `test` | Runs all the tests |
| `test (show balance numbers)` | Tests plus the 1000-battle report |
| `play the game` | Launches the game |
| `open Godot editor` | Opens the Godot editor |

These are defined in [`.vscode/tasks.json`](../.vscode/tasks.json).

### Option D — headless (no window)

```bash
godotc --headless --path game --quit-after 300
```

Runs the game with no graphics for 300 frames, then exits. Not useful for
playing, very useful for checking that everything still loads and runs — it is
exactly how this project is verified after a change.

Exit code `0` means it ran cleanly.

---

## Regenerating the art

Every PNG in `game/assets/` is generated. To change the art, edit the script and
re-run it:

```bash
python tools/make_art.py
```

Preview the sprites as ASCII without opening an image viewer:

```bash
python tools/make_art.py --preview
```

Full explanation in [The art pipeline](09-art-pipeline.md).

---

## Seeing what the screens look like

```bash
godot --path game -- --shots
```

Walks the title, battle and results screens, saves a PNG of each, prints the
folder, and quits. Useful when you are nudging layout and do not want to click
through the game every time.

---

## Building without running

```bash
dotnet build
```

Compiles all three projects and reports any errors. Expected output:

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

> ### The Godot trap worth knowing about
>
> When you press **F5** in the Godot editor, Godot builds your C# first. **If
> that build fails, Godot runs the last version that built successfully** — so
> the game launches normally and simply ignores your change.
>
> This is the single most confusing thing about Godot + C#. If your edits seem
> to do nothing, either check the **Output** panel at the bottom of the editor,
> or run `dotnet build` in a terminal, where an error cannot be missed.

To build one project at a time:

```bash
dotnet build src/Rpg.Core/Rpg.Core.csproj
```

```bash
dotnet build game/StickmanRpg.Game.csproj
```

---

## The development loop you will actually use

**For rules and mechanics** — which is most of your work:

```
edit src/Rpg.Core/...  ->  dotnet test  ->  read the numbers  ->  repeat
```

You do not need Godot open at all. This loop takes about a second per cycle.
That is the entire reason the project is structured the way it is
([why](04-architecture.md)).

**For anything visual:**

```
edit game/scripts/...  ->  F5 in the Godot editor  ->  look at it
```

**For balancing a fight:**

```
edit ContentDatabase.cs  ->  dotnet test --logger "console;verbosity=detailed"
                          ->  read the win rate  ->  repeat
```

Change **one** number at a time. See
[Recipes: tuning the difficulty](07-recipes.md#tuning-the-difficulty).

---

## Editing the code

**VS Code is set up and ready.** Open the **repository root** — not the `game`
folder — so you can see the rules engine and the game side by side:

```bash
code .
```

Three extensions are installed:

| Extension | What it gives you |
|---|---|
| **C#** (`ms-dotnettools.csharp`) | IntelliSense, error squiggles, go-to-definition, rename-refactoring |
| **C# Dev Kit** (`ms-dotnettools.csdevkit`) | Solution Explorer, and a **Test Explorer** — run and debug individual tests by clicking them |
| **Godot Tools** (`geequlim.godot-tools`) | `.tscn` and `.gd` file support |

### Running tests by clicking instead of typing

With C# Dev Kit installed, open the **Testing** panel in the left sidebar (the
flask icon). All 39 tests appear as a tree. From there you can:

- run everything, or one class, or one test, with a click
- **set a breakpoint in a test and debug it** — this is the single best way to
  understand what `Battle.TakeTurn` is doing, because you can step through the
  whole turn with no game running
- see failures inline, at the line that failed

For a mechanics-heavy project like this one, that is where you will spend most of
your time.

> ### One-time sign-in
>
> C# Dev Kit asks you to sign in with a Microsoft account the first time it
> loads. A free personal account is fine for individual use. If you would rather
> not sign in, uninstall it — the plain **C#** extension above does all the
> language work on its own, and everything in this project still builds, runs and
> tests from the terminal exactly as documented.
>
> Its licence is free for individuals and for smaller organisations, but
> restricted for large ones. Worth a glance if this is a work machine.

### "Why is it downloading .NET 10? I installed .NET 8"

On first activation, C# Dev Kit's log says something like:

```
Locating .NET runtime version 10.0.5
Did not find .NET 10.0.5 on path, falling back to acquire runtime
Dotnet path: ...\globalStorage\ms-dotnettools.vscode-dotnet-runtime\.dotnet\10.0.11~x64~aspnetcore\dotnet.exe
```

This is expected and harmless. C# Dev Kit's language server **is itself a .NET
program**, built against .NET 10, so it needs a .NET 10 runtime to run *itself*.
That is unrelated to what your code targets.

|  | Runtime | Where | On PATH? |
|---|---|---|---|
| Builds **your project** | .NET SDK 8.0.424 | `C:\Program Files\dotnet\` | yes |
| Runs the **editor tooling** | .NET 10 (private copy) | VS Code's `globalStorage\` | no |

The private copy is sandboxed inside VS Code's storage, is not on your PATH, and
cannot affect `dotnet build` or `dotnet test`. All three projects still target
`net8.0` and still compile with SDK 8.0.424.

The only real cost is about 105 MB of disk. Uninstalling C# Dev Kit does not
always remove it; deleting that `.dotnet` folder is safe if you ever want the
space back.

### Confirming Dev Kit loaded the project properly

Its output log (**View → Output**, then pick *C# Dev Kit* from the dropdown)
should show all three projects:

```
Project ...\src\Rpg.Core\Rpg.Core.csproj             loaded by C# Dev Kit
Project ...\game\StickmanRpg.Game.csproj             loaded by C# Dev Kit
Project ...\src\Rpg.Core.Tests\Rpg.Core.Tests.csproj loaded by C# Dev Kit
```

If the Godot project (`StickmanRpg.Game.csproj`) is missing from that list, you
opened the `game` folder instead of the repository root. Open the root — the
folder containing `StickmanRpg.sln`.

### Alternative: JetBrains Rider

The best C# experience there is, with genuinely good Godot integration —
including real breakpoint debugging inside the running game, which VS Code does
less well. Free for non-commercial use.

---

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `dotnet` **is not recognised** | Terminal opened before the SDK was installed | Open a brand-new terminal. Reboot if that fails. |
| `godot` **is not recognised** | Same, or the shim is gone | New terminal first. If it persists, see below. |
| Godot says **".NET Sdk not found"** | Godot was launched from a shell with a stale PATH | Close Godot completely, open a fresh terminal, launch Godot again. |
| Godot says **SDK version mismatch** | Godot was upgraded | Edit [`game/StickmanRpg.Game.csproj`](../game/StickmanRpg.Game.csproj) and change the number in `Sdk="Godot.NET.Sdk/4.7.2"` to your Godot version. That one number is the only link to a specific release. |
| **Your C# changes do nothing** | The build failed and Godot ran the old version | Check the editor's **Output** panel, or run `dotnet build`. |
| **Godot behaves strangely** | Corrupt import cache | Delete the `game/.godot/` folder. It is regenerated automatically and is git-ignored. |
| **Project will not import** | Wrong folder selected | You must pick `game/project.godot`, not the repository root. |
| **A test fails after you changed a number** | You changed the game balance | Working as designed. See [Recipes](07-recipes.md#tuning-the-difficulty). |
| VS Code: **"`editorPath.godot4` value of `...\stickman-RPG\godot` is not a valid Godot executable"** | Godot Tools needs a real path to the `.exe` | Already fixed in [`.vscode/settings.json`](../.vscode/settings.json). See below. |
| VS Code: **"Couldn't connect to the GDScript language server at 127.0.0.1:6008"** | Harmless — that server only runs while the Godot editor is open, and we write C#, not GDScript | Already silenced in [`.vscode/settings.json`](../.vscode/settings.json). See below. |

### The two Godot Tools errors

Both are configured away in [`.vscode/settings.json`](../.vscode/settings.json),
but they are worth understanding because **a Godot upgrade will bring the first
one back**.

**1. "not a valid Godot executable"**

The `godotTools.editorPath.godot4` setting defaults to the bare word `godot`.
VS Code resolves a bare relative path **against the workspace folder**, so it
looks for `f:\WORKSTATION\Github\stickman-RPG\godot` — which does not exist.

It also will not accept the `godot.cmd` shim that works in your terminal, because
the extension launches the process directly rather than through a shell. It needs
the real `.exe`:

```json
"godotTools.editorPath.godot4": "${env:LOCALAPPDATA}\\Microsoft\\WinGet\\Packages\\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\\Godot_v4.7.2-stable_mono_win64\\Godot_v4.7.2-stable_mono_win64.exe"
```

`${env:LOCALAPPDATA}` is used rather than `C:\Users\<you>` so the file contains no
username. **After upgrading Godot, update the two version numbers in that path.**
To find the new one:

```bash
winget list --id GodotEngine.GodotEngine.Mono
```

**2. "Couldn't connect to the GDScript language server"**

Godot Tools tries to reach Godot's built-in GDScript language server on port
6008. That server only exists while the Godot *editor* is running, so outside the
editor the attempt always fails and retries ten times.

This project is written in C#, so that server does nothing for us and the warning
is pure noise. It is turned off with:

```json
"godotTools.lsp.autoReconnect.enabled": false
```

Everything else the extension does — `.tscn` support, launching the editor — is
unaffected. If you ever do write GDScript, set it back to `true` and keep the
Godot editor open, or set `"godotTools.lsp.headless": true` to have the extension
start its own server.

### If `godot` stops working

The `godot` command is a small shim file living next to the Godot executable. A
Godot upgrade will replace that folder and remove it.

To recreate it, find where Godot lives:

```bash
winget list --id GodotEngine.GodotEngine.Mono
```

Then create `godot.cmd` in that folder containing:

```bat
@echo off
"%~dp0Godot_v4.7.2-stable_mono_win64.exe" %*
```

...adjusting the exe name to match. Add `godotc.cmd` the same way, pointing at
the `..._console.exe` instead.

Or skip the shim entirely and call the executable by its full path.

---

## Setting this up on a different machine

Everything needed is free and takes about fifteen minutes, mostly downloading.

**1. Install the .NET 8 SDK** — <https://dotnet.microsoft.com/download>

**2. Install Godot 4, the .NET build** — <https://godotengine.org/download>

> Take the version labelled **.NET** (sometimes "Mono"). The standard build
> cannot run C# at all, and this project will simply refuse to open. This is the
> most common setup mistake.
>
> Godot has no installer — it is a zip containing one executable.

**3. Open a fresh terminal** and confirm:

```bash
dotnet --version
```

**4. Clone and test:**

```bash
git clone <your-repo-url> stickman-RPG
```

```bash
cd stickman-RPG && dotnet test
```

If that says `Passed! - Failed: 0`, you are done — the rules engine works. Then
point Godot at `game/project.godot` to play it.

On Windows you can do steps 1 and 2 with winget instead:

```bash
winget install Microsoft.DotNet.SDK.8 GodotEngine.GodotEngine.Mono
```

---

## Next

[Getting started](01-getting-started.md) for a gentler walkthrough, or
[Anatomy of a turn](06-anatomy-of-a-turn.md) to understand what the code is
actually doing.
