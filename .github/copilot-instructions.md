# SardineTail AI Coding Guidelines

## Project Overview
SardineTail is a BepInEx plugin for Unity IL2CPP games (SamabakeScramble, Aicomi, DigitalCraft) that enables soft modding of character customization assets. It loads mod packages from STP archives, patches Unity asset loading, and integrates mods into the game's character system.

## Architecture
- **Multi-target .NET 6.0 projects**: Separate assemblies for each game (SVS_SardineTail.dll, AC_SardineTail.dll, DC_SardineTail.dll) sharing core logic via partial classes.
- **Harmony patching**: Hooks into Unity methods like `MaterialHelper.LoadPatchMaterial` and `AssetBundle.LoadAsset` to intercept asset requests.
- **Il2CppInterop**: Bridges managed code with IL2CPP runtime for runtime type injection and method calls.
- **Reactive Extensions**: Event-driven architecture using Rx.NET for UI updates and lifecycle management.
- **Mod loading pipeline**: STP packages → parsed into `ListInfoBase` → registered in `Human.lstCtrl._table` → resolved via `CategoryExtension`.

## Key Components
- **ModPackage**: Abstract base for loading mod packages from directories or archives. Registers mods by category and assigns unique IDs starting from `ModInfo.MIN_ID` (1,000,000,000).
- **CategoryExtension**: Defines data schemas for character categories (e.g., `CatNo.bo_body`, `CatNo.co_top`) with entries like `Ktype.MainAB`, `Ktype.MainData`. Handles ID assignment and normalization.
- **Hooks**: Game-specific Harmony patches for asset loading. Prefixes/postfixes wrap Unity methods to inject mod assets.
- **ModLeaf/ModNode**: Hierarchical parsing of mod directories (e.g., `values.json`, `*.png`, `*.csv`) into key-value pairs.
- **UnityFS**: Custom parser for UnityFS asset bundle format, reading big-endian headers and node structures.

## Data Flow
1. Mod packages placed in `sardines/` directory.
2. `ModPackage.InitializePackages()` scans directories/archives.
3. `CategoryCollector` parses mod structures using `Contents()`, `GetValues()`, `GetContent()`.
4. Mods resolved to `ListInfoBase` with normalized values (assets prefixed with `{gameId}:{pkgId}:`).
5. `Human.lstCtrl._table[category][id]` populated for game access.
6. Patches redirect asset requests to mod bundles.

## Development Workflows
- **Building**: Use `dotnet build --configuration Debug/Release` from solution root. Tasks available: `dotnet: build debug`, `dotnet: build release`.
- **Mod development**: Create dev packages as directories (e.g., `MyMod-1.0.0/` with `values.json`, asset files). Run `StpBuilder.exe MyMod-1.0.0` to create `MyMod-1.0.0.stp`.
- **Testing mods**: Enable `DevelopmentMode` config to load unpacked dev directories instead of STP archives.
- **Hardmod conversion**: Enable `HardmodConversion` to migrate legacy mods from `abdata/` to STP format, generating reports in `UserData/Plugins/SardineTail/invalids/`.

## Code Patterns
- **Partial classes**: Core logic split across `SardineTail.cs`, game-specific files (e.g., `SVS/SVS_SardineTail.cs`), and shared files (e.g., `Custom.cs`).
- **Functional style**: Heavy use of LINQ, extension methods, and immutable records. Methods like `With()`, `Either()`, `F.Apply()`.
- **Error handling**: Patches wrapped with `wrapTryCatch = true`. Use `Try()` extension for fallible operations.
- **Naming**: `CatNo` for categories, `Ktype` for keys, `Vtype` for value types (Name, Asset, Image, Text, Store).
- **Asset resolution**: Images/assets prefixed with `{gameId}:{pkgId}:{bundle}:{path}` or `{gameId}:{pkgId}:{path}`. "0" indicates default/unset.
- **Migrations**: `softmig.json`/`hardmig.json` for version compatibility. Hard migrations map old IDs to new mod structures.

## Game-Specific Details
- **SamabakeScramble (SVS)**: AssetPath = "abdata", MainManifest = "abdata". Hooks `MaterialHelper.LoadPatchMaterial(string)`.
- **Aicomi (AC)**: AssetPath = "lib", MainManifest = "lib000_03". Hooks `ChaListControl.LoadListInfoAll` for conversion. Shader translation from LIF/ to AC/ prefixes.
- **DigitalCraft (DC)**: Supports multiple games (IDs 0,10,20,30). AssetPath dynamic via `PathManager`. Hooks `MaterialHelper.LoadPatchMaterial(int, string, string, bool)`. Uses `CharaMods`/`CoordMods` extensions.

## Dependencies & Integration
- **BepInEx**: Plugin framework with IL2CPP support.
- **Fishbone/CoastalSmell**: Core modding library providing `Extension<T>`, `ModPackage` base.
- **Harmony**: Runtime patching library.
- **Il2CppInterop**: IL2CPP bridge for type injection and method calls.
- **Reactive Extensions**: For observable UI events and subscriptions.
- **7zip**: Embedded LZMA compression for asset bundles.

## Common Tasks
- **Adding new category**: Define in `Definitions.cs` with `Entry[]` array. Update `CategoryExtension.All`.
- **New patch**: Add to `Hooks.SpecPrefixes/Postfixes` dictionary with `MethodInfo` targets.
- **Mod parsing**: Implement `CategoryCollector` subclass for new package formats.
- **UI customization**: Use `UGUI` helpers and Rx subscriptions in `Custom.cs`.

## Key Files
- `SardineTail.cs`: Main plugin class, hooks initialization.
- `Definitions.cs`: Category and entry schemas.
- `Modifications.cs`: Mod application logic for `HumanData`.
- `Custom.cs`: UI extensions for character creation.
- `UnityFS.cs`: Asset bundle parsing.
- `Conversion.cs`: Hardmod to softmod migration.
- `StpBuilder/StpBuilder.cs`: Tool for building STP archives (unity3d uncompressed, others optimal).</content>
<parameter name="filePath">f:\Repositories\SVS-SardineTail\.github\copilot-instructions.md