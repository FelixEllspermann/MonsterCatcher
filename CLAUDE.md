# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

`MonsterCatcher` is a **Unity 6 (Editor `6000.4.3f1`) 2D game project**, currently greenfield — there is no gameplay C# in `Assets/` yet, just the default scene and render/input configuration. Expect to be authoring the game from near-scratch.

## How to work in this project (read first)

This project is driven through the **MCP for Unity bridge** (`com.coplaydev.unity-mcp`), not a terminal build loop. Claude manipulates the live Editor — GameObjects, components, scenes, assets, and scripts — over MCP.

- **Use the `unity-mcp-skill` skill for any Editor automation** (creating/modifying GameObjects, editing scripts, managing scenes, running tests). Invoke it before reaching for raw file edits.
- The bridge only works when the **Unity Editor is open with the bridge running**: `Window > MCP for Unity > Start Bridge`. MCP tools fail if the Editor is closed or the bridge is stopped.
- **Prefer creating/modifying assets through the MCP/Editor rather than writing files directly.** The Editor generates `.meta` files and GUIDs and keeps references intact; hand-creating an asset on disk leaves it without a `.meta` until the Editor regains focus and reimports.

There is no committed lint/format config. Tests use the **Unity Test Framework** (`com.unity.test-framework`) — run them via the Editor's Test Runner (`Window > General > Test Runner`) or through the MCP test tooling. A single test is run by selecting it in the Test Runner; CLI batch-mode test runs require a local Unity install (`Unity.exe -runTests -projectPath . -testPlatform EditMode|PlayMode -testFilter <name>`).

## Source vs. generated — do not edit generated dirs

Version-controlled / source of truth: **`Assets/`, `Packages/`, `ProjectSettings/`**.

Generated and safe to ignore (never hand-edit): **`Library/`, `Temp/`, `Logs/`, `obj/`, `UserSettings/`**. Note `Library/PackageCache/` holds the read-only source of installed packages — useful **reference** (e.g. the `com.coplaydev.unity-mcp` README documents the bridge UI), but changes there are transient.

**`.meta` files matter.** Every asset has a sibling `<name>.meta` carrying its GUID; scenes/prefabs/settings reference assets by GUID, not path. Keep an asset and its `.meta` together when moving/renaming/deleting, or references break.

## Architecture

The interesting configuration lives in a few wiring points, since there's no gameplay code yet:

- **Rendering — URP 2D.** Universal Render Pipeline with the 2D Renderer. Pipeline asset `Assets/Settings/UniversalRP.asset`, renderer `Assets/Settings/Renderer2D.asset`, project-wide global settings `Assets/UniversalRenderPipelineGlobalSettings.asset`, post-processing defaults `Assets/DefaultVolumeProfile.asset`. New 2D scenes should be made from the `Assets/Settings/Lit2DSceneTemplate.scenetemplate` so lighting/renderer wiring is correct.
- **Input — new Input System** (`com.unity.inputsystem`, the legacy `Input` manager is not the active path). Actions are defined in `Assets/InputSystem_Actions.inputactions` and registered as the **project-wide** action asset (via `ProjectSettings`’ `com.unity.input.settings.actions`). It ships two maps: `Player` (`Move`, `Look`, `Attack`, `Interact` [Hold], `Crouch`, `Jump`) and `UI`. Add gameplay input by extending these maps, not by polling the old input API.
- **Scenes.** `Assets/Scenes/SampleScene.unity` is the only scene and the sole entry in Build Settings. New scenes must be added to Build Settings to be included in a build.
- **2D toolchain available** (already in `Packages/manifest.json`): Tilemap (+ extras), Sprite Shape, 2D Animation, Aseprite and PSD importers. Reach for these for tilemaps, rigged 2D animation, and art import rather than rolling custom solutions.

## Conventions

- Place game C# under `Assets/` (e.g. an `Assets/Scripts/` folder). Once multiple scripts exist, consider an Assembly Definition (`.asmdef`) to keep compile times and references sane — there is none yet.
- After writing/changing C# from outside the Editor, the Editor must recompile (it does so on focus or via the MCP bridge); MCP script tools also run validation (configurable in the MCP for Unity window: Basic → Strict).
