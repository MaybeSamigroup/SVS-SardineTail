using System;
using System.Collections.Generic;
using System.Linq;
using Character;
using CatNo = ChaListDefine.CategoryNo;
using CharaLimit = Character.HumanData.LoadLimited.Flags;
using CoordLimit = Character.HumanDataCoordinate.LoadLimited.Flags;
using Fishbone;
using CoastalSmell;

namespace SardineTail
{
    public record ModInfo
    {
        public const int MIN_ID = 1000_000_000;
        public string PkgId { get; init; }
        public string ModId { get; init; }
        public CatNo Category { get; init; }
        public Version PkgVersion { get; init; }
        public static Dictionary<CatNo, (Func<ModInfo, int, int> ToId, Func<int, ModInfo> ToMod)> Map =
            Enum.GetValues<CatNo>().ToDictionary(categoryNo => categoryNo, categoryNo => (ModPackage.ToId.Apply(categoryNo), ModPackage.FromId.Apply(categoryNo)));
    }

    public static class ModificationExtension
    {
        internal static Dictionary<K, V> Defaults<K, V>(this Dictionary<K, V> values) where K : struct, Enum where V : new() =>
            (values?.Count ?? 0) is not 0 ? values : Enum.GetValues<K>().ToDictionary(item => item, item => new V());

        internal static Dictionary<int, V> Defaults<V>(this Dictionary<int, V> values, int count) where V : new() =>
            (values?.Count ?? 0) is not 0 ? values : Enumerable.Range(0, count).ToDictionary(item => item, item => new V());

        internal static T Defaults<T>(this T values) where T : new() =>
            values ?? new T();

        internal static (int, ModInfo)[] ToEntry(this int index, ModInfo mod) => mod == null ? [] : [(index, mod)];

        internal static (T, ModInfo)[] ToEntry<T>(this T index, ModInfo mod) => mod == null ? [] : [(index, mod)];
    }

    public record HairsMods
    {
        public ModInfo HairGloss { get; init; }
        public Dictionary<ChaFileDefine.HairKind, ModInfo> Hairs { get; init; }
        public void Apply(HumanDataHair data)
        {
            data.glossId = ModInfo.Map[CatNo.mt_hairgloss].ToId(HairGloss, data.glossId);
            Hairs.Defaults().ForEach(entry =>
                data.parts[(int)entry.Key].id = data.parts[(int)entry.Key].bundleId =
                    ModInfo.Map[ToCategoryNo(entry.Key)].ToId(entry.Value, data.parts[(int)entry.Key].id)
            );
        }
        public static HairsMods ToMods(HumanDataHair data) => new()
            {
                HairGloss = ModInfo.Map[CatNo.mt_hairgloss].ToMod(data.glossId),
                Hairs = Enum.GetValues<ChaFileDefine.HairKind>()
                    .Where(part => data.parts[(int)part] is not null)
                    .SelectMany(part => part.ToEntry(ModInfo.Map[ToCategoryNo(part)].ToMod(data.parts[(int)part].id)))
                    .ToDictionary()
            };
        static CatNo ToCategoryNo(ChaFileDefine.HairKind value) => value switch
        {
            ChaFileDefine.HairKind.back => CatNo.bo_hair_b,
            ChaFileDefine.HairKind.front => CatNo.bo_hair_f,
            ChaFileDefine.HairKind.side => CatNo.bo_hair_s,
            ChaFileDefine.HairKind.option => CatNo.bo_hair_o,
            _ => throw new ArgumentException()
        };
    }

    public record ClothMods
    {
        public ModInfo Part { get; init; }
        public Dictionary<int, ModInfo> Paints { get; init; }
        public Dictionary<int, ModInfo> Patterns { get; init; }
        internal void Apply(ChaFileDefine.ClothesKind part, HumanDataClothes.PartsInfo data)
        {
            data.id = ModInfo.Map[ToCategoryNo(part)].ToId(Part, data.id);
            Paints.Defaults(data.paintInfos.Count).ForEach(entry =>
                data.paintInfos[entry.Key].ID = ModInfo.Map[CatNo.mt_body_paint].ToId(entry.Value, data.paintInfos[entry.Key].ID));
            Patterns.Defaults(data.colorInfo.Count).ForEach(entry =>
                data.colorInfo[entry.Key].patternInfo.pattern = ModInfo.Map[CatNo.mt_pattern].ToId(entry.Value, data.colorInfo[entry.Key].patternInfo.pattern));
        }

