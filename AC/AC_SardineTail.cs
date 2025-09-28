using System.Collections.Generic;
using System.Reflection;
using BepInEx.Unity.IL2CPP;

namespace SardineTail
{
    static partial class Hooks
    {

        static Dictionary<string, MethodInfo[]> SpecPrefixes => new();
        static Dictionary<string, MethodInfo[]> SpecPostfixes => new();
    }

    public partial class Plugin : BasePlugin
    {
        public const string Process = "Aicomi";
    }
}