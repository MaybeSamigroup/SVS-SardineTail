using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using System;
using System.IO;
using Character;
using Fishbone;

namespace SardineTail
{
    internal static partial class CategoryExtensions
    {
        internal static Action<ListInfoBase> RegisterMod(this Category category) =>
             info => Human.lstCtrl._table[category.Index].Add(info.Id, info);
    }
    internal static partial class ModificationExtensions
    {
        static internal void Initialize()
        {
            Event.OnActorSerialize +=
                (actor, archive) => archive.Save(actor.charFile);
            Event.OnCharacterSerialize +=
                (data, archive) => archive.Save(data);
            Event.OnCoordinateSerialize +=
                (data, archive) => archive.Save(data);
            Event.OnActorDeserialize +=
                (actor, archive) => archive.Load(actor.charFile);
            Event.OnPreCharacterDeserialize +=
                (data, limits, archive, current) => data.With(archive.Load, limits).With(current.Save);
            Event.OnPreCoordinateDeserialize +=
                (_, data, limits, archive) => archive.Load(limits, data);
        }
    }
    [BepInProcess(Process)]
    [BepInDependency(Fishbone.Plugin.Guid)]
    [BepInPlugin(Guid, Name, Version)]
    public partial class Plugin : BasePlugin
    {
        public const string Process = "SamabakeScramble";
        public const string Guid = $"{Process}.{Name}";
        internal static readonly string ConversionsPath = Path.Combine(Paths.GameRootPath, "UserData", "plugins", Guid, "hardmods");
        internal static ConfigEntry<bool> DevelopmentMode;
        internal static ConfigEntry<bool> HardmodConversion;
        internal static ConfigEntry<bool> StructureConversion;
        private Harmony Patch;
        public override void Load()
        {
            Patch = new Harmony($"{Name}.Hooks");
            Hooks.ApplyPatches(Patch);
            Instance = this;
            DevelopmentMode = Config.Bind("General", "Enable development package loading.", false);
            HardmodConversion = Config.Bind("General", "Enable hardmod conversion at startup.", false);
            StructureConversion = Config.Bind("General", "Convert hardmod into structured form.", false);
            Paths.GameRootPath.InitializePackages();
            ModificationExtensions.Initialize();
            CategoryExtensions.Initialize();
        }
        public override bool Unload() =>
            true.With(Patch.UnpatchSelf) && base.Unload();
    }

}