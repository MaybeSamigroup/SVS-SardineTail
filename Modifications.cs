using System;
using System.Collections.Generic;
using System.Linq;
using Character;
using Fishbone;
using CoastalSmell;
using CatNo = ChaListDefine.CategoryNo;
using CharaLimit = Character.HumanData.LoadLimited.Flags;
using CoordLimit = Character.HumanDataCoordinate.LoadLimited.Flags;

namespace SardineTail
{
    file static partial class ModInfoExtension
    {
        internal static Dictionary<K, V> Defaults<K, V>(this Dictionary<K, V> values) where K : struct, Enum where V : new() =>
            (values?.Count ?? 0) is not 0 ? values : Enum.GetValues<K>().ToDictionary(item => item, item => new V());

        internal static Dictionary<int, V> Defaults<V>(this Dictionary<int, V> values, int count) where V : new() =>
            (values?.Count ?? 0) is not 0 ? values : Enumerable.Range(0, count).ToDictionary(item => item, item => new V());

        internal static T Defaults<T>(this T values) where T : new() =>
            values ?? new T();

        internal static (int, ModInfo)[] ToEntry(this int index, ModInfo mod) => mod == null ? [] : [(index, mod)];

        internal static (T, ModInfo)[] ToEntry<T>(this T index, ModInfo mod) => mod == null ? [] : [(index, mod)];

        internal static int ToId(this GameId gameId, CatNo categoryNo, ModInfo mod, int id) =>
            Packages.TranslateId(gameId, categoryNo, mod, id);

        internal static ModInfo ToMod(this GameId gameId, CatNo categoryNo, int id) =>
            Packages.TryGetValue(gameId, categoryNo, id, out var mod) ? mod : null;

    }

    public record ModInfo
    {
        public const int MIN_ID = 1000_000_000;
        public string PkgId { get; init; }
        public string ModId { get; init; }
        public CatNo Category { get; init; }
        public Version PkgVersion { get; init; }
    }

    public record HairsMods
    {
        public ModInfo HairGloss { get; init; }
        public Dictionary<ChaFileDefine.HairKind, ModInfo> Hairs { get; init; }
        public void Apply(GameId gameId, HumanDataHair data)
        {
            data.glossId = gameId.ToId(CatNo.mt_hairgloss, HairGloss, data.glossId);
            Hairs.Defaults().ForEach(entry =>
                data.parts[(int)entry.Key].id = data.parts[(int)entry.Key].bundleId =
                    gameId.ToId(entry.Key.ToCategoryNo(), entry.Value, data.parts[(int)entry.Key].id)
            );
        }
        public static HairsMods Store(GameId gameId, HumanDataHair data) => new()
        {
            HairGloss = gameId.ToMod(CatNo.mt_hairgloss, data.glossId),
            Hairs = Enum.GetValues<ChaFileDefine.HairKind>()
                    .Where(part => data.parts[(int)part] is not null)
                    .SelectMany(part => part.ToEntry(gameId.ToMod(part.ToCategoryNo(), data.parts[(int)part].id)))
                    .ToDictionary()
        };
    }

    public record ClothMods
    {
        public ModInfo Part { get; init; }
        public Dictionary<int, ModInfo> Paints { get; init; }
        public Dictionary<int, ModInfo> Patterns { get; init; }
        internal void Apply(ChaFileDefine.ClothesKind part, GameId gameId, HumanDataClothes.PartsInfo data)
        {
            data.id = gameId.ToId(part.ToCategoryNo(), Part, data.id);
            Paints.Defaults(data.paintInfos.Count).ForEach(entry =>
                data.paintInfos[entry.Key].ID = gameId.ToId(CatNo.mt_body_paint, entry.Value, data.paintInfos[entry.Key].ID));
            Patterns.Defaults(data.colorInfo.Count).ForEach(entry =>
                data.colorInfo[entry.Key].patternInfo.pattern = gameId.ToId(CatNo.mt_pattern, entry.Value, data.colorInfo[entry.Key].patternInfo.pattern));
        }

        static Tuple<ChaFileDefine.ClothesKind, ClothMods> Store(GameId gameId, ChaFileDefine.ClothesKind part, HumanDataClothes.PartsInfo data) =>
            new(part, new ClothMods
            {
                Part = gameId.ToMod(part.ToCategoryNo(), data.id),
                Paints = data.paintInfos.SelectMany((data, index) =>
                    index.ToEntry(gameId.ToMod(CatNo.mt_body_paint, data.ID))).ToDictionary(),
                Patterns = data.colorInfo.SelectMany((data, index) =>
                    index.ToEntry(gameId.ToMod(CatNo.mt_pattern, data.patternInfo.pattern))).ToDictionary(),
            });

