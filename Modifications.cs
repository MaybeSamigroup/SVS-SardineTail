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

        internal static int GameId(this HumanDataCoordinate data) => data.Tag switch
        {
            "【HCClothes】" => 10,
            "【SVClothes】" => 20,
            "【ACClothes】" => 30,
            _ => 0
        };
        internal static int GameId(this HumanData data) => data.Tag switch
        {
            "【HCChara】" => 10,
            "【SVChara】" => 20,
            "【ACChara】" => 30,
            _ => 0
        };

    }

    public record HairsMods
    {
        public ModInfo HairGloss { get; init; }
        public Dictionary<ChaFileDefine.HairKind, ModInfo> Hairs { get; init; }
        public void Apply(int gameId, HumanDataHair data)
        {
            data.glossId = ModPackage.ToId(gameId, CatNo.mt_hairgloss, HairGloss, data.glossId);
            Hairs.Defaults().ForEach(entry =>
                data.parts[(int)entry.Key].id = data.parts[(int)entry.Key].bundleId =
                    ModPackage.ToId(gameId, ToCategoryNo(entry.Key), entry.Value, data.parts[(int)entry.Key].id)
            );
        }
        public static HairsMods ToMods(HumanDataHair data) => new()
            {
                HairGloss = ModPackage.FromId(CatNo.mt_hairgloss, data.glossId),
                Hairs = Enum.GetValues<ChaFileDefine.HairKind>()
                    .Where(part => data.parts[(int)part] is not null)
                    .SelectMany(part => part.ToEntry(ModPackage.FromId(ToCategoryNo(part), data.parts[(int)part].id)))
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
        internal void Apply(ChaFileDefine.ClothesKind part, int gameId, HumanDataClothes.PartsInfo data)
        {
            data.id = ModPackage.ToId(gameId, ToCategoryNo(part), Part, data.id);
            Paints.Defaults(data.paintInfos.Count).ForEach(entry =>
                data.paintInfos[entry.Key].ID = ModPackage.ToId(gameId, CatNo.mt_body_paint, entry.Value, data.paintInfos[entry.Key].ID));
            Patterns.Defaults(data.colorInfo.Count).ForEach(entry =>
                data.colorInfo[entry.Key].patternInfo.pattern = ModPackage.ToId(gameId, CatNo.mt_pattern, entry.Value, data.colorInfo[entry.Key].patternInfo.pattern));
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
                Part = ModPackage.FromId(ToCategoryNo(part), data.id),
                Paints = data.paintInfos.SelectMany((data, index) => index.ToEntry(ModPackage.FromId(CatNo.mt_body_paint, data.ID))).ToDictionary(),
                Patterns = data.colorInfo.SelectMany((data, index) => index.ToEntry(ModPackage.FromId(CatNo.mt_pattern, data.patternInfo.pattern))).ToDictionary(),
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
        internal void Apply(int gameId, HumanDataAccessory.PartsInfo data)
        {
            data.id = ModPackage.ToId(gameId, (CatNo)data.type, Part, data.id);
            Patterns.Defaults(data.colorInfo.Count).ForEach(entry =>
                data.colorInfo[entry.Key].pattern = ModPackage.ToId(gameId, CatNo.mt_pattern, entry.Value, data.colorInfo[entry.Key].pattern));
        }

        static Tuple<int, AccessoryMods> ToMod(int slot, HumanDataAccessory.PartsInfo data) =>
            new(slot, new AccessoryMods
            {
                Part = ModPackage.FromId((CatNo)data.type, data.id),
                Patterns = data.colorInfo.SelectMany((data, index) => index.ToEntry(ModPackage.FromId(CatNo.mt_pattern, data.pattern))).ToDictionary(),
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

        internal void Apply(int gameId, HumanDataFaceMakeup data)
        {
            data.eyeshadowId = ModPackage.ToId(gameId, CatNo.mt_eyeshadow, Eyeshadow, data.eyeshadowId);
            data.cheekId = ModPackage.ToId(gameId, CatNo.mt_cheek, Cheek, data.cheekId);
            data.lipId = ModPackage.ToId(gameId, CatNo.mt_lip, Lip, data.lipId);
            Paints.Defaults(data.paintInfos.Count).ForEach(entry =>
                data.paintInfos[entry.Key].ID = ModPackage.ToId(gameId, CatNo.mt_face_paint, entry.Value, data.paintInfos[entry.Key].ID));
            Layouts.Defaults(data.paintInfos.Count).ForEach(entry =>
                data.paintInfos[entry.Key].layoutID = ModPackage.ToId(gameId, CatNo.facepaint_layout, entry.Value, data.paintInfos[entry.Key].layoutID));
        }

        internal static FaceMakeupMods ToMod(HumanDataFaceMakeup data) => new()
        {
            Eyeshadow = ModPackage.FromId(CatNo.mt_eyeshadow, data.eyeshadowId),
            Cheek = ModPackage.FromId(CatNo.mt_cheek, data.cheekId),
            Lip = ModPackage.FromId(CatNo.mt_lip, data.lipId),
            Paints = data.paintInfos.SelectMany((data, index) => index.ToEntry(ModPackage.FromId(CatNo.mt_face_paint, data.ID))).ToDictionary(),
            Layouts = data.paintInfos.SelectMany((data, index) => index.ToEntry(ModPackage.FromId(CatNo.facepaint_layout, data.layoutID))).ToDictionary(),
        };
    }

    public record BodyMakeupMods
    {
        public ModInfo Nail { get; init; }
        public ModInfo NailLeg { get; init; }
        public Dictionary<int, ModInfo> Paints { get; init; }
        public Dictionary<int, ModInfo> Layouts { get; init; }

        internal void Apply(int gameId, HumanDataBodyMakeup data)
        {
            data.nailInfo.ID = ModPackage.ToId(gameId, CatNo.bo_nail, Nail, data.nailInfo.ID);
            data.nailLegInfo.ID = ModPackage.ToId(gameId, CatNo.bo_nail, NailLeg, data.nailLegInfo.ID);
            Paints.Defaults(data.paintInfos.Count).ForEach(entry =>
                data.paintInfos[entry.Key].ID = ModPackage.ToId(gameId, CatNo.mt_body_paint, entry.Value, data.paintInfos[entry.Key].ID));
            Layouts.Defaults(data.paintInfos.Count).ForEach(entry =>
                data.paintInfos[entry.Key].layoutID = ModPackage.ToId(gameId, CatNo.bodypaint_layout, entry.Value, data.paintInfos[entry.Key].layoutID));
        }

        internal static BodyMakeupMods ToMod(HumanDataBodyMakeup data) => new()
        {
            Nail = ModPackage.FromId(CatNo.bo_nail, data.nailInfo.ID),
            NailLeg = ModPackage.FromId(CatNo.bo_nail_leg, data.nailLegInfo.ID),
            Paints = data.paintInfos.SelectMany((data, index) => index.ToEntry(ModPackage.FromId(CatNo.mt_body_paint, data.ID))).ToDictionary(),
            Layouts = data.paintInfos.SelectMany((data, index) => index.ToEntry(ModPackage.FromId(CatNo.bodypaint_layout, data.layoutID))).ToDictionary(),
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

        internal void Apply(HumanDataCoordinate data) => Apply(data.GameId(), data);

        internal void Apply(int gameId, HumanDataCoordinate data)
        {
            BodyMakeup.Defaults().Apply(gameId, data.BodyMakeup);
            FaceMakeup.Defaults().Apply(gameId, data.FaceMakeup);
            Hairs.Defaults().Apply(gameId, data.Hair);
            Clothes.Defaults()
                .Where(entry => (int)entry.Key < data.Clothes.parts.Count)
                .ForEach(entry => entry.Value.Apply(entry.Key, gameId, data.Clothes.parts[(int)entry.Key]));
            Accessories.Defaults(data.Accessory.parts.Count)
                .Where(entry => entry.Key < data.Accessory.parts.Count)
                .ForEach(entry => entry.Value.Apply(gameId, data.Accessory.parts[entry.Key]));
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

        internal void Apply(int gameId, HumanDataFace.PupilInfo data)
        {
            data.id = ModPackage.ToId(gameId, CatNo.mt_eye, Eye, data.id);
            data.overId = ModPackage.ToId(gameId, CatNo.mt_eyepipil, Pupil, data.overId);
            data.gradMaskId = ModPackage.ToId(gameId, CatNo.mt_eye_gradation, Gradation, data.gradMaskId);
            Highlights.Defaults(data.highlightInfos.Count)
                .ForEach((key, val) => data.highlightInfos[key].id =
                     ModPackage.ToId(gameId, CatNo.mt_eye_hi_up, val, data.highlightInfos[key].id));
        }

        internal static EyeMods ToMod(HumanDataFace.PupilInfo data) => new()
        {
            Eye = ModPackage.FromId(CatNo.mt_eye, data.id),
            Pupil = ModPackage.FromId(CatNo.mt_eyepipil, data.overId),
            Gradation = ModPackage.FromId(CatNo.mt_eye_gradation, data.gradMaskId),
            Highlights = data.highlightInfos.SelectMany((data, index) => index.ToEntry(ModPackage.FromId(CatNo.mt_eye_hi_up, data.id))).ToDictionary(),
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

        internal void Apply(int gameId, HumanDataFace data)
        {
            data.headId = ModPackage.ToId(gameId, CatNo.bo_head, Head, data.headId);
            data.detailId = ModPackage.ToId(gameId, CatNo.mt_face_detail, Detail, data.detailId);
            data.moleInfo.ID = ModPackage.ToId(gameId, CatNo.mt_mole, Mole, data.moleInfo.ID);
            data.moleInfo.layoutID = ModPackage.ToId(gameId, CatNo.mole_layout, MoleLayout, data.moleInfo.layoutID);
            data.noseId = ModPackage.ToId(gameId, CatNo.mt_nose, Nose, data.noseId);
            data.lipLineId = ModPackage.ToId(gameId, CatNo.mt_lipline, LipLine, data.lipLineId);
            data.eyebrowId = ModPackage.ToId(gameId, CatNo.mt_eyebrow, Eyebrows, data.eyebrowId);
            data.eyelidId = ModPackage.ToId(gameId, CatNo.mt_eyelid, Eyelid, data.eyelidId);
            data.eyelineDownId = ModPackage.ToId(gameId, CatNo.mt_eyeline_down, EyelineDown, data.eyelineDownId);
            data.eyelineUpId = ModPackage.ToId(gameId, CatNo.mt_eyeline_up, EyelineUp, data.eyelineUpId);
            data.whiteId = ModPackage.ToId(gameId, CatNo.mt_eye_white, EyeWhite, data.whiteId);
            Eyes.Defaults(data.pupil.Count).ForEach((index, value) => value.Apply(gameId, data.pupil[index]));
        }

        internal static FaceMods ToMod(HumanDataFace data) => new()
        {
            Head = ModPackage.FromId(CatNo.bo_head, data.headId),
            Detail = ModPackage.FromId(CatNo.mt_face_detail, data.detailId),
            Mole = ModPackage.FromId(CatNo.mt_mole, data.moleInfo.ID),
            MoleLayout = ModPackage.FromId(CatNo.mole_layout, data.moleInfo.layoutID),
            Nose = ModPackage.FromId(CatNo.mt_nose, data.noseId),
            LipLine = ModPackage.FromId(CatNo.mt_lipline, data.lipLineId),
            Eyebrows = ModPackage.FromId(CatNo.mt_eyebrow, data.eyebrowId),
            Eyelid = ModPackage.FromId(CatNo.mt_eyelid, data.eyelidId),
            EyelineDown = ModPackage.FromId(CatNo.mt_eyeline_down, data.eyelineDownId),
            EyelineUp = ModPackage.FromId(CatNo.mt_eyeline_up, data.eyelineUpId),
            EyeWhite = ModPackage.FromId(CatNo.mt_eye_white, data.whiteId),
            Eyes = data.pupil.Select((data, index) => (index, EyeMods.ToMod(data))).ToDictionary()
        };
    }

    public class BodyMods
    {
        public ModInfo Detail { get; set; }
        public ModInfo Sunburn { get; set; }
        public ModInfo Nip { get; set; }
        public ModInfo Underhair { get; set; }

        internal void Apply(int gameId, HumanDataBody data)
        {
            data.detailId = ModPackage.ToId(gameId, CatNo.mt_body_detail, Detail, data.detailId);
            data.sunburnId = ModPackage.ToId(gameId, CatNo.mt_sunburn, Sunburn, data.sunburnId);
            data.nipId = ModPackage.ToId(gameId, CatNo.mt_nip, Nip, data.nipId);
            data.underhairId = ModPackage.ToId(gameId, CatNo.mt_underhair, Underhair, data.underhairId);
        }

        internal static BodyMods ToMod(HumanDataBody data) => new()
        {
            Detail = ModPackage.FromId(CatNo.mt_body_detail, data.detailId),
            Sunburn = ModPackage.FromId(CatNo.mt_sunburn, data.sunburnId),
            Nip = ModPackage.FromId(CatNo.mt_nip, data.nipId),
            Underhair = ModPackage.FromId(CatNo.mt_underhair, data.underhairId)
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
        public CharaMods Merge(CharaLimit limit, CharaMods mods) => new()
        {
            Figure = (limit & CharaLimit.Body) is CharaLimit.None ? Figure : mods.Figure,
            Body = (limit & CharaLimit.Body) is CharaLimit.None ? Body : mods.Body,
            Face = (limit & CharaLimit.Face) is CharaLimit.None ? Face : mods.Face,
            Graphic = (limit & CharaLimit.Graphic) is CharaLimit.None ? Graphic : mods.Graphic,
            Coordinates = (limit & CharaLimit.Coorde) is CharaLimit.None ? Coordinates : mods.Coordinates,
        };
        public CharaMods Convert(HumanData data) => this with
        {
            Body = BodyMods.ToMod(data.Custom.Body),
            Face = FaceMods.ToMod(data.Custom.Face),
            Graphic = ModPackage.FromId(CatNo.mt_ramp, data.Graphic.RampID),
            Coordinates = data.Coordinates.Index().ToDictionary(tuple => tuple.Item2, tuple => CoordMods.ToMods(tuple.Item1))
        };

        public CoordMods Get(int coordinateType) =>
            Coordinates.Defaults().GetValueOrDefault(coordinateType, new ());

        public CharaMods Merge(int coordinateType, CoordMods mods) => new()
        {
            Figure = Figure,
            Body = Body,
            Face = Face,
            Graphic = Graphic,
            Coordinates = Coordinates.Merge(coordinateType, mods)
        };

        internal void Apply(HumanData data)
        {
            data.Graphic.RampID = ModPackage.ToId(data.GameId(), CatNo.mt_ramp, Graphic, data.Graphic.RampID);
            Body.Defaults().Apply(data.GameId(), data.Custom.Body);
            Face.Defaults().Apply(data.GameId(), data.Custom.Face);
            Coordinates.Defaults(data.Coordinates.Count)
                .ForEach((index, value) => value.Apply(data.GameId(), data.Coordinates[index]));
        }

        internal static CharaMods Store(Human human, int figureId) =>
            Extension<CharaMods, CoordMods>.Humans[human] =
                Extension<CharaMods, CoordMods>.Humans[human] with {
                    Figure = ModPackage.FromId(CatNo.bo_body, figureId)
                };

        internal static void Store(Human human) =>
            Extension<CharaMods, CoordMods>.Humans[human] = ToMods(human, Extension<CharaMods, CoordMods>.Humans[human]);

        static CharaMods ToMods(Human human, CharaMods mods) => new CharaMods()
        {
            Figure = mods.Figure,
            Body = BodyMods.ToMod(human.data.Custom.Body),
            Face = FaceMods.ToMod(human.data.Custom.Face),
            Graphic = ModPackage.FromId(CatNo.mt_ramp, human.data.Graphic.RampID),
            Coordinates = human.data.Coordinates.Index()
                .ToDictionary(tuple => tuple.Item2, tuple => CoordMods.ToMods(tuple.Item1))
        };
        internal int FigureId(Human human) => ModPackage.ToId(human.data.GameId(), CatNo.bo_body, Figure, -1);
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