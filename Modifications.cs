using HarmonyLib;
using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using Character;
using CharacterCreation;
using CatNo = ChaListDefine.CategoryNo;
using CharaLimit = Character.HumanData.LoadLimited.Flags;
using CoordLimit = Character.HumanDataCoordinate.LoadLimited.Flags;
using UniRx;
using Fishbone;

namespace SardineTail
{
    public class ModInfo
    {
        public string PkgId { get; set; }
        public string ModId { get; set; }
        public CatNo Category { get; set; }
        public Version PkgVersion { get; set; }
    }
    public class HairsMods
    {
        public ModInfo HairGloss { get; set; }
        public Dictionary<ChaFileDefine.HairKind, ModInfo> Hairs { get; set; }
    }
    public class ClothMods
    {
        public ModInfo Part { get; set; }
        public Dictionary<int, ModInfo> Paints { get; set; }
        public Dictionary<int, ModInfo> Patterns { get; set; }
    }
    public class AccessoryMods
    {
        public ModInfo Part { get; set; }
        public Dictionary<int, ModInfo> Patterns { get; set; }
    }
    public class FaceMakeupMods
    {
        public ModInfo Eyeshadow { get; set; }
        public ModInfo Cheek { get; set; }
        public ModInfo Lip { get; set; }
        public Dictionary<int, ModInfo> Paints { get; set; }
        public Dictionary<int, ModInfo> Layouts { get; set; }
    }
    public class BodyMakeupMods
    {
        public ModInfo Nail { get; set; }
        public ModInfo NailLeg { get; set; }
        public Dictionary<int, ModInfo> Paints { get; set; }
        public Dictionary<int, ModInfo> Layouts { get; set; }
    }
    public class CoordinateMods
    {
        public HairsMods Hairs { get; set; }
        public Dictionary<ChaFileDefine.ClothesKind, ClothMods> Clothes { get; set; }
        public Dictionary<int, AccessoryMods> Accessories { get; set; }
        public BodyMakeupMods BodyMakeup { get; set; }
        public FaceMakeupMods FaceMakeup { get; set; }
    }
    public class EyeMods
    {
        public ModInfo Eye { get; set; }
        public ModInfo Gradation { get; set; }
        public Dictionary<int, ModInfo> Highlights { get; set; }
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
    }
    public class BodyMods
    {
        public ModInfo Detail { get; set; }
        public ModInfo Sunburn { get; set; }
        public ModInfo Nip { get; set; }
        public ModInfo Underhair { get; set; }
    }
    public class CharacterMods
    {
        public FaceMods Face { get; set; }
        public BodyMods Body { get; set; }
        public Dictionary<ChaFileDefine.CoordinateType, CoordinateMods> Coordinates { get; set; }
    }
    internal static class ModificationExtensions
    {
        static HairsMods ToMods(this HumanDataHair value, Func<CatNo, int, ModInfo> translate) =>
            new HairsMods()
            {
                HairGloss = translate(CatNo.mt_hairgloss, value.glossId),
                Hairs = Enum.GetValues<ChaFileDefine.HairKind>()
                    .Select(index => new Tuple<ChaFileDefine.HairKind, ModInfo>(index, translate(index.ToCategoryNo(), value.parts[(int)index].id)))
                    .Where(tuple => tuple.Item2 != null).ToDictionary(tuple => tuple.Item1, pair => pair.Item2)
            };
        static ClothMods ToMods(this ChaFileDefine.ClothesKind id, HumanDataClothes.PartsInfo value, Func<CatNo, int, ModInfo> translate) =>
            new ClothMods()
            {
                Part = translate(id.ToCategoryNo(), value.id),
                Paints = Enumerable.Range(0, value.paintInfos.Count)
                    .Select(index => new Tuple<int, ModInfo>(index, translate(CatNo.mt_body_paint, value.paintInfos[index].ID)))
                    .Where(tuple => tuple.Item2 != null).ToDictionary(tuple => tuple.Item1, pair => pair.Item2),
                Patterns = Enumerable.Range(0, value.colorInfo.Count)
                    .Select(index => new Tuple<int, ModInfo>(index, translate(CatNo.mt_pattern, value.colorInfo[index].patternInfo.pattern)))
                    .Where(tuple => tuple.Item2 != null).ToDictionary(tuple => tuple.Item1, pair => pair.Item2)
            };
        static Dictionary<ChaFileDefine.ClothesKind, ClothMods> ToMods(this HumanDataClothes value, Func<CatNo, int, ModInfo> translate) =>
            Enum.GetValues<ChaFileDefine.ClothesKind>()
                .ToDictionary(index => index, index => index.ToMods(value.parts[(int)index], translate));
        static AccessoryMods ToMods(this HumanDataAccessory.PartsInfo value, Func<CatNo, int, ModInfo> translate) =>
            new AccessoryMods()
            {
                Part = translate((CatNo) value.type, value.id),
                Patterns = Enumerable.Range(0, value.colorInfo.Count)
                    .Select(index => new Tuple<int, ModInfo>(index, translate(CatNo.mt_pattern, value.colorInfo[index].pattern)))
                    .Where(tuple => tuple.Item2 != null).ToDictionary(tuple => tuple.Item1, pair => pair.Item2)
            };
        static Dictionary<int, AccessoryMods> ToMods(this HumanDataAccessory value, Func<CatNo, int, ModInfo> translate) =>
            Enumerable.Range(0, value.parts.Count).ToDictionary(index => index, index => value.parts[index].ToMods(translate));
        static BodyMakeupMods ToMods(this HumanDataBodyMakeup value, Func<CatNo, int, ModInfo> translate) =>
            new BodyMakeupMods()
            {
                Nail = translate(CatNo.bo_nail, value.nailInfo.ID),
                NailLeg = translate(CatNo.bo_nail_leg, value.nailLegInfo.ID),
                Paints = Enumerable.Range(0, value.paintInfos.Count)
                    .Select(index => new Tuple<int, ModInfo>(index, translate(CatNo.mt_body_paint, value.paintInfos[index].ID)))
                    .Where(tuple => tuple.Item2 != null).ToDictionary(tuple => tuple.Item1, pair => pair.Item2),
                Layouts = Enumerable.Range(0, value.paintInfos.Count)
                    .Select(index => new Tuple<int, ModInfo>(index, translate(CatNo.bodypaint_layout, value.paintInfos[index].layoutID)))
                    .Where(tuple => tuple.Item2 != null).ToDictionary(tuple => tuple.Item1, pair => pair.Item2)
            };
        static FaceMakeupMods ToMods(this HumanDataFaceMakeup value, Func<CatNo, int, ModInfo> translate) =>
            new FaceMakeupMods()
            {
                Eyeshadow = translate(CatNo.mt_eyeshadow, value.eyeshadowId),
                Cheek = translate(CatNo.mt_cheek, value.cheekId),
                Lip = translate(CatNo.mt_lip, value.lipId),
                Paints = Enumerable.Range(0, value.paintInfos.Count)
                    .Select(index => new Tuple<int, ModInfo>(index, translate(CatNo.mt_face_paint, value.paintInfos[index].ID)))
                    .Where(tuple => tuple.Item2 != null).ToDictionary(tuple => tuple.Item1, pair => pair.Item2),
                Layouts = Enumerable.Range(0, value.paintInfos.Count)
                    .Select(index => new Tuple<int, ModInfo>(index, translate(CatNo.facepaint_layout, value.paintInfos[index].layoutID)))
                    .Where(tuple => tuple.Item2 != null).ToDictionary(tuple => tuple.Item1, pair => pair.Item2)
            };
        static CoordinateMods ToMods(this HumanDataCoordinate value, Func<CatNo, int, ModInfo> translate) =>
            new CoordinateMods()
            {
                Hairs = value.Hair.ToMods(translate),
                Clothes = value.Clothes.ToMods(translate),
                Accessories = value.Accessory.ToMods(translate),
                FaceMakeup = value.FaceMakeup.ToMods(translate),
                BodyMakeup = value.BodyMakeup.ToMods(translate),
            };
        static EyeMods ToMods(this HumanDataFace.PupilInfo value, Func<CatNo, int, ModInfo> translate) =>
            new EyeMods()
            {
                Eye = translate(CatNo.mt_eye, value.id),
                Gradation = translate(CatNo.mt_eye_gradation, value.gradMaskId),
                Highlights = Enumerable.Range(0, value.highlightInfos.Count)
                    .Select(index => new Tuple<int, ModInfo>(index, translate(CatNo.mt_eye_hi_up, value.highlightInfos[index].id)))
                    .Where(tuple => tuple.Item2 != null).ToDictionary(tuple => tuple.Item1, pair => pair.Item2)
            };
        static FaceMods ToMods(this HumanDataFace value, Func<CatNo, int, ModInfo> translate) =>
            new FaceMods()
            {
                Head = translate(CatNo.bo_head, value.headId),
                Detail = translate(CatNo.mt_face_detail, value.detailId),
                Mole = translate(CatNo.mt_mole, value.moleInfo.ID),
                MoleLayout = translate(CatNo.mole_layout, value.moleInfo.layoutID),
                Nose = translate(CatNo.mt_nose, value.noseId),
                LipLine = translate(CatNo.mt_lipline, value.lipLineId),
                Eyebrows = translate(CatNo.mt_eyebrow, value.eyebrowId),
                Eyelid = translate(CatNo.mt_eyelid, value.eyelidId),
                EyelineDown = translate(CatNo.mt_eyeline_down, value.eyelineDownId),
                EyelineUp = translate(CatNo.mt_eyeline_up, value.eyelineUpId),
                Eyes = Enumerable.Range(0, value.pupil.Count)
                    .ToDictionary(index => index, index => value.pupil[index].ToMods(translate))
            };
        static BodyMods ToMods(this HumanDataBody value, Func<CatNo, int, ModInfo> translate) =>
            new BodyMods()
            {
                Detail = translate(CatNo.mt_body_detail, value.detailId),
                Sunburn = translate(CatNo.mt_sunburn, value.sunburnId),
                Nip = translate(CatNo.mt_nip, value.nipId),
                Underhair = translate(CatNo.mt_underhair, value.underhairId)
            };
        static CharacterMods ToMods(this HumanData value, Func<CatNo, int, ModInfo> translate) =>
            new CharacterMods()
            {
                Face = value.Custom.Face.ToMods(translate),
                Body = value.Custom.Body.ToMods(translate),
                Coordinates = Enum.GetValues<ChaFileDefine.CoordinateType>()
                    .ToDictionary(index => index, index => value.Coordinates[(int)index].ToMods(translate))
            };
        internal static CoordinateMods TranslateSoftMods(this HumanDataCoordinate value) =>
            value.ToMods(ModPackageExtensions.TranslateSoftMods);
        internal static CharacterMods TranslateSoftMods(this HumanData value) =>
            value.ToMods(ModPackageExtensions.TranslateSoftMods);
        internal static CoordinateMods TranslateHardMods(this HumanDataCoordinate value) =>
            value.ToMods(ModPackageExtensions.TranslateHardMods);
        internal static CharacterMods TranslateHardMods(this HumanData value) =>
            value.ToMods(ModPackageExtensions.TranslateHardMods);
        static void Apply(this ModInfo mod, CatNo catNo, HumanDataHair.PartsInfo data) =>
            data.id = mod.ToId(catNo, data.id);
        static void Apply(this ModInfo mod, HumanDataHair data) =>
            data.glossId = mod.ToId(CatNo.mt_hairgloss, data.glossId);
        static void Apply(this Dictionary<ChaFileDefine.HairKind, ModInfo> mods, HumanDataHair data) =>
            mods?.Do(entry => entry.Value.Apply(entry.Key.ToCategoryNo(), data.parts[(int)entry.Key]));
        static void Apply(this HairsMods mods, HumanDataCoordinate data) =>
            data.Hair.With(mods.Hairs.Apply).With(mods.HairGloss.Apply);
        static void Apply(this ModInfo mod, CatNo catNo, HumanDataPaintInfo data) =>
            data.ID = mod.ToId(catNo, data.ID);
        static void Apply(this ModInfo mod, CatNo catNo, HumanDataClothes.PartsInfo data) =>
            data.id = mod.ToId(catNo, data.id);
        static void Apply(this ModInfo mod, HumanDataClothes.PartsInfo.PatternInfo data) =>
            data.pattern = mod.ToId(CatNo.mt_pattern, data.pattern);
        static void ApplyPaint(this Dictionary<int, ModInfo> mods, HumanDataClothes.PartsInfo data) =>
            mods?.Do(entry => entry.Value.Apply(CatNo.mt_body_paint, data.paintInfos[entry.Key]));
        static void ApplyPattern(this Dictionary<int, ModInfo> mods, HumanDataClothes.PartsInfo data) =>
            mods?.Do(entry => entry.Value.Apply(data.colorInfo[entry.Key].patternInfo));
        static void Apply(this ClothMods mods, CatNo catNo, HumanDataClothes.PartsInfo data) =>
            mods?.Part.Apply(catNo, data.With(mods.Paints.ApplyPaint).With(mods.Patterns.ApplyPattern));
        static void Apply(this Dictionary<ChaFileDefine.ClothesKind, ClothMods> mods, HumanDataCoordinate data) =>
            mods?.Do(entry => entry.Value.Apply(entry.Key.ToCategoryNo(), data.Clothes.parts[(int)entry.Key]));
        static void Apply(this ModInfo mod, HumanDataAccessory.PartsInfo data) =>
            (data.id, data.type) = (mod.ToId((CatNo)data.type, data.id), mod != null ? (int) mod.Category : data.type);
        static void Apply(this ModInfo mod, HumanDataAccessory.PartsInfo.ColorInfo data) =>
            data.pattern = mod.ToId(CatNo.mt_pattern, data.pattern);
        static void Apply(this Dictionary<int, ModInfo> mods, HumanDataAccessory.PartsInfo data) =>
            mods?.Do(entry => entry.Value.Apply(data.colorInfo[entry.Key]));
        static void Apply(this AccessoryMods mods, HumanDataAccessory.PartsInfo data) =>
            data.With(mods.Part.Apply).With(mods.Patterns.Apply);
        static void Apply(this Dictionary<int, AccessoryMods> mods, HumanDataCoordinate data) =>
            mods?.Do(entry => entry.Value.Apply(data.Accessory.parts[entry.Key]));
        static void ApplyLayout(this ModInfo mod, CatNo catNo, HumanDataPresetPaintInfo data) =>
            data.layoutID = mod.ToId(catNo, data.layoutID);
        static void ApplyPaint(this Dictionary<int, ModInfo> mods, HumanDataFaceMakeup data) =>
            mods?.Do(entry => entry.Value.Apply(CatNo.mt_face_paint, data.paintInfos[entry.Key]));
        static void ApplyLayout(this Dictionary<int, ModInfo> mods, HumanDataFaceMakeup data) =>
            mods?.Do(entry => entry.Value.ApplyLayout(CatNo.facepaint_layout, data.paintInfos[entry.Key]));
        static void ApplyEyeshadow(this ModInfo mod, HumanDataFaceMakeup data) =>
            data.eyeshadowId = mod.ToId(CatNo.mt_eyeshadow, data.eyeshadowId);
        static void ApplyCheek(this ModInfo mod, HumanDataFaceMakeup data) =>
            data.cheekId = mod.ToId(CatNo.mt_cheek, data.cheekId);
        static void ApplyLip(this ModInfo mod, HumanDataFaceMakeup data) =>
            data.lipId = mod.ToId(CatNo.mt_lip, data.lipId);
        static void Apply(this FaceMakeupMods mods, HumanDataCoordinate data) =>
            data.FaceMakeup.With(mods.Paints.ApplyPaint).With(mods.Layouts.ApplyLayout)
                .With(mods.Eyeshadow.ApplyEyeshadow).With(mods.Cheek.ApplyCheek).With(mods.Lip.ApplyLip);
        static void Apply(this ModInfo mod, CatNo catNo, HumanDataBodyMakeup.NailInfo data) =>
            data.ID = mod.ToId(catNo, data.ID);
        static void ApplyNail(this BodyMakeupMods mods, HumanDataBodyMakeup data) =>
            mods?.Nail.Apply(CatNo.bo_nail, data.nailInfo);
        static void ApplyNailLeg(this BodyMakeupMods mods, HumanDataBodyMakeup data) =>
            mods?.NailLeg.Apply(CatNo.bo_nail_leg, data.nailLegInfo);
        static void ApplyPaint(this Dictionary<int, ModInfo> mods, HumanDataBodyMakeup data) =>
            mods?.Do(entry => entry.Value.Apply(CatNo.mt_body_paint, data.paintInfos[entry.Key]));
        static void ApplyLayout(this Dictionary<int, ModInfo> mods, HumanDataBodyMakeup data) =>
            mods?.Do(entry => entry.Value.ApplyLayout(CatNo.bodypaint_layout, data.paintInfos[entry.Key]));
        static void Apply(this BodyMakeupMods mods, HumanDataCoordinate data) =>
            data.BodyMakeup.With(mods.Paints.ApplyPaint).With(mods.Layouts.ApplyLayout).With(mods.ApplyNail).With(mods.ApplyNailLeg);
        internal static void Apply(this CoordinateMods mods, HumanDataCoordinate data) =>
            data.With(mods.Hairs.Apply).With(mods.Clothes.Apply)
                .With(mods.Accessories.Apply).With(mods.FaceMakeup.Apply).With(mods.BodyMakeup.Apply);
        static void Apply(this ModInfo mod, HumanDataFace.HighlightInfo data) =>
            data.id = mod.ToId(CatNo.mt_eye_hi_up, data.id);
        static void Apply(this Dictionary<int, ModInfo> mods, HumanDataFace.PupilInfo data) =>
            mods?.Do(entry => entry.Value.Apply(data.highlightInfos[entry.Key]));
        static void Apply(this ModInfo mod, HumanDataFace.PupilInfo data) =>
            data.id = mod.ToId(CatNo.mt_eye, data.id);
        static void ApplyGradation(this ModInfo mod, HumanDataFace.PupilInfo data) =>
            data.gradMaskId = mod.ToId(CatNo.mt_eye_gradation, data.gradMaskId);
        static void Apply(this EyeMods mods, HumanDataFace.PupilInfo data) =>
            data.With(mods.Eye.Apply).With(mods.Gradation.ApplyGradation).With(mods.Highlights.Apply);
        static void Apply(this Dictionary<int, EyeMods> mods, HumanDataFace data) =>
            mods?.Do(entry => entry.Value.Apply(data.pupil[entry.Key]));
        static void ApplyDetail(this ModInfo mod, HumanDataFace data) =>
            data.detailId = mod.ToId(CatNo.mt_face_detail, data.detailId);
        static void ApplyEyebrows(this ModInfo mod, HumanDataFace data) =>
            data.eyebrowId = mod.ToId(CatNo.mt_eyebrow, data.eyebrowId);
        static void ApplyEyelid(this ModInfo mod, HumanDataFace data) =>
            data.eyelidId = mod.ToId(CatNo.mt_eyelid, data.eyelidId);
        static void ApplyEyelineDown(this ModInfo mod, HumanDataFace data) =>
            data.eyelineDownId = mod.ToId(CatNo.mt_eyeline_down, data.eyelineDownId);
        static void ApplyEyelineUp(this ModInfo mod, HumanDataFace data) =>
            data.eyelineUpId = mod.ToId(CatNo.mt_eyeline_up, data.eyelineUpId);
        static void ApplyEyeWhite(this ModInfo mod, HumanDataFace data) =>
            data.whiteId = mod.ToId(CatNo.mt_eye_white, data.whiteId);
        static void ApplyHead(this ModInfo mod, HumanDataFace data) =>
            data.headId = mod.ToId(CatNo.bo_head, data.headId);
        static void ApplyNose(this ModInfo mod, HumanDataFace data) =>
            data.noseId = mod.ToId(CatNo.mt_nose, data.noseId);
        static void ApplyLipLine(this ModInfo mod, HumanDataFace data) =>
            data.lipLineId = mod.ToId(CatNo.mt_lipline, data.lipLineId);
        static void ApplyMole(this ModInfo mod, HumanDataFace data) =>
            data.moleInfo.ID = mod.ToId(CatNo.mt_mole, data.moleInfo.ID);
        static void ApplyMoleLayout(this ModInfo mod, HumanDataFace data) =>
            data.moleInfo.layoutID = mod.ToId(CatNo.mole_layout, data.moleInfo.layoutID);
        static void Apply(this Dictionary<ChaFileDefine.CoordinateType, CoordinateMods> mods, HumanData data) =>
            mods.Do(entry => entry.Value.Apply(data.Coordinates[(int)entry.Key]));
        static void Apply(this FaceMods mods, HumanData data) =>
            data.Custom.Face.With(mods.Detail.ApplyDetail)
                .With(mods.Eyes.Apply)
                .With(mods.Eyebrows.ApplyEyebrows)
                .With(mods.Eyelid.ApplyEyelid)
                .With(mods.EyelineDown.ApplyEyelineDown)
                .With(mods.EyelineUp.ApplyEyelineUp)
                .With(mods.EyeWhite.ApplyEyeWhite)
                .With(mods.Head.ApplyHead)
                .With(mods.Nose.ApplyNose)
                .With(mods.LipLine.ApplyLipLine)
                .With(mods.Mole.ApplyMole)
                .With(mods.MoleLayout.ApplyMoleLayout);
        static void ApplyDetail(this ModInfo mod, HumanDataBody data) =>
            data.detailId = mod.ToId(CatNo.mt_body_detail, data.detailId);
        static void ApplyNip(this ModInfo mod, HumanDataBody data) =>
            data.nipId = mod.ToId(CatNo.mt_nip, data.nipId);
        static void ApplySunburn(this ModInfo mod, HumanDataBody data) =>
            data.sunburnId = mod.ToId(CatNo.mt_sunburn, data.sunburnId);
        static void ApplyUnderhair(this ModInfo mod, HumanDataBody data) =>
            data.underhairId = mod.ToId(CatNo.mt_underhair, data.underhairId);
        static void Apply(this BodyMods mods, HumanData data) =>
            data.Custom.Body.With(mods.Detail.ApplyDetail).With(mods.Nip.ApplyNip)
                .With(mods.Sunburn.ApplySunburn).With(mods.Underhair.ApplyUnderhair);
        internal static void Apply(this CharacterMods mods, HumanData data) =>
            data.With(mods.Coordinates.Apply).With(mods.Body.Apply).With(mods.Face.Apply);
        static readonly string ModificationPath = Path.Combine(Plugin.Guid, "modification.json");
        static void Serialize(ZipArchive archive) =>
            HumanCustom.Instance.Human.data.TranslateSoftMods().Serialize(archive);
        static void SerializeCoordinate(ZipArchive archive) =>
            HumanCustom.Instance.Human.data.Coordinates
                [HumanCustom.Instance.Human.data.Status.coordinateType].TranslateSoftMods().Serialize(archive);
        static void Deserialize(HumanData data, CharaLimit limits, ZipArchive archive) =>
            archive.Deserialize(data.TranslateHardMods, limits.Merge(data));
        static void Deserialize(Human human, HumanDataCoordinate coord, CoordLimit limits, ZipArchive archive) =>
            archive.Deserialize(coord.TranslateHardMods, limits.Merge(coord));
        static void Serialize(int index, ZipArchive archive) =>
            Manager.Game.saveData.Charas[index].charFile.TranslateSoftMods().Serialize(archive);
        static void Deserialize(int index, ZipArchive archive) =>
            archive.Deserialize(
                Manager.Game.saveData.Charas[index].charFile.TranslateHardMods,
                CharaLimit.All.Merge(Manager.Game.saveData.Charas[index].charFile));
        static void Serialize(this CharacterMods mods, ZipArchive archive) =>
            new StreamWriter(archive.CreateEntry(ModificationPath).Open())
                .With(stream => stream.Write(JsonSerializer.Serialize(mods))).Close();
        static void Serialize(this CoordinateMods mods, ZipArchive archive) =>
             new StreamWriter(archive.CreateEntry(ModificationPath).Open())
                .With(stream => stream.Write(JsonSerializer.Serialize(mods))).Close();
        static void Deserialize<T>(this ZipArchive archive, Func<T> supply, Action<T> action) =>
            archive.GetEntry(ModificationPath).Deserialize(supply, action);
        static void Deserialize<T>(this ZipArchiveEntry entry, Func<T> supply, Action<T> action) =>
            (entry != null).Either(() => action(supply()), () => entry.Open().With(stream => action(JsonSerializer.Deserialize<T>(stream))).Close());
        static Action<CharacterMods> Merge(this CharaLimit limits, HumanData data) =>
            limits.MergeCoordinates(data) + limits.MergeBody(data) + limits.MergeFace(data);
        static Action<CharacterMods> MergeFace(this CharaLimit limits, HumanData data) =>
            mods => (CharaLimit.None != (limits & CharaLimit.Face)).Maybe(() => mods.Face.Apply(data));
        static Action<CharacterMods> MergeBody(this CharaLimit limits, HumanData data) =>
            mods => (CharaLimit.None != (limits & CharaLimit.Body)).Maybe(() => mods.Body.Apply(data));
        static Action<CharacterMods> MergeCoordinates(this CharaLimit limits, HumanData data) =>
            mods => mods.Coordinates.Do(entry => limits.Merge(data.Coordinates[(int)entry.Key])(entry.Value));
        static Action<CoordinateMods> Merge(this CharaLimit limits, HumanDataCoordinate data) =>
            limits.MergeHair(data) + limits.MergeClothes(data) + limits.MergeAccessories(data) + limits.MergeFaceMakeup(data) + limits.MergeBodyMakeup(data);
        static Action<CoordinateMods> MergeHair(this CharaLimit limits, HumanDataCoordinate data) =>
            mods => (CharaLimit.None != (limits & CharaLimit.Hair)).Maybe(() => mods.Hairs.Apply(data));
        static Action<CoordinateMods> MergeClothes(this CharaLimit limits, HumanDataCoordinate data) =>
            mods => (CharaLimit.None != (limits & CharaLimit.Coorde)).Maybe(() => mods.Clothes.Apply(data));
        static Action<CoordinateMods> MergeAccessories(this CharaLimit limits, HumanDataCoordinate data) =>
            mods => (CharaLimit.None != (limits & CharaLimit.Coorde)).Maybe(() => mods.Accessories.Apply(data));
        static Action<CoordinateMods> MergeFaceMakeup(this CharaLimit limits, HumanDataCoordinate data) =>
            mods => (CharaLimit.None != (limits & CharaLimit.Coorde)).Maybe(() => mods.FaceMakeup.Apply(data));
        static Action<CoordinateMods> MergeBodyMakeup(this CharaLimit limits, HumanDataCoordinate data) =>
            mods => (CharaLimit.None != (limits & CharaLimit.Coorde)).Maybe(() => mods.BodyMakeup.Apply(data));
        static Action<CoordinateMods> Merge(this CoordLimit limits, HumanDataCoordinate data) =>
            limits.MergeHair(data) + limits.MergeClothes(data) + limits.MergeAccessories(data) + limits.MergeFaceMakeup(data) + limits.MergeBodyMakeup(data);
        static Action<CoordinateMods> MergeHair(this CoordLimit limits, HumanDataCoordinate data) =>
            mods => (CoordLimit.None != (limits & CoordLimit.Hair)).Maybe(() => mods.Hairs.Apply(data));
        static Action<CoordinateMods> MergeClothes(this CoordLimit limits, HumanDataCoordinate data) =>
            mods => (CoordLimit.None != (limits & CoordLimit.Clothes)).Maybe(() => mods.Clothes.Apply(data));
        static Action<CoordinateMods> MergeAccessories(this CoordLimit limits, HumanDataCoordinate data) =>
            mods => (CoordLimit.None != (limits & CoordLimit.Accessory)).Maybe(() => mods.Accessories.Apply(data));
        static Action<CoordinateMods> MergeFaceMakeup(this CoordLimit limits, HumanDataCoordinate data) =>
            mods => (CoordLimit.None != (limits & CoordLimit.FaceMakeup)).Maybe(() => mods.FaceMakeup.Apply(data));
        static Action<CoordinateMods> MergeBodyMakeup(this CoordLimit limits, HumanDataCoordinate data) =>
            mods => (CoordLimit.None != (limits & CoordLimit.BodyMakeup)).Maybe(() => mods.BodyMakeup.Apply(data));
        static internal void Initialize()
        {
            Event.OnCharacterCreationSerialize += Serialize;
            Event.OnCharacterCreationDeserialize += Deserialize;
            Event.OnCoordinateSerialize += SerializeCoordinate;
            Event.OnCoordinateDeserialize += Deserialize;
            Event.OnActorSerialize += Serialize;
            Event.OnActorDeserialize += Deserialize;
        }
    }
}