        internal static Dictionary<ChaFileDefine.ClothesKind, ClothMods> Store(GameId gameId, HumanDataClothes data) =>
            Enum.GetValues<ChaFileDefine.ClothesKind>()
                .Where(part => data.parts[(int)part] is not null)
                .Select(part => Store(gameId, part, data.parts[(int)part]))
                .ToDictionary();
    }

    public record AccessoryMods
    {
        public ModInfo Part { get; init; }
        public Dictionary<int, ModInfo> Patterns { get; init; }
        internal void Apply(GameId gameId, HumanDataAccessory.PartsInfo data)
        {
            data.id = gameId.ToId((CatNo)data.type, Part, data.id);
            Patterns.Defaults(data.colorInfo.Count).ForEach(entry =>
                data.colorInfo[entry.Key].pattern = gameId.ToId(CatNo.mt_pattern, entry.Value, data.colorInfo[entry.Key].pattern));
        }

        static Tuple<int, AccessoryMods> Store(GameId gameId, int slot, HumanDataAccessory.PartsInfo data) =>
            new(slot, new AccessoryMods
            {
                Part = gameId.ToMod((CatNo)data.type, data.id),
                Patterns = data.colorInfo.SelectMany((data, index) =>
                    index.ToEntry(gameId.ToMod(CatNo.mt_pattern, data.pattern))).ToDictionary(),
            });

        internal static Dictionary<int, AccessoryMods> Store(GameId gameId, HumanDataAccessory data) =>
            Enumerable.Range(0, data.parts.Count)
                .Where(slot => data.parts[slot] is not null)
                .Select(slot => Store(gameId, slot, data.parts[slot]))
                .ToDictionary();
    }

    public record FaceMakeupMods
    {
        public ModInfo Eyeshadow { get; init; }
        public ModInfo Cheek { get; init; }
        public ModInfo Lip { get; init; }
        public Dictionary<int, ModInfo> Paints { get; init; }
        public Dictionary<int, ModInfo> Layouts { get; init; }

        internal void Apply(GameId gameId, HumanDataFaceMakeup data)
        {
            data.eyeshadowId = gameId.ToId(CatNo.mt_eyeshadow, Eyeshadow, data.eyeshadowId);
            data.cheekId = gameId.ToId(CatNo.mt_cheek, Cheek, data.cheekId);
            data.lipId = gameId.ToId(CatNo.mt_lip, Lip, data.lipId);
            Paints.Defaults(data.paintInfos.Count).ForEach(entry =>
                data.paintInfos[entry.Key].ID = gameId.ToId(CatNo.mt_face_paint, entry.Value, data.paintInfos[entry.Key].ID));
            Layouts.Defaults(data.paintInfos.Count).ForEach(entry =>
                data.paintInfos[entry.Key].layoutID = gameId.ToId(CatNo.facepaint_layout, entry.Value, data.paintInfos[entry.Key].layoutID));
        }

        internal static FaceMakeupMods Store(GameId gameId, HumanDataFaceMakeup data) => new()
        {
            Eyeshadow = gameId.ToMod(CatNo.mt_eyeshadow, data.eyeshadowId),
            Cheek = gameId.ToMod(CatNo.mt_cheek, data.cheekId),
            Lip = gameId.ToMod(CatNo.mt_lip, data.lipId),
            Paints = data.paintInfos.SelectMany((data, index) =>
                index.ToEntry(gameId.ToMod(CatNo.mt_face_paint, data.ID))).ToDictionary(),
            Layouts = data.paintInfos.SelectMany((data, index) =>
                index.ToEntry(gameId.ToMod(CatNo.facepaint_layout, data.layoutID))).ToDictionary(),
        };
    }

    public record BodyMakeupMods
    {
        public ModInfo Nail { get; init; }
        public ModInfo NailLeg { get; init; }
        public Dictionary<int, ModInfo> Paints { get; init; }
        public Dictionary<int, ModInfo> Layouts { get; init; }
        internal void Apply(GameId gameId, HumanDataBodyMakeup data)
        {
            data.nailInfo.ID = gameId.ToId(CatNo.bo_nail, Nail, data.nailInfo.ID);
            data.nailLegInfo.ID = gameId.ToId(CatNo.bo_nail, NailLeg, data.nailLegInfo.ID);
            Paints.Defaults(data.paintInfos.Count).ForEach(entry =>
                data.paintInfos[entry.Key].ID = gameId.ToId(CatNo.mt_body_paint, entry.Value, data.paintInfos[entry.Key].ID));
            Layouts.Defaults(data.paintInfos.Count).ForEach(entry =>
                data.paintInfos[entry.Key].layoutID = gameId.ToId(CatNo.bodypaint_layout, entry.Value, data.paintInfos[entry.Key].layoutID));
        }

