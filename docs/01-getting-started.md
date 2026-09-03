# 1. Getting started

Goal: two programs installed, tests passing, game running. About 15 minutes,
most of it downloading.

> ### Already done on this machine
>
> The .NET SDK, Godot, and the VS Code extensions are **already installed and
> verified working here**. If you are on the machine this project was set up on,
> skip straight to [How to run](00-how-to-run.md) — you only need three commands.
>
> Read on if you are setting up a second machine, or if you want to understand
> what got installed and why.

---

## Step 1 — Install the .NET SDK

**What it is:** the compiler and tooling for C#. "SDK" just means "the developer
version" (as opposed to the *runtime*, which only runs C# programs but cannot
build them). You need the SDK.

**Get it:** <https://dotnet.microsoft.com/download> — take **.NET 8**.

**Check it worked.** Open a *new* terminal (important — see the warning below)
and run:

```bash
dotnet --version
```

You should see something like `8.0.424`.

> ### The single most common setup problem
>
> Installing the SDK adds it to your system PATH, but **terminals and programs
> that were already open keep the old PATH**. If `dotnet --version` says "command
> not found", or if Godot later says `.NET Sdk not found`, the fix is almost
> always: **close everything and open a fresh terminal**, or reboot.

---

## Step 2 — Install Godot

**What it is:** the game engine — the program that opens a window, draws things,
and reads your mouse and keyboard.

**Get it:** <https://godotengine.org/download>

**Take the ".NET" build.** The download page offers two versions:

| Version | Use it? |
|---|---|
| **Godot Engine** (standard) | ❌ No. This one cannot run C#. |
| **Godot Engine — .NET** (sometimes labelled "Mono") | ✅ **Yes.** |

If you download the wrong one, the project will simply refuse to open. That is
the most common "why doesn't this work" moment.

Godot has **no installer**. It is a `.zip` containing a single `.exe`. Unzip it
somewhere sensible and run the exe directly. It is about 120 MB — smaller than
most phone apps.

---

## Step 3 — Run the tests (do this before opening Godot)

From this folder:

```bash
dotnet test
```

Expected output:

```
Passed!  - Failed: 0, Passed: 21, Skipped: 0, Total: 21, Duration: 140 ms
```

**Stop and appreciate what just happened.** You ran the entire combat system of a
role-playing game — damage, poison, stun, cooldowns, turn order, monster AI, and
one thousand complete simulated battles — in about a seventh of a second, without
opening a game engine, without drawing a single pixel, without a window ever
appearing.

That is not a trick. It is the whole reason this project is structured the way it
is, and it is explained in [How it all fits together](04-architecture.md).

### See the interesting numbers

```bash
dotnet test --logger "console;verbosity=detailed"
```

Scroll and you will find:

```
Battles simulated : 1000
Hero wins         : 749 (74.9%)
Monster wins      : 251
Draws (hit round limit) : 0
Average length    : 6.7 rounds
```

Those numbers come from playing a thousand full battles with the computer
controlling **both** sides. This is how the fight was balanced — by measuring,
not by guessing. More on that in [Recipes](07-recipes.md#tuning-the-difficulty).

---

## Step 4 — Run the game

1. Open Godot. You get the **Project Manager** window (a list of projects, empty
   for now).
2. Click **Import**.
3. Navigate to this folder and select **`game/project.godot`**. Not the repo root
   — the `game` subfolder. `project.godot` is the file Godot recognises as "a
   project lives here".
4. Click **Import & Edit**.
5. Godot opens the editor. The first time, it spends a few seconds building the
   C# and generating a `.godot/` folder (that folder is cache — it is
   git-ignored, and deleting it is always safe).
6. Press **F5** (or the ▶ play button, top right).

You should get a window with four stick figures, health bars, a row of buttons,
and a combat log.

**How to play:** the monsters move on their own. When it is a hero's turn, the
buttons on the right show every legal move. Click one. Watch the log.

---

## If something goes wrong

| Symptom | Cause | Fix |
|---|---|---|
| Godot says **".NET Sdk not found"** | Godot was launched before the SDK was installed, or from a stale shell | Close Godot completely, open a fresh terminal, relaunch Godot. Reboot if needed. |
| Godot says the **SDK version does not match** | `game/StickmanRpg.Game.csproj` pins a Godot version | Open that file, change the number in `Sdk="Godot.NET.Sdk/4.7.2"` to your Godot version (shown in Godot's title bar). That one number is the only link to a specific Godot release. |
| **No C# option anywhere** in Godot | You downloaded the standard build, not the .NET one | Re-download the **.NET / Mono** build. |
| **The project will not import** | You selected the wrong folder | You must select `game/project.godot`, not the repository root. |
| `dotnet` **is not recognised** | Stale PATH | Open a brand-new terminal. Reboot if that fails. |
| Tests fail after you changed something | You probably changed a balance number | Expected! See [Recipes](07-recipes.md#tuning-the-difficulty) — the balance test is *designed* to fail when you change the fight. |

---

## What next

For the full set of commands — running one test at a time, headless runs, the
VS Code tasks, the day-to-day development loop — see
[How to run](00-how-to-run.md).

Read the [C# crash course](02-csharp-crash-course.md) next if C# is unfamiliar —
it is short and it covers only the syntax that actually appears in this repo.

If you would rather dive straight into the code, jump to
[Anatomy of a turn](06-anatomy-of-a-turn.md).
