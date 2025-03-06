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
using Cysharp.Threading.Tasks;
using Fishbone;

namespace SardineTail
{
    public class ModInfo
    {
        public string PkgId { get; set; }
        public string ModId { get; set; }
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
        static int ToCategory(this ModInfo info, int original) =>
            info != null ? (int)Enum.Parse<CatNo>(info.ModId.Split(Path.AltDirectorySeparatorChar)[0]) : original;
            
        static HairsMods ToMods(this HumanDataHair value) =>
            new HairsMods()
            {
                HairGloss = CatNo.mt_hairgloss.ToModInfo(value.glossId),
                Hairs = Enum.GetValues<ChaFileDefine.HairKind>()
                    .Where(index => null != index.ToModInfo(value.parts[(int)index].id))
                    .ToDictionary(index => index, index => index.ToModInfo(value.parts[(int)index].id))
            };
        static ClothMods ToMods(this ChaFileDefine.ClothesKind id, HumanDataClothes.PartsInfo value) =>
            new ClothMods()
            {
                Part = id.ToModInfo(value.id),
                Paints = Enumerable.Range(0, value.paintInfos.Count)
                    .Where(index => null != CatNo.mt_body_paint.ToModInfo(value.paintInfos[index].ID))
                    .ToDictionary(index => index, index => CatNo.mt_body_paint.ToModInfo(value.paintInfos[index].ID)),
                Patterns = Enumerable.Range(0, value.colorInfo.Count)
                    .Where(index => null !=  CatNo.mt_pattern.ToModInfo(value.colorInfo[index].patternInfo.pattern))
                    .ToDictionary(index => index, index => CatNo.mt_pattern.ToModInfo(value.colorInfo[index].patternInfo.pattern))
            };
        static Dictionary<ChaFileDefine.ClothesKind, ClothMods> ToMods(this HumanDataClothes value) =>
            Enum.GetValues<ChaFileDefine.ClothesKind>()
                .ToDictionary(index => index, index => index.ToMods(value.parts[(int)index]));
        static AccessoryMods ToMods(this HumanDataAccessory.PartsInfo value) =>
            new AccessoryMods()
            {
                Part = value.ToModInfo(value.id),
                Patterns = Enumerable.Range(0, value.colorInfo.Count)
                    .Where(index => null !=  CatNo.mt_pattern.ToModInfo(value.colorInfo[index].pattern))
                    .ToDictionary(index => index, index => CatNo.mt_pattern.ToModInfo(value.colorInfo[index].pattern))
            };
        static Dictionary<int, AccessoryMods> ToMods(this HumanDataAccessory value) =>
            Enumerable.Range(0, value.parts.Count)
                .ToDictionary(index => index, index => value.parts[index].ToMods());
        static BodyMakeupMods ToMods(this HumanDataBodyMakeup value) =>
            new BodyMakeupMods()
            {
                Nail = CatNo.bo_nail.ToModInfo(value.nailInfo.ID),
                NailLeg = CatNo.bo_nail_leg.ToModInfo(value.nailLegInfo.ID),
                Paints = Enumerable.Range(0, value.paintInfos.Count)
                    .Where(index => null != CatNo.bodypaint_layout.ToModInfo(value.paintInfos[index].ID))
                    .ToDictionary(index => index, index => CatNo.bodypaint_layout.ToModInfo(value.paintInfos[index].ID)),
                Layouts = Enumerable.Range(0, value.paintInfos.Count)
                    .Where(index => null != CatNo.bodypaint_layout.ToModInfo(value.paintInfos[index].layoutID))
                    .ToDictionary(index => index, index => CatNo.bodypaint_layout.ToModInfo(value.paintInfos[index].layoutID)),
            };
        static FaceMakeupMods ToMods(this HumanDataFaceMakeup value) =>
            new FaceMakeupMods()
            {
                Eyeshadow = CatNo.mt_eyeshadow.ToModInfo(value.eyeshadowId),
                Cheek = CatNo.mt_cheek.ToModInfo(value.cheekId),
                Lip = CatNo.mt_lip.ToModInfo(value.lipId),
                Paints = Enumerable.Range(0, value.paintInfos.Count)
                    .Where(index =>  null != CatNo.bodypaint_layout.ToModInfo(value.paintInfos[index].ID))
                    .ToDictionary(index => index, index => CatNo.bodypaint_layout.ToModInfo(value.paintInfos[index].ID)),
                Layouts = Enumerable.Range(0, value.paintInfos.Count)
                    .Where(index => null != CatNo.bodypaint_layout.ToModInfo(value.paintInfos[index].layoutID))
                    .ToDictionary(index => index, index => CatNo.bodypaint_layout.ToModInfo(value.paintInfos[index].layoutID)),
            };
        internal static CoordinateMods ToMods(this HumanDataCoordinate value) =>
            new CoordinateMods()
            {
                Hairs = value.Hair.ToMods(),
                Clothes = value.Clothes.ToMods(),
                Accessories = value.Accessory.ToMods(),
                FaceMakeup = value.FaceMakeup.ToMods(),
                BodyMakeup = value.BodyMakeup.ToMods(),
            };