        static CatNo ToCategoryNo(ChaFileDefine.ClothesKind value) => value switch
        {
            ChaFileDefine.ClothesKind.top => CatNo.co_top,
            ChaFileDefine.ClothesKind.bot => CatNo.co_bot,
            ChaFileDefine.ClothesKind.bra => CatNo.co_bra,
            ChaFileDefine.ClothesKind.shorts => CatNo.co_shorts,
            ChaFileDefine.ClothesKind.gloves => CatNo.co_gloves,
            ChaFileDefine.ClothesKind.panst => CatNo.co_panst,
            ChaFileDefine.ClothesKind.socks => CatNo.co_socks,
            ChaFileDefine.ClothesKind.shoes => CatNo.co_shoes,
            _ => throw new ArgumentException()
        };

        static Tuple<ChaFileDefine.ClothesKind, ClothMods> ToMod(ChaFileDefine.ClothesKind part, HumanDataClothes.PartsInfo data) =>
            new(part, new ClothMods
            {
                Part = ModInfo.Map[ToCategoryNo(part)].ToMod(data.id),
                Paints = data.paintInfos.SelectMany((data, index) => index.ToEntry(ModInfo.Map[CatNo.mt_body_paint].ToMod(data.ID))).ToDictionary(),
                Patterns = data.colorInfo.SelectMany((data, index) => index.ToEntry(ModInfo.Map[CatNo.mt_pattern].ToMod(data.patternInfo.pattern))).ToDictionary(),
            });

        internal static Dictionary<ChaFileDefine.ClothesKind, ClothMods> ToMod(HumanDataClothes data) =>
            Enum.GetValues<ChaFileDefine.ClothesKind>()
                .Where(part => data.parts[(int)part] is not null)
                .Select(part => ToMod(part, data.parts[(int)part]))
                .ToDictionary();
    }

    public record AccessoryMods
    {
        public ModInfo Part { get; init; }
        public Dictionary<int, ModInfo> Patterns { get; init; }
        internal void Apply(HumanDataAccessory.PartsInfo data)
        {
            data.id = ModInfo.Map[(CatNo)data.type].ToId(Part, data.id);
            Patterns.Defaults(data.colorInfo.Count).ForEach(entry =>
                data.colorInfo[entry.Key].pattern = ModInfo.Map[CatNo.mt_pattern].ToId(entry.Value, data.colorInfo[entry.Key].pattern));
        }

        static Tuple<int, AccessoryMods> ToMod(int slot, HumanDataAccessory.PartsInfo data) =>
            new(slot, new AccessoryMods
            {
                Part = ModInfo.Map[(CatNo)data.type].ToMod(data.id),
                Patterns = data.colorInfo.SelectMany((data, index) => index.ToEntry(ModInfo.Map[CatNo.mt_pattern].ToMod(data.pattern))).ToDictionary(),
            });

        internal static Dictionary<int, AccessoryMods> ToMod(HumanDataAccessory data) =>
            Enumerable.Range(0, data.parts.Count)
                .Where(slot => data.parts[slot] is not null)
                .Select(slot => ToMod(slot, data.parts[slot]))
                .ToDictionary();
    }

    public record FaceMakeupMods
    {
        public ModInfo Eyeshadow { get; init; }
        public ModInfo Cheek { get; init; }
        public ModInfo Lip { get; init; }
        public Dictionary<int, ModInfo> Paints { get; init; }
        public Dictionary<int, ModInfo> Layouts { get; init; }

