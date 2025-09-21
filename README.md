# SVS-SardineTail

Fundamental plugin to develop soft mod for SamabakeScramble and modification loader for DigitalCraft.

## Prerequisites

- [SVS-HF_Patch](https://github.com/ManlyMarco/SVS-HF_Patch)
  - Message Center
  - BepInEx.ConfigurationManager
  - SVS_BepisPlugins
- [Fishbone/CoastalSmell](https://github.com/MaybeSamigroup/SVS-Fishbone)
  - 3.0.0/1.0.6 or later

Confirmed working under SamabakeScramble 1.1.6 and DigitalCraft 2.0.0

## Installation

Extract the [latest release](https://github.com/MaybeSamigroup/SVS-SardineTail/releases/latest) to your game install directory.

## Migration from older release

Remove SardineTail.dll from BepinEx/plugins.

Plugin assembly names are now SVS_SardineTail.dll and DC_SardineTail.dll.

## Migration from 1.X.X to 2.X.X

These directories contained in previous releases are no longer used.
Please move it contents to new one and delete it.

- (GameRoot)/UserData/plugins/SamabakeScramble.SardineTail
  - New! (GameRoot)/UserData/plugins/SardineTail

## How to use

For mod user, acquire sardine tail package from somewhere and place it in sardines directory at  your game install directory.

For mod developer, refer the [package specification](https://github.com/MaybeSamigroup/SVS-SardineTail/wiki)

## How to use in DC

Please follow [installation instruction](https://github.com/MaybeSamigroup/SVS-Fishbone).

Mods for SamabakeScramble made characters are loaded from sardines directory in SamabakeScramble install directory.

Currently there is no use of sardines directory under Digital Craft install directory.
