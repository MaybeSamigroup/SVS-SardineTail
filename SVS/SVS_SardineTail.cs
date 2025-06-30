using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using System;
using System.IO;
using Character;
using Fishbone;
using CoastalSmell;

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
                (data, limits, archive, current) => data.With(archive.Load(limits)).With(current.Save);
            Event.OnPreCoordinateDeserialize +=
                (_, data, limits, archive, _) => archive.Load(limits)(data);
        }
    }
    [BepInProcess(Process)]
    [BepInDependency(Fishbone.Plugin.Guid)]
    [BepInPlugin(Guid, Name, Version)]
    public partial class Plugin : BasePlugin
    {
        public const string Process = "SamabakeScramble";
        internal static readonly string ConversionsPath = Path.Combine(Paths.GameRootPath, "UserData", "plugins", Guid, "hardmods");
        internal static ConfigEntry<bool> HardmodConversion;
        internal static ConfigEntry<bool> StructureConversion;
        public override void Load()
        {
            Hooks.ApplyPatches(Patch = new Harmony($"{Name}.Hooks"));
            Instance = this;
            DevelopmentMode = Config.Bind("General", "Enable development package loading.", false);
            HardmodConversion = Config.Bind("General", "Enable hardmod conversion at startup.", false);
            StructureConversion = Config.Bind("General", "Convert hardmod into structured form.", false);
            Paths.GameRootPath.InitializePackages();
            ModificationExtensions.Initialize();
            CategoryExtensions.Initialize();
        }
    }

}