        internal void Apply(HumanDataFaceMakeup data)
        {
            data.eyeshadowId = ModInfo.Map[CatNo.mt_eyeshadow].ToId(Eyeshadow, data.eyeshadowId);
            data.cheekId = ModInfo.Map[CatNo.mt_cheek].ToId(Cheek, data.cheekId);
            data.lipId = ModInfo.Map[CatNo.mt_lip].ToId(Lip, data.lipId);
            Paints.Defaults(data.paintInfos.Count).ForEach(entry =>
                data.paintInfos[entry.Key].ID = ModInfo.Map[CatNo.mt_face_paint].ToId(entry.Value, data.paintInfos[entry.Key].ID));
            Layouts.Defaults(data.paintInfos.Count).ForEach(entry =>
                data.paintInfos[entry.Key].layoutID = ModInfo.Map[CatNo.facepaint_layout].ToId(entry.Value, data.paintInfos[entry.Key].layoutID));
        }

        internal static FaceMakeupMods ToMod(HumanDataFaceMakeup data) => new()
        {
            Eyeshadow = ModInfo.Map[CatNo.mt_eyeshadow].ToMod(data.eyeshadowId),
            Cheek = ModInfo.Map[CatNo.mt_cheek].ToMod(data.cheekId),
            Lip = ModInfo.Map[CatNo.mt_lip].ToMod(data.lipId),
            Paints = data.paintInfos.SelectMany((data, index) => index.ToEntry(ModInfo.Map[CatNo.mt_face_paint].ToMod(data.ID))).ToDictionary(),
            Layouts = data.paintInfos.SelectMany((data, index) => index.ToEntry(ModInfo.Map[CatNo.facepaint_layout].ToMod(data.layoutID))).ToDictionary(),
        };
    }

    public record BodyMakeupMods
    {
        public ModInfo Nail { get; init; }
        public ModInfo NailLeg { get; init; }
        public Dictionary<int, ModInfo> Paints { get; init; }
        public Dictionary<int, ModInfo> Layouts { get; init; }

        internal void Apply(HumanDataBodyMakeup data)
        {
            data.nailInfo.ID = ModInfo.Map[CatNo.bo_nail].ToId(Nail, data.nailInfo.ID);
            data.nailLegInfo.ID = ModInfo.Map[CatNo.bo_nail].ToId(NailLeg, data.nailLegInfo.ID);
            Paints.Defaults(data.paintInfos.Count).ForEach(entry =>
                data.paintInfos[entry.Key].ID = ModInfo.Map[CatNo.mt_body_paint].ToId(entry.Value, data.paintInfos[entry.Key].ID));
            Layouts.Defaults(data.paintInfos.Count).ForEach(entry =>
                data.paintInfos[entry.Key].layoutID = ModInfo.Map[CatNo.bodypaint_layout].ToId(entry.Value, data.paintInfos[entry.Key].layoutID));
        }

        internal static BodyMakeupMods ToMod(HumanDataBodyMakeup data) => new()
        {
            Nail = ModInfo.Map[CatNo.bo_nail].ToMod(data.nailInfo.ID),
            NailLeg = ModInfo.Map[CatNo.bo_nail_leg].ToMod(data.nailLegInfo.ID),
            Paints = data.paintInfos.SelectMany((data, index) => index.ToEntry(ModInfo.Map[CatNo.mt_body_paint].ToMod(data.ID))).ToDictionary(),
            Layouts = data.paintInfos.SelectMany((data, index) => index.ToEntry(ModInfo.Map[CatNo.bodypaint_layout].ToMod(data.layoutID))).ToDictionary(),
        };
    }

    public record CoordMods : CoordinateExtension<CoordMods>, CoordinateConversion<CoordMods>
    {
        public HairsMods Hairs { get; init; }
        public Dictionary<ChaFileDefine.ClothesKind, ClothMods> Clothes { get; init; }
        public Dictionary<int, AccessoryMods> Accessories { get; init; }
        public BodyMakeupMods BodyMakeup { get; init; }
        public FaceMakeupMods FaceMakeup { get; init; }

