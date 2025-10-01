# SVS-SardineTail

Fundamental plugin to develop soft mod for Aicomi, SamabakeScramble and modification loader for DigitalCraft.

## Prerequisites (Aicomi)

- [BepInEx](https://github.com/BepInEx/BepInEx)
  - [Bleeding Edge (BE) build](https://builds.bepinex.dev/projects/bepinex_be) #738 or later
- [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager)
  - IL2CPP version 18.4.1 or later

Confirmed working under Aicomi 1.0.1.

## Prerequisites (SamabakeScramble)

- [SVS-HF_Patch](https://github.com/ManlyMarco/SVS-HF_Patch)
  - Message Center
  - BepInEx.ConfigurationManager
  - SVS_BepisPlugins

Confirmed working under SamabakeScramble 1.1.6

## Prerequisites (DigitalCraft Standalone)

- [BepInEx](https://github.com/BepInEx/BepInEx)
  - [Bleeding Edge (BE) build](https://builds.bepinex.dev/projects/bepinex_be) #697 or later

Confirmed working under DigitalCraft 2.0.0.

## Prerequisites (Dependent BepInEx plugin)

- [Fishbone/CoastalSmell](https://github.com/MaybeSamigroup/SVS-Fishbone)
  - 3.1.3/1.1.9 or later

## Installation

Extract the [latest release](https://github.com/MaybeSamigroup/SVS-SardineTail/releases/latest) to your game install directory.

## Migration from older release

Remove `SardineTail.dll` from BepinEx/plugins.

Plugin assembly names are now `SVS_SardineTail.dll` and `DC_SardineTail.dll`.

## Migration from 1.X.X to 2.X.X

These directories contained in previous releases are no longer used.
Please move it contents to new one and delete it.

- `UserData/plugins/SamabakeScramble.SardineTail`
  - New! `UserData/plugins/SardineTail`

## How to Use

**For mod users:**  
Acquire a SardineTail package from a trusted source and place it in the `sardines` directory in your game installation directory.

**For mod developers:**  
Refer to the [package specification](https://github.com/MaybeSamigroup/SVS-SardineTail/wiki).

## How to Use in DigitalCraft

Please follow the [installation instructions](https://github.com/MaybeSamigroup/SVS-Fishbone).

Mods for SamabakeScramble-made characters are loaded from the `sardines` directory in the SamabakeScramble installation directory.

Currently, the `sardines` directory under the DigitalCraft installation directory is not used.

## Hardmod Conversion Guide

1. Make a clean install of SamabakeScramble.
2. Back up the `abdata` directory as `abdata.soft`.
3. Install the latest [SVS-HF_Patch](https://github.com/ManlyMarco/SVS-HF_Patch) with the Hardmod pack option enabled.
4. Install the latest [Fishbone/CoastalSmell](https://github.com/MaybeSamigroup/SVS-Fishbone) and [SardineTail](https://github.com/MaybeSamigroup/SVS-SardineTail/releases/latest).
5. Start SamabakeScramble.
   1. Open the plugin settings and enable SardineTail's `Enable hardmod conversion at startup` option.
   2. Exit SamabakeScramble.
6. (Re)Start SamabakeScramble.
   1. Wait until the title scene appears.
   2. Exit SamabakeScramble.
7. Rename the `abdata` directory to `abdata.hard` and rename `abdata.soft` back to `abdata`.
8. Copy `abdata.hard/sardinetail.unity3d` to `abdata/sardinetail.unity3d`.
9. Move `UserData/Plugins/SardineTail/hardmods` to `sardines`.

### About the `invalids` Directory

Item data that references asset bundles not present in `abdata` will not be converted and will be reported in subdirectories under `UserData/Plugins/SardineTail/invalids`.

The directory name corresponds to the item category, and each report is named `(Item's hard id).json`.

The `Invalid` field in the report lists asset bundles that were not found in the `abdata` directory.

**Report example:**

```json
{
  "Id": 111001,
  "Category": 101,
  "Manifest": "abdata",
  "Bundles": [],
  "Invalid": [
    "chara/bo_hair_b_00.unity3d"
  ],
  "Values": {
    "Kind": "1",
    "Possess": "1",
    "MainManifest": "abdata",
    "MainAB": "chara/bo_hair_b_00.unity3d",
    "MainData": "p_cf_hair_b_20_00",
    "SetHair": "0",
    "ThumbAB": null,
    "ThumbTex": "",
    "Image": null,
    "Attribute": "",
    "Detail": "0/1/2/3/4/5"
  }
}
```