        internal static BodyMakeupMods Store(GameId gameId, HumanDataBodyMakeup data) => new()
        {
            Nail = gameId.ToMod(CatNo.bo_nail, data.nailInfo.ID),
            NailLeg = gameId.ToMod(CatNo.bo_nail_leg, data.nailLegInfo.ID),
            Paints = data.paintInfos.SelectMany((data, index) =>
                index.ToEntry(gameId.ToMod(CatNo.mt_body_paint, data.ID))).ToDictionary(),
            Layouts = data.paintInfos.SelectMany((data, index) =>
                index.ToEntry(gameId.ToMod(CatNo.bodypaint_layout, data.layoutID))).ToDictionary(),
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
        public CoordMods Convert(HumanDataCoordinate data) => Store(data.ToGameId(), data);

        internal void Apply(HumanDataCoordinate data) => Apply(data.ToGameId(), data);

        internal void Apply(GameId gameId, HumanDataCoordinate data)
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
            Extension<CharaMods, CoordMods>.Humans.NowCoordinate[human] =
                Store(human.coorde.Now.ToGameId(), human.coorde.Now);

        internal static CoordMods Store(GameId gameId, HumanDataCoordinate data) => new()
        {
            BodyMakeup = BodyMakeupMods.Store(gameId, data.BodyMakeup),
            FaceMakeup = FaceMakeupMods.Store(gameId, data.FaceMakeup),
            Hairs = HairsMods.Store(gameId, data.Hair),
            Clothes = ClothMods.Store(gameId, data.Clothes),
            Accessories = AccessoryMods.Store(gameId, data.Accessory)
        };
    }

    public record EyeMods
    {
        public ModInfo Eye { get; init; }
        public ModInfo Pupil { get; init; }
        public ModInfo Gradation { get; init; }
        public Dictionary<int, ModInfo> Highlights { get; init; }

        internal void Apply(GameId gameId, HumanDataFace.PupilInfo data)
        {
            data.id = gameId.ToId(CatNo.mt_eye, Eye, data.id);
            data.overId = gameId.ToId(CatNo.mt_eyepipil, Pupil, data.overId);
            data.gradMaskId = gameId.ToId(CatNo.mt_eye_gradation, Gradation, data.gradMaskId);
            Highlights.Defaults(data.highlightInfos.Count)
                .ForEach((key, val) => data.highlightInfos[key].id =
                    gameId.ToId(CatNo.mt_eye_hi_up, val, data.highlightInfos[key].id));
        }

        internal static EyeMods Store(GameId gameId, HumanDataFace.PupilInfo data) => new()
        {
            Eye = gameId.ToMod(CatNo.mt_eye, data.id),
            Pupil = gameId.ToMod(CatNo.mt_eyepipil, data.overId),
            Gradation = gameId.ToMod(CatNo.mt_eye_gradation, data.gradMaskId),
            Highlights = data.highlightInfos.SelectMany((data, index) =>
                index.ToEntry(gameId.ToMod(CatNo.mt_eye_hi_up, data.id))).ToDictionary(),
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

        internal void Apply(GameId gameId, HumanDataFace data)
        {
            data.headId = gameId.ToId(CatNo.bo_head, Head, data.headId);
            data.detailId = gameId.ToId(CatNo.mt_face_detail, Detail, data.detailId);
            data.moleInfo.ID = gameId.ToId(CatNo.mt_mole, Mole, data.moleInfo.ID);
            data.moleInfo.layoutID = gameId.ToId(CatNo.mole_layout, MoleLayout, data.moleInfo.layoutID);
            data.noseId = gameId.ToId(CatNo.mt_nose, Nose, data.noseId);
            data.lipLineId = gameId.ToId(CatNo.mt_lipline, LipLine, data.lipLineId);
            data.eyebrowId = gameId.ToId(CatNo.mt_eyebrow, Eyebrows, data.eyebrowId);
            data.eyelidId = gameId.ToId(CatNo.mt_eyelid, Eyelid, data.eyelidId);
            data.eyelineDownId = gameId.ToId(CatNo.mt_eyeline_down, EyelineDown, data.eyelineDownId);
            data.eyelineUpId = gameId.ToId(CatNo.mt_eyeline_up, EyelineUp, data.eyelineUpId);
            data.whiteId = gameId.ToId(CatNo.mt_eye_white, EyeWhite, data.whiteId);
            Eyes.Defaults(data.pupil.Count).ForEach((index, value) => value.Apply(gameId, data.pupil[index]));
        }

        internal static FaceMods Store(GameId gameId, HumanDataFace data) => new()
        {
            Head = gameId.ToMod(CatNo.bo_head, data.headId),
            Detail = gameId.ToMod(CatNo.mt_face_detail, data.detailId),
            Mole = gameId.ToMod(CatNo.mt_mole, data.moleInfo.ID),
            MoleLayout = gameId.ToMod(CatNo.mole_layout, data.moleInfo.layoutID),
            Nose = gameId.ToMod(CatNo.mt_nose, data.noseId),
            LipLine = gameId.ToMod(CatNo.mt_lipline, data.lipLineId),
            Eyebrows = gameId.ToMod(CatNo.mt_eyebrow, data.eyebrowId),
            Eyelid = gameId.ToMod(CatNo.mt_eyelid, data.eyelidId),
            EyelineDown = gameId.ToMod(CatNo.mt_eyeline_down, data.eyelineDownId),
            EyelineUp = gameId.ToMod(CatNo.mt_eyeline_up, data.eyelineUpId),
            EyeWhite = gameId.ToMod(CatNo.mt_eye_white, data.whiteId),
            Eyes = data.pupil.Select((data, index) => (index, EyeMods.Store(gameId, data))).ToDictionary()
        };
    }