        static EyeMods ToMods(this HumanDataFace.PupilInfo value) =>
            new EyeMods()
            {
                Eye = CatNo.mt_eye.ToModInfo(value.id),
                Gradation = CatNo.mt_eye_gradation.ToModInfo(value.gradMaskId),
                Highlights = Enumerable.Range(0, value.highlightInfos.Count)
                    .Where(index => null != CatNo.mt_eye_hi_up.ToModInfo(value.highlightInfos[index].id))
                    .ToDictionary(index => index, index => CatNo.mt_eye_hi_up.ToModInfo(value.highlightInfos[index].id))
            };
        static FaceMods ToMods(this HumanDataFace value) =>
            new FaceMods()
            {
                Head = CatNo.bo_head.ToModInfo(value.headId),
                Detail = CatNo.mt_face_detail.ToModInfo(value.detailId),
                Mole = CatNo.mt_mole.ToModInfo(value.moleInfo.ID),
                MoleLayout = CatNo.mole_layout.ToModInfo(value.moleInfo.layoutID),
                Nose = CatNo.mt_nose.ToModInfo(value.noseId),
                LipLine = CatNo.mt_lipline.ToModInfo(value.lipLineId),
                Eyebrows = CatNo.mt_eyebrow.ToModInfo(value.eyebrowId),
                Eyelid = CatNo.mt_eyelid.ToModInfo(value.eyelidId),
                EyelineDown = CatNo.mt_eyeline_down.ToModInfo(value.eyelineDownId),
                EyelineUp = CatNo.mt_eyeline_up.ToModInfo(value.eyelineUpId),
                Eyes = Enumerable.Range(0, value.pupil.Count)
                    .ToDictionary(index => index, index => value.pupil[index].ToMods())
            };
        static BodyMods ToMods(this HumanDataBody value) =>
            new BodyMods()
            {
                Detail = CatNo.mt_body_detail.ToModInfo(value.detailId),
                Sunburn = CatNo.mt_sunburn.ToModInfo(value.sunburnId),
                Nip = CatNo.mt_nip.ToModInfo(value.nipId),
                Underhair = CatNo.mt_underhair.ToModInfo(value.underhairId)
            };
        internal static CharacterMods ToMods(this HumanData value) =>
            new CharacterMods()
            {
                Face = value.Custom.Face.ToMods(),
                Body = value.Custom.Body.ToMods(),
                Coordinates = Enum.GetValues<ChaFileDefine.CoordinateType>()
                    .ToDictionary(index => index, index => value.Coordinates[(int)index].ToMods())
            };
        static int Apply(this ModInfo mod, HumanDataHair.PartsInfo data) =>
            data.id = mod.ToId(data.id);
        static IEnumerable<int> Apply(this HairsMods mods, HumanDataHair data) =>
            mods?.Hairs.Select(entry => entry.Value.Apply(data.parts[(int)entry.Key])) ?? [];
        static IEnumerable<int> Apply(this ModInfo mod, HumanDataClothes.PartsInfo data) =>
            [data.id = mod.ToId(data.id)];
        static int Apply(this ModInfo mod, HumanDataClothes.PartsInfo.PatternInfo data) =>
            data.pattern = mod.ToId(data.pattern);
        static int Apply(this ModInfo mod, HumanDataPaintInfo data) =>
            data.ID = mod.ToId(data.ID);
        static IEnumerable<int> Apply(this ClothMods mods, HumanDataClothes.PartsInfo data) =>
            mods?.Part.Apply(data)
                .Concat(mods.Paints.Select(entry => entry.Value.Apply(data.paintInfos[entry.Key])))
                .Concat(mods.Patterns.Select(entry => entry.Value.Apply(data.colorInfo[entry.Key].patternInfo))) ?? [];
        static IEnumerable<int> Apply(this Dictionary<ChaFileDefine.ClothesKind, ClothMods> mods, HumanDataClothes data) =>
            mods.SelectMany(entry => entry.Value.Apply(data.parts[(int)entry.Key]));
        static IEnumerable<int> Apply(this ModInfo mod, HumanDataAccessory.PartsInfo data) =>
            [data.id = mod.ToId(data.id), data.type = mod.ToCategory(data.type)];
        static int Apply(this ModInfo mod, HumanDataAccessory.PartsInfo.ColorInfo data) =>
            data.pattern = mod.ToId(data.pattern);
        static IEnumerable<int> Apply(this AccessoryMods mods, HumanDataAccessory.PartsInfo data) =>
            mods?.Part.Apply(data)
                .Concat(mods.Patterns.Select(entry => entry.Value.Apply(data.colorInfo[entry.Key]))) ?? [];
        static IEnumerable<int> Apply(this Dictionary<int, AccessoryMods> mods, HumanDataAccessory data) =>
            mods.SelectMany(entry => entry.Value.Apply(data.parts[entry.Key]));
        static int ApplyLayout(this ModInfo mod, HumanDataPresetPaintInfo data) =>
            data.layoutID = mod.ToId(data.layoutID);
        static int ApplyNail(this ModInfo mod, HumanDataBodyMakeup.NailInfo data) =>
            data.ID = mod.ToId(data.ID);
        static int ApplyNailLeg(this ModInfo mod, HumanDataBodyMakeup.NailInfo data) =>
            data.ID = mod.ToId(data.ID);
        static IEnumerable<int> Apply(this BodyMakeupMods mods, HumanDataBodyMakeup data) =>
            mods?.Layouts.Select(entry => entry.Value.ApplyLayout(data.paintInfos[entry.Key]))
                .Concat(mods.Paints.Select(entry => entry.Value.Apply(data.paintInfos[entry.Key])))
                .Concat([mods.Nail.ApplyNail(data.nailInfo), mods.NailLeg.ApplyNailLeg(data.nailLegInfo)]) ?? [];
        static IEnumerable<int> Apply(this FaceMakeupMods mods, HumanDataFaceMakeup data) =>
            mods?.Layouts.Select(entry => entry.Value.ApplyLayout(data.paintInfos[entry.Key]))
                .Concat(mods.Paints.Select(entry => entry.Value.Apply(data.paintInfos[entry.Key])))
                .Concat([data.eyeshadowId = mods.Eyeshadow.ToId(data.eyeshadowId),
                    data.cheekId = mods.Cheek.ToId(data.cheekId), data.lipId = mods.Lip.ToId(data.lipId)]) ?? [];
        internal static IEnumerable<int> Apply(this CoordinateMods mods, HumanDataCoordinate data) =>
            (mods?.Hairs.Apply(data.Hair) ?? [])
                    .Concat(mods?.Clothes.Apply(data.Clothes) ?? [])
                    .Concat(mods?.Accessories.Apply(data.Accessory) ?? [])
                    .Concat(mods?.FaceMakeup.Apply(data.FaceMakeup) ?? [])
                    .Concat(mods?.BodyMakeup.Apply(data.BodyMakeup) ?? []);
        static int Apply(this ModInfo mod, HumanDataFace.HighlightInfo data) =>
            data.id = mod.ToId(data.id);
        static IEnumerable<int> Apply(this EyeMods mods, HumanDataFace.PupilInfo data) =>
            mods?.Highlights.Select(entry => entry.Value.Apply(data.highlightInfos[entry.Key]))
                .Concat([data.id = mods.Eye.ToId(data.id), data.gradMaskId = mods.Gradation.ToId(data.gradMaskId)]) ?? [];
        static IEnumerable<int> Apply(this FaceMods mods, HumanDataFace data) =>
            mods?.Eyes.SelectMany(entry => entry.Value.Apply(data.pupil[entry.Key]))
                .Concat([
                    data.headId = mods.Head.ToId(data.headId),
                    data.detailId = mods.Head.ToId(data.detailId),
                    mods.MoleLayout.ApplyLayout(data.moleInfo),
                    mods.Mole.Apply(data.moleInfo),
                    data.noseId = mods.Nose.ToId(data.noseId),
                    data.lipLineId = mods.LipLine.ToId(data.lipLineId),
                    data.eyebrowId = mods.Eyebrows.ToId(data.eyebrowId),
                    data.eyelidId = mods.Eyelid.ToId(data.eyelidId),
                    data.eyelineDownId = mods.EyelineDown.ToId(data.eyelineDownId),
                    data.eyelineUpId = mods.EyelineUp.ToId(data.eyelineUpId),
                    data.whiteId = mods.EyeWhite.ToId(data.whiteId),
                ]) ?? [];
        static IEnumerable<int> Apply(this BodyMods mods, HumanDataBody data) =>
            [
                data.detailId = mods.Detail.ToId(data.detailId),
                data.sunburnId = mods.Sunburn.ToId(data.sunburnId),
                data.nipId = mods.Nip.ToId(data.nipId),
                data.underhairId = mods.Underhair.ToId(data.underhairId)
            ];
        internal static IEnumerable<int> Apply(this CharacterMods mods, HumanData data) =>
            (mods?.Coordinates.SelectMany(entry => entry.Value.Apply(data.Coordinates[(int)entry.Key])) ?? [])
                .Concat(mods.Body.Apply(data.Custom.Body) ?? [])
                .Concat(mods.Face.Apply(data.Custom.Face) ?? []);
        static readonly string ModificationPath = Path.Combine(Plugin.Guid, "modification.json");
        static void Serialize(ZipArchive archive) =>
            HumanCustom.Instance.Human.data.ToMods().Serialize(archive);
        static void SerializeCoordinate(ZipArchive archive) =>
            HumanCustom.Instance.Human.data.Coordinates[HumanCustom.Instance.Human.data.Status.coordinateType].ToMods().Serialize(archive);
        static void Deserialize(HumanData data, CharaLimit limits, ZipArchive archive) =>
            archive.Deserialize(Merge(data, limits));
        static void Deserialize(Human human, CoordLimit limits, ZipArchive archive) =>
            archive.Deserialize(Merge(human.data.Coordinates[human.data.Status.coordinateType], limits, human));
        static void Serialize(int index, ZipArchive archive) =>
            Manager.Game.saveData.Charas[index].charFile.ToMods().Serialize(archive);
        static void Deserialize(int index, ZipArchive archive) =>
            archive.Deserialize(Merge(Manager.Game.saveData.Charas[index].charFile, CharaLimit.All));
        static void Serialize(this CharacterMods mods, ZipArchive archive) =>
            new StreamWriter(archive.CreateEntry(ModificationPath).Open())
                .With(stream => stream.Write(JsonSerializer.Serialize(mods))).Close();
        static void Serialize(this CoordinateMods mods, ZipArchive archive) =>
             new StreamWriter(archive.CreateEntry(ModificationPath).Open())
                .With(stream => stream.Write(JsonSerializer.Serialize(mods))).Close();
        static void Deserialize<T>(this ZipArchive archive, Action<T> action) where T : new() =>
            archive.GetEntry(ModificationPath).Deserialize(action);
        static void Deserialize<T>(this ZipArchiveEntry entry, Action<T> action) where T : new() =>
            (entry != null).Maybe(() => entry.Open().With(stream => action(JsonSerializer.Deserialize<T>(stream))).Close());
        static Action<CharacterMods> Merge(this HumanData data, CharaLimit limits) =>
            mods => limits.With(MergeCoordinates(mods.Coordinates, data))
                .With(MergeFace(mods.Face, data.Custom.Face))
                .With(MergeBody(mods.Body, data.Custom.Body));
        static Action<CharaLimit> MergeFace(FaceMods src, HumanDataFace dst) =>
            limits => (CharaLimit.None != (limits & CharaLimit.Face)).Maybe(() => src.Apply(dst).Count());
        static Action<CharaLimit> MergeBody(BodyMods src, HumanDataBody dst) =>
            limits => (CharaLimit.None != (limits & CharaLimit.Body)).Maybe(() => src.Apply(dst).Count());
        static Action<CharaLimit> MergeCoordinates(Dictionary<ChaFileDefine.CoordinateType, CoordinateMods> src, HumanData dst) =>
            limits => src.Select(entry => limits.MergeHair(entry.Value, dst.Coordinates[(int)entry.Key])
                .With(() => limits.MergeClothes(entry.Value, dst.Coordinates[(int)entry.Key]))
                .With(() => limits.MergeAccessories(entry.Value, dst.Coordinates[(int)entry.Key]))
                .With(() => limits.MergeFaceMakeup(entry.Value, dst.Coordinates[(int)entry.Key]))
                .With(() => limits.MergeBodyMakeup(entry.Value, dst.Coordinates[(int)entry.Key]))).Count();
        static int MergeHair(this CharaLimit limits, CoordinateMods src, HumanDataCoordinate dst) =>
            (CharaLimit.None != (limits & CharaLimit.Hair)) ? src.Hairs.Apply(dst.Hair).Count() : 0;
        static int MergeClothes(this CharaLimit limits, CoordinateMods src, HumanDataCoordinate dst) =>
            (CharaLimit.None != (limits & CharaLimit.Coorde)) ? src.Clothes.Apply(dst.Clothes).Count() : 0;
        static int MergeAccessories(this CharaLimit limits, CoordinateMods src, HumanDataCoordinate dst) =>
            (CharaLimit.None != (limits & CharaLimit.Coorde)) ? src.Accessories.Apply(dst.Accessory).Count() : 0;
        static int MergeFaceMakeup(this CharaLimit limits, CoordinateMods src, HumanDataCoordinate dst) =>
           (CharaLimit.None != (limits & CharaLimit.Coorde)) ? src.FaceMakeup.Apply(dst.FaceMakeup).Count() : 0;
        static int MergeBodyMakeup(this CharaLimit limits, CoordinateMods src, HumanDataCoordinate dst) =>
           (CharaLimit.None != (limits & CharaLimit.Coorde)) ? src.BodyMakeup.Apply(dst.BodyMakeup).Count() : 0;
        static Action<CoordinateMods> Merge(HumanDataCoordinate data, CoordLimit limits, Human human) =>
            mods => limits.MergeHair(mods, data)
                .With(() => limits.MergeClothes(mods, data))
                .With(() => limits.MergeAccessories(mods, data))
                .With(() => limits.MergeFaceMakeup(mods, data))
                .With(() => limits.MergeBodyMakeup(mods, data));
        static int MergeHair(this CoordLimit limits, CoordinateMods src, HumanDataCoordinate dst) =>
            (CoordLimit.None != (limits & CoordLimit.Hair)) ? src.Hairs.Apply(dst.Hair).Count() : 0;
        static int MergeClothes(this CoordLimit limits, CoordinateMods src, HumanDataCoordinate dst) =>
            (CoordLimit.None != (limits & CoordLimit.Clothes)) ? src.Clothes.Apply(dst.Clothes).Count() : 0;
        static int MergeAccessories(this CoordLimit limits, CoordinateMods src, HumanDataCoordinate dst) =>
            (CoordLimit.None != (limits & CoordLimit.Accessory)) ? src.Accessories.Apply(dst.Accessory).Count() : 0;
        static int MergeFaceMakeup(this CoordLimit limits, CoordinateMods src, HumanDataCoordinate dst) =>
            (CoordLimit.None != (limits & CoordLimit.FaceMakeup)) ? src.FaceMakeup.Apply(dst.FaceMakeup).Count() : 0;
        static int MergeBodyMakeup(this CoordLimit limits, CoordinateMods src, HumanDataCoordinate dst) =>
            (CoordLimit.None != (limits & CoordLimit.BodyMakeup)) ? src.BodyMakeup.Apply(dst.BodyMakeup).Count() : 0;
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