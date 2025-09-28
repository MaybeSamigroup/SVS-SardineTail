using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using BepInEx;
using BepInEx.Unity.IL2CPP;

namespace SardineTail
{
    static partial class Hooks
    {
        static Dictionary<string, MethodInfo[]> SpecPrefixes => new();

        static Dictionary<string, MethodInfo[]> SpecPostfixes => new()
        {
            [nameof(LoadAssetWithoutTypePostfix)] = [
                typeof(AssetBundle).GetMethod(nameof(AssetBundle.LoadAsset), 0, [typeof(string)])
            ],
        };
    }
    public partial class EntryWrapper : Il2CppSystem.IO.Stream
    {
        public override void SetLength(long value) =>
            throw new NotImplementedException();
    }
    [BepInDependency(VarietyOfScales.Plugin.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    public partial class Plugin : BasePlugin
    {
        public const string Process = "SamabakeScramble";
    }
}