    public class BodyMods
    {
        public ModInfo Detail { get; set; }
        public ModInfo Sunburn { get; set; }
        public ModInfo Nip { get; set; }
        public ModInfo Underhair { get; set; }

        internal void Apply(GameId gameId, HumanDataBody data)
        {
            data.detailId = gameId.ToId(CatNo.mt_body_detail, Detail, data.detailId);
            data.sunburnId = gameId.ToId(CatNo.mt_sunburn, Sunburn, data.sunburnId);
            data.nipId = gameId.ToId(CatNo.mt_nip, Nip, data.nipId);
            data.underhairId = gameId.ToId(CatNo.mt_underhair, Underhair, data.underhairId);
        }

        internal static BodyMods Store(GameId gameId, HumanDataBody data) => new()
        {
            Detail = gameId.ToMod(CatNo.mt_body_detail, data.detailId),
            Sunburn = gameId.ToMod(CatNo.mt_sunburn, data.sunburnId),
            Nip = gameId.ToMod(CatNo.mt_nip, data.nipId),
            Underhair = gameId.ToMod(CatNo.mt_underhair, data.underhairId)
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

        public CharaMods Convert(HumanData data) => Convert(data.ToGameId(), data);

        CharaMods Convert(GameId gameId, HumanData data) => this with
        {
            Body = BodyMods.Store(gameId, data.Custom.Body),
            Face = FaceMods.Store(gameId, data.Custom.Face),
            Graphic = gameId.ToMod(CatNo.mt_ramp, data.Graphic.RampID),
            Coordinates = data.Coordinates.Index().ToDictionary(tuple => tuple.Item2, tuple => CoordMods.Store(gameId, tuple.Item1))
        };


        public CoordMods Get(int coordinateType) =>
            Coordinates.Defaults().GetValueOrDefault(coordinateType, new());

        public CharaMods Merge(int coordinateType, CoordMods mods) => new()
        {
            Figure = Figure,
            Body = Body,
            Face = Face,
            Graphic = Graphic,
            Coordinates = Coordinates.Merge(coordinateType, mods)
        };

        internal void Apply(HumanData data) => Apply(data.ToGameId(), data);
        void Apply(GameId gameId, HumanData data)
        {
            data.Graphic.RampID = gameId.ToId(CatNo.mt_ramp, Graphic, data.Graphic.RampID);
            Body.Defaults().Apply(gameId, data.Custom.Body);
            Face.Defaults().Apply(gameId, data.Custom.Face);
            Coordinates.Defaults(data.Coordinates.Count)
                .ForEach((index, value) => value.Apply(gameId, data.Coordinates[index]));
        }

        internal static CharaMods Store(Human human, int figureId) =>
            Extension<CharaMods, CoordMods>.Humans[human] =
                Extension<CharaMods, CoordMods>.Humans[human] with
                {
                    Figure = human.data.ToGameId().ToMod(CatNo.bo_body, figureId)
                };

        internal static void Store(Human human) =>
            Extension<CharaMods, CoordMods>.Humans[human] = Store(human.data.ToGameId(), human, Extension<CharaMods, CoordMods>.Humans[human]);

        static CharaMods Store(GameId gameId, Human human, CharaMods mods) => new CharaMods()
        {
            Figure = mods.Figure,
            Body = BodyMods.Store(gameId, human.data.Custom.Body),
            Face = FaceMods.Store(gameId, human.data.Custom.Face),
            Graphic = gameId.ToMod(CatNo.mt_ramp, human.data.Graphic.RampID),
            Coordinates = human.data.Coordinates.Index()
                .ToDictionary(tuple => tuple.Item2, tuple => CoordMods.Store(gameId, tuple.Item1))
        };
        internal int FigureId(Human human) => human.data.ToGameId().ToId(CatNo.bo_body, Figure, -1);
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