        public CoordMods Merge(CoordLimit limit, CoordMods mods) => new()
        {
            BodyMakeup = (limit & CoordLimit.BodyMakeup) is CoordLimit.None ? BodyMakeup : mods.BodyMakeup,
            FaceMakeup = (limit & CoordLimit.FaceMakeup) is CoordLimit.None ? FaceMakeup : mods.FaceMakeup,
            Accessories = (limit & CoordLimit.Accessory) is CoordLimit.None ? Accessories : mods.Accessories,
            Clothes = (limit & CoordLimit.Clothes) is CoordLimit.None ? Clothes : mods.Clothes,
            Hairs = (limit & CoordLimit.Hair) is CoordLimit.None ? Hairs : mods.Hairs,
        };
        public CoordMods Convert(HumanDataCoordinate data) => ToMods(data);

        internal void Apply(HumanDataCoordinate data)
        {
            BodyMakeup.Defaults().Apply(data.BodyMakeup);
            FaceMakeup.Defaults().Apply(data.FaceMakeup);
            Hairs.Defaults().Apply(data.Hair);
            Clothes.Defaults()
                .Where(entry => (int)entry.Key < data.Clothes.parts.Count)
                .ForEach(entry => entry.Value.Apply(entry.Key, data.Clothes.parts[(int)entry.Key]));
            Accessories.Defaults(data.Accessory.parts.Count)
                .Where(entry => entry.Key < data.Accessory.parts.Count)
                .ForEach(entry => entry.Value.Apply(data.Accessory.parts[entry.Key]));
        }

        internal static void Store(Human human) =>
            Extension<CharaMods, CoordMods>.Humans.NowCoordinate[human] = ToMods(human.coorde.Now);

        internal static CoordMods ToMods(HumanDataCoordinate data) => new()
        {
            BodyMakeup = BodyMakeupMods.ToMod(data.BodyMakeup),
            FaceMakeup = FaceMakeupMods.ToMod(data.FaceMakeup),
            Hairs = HairsMods.ToMods(data.Hair),
            Clothes = ClothMods.ToMod(data.Clothes),
            Accessories = AccessoryMods.ToMod(data.Accessory)
        };
    }

    public record EyeMods
    {
        public ModInfo Eye { get; init; }
        public ModInfo Pupil { get; init; }
        public ModInfo Gradation { get; init; }
        public Dictionary<int, ModInfo> Highlights { get; init; }

        internal void Apply(HumanDataFace.PupilInfo data)
        {
            data.id = ModInfo.Map[CatNo.mt_eye].ToId(Eye, data.id);
            data.overId = ModInfo.Map[CatNo.mt_eyepipil].ToId(Pupil, data.overId);
            data.gradMaskId = ModInfo.Map[CatNo.mt_eye_gradation].ToId(Gradation, data.gradMaskId);
            Highlights.Defaults(data.highlightInfos.Count)
                .ForEach((key, val) => data.highlightInfos[key].id =
                     ModInfo.Map[CatNo.mt_eye_hi_up].ToId(val, data.highlightInfos[key].id));
        }

        internal static EyeMods ToMod(HumanDataFace.PupilInfo data) => new()
        {
            Eye = ModInfo.Map[CatNo.mt_eye].ToMod(data.id),
            Pupil = ModInfo.Map[CatNo.mt_eyepipil].ToMod(data.overId),
            Gradation = ModInfo.Map[CatNo.mt_eye_gradation].ToMod(data.gradMaskId),
            Highlights = data.highlightInfos.SelectMany((data, index) => index.ToEntry(ModInfo.Map[CatNo.mt_eye_hi_up].ToMod(data.id))).ToDictionary(),
        };
    }

    public class FaceMods
    {
        public ModInfo Head { get; set; }
        public ModInfo Detail { get; set; }
        public ModInfo Mole { get; set; }
        public ModInfo MoleLayout { get; set; }
        public ModInfo Nose { get; set; }
        public ModInfo LipLine { get; set; }
        public ModInfo Eyebrows { get; set; }
        public ModInfo Eyelid { get; set; }
        public ModInfo EyelineDown { get; set; }
        public ModInfo EyelineUp { get; set; }
        public ModInfo EyeWhite { get; set; }
        public Dictionary<int, EyeMods> Eyes { get; set; }

