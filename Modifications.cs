using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Character;
using CatNo = ChaListDefine.CategoryNo;
using CharaLimit = Character.HumanData.LoadLimited.Flags;
using CoordLimit = Character.HumanDataCoordinate.LoadLimited.Flags;
using UniRx;
using Fishbone;
using CoastalSmell;

namespace SardineTail
{
    internal abstract partial class ModPackage
    {
        protected static Func<string[], IEnumerable<Version>> ToVersion =>
            items => items.Length switch
            {
                0 => [],
                1 => [],
                _ => Version.TryParse(items[^1], out var version) ? [version] : [],
            };
        static Dictionary<string, ModPackage> Packages = new();
        static Dictionary<CatNo, Dictionary<int, ModInfo>> IdToMod = new();
        static Dictionary<CatNo, Dictionary<int, ModInfo>> HardMigrations = new();
        static Action<CatNo, int, ModInfo> RegisterIdToMod =
            (categoryNo, id, mod) => (IdToMod[categoryNo] = IdToMod.TryGetValue(categoryNo, out var mods) ? mods : new()).Add(id, mod);
        static Action<CatNo, int, ModInfo> RegisterHardMigration =
            (categoryNo, id, info) => (HardMigrations.GetValueOrDefault(categoryNo) ?? (HardMigrations[categoryNo] = new())).TryAdd(id, info);
    }
    internal partial class DirectoryPackage : ModPackage
    {
        static Func<string, string, string> ToPkgId =
            (root, path) => string.Join('-', Path.GetRelativePath(root, path).Split('-')[0..^1]);
        static Func<string, string, Version, DirectoryPackage> ToPackage =
            (root, path, version) => new DirectoryPackage(ToPkgId(root, path), version, path);
        static Func<string, string, IEnumerable<ModPackage>> ToPackages =
            (root, path) => ToVersion(Path.GetRelativePath(root, path).Split('-')).Select(ToPackage.Apply(root).Apply(path));
        internal static Func<string, IEnumerable<ModPackage>> Collect =
            path => Plugin.DevelopmentMode.Value ? Directory.GetDirectories(path).SelectMany(ToPackages.Apply(path)) : [];
    }
    internal partial class ArchivePackage : ModPackage
    {
        static Func<string, bool> IsPackage =>
            path => ".stp".Equals(Path.GetExtension(path), StringComparison.OrdinalIgnoreCase);
        static Func<string, string> ToPkgId =>
            (path) => string.Join('-', Path.GetFileName(path).Split('-')[0..^1]);
        static Func<string, Version, ArchivePackage> ToPackage =
            (path, version) => new ArchivePackage(ToPkgId(path), version, path);
        static Func<string, IEnumerable<ModPackage>> ToPackages =
            path => ToVersion(Path.GetFileNameWithoutExtension(path).Split('-')).Select(ToPackage.Apply(path));
        internal static Func<string, IEnumerable<ModPackage>> Collect =
            path => Directory.GetFiles(path).Where(IsPackage).SelectMany(ToPackages).Concat(Directory.GetDirectories(path).SelectMany(Collect));
    }
    public partial class HairsMods
    {
        static Action<ChaFileDefine.HairKind, ModInfo> ApplyParts(HumanDataHair data) =>
            (index, mod) => data.parts[(int)index].id =
                ModInfo.Translate[ToCategoryNo(index)].ToId(mod, data.parts[(int)index].id);
        internal partial void Apply(HumanDataHair data)
        {
            data.glossId =
                ModInfo.Translate[CatNo.mt_hairgloss].ToId(HairGloss, data.glossId);
            (Hairs ?? [])
                .ForEach(ApplyParts(data));
        }
        static CatNo ToCategoryNo(ChaFileDefine.HairKind value) =>
             value switch
             {
                 ChaFileDefine.HairKind.back => CatNo.bo_hair_b,
                 ChaFileDefine.HairKind.front => CatNo.bo_hair_f,
                 ChaFileDefine.HairKind.side => CatNo.bo_hair_s,
                 ChaFileDefine.HairKind.option => CatNo.bo_hair_o,
                 _ => throw new ArgumentException()
             };
        static Tuple<ChaFileDefine.HairKind, ModInfo> FromParts(ChaFileDefine.HairKind part, int id) =>
            new(part, ModInfo.Translate[ToCategoryNo(part)].FromId(id));
        static Tuple<ChaFileDefine.HairKind, ModInfo> FromParts(HumanDataHair.PartsInfo val, int idx) =>
            FromParts((ChaFileDefine.HairKind)idx, val.id);
        static HairsMods()
        {
            ToMods = value => new HairsMods()
            {
                HairGloss = ModInfo.Translate[CatNo.mt_hairgloss].FromId(value.glossId),
                Hairs = value.parts.Select(FromParts).Where(item => item.Item2 != null).ToDictionary()
            };
        }
    }
    public partial class ClothMods
    {
        static Action<int, ModInfo> ApplyPaint(HumanDataClothes.PartsInfo data) =>
            (index, mod) => data.paintInfos[index].ID =
                ModInfo.Translate[CatNo.mt_body_paint].ToId(mod, data.paintInfos[index].ID);
        static Action<int, ModInfo> ApplyPattern(HumanDataClothes.PartsInfo data) =>
            (index, mod) => data.colorInfo[index].patternInfo.pattern =
                ModInfo.Translate[CatNo.mt_pattern].ToId(mod, data.colorInfo[index].patternInfo.pattern);
        internal partial void Apply(ChaFileDefine.ClothesKind part, HumanDataClothes.PartsInfo data) {
            data.id =
                ModInfo.Translate[ToCategoryNo(part)].ToId(Part, data.id);
            (Paints ?? [])
                .ForEach(ApplyPaint(data));
            (Patterns ?? [])
                .ForEach(ApplyPattern(data));
        }
        static CatNo ToCategoryNo(ChaFileDefine.ClothesKind value) =>
            value switch
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
        static Tuple<int, ModInfo> FromPaint(HumanDataPaintInfo data, int index) =>
            new(index, ModInfo.Translate[CatNo.mt_body_paint].FromId(data.ID));
        static Tuple<int, ModInfo> FromPattern(HumanDataClothes.PartsInfo.ColorInfo data, int index) =>
            new(index, ModInfo.Translate[CatNo.mt_pattern].FromId(data.patternInfo.pattern));
        static Tuple<ChaFileDefine.ClothesKind, ClothMods> FromParts(ChaFileDefine.ClothesKind part, HumanDataClothes.PartsInfo data) =>
            new(part, new ClothMods()
            {
                Part =
                    ModInfo.Translate[ToCategoryNo(part)].FromId(data.id),
                Paints =
                    data.paintInfos.Select(FromPaint).Where(item => item.Item2 != null).ToDictionary(),
                Patterns =
                    data.colorInfo.Select(FromPattern).Where(item => item.Item2 != null).ToDictionary(),
            });
        static Tuple<ChaFileDefine.ClothesKind, ClothMods> FromParts(HumanDataClothes.PartsInfo val, int idx) =>
            FromParts((ChaFileDefine.ClothesKind)idx, val);
        static ClothMods()
        {
            ToMods = value => value.parts.Select(FromParts).ToDictionary();
        }
    }
    public partial class AccessoryMods
    {
        static Action<int, ModInfo> ApplyPattern(HumanDataAccessory.PartsInfo data) =>
            (index, mod) => data.colorInfo[index].pattern =
                ModInfo.Translate[CatNo.mt_pattern].ToId(mod, data.colorInfo[index].pattern);
        internal partial void Apply(HumanDataAccessory.PartsInfo data)
        {
            data.id =
                ModInfo.Translate[(CatNo)data.type].ToId(Part, data.id);
            (Patterns ?? [])
                .ForEach(ApplyPattern(data));
        }
        static Tuple<int, ModInfo> FromPattern(HumanDataAccessory.PartsInfo.ColorInfo data, int index) =>
            new(index, ModInfo.Translate[CatNo.mt_pattern].FromId(data.pattern));
        static Tuple<int, AccessoryMods> FromPartsInfo(HumanDataAccessory.PartsInfo data, int slot) =>
            new(slot, new AccessoryMods()
            {
                Part =
                    ModInfo.Translate[(CatNo)data.type].FromId(data.id),
                Patterns =
                    data.colorInfo.Select(FromPattern).Where(item => item.Item2 != null).ToDictionary(),
            });
        static AccessoryMods()
        {
            ToMods = value => value.parts.Select(FromPartsInfo).ToDictionary();
        }
    }
    public partial class FaceMakeupMods
    {
        static Action<int, ModInfo> ApplyPaint(HumanDataFaceMakeup data) =>
            (index, mod) => data.paintInfos[index].ID =
                ModInfo.Translate[CatNo.mt_face_paint].ToId(mod, data.paintInfos[index].ID);
        static Action<int, ModInfo> ApplyPaintLayout(HumanDataFaceMakeup data) =>
            (index, mod) => data.paintInfos[index].layoutID =
                ModInfo.Translate[CatNo.facepaint_layout].ToId(mod, data.paintInfos[index].layoutID);
        internal partial void Apply(HumanDataFaceMakeup data)
        {
            data.eyeshadowId =
                ModInfo.Translate[CatNo.mt_eyeshadow].ToId(Eyeshadow, data.eyeshadowId);
            data.cheekId =
                ModInfo.Translate[CatNo.mt_cheek].ToId(Cheek, data.cheekId);
            data.lipId =
                ModInfo.Translate[CatNo.mt_lip].ToId(Lip, data.lipId);
            (Paints ?? [])
                .ForEach(ApplyPaint(data));
            (Layouts ?? [])
                .ForEach(ApplyPaintLayout(data));
        }
        static Tuple<int, ModInfo> FromPaint(HumanDataPresetPaintInfo data, int index) =>
            new(index, ModInfo.Translate[CatNo.mt_face_paint].FromId(data.ID));
        static Tuple<int, ModInfo> FromPaintLayout(HumanDataPresetPaintInfo data, int index) =>
            new(index, ModInfo.Translate[CatNo.facepaint_layout].FromId(data.layoutID));
        static FaceMakeupMods()
        {
            ToMods = value => new FaceMakeupMods()
            {
                Eyeshadow =
                    ModInfo.Translate[CatNo.mt_eyeshadow].FromId(value.eyeshadowId),
                Cheek =
                    ModInfo.Translate[CatNo.mt_cheek].FromId(value.cheekId),
                Lip =
                    ModInfo.Translate[CatNo.mt_lip].FromId(value.lipId),
                Paints =
                    value.paintInfos.Select(FromPaint).Where(item => item.Item2 != null).ToDictionary(),
                Layouts =
                    value.paintInfos.Select(FromPaintLayout).Where(item => item.Item2 != null).ToDictionary(),
            };
        }
    }
    public partial class BodyMakeupMods
    {
        static Action<int, ModInfo> ApplyPaint(HumanDataBodyMakeup data) =>
            (index, mod) => data.paintInfos[index].ID =
                ModInfo.Translate[CatNo.mt_body_paint].ToId(mod, data.paintInfos[index].ID);
        static Action<int, ModInfo> ApplyPaintLayout(HumanDataBodyMakeup data) =>
            (index, mod) => data.paintInfos[index].layoutID =
                ModInfo.Translate[CatNo.bodypaint_layout].ToId(mod, data.paintInfos[index].layoutID);
        internal partial void Apply(HumanDataBodyMakeup data)
        {
            data.nailInfo.ID =
                ModInfo.Translate[CatNo.bo_nail].ToId(Nail, data.nailInfo.ID);
            data.nailLegInfo.ID =
                ModInfo.Translate[CatNo.bo_nail].ToId(NailLeg, data.nailLegInfo.ID);
            (Paints ?? [])
                .ForEach(ApplyPaint(data));
            (Layouts ?? [])
                .ForEach(ApplyPaintLayout(data));
        }
        static Tuple<int, ModInfo> FromPaint(HumanDataPresetPaintInfo data, int index) =>
            new(index, ModInfo.Translate[CatNo.mt_body_paint].FromId(data.ID));
        static Tuple<int, ModInfo> FromPaintLayout(HumanDataPresetPaintInfo data, int index) =>
            new(index, ModInfo.Translate[CatNo.bodypaint_layout].FromId(data.layoutID));
        static BodyMakeupMods()
        {
            ToMods = value => new BodyMakeupMods()
            {
                Nail =
                    ModInfo.Translate[CatNo.bo_nail].FromId(value.nailInfo.ID),
                NailLeg =
                    ModInfo.Translate[CatNo.bo_nail_leg].FromId(value.nailLegInfo.ID),
                Paints =
                    value.paintInfos.Select(FromPaint).Where(item => item.Item2 != null).ToDictionary(),
                Layouts =
                    value.paintInfos.Select(FromPaintLayout).Where(item => item.Item2 != null).ToDictionary(),
            };
        }
    }
    public partial class CoordMods
    {
        Action<ChaFileDefine.ClothesKind, ClothMods> Apply(HumanDataClothes data) =>
            (part, mod) => mod.Apply(part, data.parts[(int)part]);
        Action<int, AccessoryMods> Apply(HumanDataAccessory data) =>
            (index, mod) => mod.Apply(data.parts[index]);
        internal partial Action<HumanDataCoordinate> Apply(CoordLimit limits) =>
            data =>
            {
                ((limits & CoordLimit.BodyMakeup) is not CoordLimit.None)
                    .Maybe(F.Apply((BodyMakeup ?? new()).Apply, data.BodyMakeup));
                ((limits & CoordLimit.FaceMakeup) is not CoordLimit.None)
                    .Maybe(F.Apply((FaceMakeup ?? new()).Apply, data.FaceMakeup));
                ((limits & CoordLimit.Accessory) is not CoordLimit.None)
                    .Maybe(F.Apply((Accessories ?? new()).ForEach, Apply(data.Accessory)));
                ((limits & CoordLimit.Clothes) is not CoordLimit.None)
                    .Maybe(F.Apply((Clothes ?? new ()).ForEach, Apply(data.Clothes)));
                ((limits & CoordLimit.Hair) is not CoordLimit.None)
                    .Maybe(F.Apply((Hairs ?? new ()).Apply, data.Hair));
            };
        internal partial void Apply(HumanDataCoordinate data) =>
            Apply(CoordLimit.All)(data);
        internal partial void Save(ZipArchive archive) =>
            BonesToStuck<CoordMods>.Save(archive, this);
        static CoordMods() {
            ToMods = value => new CoordMods()
            {
                BodyMakeup =
                    BodyMakeupMods.ToMods(value.BodyMakeup),
                FaceMakeup =
                    FaceMakeupMods.ToMods(value.FaceMakeup),
                Accessories =
                    AccessoryMods.ToMods(value.Accessory),
                Clothes =
                    ClothMods.ToMods(value.Clothes),
                Hairs =
                    HairsMods.ToMods(value.Hair)
            };
            Load = archive =>
                BonesToStuck<CoordMods>.Load(archive, out var mods) ? mods :
                BonesToStuck<LegacyCoordMods>.Load(archive, out var legacy) ? legacy : new();
        }
    }
    public partial class EyeMods
    {
        static Action<int, ModInfo> ApplyHighlights(HumanDataFace.PupilInfo data) =>
            (index, mod) => data.highlightInfos[index].id =
                ModInfo.Translate[CatNo.mt_eye_hi_up].ToId(mod, data.highlightInfos[index].id);
        internal partial void Apply(HumanDataFace.PupilInfo data)
        {
            data.id =
                ModInfo.Translate[CatNo.mt_eye].ToId(Eye, data.id);
            data.gradMaskId =
                ModInfo.Translate[CatNo.mt_eye_gradation].ToId(Gradation, data.gradMaskId);
            Highlights
                .ForEach(ApplyHighlights(data));
        }
        static Tuple<int, ModInfo> FromHighlights(HumanDataFace.HighlightInfo data, int index) =>
            new(index, ModInfo.Translate[CatNo.mt_eye_hi_up].FromId(data.id));
        static EyeMods()
        {
            ToMods = value => new EyeMods()
            {
                Eye =
                    ModInfo.Translate[CatNo.mt_eye].FromId(value.id),
                Gradation =
                    ModInfo.Translate[CatNo.mt_eye_gradation].FromId(value.gradMaskId),
                Highlights =
                    value.highlightInfos.Select(FromHighlights).Where(item => item.Item2 != null).ToDictionary(),
            };
        }
    }
    public partial class FaceMods
    {
        static Action<int, EyeMods> ApplyEye(HumanDataFace data) =>
            (index, mod) => mod.Apply(data.pupil[index]);
        internal partial void Apply(HumanDataFace data)
        {
            data.headId =
                ModInfo.Translate[CatNo.bo_head].ToId(Head, data.headId);
            data.detailId =
                ModInfo.Translate[CatNo.mt_face_detail].ToId(Detail, data.detailId);
            data.moleInfo.ID =
                ModInfo.Translate[CatNo.mt_mole].ToId(Mole, data.moleInfo.ID);
            data.moleInfo.layoutID =
                ModInfo.Translate[CatNo.mole_layout].ToId(MoleLayout, data.moleInfo.layoutID);
            data.noseId =
                ModInfo.Translate[CatNo.mt_nose].ToId(Nose, data.noseId);
            data.lipLineId =
                ModInfo.Translate[CatNo.mt_lipline].ToId(LipLine, data.lipLineId);
            data.eyebrowId =
                ModInfo.Translate[CatNo.mt_eyebrow].ToId(Eyebrows, data.eyebrowId);
            data.eyelidId =
                ModInfo.Translate[CatNo.mt_eyelid].ToId(Eyelid, data.eyelidId);
            data.eyelineDownId =
                ModInfo.Translate[CatNo.mt_eyeline_down].ToId(EyelineDown, data.eyelineDownId);
            data.eyelineUpId =
                ModInfo.Translate[CatNo.mt_eyeline_up].ToId(EyelineUp, data.eyelineUpId);
            data.whiteId =
                ModInfo.Translate[CatNo.mt_eye_white].ToId(EyeWhite, data.whiteId);
            (Eyes ?? [])
                .ForEach(ApplyEye(data));
        }
        static Tuple<int, EyeMods> FromEyes(HumanDataFace.PupilInfo data, int index) =>
            new(index, EyeMods.ToMods(data));
        static FaceMods()
        {
            ToMods = value => new FaceMods()
            {
                Head =
                    ModInfo.Translate[CatNo.bo_head].FromId(value.headId),
                Detail =
                    ModInfo.Translate[CatNo.mt_face_detail].FromId(value.detailId),
                Mole =
                    ModInfo.Translate[CatNo.mt_mole].FromId(value.moleInfo.ID),
                MoleLayout =
                    ModInfo.Translate[CatNo.mole_layout].FromId(value.moleInfo.layoutID),
                Nose =
                    ModInfo.Translate[CatNo.mt_nose].FromId(value.noseId),
                LipLine =
                    ModInfo.Translate[CatNo.mt_lipline].FromId(value.lipLineId),
                Eyebrows =
                    ModInfo.Translate[CatNo.mt_eyebrow].FromId(value.eyebrowId),
                Eyelid =
                    ModInfo.Translate[CatNo.mt_eyelid].FromId(value.eyelidId),
                EyelineDown =
                    ModInfo.Translate[CatNo.mt_eyeline_down].FromId(value.eyelineDownId),
                EyelineUp =
                    ModInfo.Translate[CatNo.mt_eyeline_up].FromId(value.eyelineUpId),
                EyeWhite =
                    ModInfo.Translate[CatNo.mt_eye_white].FromId(value.whiteId),
                Eyes =
                    value.pupil.Select(FromEyes).Where(item => item.Item2 != null).ToDictionary()
            };
        }
    }
    public partial class BodyMods
    {
        internal partial void Apply(HumanDataBody data)
        {
            data.detailId =
                ModInfo.Translate[CatNo.mt_body_detail].ToId(Detail, data.detailId);
            data.sunburnId =
                ModInfo.Translate[CatNo.mt_sunburn].ToId(Sunburn, data.sunburnId);
            data.nipId =
                ModInfo.Translate[CatNo.mt_nip].ToId(Nip, data.nipId);
            data.underhairId =
                ModInfo.Translate[CatNo.mt_underhair].ToId(Underhair, data.underhairId);
        }
        static BodyMods()
        {
            ToMods = value => new BodyMods()
            { 
                Detail =
                    ModInfo.Translate[CatNo.mt_body_detail].FromId(value.detailId),
                Sunburn =
                    ModInfo.Translate[CatNo.mt_sunburn].FromId(value.sunburnId),
                Nip =
                    ModInfo.Translate[CatNo.mt_nip].FromId(value.nipId),
                Underhair =
                    ModInfo.Translate[CatNo.mt_underhair].FromId(value.underhairId)
            };
        }
    }
    public partial class CharaMods
    {
        static CoordLimit Translate(CharaLimit limits) =>
            ((limits & CharaLimit.Hair) is CharaLimit.None)
                ? (CoordLimit.BodyMakeup | CoordLimit.FaceMakeup | CoordLimit.Accessory | CoordLimit.Clothes)
                : (CoordLimit.BodyMakeup | CoordLimit.FaceMakeup | CoordLimit.Accessory | CoordLimit.Clothes | CoordLimit.Hair);
        static Action<ChaFileDefine.CoordinateType, CoordMods> ApplyCoord(HumanData data, CoordLimit limits) =>
            (coord, mod) => mod.Apply(limits)(data.Coordinates[(int)coord]);
        internal void ApplyFigure(HumanData data) =>
            Extensions.OverrideBodyId = Extensions.BypassFigure ? Extensions.OverrideBodyId :
                ModInfo.Translate[CatNo.bo_body].ToId(Figure, data.Parameter.sex);
        void ApplyGraphic(HumanDataGraphic data) =>
            data.RampID =
                ModInfo.Translate[CatNo.mt_ramp].ToId(Graphic, data.RampID);
        internal partial void Apply(HumanData data) =>
            Apply(CharaLimit.All)(data);
        internal partial Action<HumanData> Apply(CharaLimit limits) =>
            data =>
            {
                ((limits & CharaLimit.Body) is not CharaLimit.None)
                    .Maybe(F.Apply(ApplyFigure, data) + F.Apply((Body ?? new ()).Apply, data.Custom.Body));
                ((limits & CharaLimit.Face) is not CharaLimit.None)
                    .Maybe(F.Apply((Face ?? new()).Apply, data.Custom.Face));
                ((limits & CharaLimit.Coorde) is not CharaLimit.None)
                    .Maybe(F.Apply((Coordinates ?? new ()).ForEach, ApplyCoord(data, Translate(limits))));
                ((limits & CharaLimit.Graphic) is not CharaLimit.None)
                    .Maybe(F.Apply(ApplyGraphic, data.Graphic));
            };
        internal partial void Save(ZipArchive archive) =>
            BonesToStuck<CharaMods>.Save(archive, this);
        static Tuple<ChaFileDefine.CoordinateType, CoordMods> FromCoordinates(HumanDataCoordinate data, int index) =>
            new((ChaFileDefine.CoordinateType)index, CoordMods.ToMods(data));

        static CharaMods() {
            ToMods = value => new CharaMods()
            {
                Figure = ModInfo.Translate[CatNo.bo_body].FromId(Extensions.OverrideBodyId),
                Face = FaceMods.ToMods(value.Custom.Face),
                Body = BodyMods.ToMods(value.Custom.Body),
                Graphic = ModInfo.Translate[CatNo.mt_ramp].FromId(value.Graphic.RampID),
                Coordinates = value.Coordinates.Select(FromCoordinates).ToDictionary()
            };
            Load = archive =>
                BonesToStuck<CharaMods>.Load(archive, out var mods) ? mods :
                BonesToStuck<LegacyCharaMods>.Load(archive, out var legacy) ? legacy : new();
        }
    }
}