        internal void Apply(HumanDataFace data)
        {
            data.headId = ModInfo.Map[CatNo.bo_head].ToId(Head, data.headId);
            data.detailId = ModInfo.Map[CatNo.mt_face_detail].ToId(Detail, data.detailId);
            data.moleInfo.ID = ModInfo.Map[CatNo.mt_mole].ToId(Mole, data.moleInfo.ID);
            data.moleInfo.layoutID = ModInfo.Map[CatNo.mole_layout].ToId(MoleLayout, data.moleInfo.layoutID);
            data.noseId = ModInfo.Map[CatNo.mt_nose].ToId(Nose, data.noseId);
            data.lipLineId = ModInfo.Map[CatNo.mt_lipline].ToId(LipLine, data.lipLineId);
            data.eyebrowId = ModInfo.Map[CatNo.mt_eyebrow].ToId(Eyebrows, data.eyebrowId);
            data.eyelidId = ModInfo.Map[CatNo.mt_eyelid].ToId(Eyelid, data.eyelidId);
            data.eyelineDownId = ModInfo.Map[CatNo.mt_eyeline_down].ToId(EyelineDown, data.eyelineDownId);
            data.eyelineUpId = ModInfo.Map[CatNo.mt_eyeline_up].ToId(EyelineUp, data.eyelineUpId);
            data.whiteId = ModInfo.Map[CatNo.mt_eye_white].ToId(EyeWhite, data.whiteId);
            Eyes.Defaults(data.pupil.Count).ForEach((index, value) => value.Apply(data.pupil[index]));
        }

        internal static FaceMods ToMod(HumanDataFace data) => new()
        {
            Head = ModInfo.Map[CatNo.bo_head].ToMod(data.headId),
            Detail = ModInfo.Map[CatNo.mt_face_detail].ToMod(data.detailId),
            Mole = ModInfo.Map[CatNo.mt_mole].ToMod(data.moleInfo.ID),
            MoleLayout = ModInfo.Map[CatNo.mole_layout].ToMod(data.moleInfo.layoutID),
            Nose = ModInfo.Map[CatNo.mt_nose].ToMod(data.noseId),
            LipLine = ModInfo.Map[CatNo.mt_lipline].ToMod(data.lipLineId),
            Eyebrows = ModInfo.Map[CatNo.mt_eyebrow].ToMod(data.eyebrowId),
            Eyelid = ModInfo.Map[CatNo.mt_eyelid].ToMod(data.eyelidId),
            EyelineDown = ModInfo.Map[CatNo.mt_eyeline_down].ToMod(data.eyelineDownId),
            EyelineUp = ModInfo.Map[CatNo.mt_eyeline_up].ToMod(data.eyelineUpId),
            EyeWhite = ModInfo.Map[CatNo.mt_eye_white].ToMod(data.whiteId),
            Eyes = data.pupil.Select((data, index) => (index, EyeMods.ToMod(data))).ToDictionary()
        };
    }

    public class BodyMods
    {
        public ModInfo Detail { get; set; }
        public ModInfo Sunburn { get; set; }
        public ModInfo Nip { get; set; }
        public ModInfo Underhair { get; set; }

        internal void Apply(HumanDataBody data)
        {
            data.detailId = ModInfo.Map[CatNo.mt_body_detail].ToId(Detail, data.detailId);
            data.sunburnId = ModInfo.Map[CatNo.mt_sunburn].ToId(Sunburn, data.sunburnId);
            data.nipId = ModInfo.Map[CatNo.mt_nip].ToId(Nip, data.nipId);
            data.underhairId = ModInfo.Map[CatNo.mt_underhair].ToId(Underhair, data.underhairId);
        }

        internal static BodyMods ToMod(HumanDataBody data) => new()
        {
            Detail = ModInfo.Map[CatNo.mt_body_detail].ToMod(data.detailId),
            Sunburn = ModInfo.Map[CatNo.mt_sunburn].ToMod(data.sunburnId),
            Nip = ModInfo.Map[CatNo.mt_nip].ToMod(data.nipId),
            Underhair = ModInfo.Map[CatNo.mt_underhair].ToMod(data.underhairId)
        };
    }
    [Extension<CharaMods, CoordMods>(Plugin.Name, "mods.json")]
    public record CharaMods :
        CharacterExtension<CharaMods>, ComplexExtension<CharaMods, CoordMods>, CharacterConversion<CharaMods>
    {
        public ModInfo Figure { get; init; }
        public ModInfo Graphic { get; init; }
        public FaceMods Face { get; init; }
        public BodyMods Body { get; init; }
        public Dictionary<int, CoordMods> Coordinates { get; init; }
        internal int FigureId = -1;
        public CharaMods Merge(CharaLimit limit, CharaMods mods) => new()
        {
            FigureId = (limit & CharaLimit.Body) is CharaLimit.None ? FigureId : mods.FigureId,
            Figure = (limit & CharaLimit.Body) is CharaLimit.None ? Figure : mods.Figure,
            Body = (limit & CharaLimit.Body) is CharaLimit.None ? Body : mods.Body,
            Face = (limit & CharaLimit.Face) is CharaLimit.None ? Face : mods.Face,
            Graphic = (limit & CharaLimit.Graphic) is CharaLimit.None ? Graphic : mods.Graphic,
            Coordinates = (limit & CharaLimit.Coorde) is CharaLimit.None ? Coordinates : mods.Coordinates,
        };
        public CharaMods Convert(HumanData data) => (this with
        {
            Body = BodyMods.ToMod(data.Custom.Body),
            Face = FaceMods.ToMod(data.Custom.Face),
            Graphic = ModInfo.Map[CatNo.mt_ramp].ToMod(data.Graphic.RampID),
            Coordinates = data.Coordinates.Index().ToDictionary(tuple => tuple.Item2, tuple => CoordMods.ToMods(tuple.Item1))
        }).With(() => Plugin.Instance.Log.LogInfo("conversion run!"));

        public CoordMods Get(int coordinateType) =>
            Coordinates.Defaults().GetValueOrDefault(coordinateType, new ());

        public CharaMods Merge(int coordinateType, CoordMods mods) => new()
        {
            FigureId = FigureId,
            Figure = Figure,
            Body = Body,
            Face = Face,
            Graphic = Graphic,
            Coordinates = Coordinates.Merge(coordinateType, mods)
        };

        internal void Apply(HumanData data)
        {
            FigureId = ModInfo.Map[CatNo.bo_body].ToId(Figure, -1);
            data.Graphic.RampID = ModInfo.Map[CatNo.mt_ramp].ToId(Graphic, data.Graphic.RampID);
            Body.Defaults().Apply(data.Custom.Body);
            Face.Defaults().Apply(data.Custom.Face);
            Coordinates.Defaults(data.Coordinates.Count)
                .ForEach((index, value) => value.Apply(data.Coordinates[index]));
        }

        internal static void Store(Human human) =>
            Extension<CharaMods, CoordMods>.Humans[human] = ToMods(human, Extension<CharaMods, CoordMods>.Humans[human]);

        static CharaMods ToMods(Human human, CharaMods mods) => new CharaMods()
        {
            FigureId = mods.FigureId,
            Figure = ModInfo.Map[CatNo.bo_body].ToMod(mods.FigureId),
            Body = BodyMods.ToMod(human.data.Custom.Body),
            Face = FaceMods.ToMod(human.data.Custom.Face),
            Graphic = ModInfo.Map[CatNo.mt_ramp].ToMod(human.data.Graphic.RampID),
            Coordinates = human.data.Coordinates.Index().ToDictionary(tuple => tuple.Item2, tuple => CoordMods.ToMods(tuple.Item1))
        };
    }
    public class LegacyCharaMods
    {
        public ModInfo Figure { get; set; }
        public ModInfo Graphic { get; set; }
        public FaceMods Face { get; set; }
        public BodyMods Body { get; set; }
        public Dictionary<ChaFileDefine.CoordinateType, CoordMods> Coordinates { get; set; }

        public static implicit operator CharaMods(LegacyCharaMods mods) => new()
        {
            Figure = mods.Figure,
            Graphic = mods.Graphic,
            Face = mods.Face,
            Body = mods.Body,
            Coordinates = mods.Coordinates.Defaults()
                .ToDictionary(entry => (int)entry.Key, entry => entry.Value)
        };
    }
}