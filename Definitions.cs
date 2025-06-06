using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using CatNo = ChaListDefine.CategoryNo;
using Ktype = ChaListDefine.KeyType;

namespace SardineTail
{
    internal static partial class CategoryExtensions
    {
        internal static readonly IEnumerable<Category> All = Enum.GetValues<CatNo>().Select(index => new Category()
        {
            Index = index,
            Entries = index switch
            {
                CatNo.ao_arm => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.Parent, Vtype.Text, "a_n_wrist_L"),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0/1/2/3/4/5/6"),
                    new Entry(Ktype.Detail, Vtype.Text, "0"),
                    new Entry(Ktype.Sort, Vtype.Text, "0")
                ],
                CatNo.ao_body => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.Parent, Vtype.Text, "a_n_back"),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0/1/2/3/5/6/7"),
                    new Entry(Ktype.Detail, Vtype.Text, "1"),
                    new Entry(Ktype.Sort, Vtype.Text, "0")
                ],
                CatNo.ao_face => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.Parent, Vtype.Text, "a_n_megane"),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0/1/2/3/4/5/6/7/11"),
                    new Entry(Ktype.Detail, Vtype.Text, "0"),
                    new Entry(Ktype.Sort, Vtype.Text, "0")
                ],
                CatNo.ao_hair => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.Parent, Vtype.Text, "a_n_hair_pin"),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0/1/2/3/4/5/6/7/11"),
                    new Entry(Ktype.Detail, Vtype.Text, "0"),
                    new Entry(Ktype.Sort, Vtype.Text, "0")
                ],
                CatNo.ao_hand => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.Parent, Vtype.Text, "a_n_ind_L"),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0/1/2/3/4/5/6/9/11"),
                    new Entry(Ktype.Detail, Vtype.Text, "0"),
                    new Entry(Ktype.Sort, Vtype.Text, "0")
                ],
                CatNo.ao_head => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.Parent, Vtype.Text, "a_n_hair_twin_L"),
                    new Entry(Ktype.HideHair, Vtype.Text, "0"),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/4"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0"),
                    new Entry(Ktype.Detail, Vtype.Text, "0"),
                    new Entry(Ktype.Sort, Vtype.Text, "0")
                ],
                CatNo.ao_kokan => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.Parent, Vtype.Text, "a_n_kokan"),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "99"),
                    new Entry(Ktype.Detail, Vtype.Text, "1"),
                    new Entry(Ktype.Sort, Vtype.Text, "0")
                ],
                CatNo.ao_leg => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.Parent, Vtype.Text, "a_n_leg_L"),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0/4/11"),
                    new Entry(Ktype.Detail, Vtype.Text, "0"),
                    new Entry(Ktype.Sort, Vtype.Text, "0")
                ],
                CatNo.ao_neck => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.Parent, Vtype.Text, "a_n_bust"),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0/1/2/3/4/5/6/7/9/11"),
                    new Entry(Ktype.Detail, Vtype.Text, "0"),
                    new Entry(Ktype.Sort, Vtype.Text, "0")
                ],
                CatNo.ao_waist => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.Parent, Vtype.Text, "a_n_waist_b"),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "99"),
                    new Entry(Ktype.Detail, Vtype.Text, "0"),
                    new Entry(Ktype.Sort, Vtype.Text, "0")
                ],
                CatNo.bodypaint_layout => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.PosX, Vtype.Text),
                    new Entry(Ktype.PosY, Vtype.Text),
                    new Entry(Ktype.Rot, Vtype.Text, "0"),
                    new Entry(Ktype.Scale, Vtype.Text, "-4"),
                    new Entry(Ktype.CenterX, Vtype.Text, "0"),
                    new Entry(Ktype.MoveX, Vtype.Text, "0"),
                    new Entry(Ktype.CenterY, Vtype.Text, "0"),
                    new Entry(Ktype.MoveY, Vtype.Text, "0"),
                    new Entry(Ktype.CenterScale, Vtype.Text, "-4"),
                    new Entry(Ktype.AddScale, Vtype.Text, "4.5"),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na")
                ],
                CatNo.bo_body => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.Sex, Vtype.Text, "3"),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.ShapeAnimeAB, Ktype.ShapeAnime),
                    new Entry(Ktype.ShapeAnime, Vtype.Asset),
                    new Entry(Ktype.MainTexAB, Ktype.MainTex),
                    new Entry(Ktype.MainTex, Vtype.Image)
                ],
                CatNo.bo_hair_b => [
                    new Entry(Ktype.Kind, Vtype.Text, "1"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.SetHair, Vtype.Text, "0"),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0/1"),
                    new Entry(Ktype.Detail, Vtype.Text, "0/4"),
                ],
                CatNo.bo_hair_f => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Detail, Vtype.Text, "0/1/2/3/4/5"),
                ],
                CatNo.bo_hair_o => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Detail, Vtype.Text, "0/1/2/3/4/5"),
                ],
                CatNo.bo_hair_s => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Detail, Vtype.Text, "0/3/4/5"),
                ],
                CatNo.bo_head => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.ShapeAnimeAB, Ktype.ShapeAnime),
                    new Entry(Ktype.ShapeAnime, Vtype.Asset),
                    new Entry(Ktype.MainTexAB, Ktype.MainTex),
                    new Entry(Ktype.MainTex, Vtype.Image),
                    new Entry(Ktype.ColorMaskAB, Ktype.ColorMaskTex),
                    new Entry(Ktype.ColorMaskTex, Vtype.Image),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Preset, Vtype.Text)
                ],
                CatNo.bo_nail => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0"),
                    new Entry(Ktype.Detail, Vtype.Text, "0")
                ],
                CatNo.bo_nail_leg => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0"),
                    new Entry(Ktype.Detail, Vtype.Text, "0")
                ],
                CatNo.co_bot => [
                    new Entry(Ktype.Kind, Vtype.Text, "4"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.StateType, Vtype.Text, "0"),
                    new Entry(Ktype.Coordinate, Vtype.Text, "0"),
                    new Entry(Ktype.HideShorts, Vtype.Text, "1"),
                    new Entry(Ktype.NoShake, Vtype.Text, "0"),
                    new Entry(Ktype.SkirtType, Vtype.Text, "0"),
                    new Entry(Ktype.Sex, Vtype.Text, "3"),
                    new Entry(Ktype.OverTopMaskAB, Ktype.OverTopMask),
                    new Entry(Ktype.OverTopMask, Vtype.Image, "0"),
                    new Entry(Ktype.OverBotMaskAB, Ktype.OverBotMask),
                    new Entry(Ktype.OverBotMask, Vtype.Image, "0"),
                    new Entry(Ktype.MainTexAB, Ktype.MainTex),
                    new Entry(Ktype.MainTex, Vtype.Image),
                    new Entry(Ktype.ColorMaskAB, Ktype.ColorMaskTex),
                    new Entry(Ktype.ColorMaskTex, Vtype.Image),
                    new Entry(Ktype.MainTex02AB, Ktype.MainTex02),
                    new Entry(Ktype.MainTex02, Vtype.Image, "0"),
                    new Entry(Ktype.ColorMask02AB, Ktype.ColorMask02Tex),
                    new Entry(Ktype.ColorMask02Tex, Vtype.Image, "0"),
                    new Entry(Ktype.MainTex03AB, Ktype.MainTex03),
                    new Entry(Ktype.MainTex03, Vtype.Image, "0"),
                    new Entry(Ktype.ColorMask03AB, Ktype.ColorMask03Tex),
                    new Entry(Ktype.ColorMask03Tex, Vtype.Image, "0"),
                    new Entry(Ktype.PaintMaskAB, Ktype.PaintMask),
                    new Entry(Ktype.PaintMask, Vtype.Image, "0"),
                    new Entry(Ktype.KokanHide, Vtype.Text, "0"),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0/3"),
                    new Entry(Ktype.Detail, Vtype.Text, "0/1/2/3")
                ],
                CatNo.co_bra => [
                    new Entry(Ktype.Kind, Vtype.Text, "1"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.NormalData, Vtype.Text, "0"),
                    new Entry(Ktype.StateType, Vtype.Text, "0"),
                    new Entry(Ktype.Coordinate, Vtype.Text, "0"),
                    new Entry(Ktype.MabUV, Vtype.Text, "0"),
                    new Entry(Ktype.Sex, Vtype.Text, "3"),
                    new Entry(Ktype.OverTopMaskAB, Ktype.OverTopMask),
                    new Entry(Ktype.OverTopMask, Vtype.Image, "0"),
                    new Entry(Ktype.OverBotMaskAB, Ktype.OverBotMask),
                    new Entry(Ktype.OverBotMask, Vtype.Image, "0"),
                    new Entry(Ktype.MainTexAB, Ktype.MainTex),
                    new Entry(Ktype.MainTex, Vtype.Image),
                    new Entry(Ktype.ColorMaskAB, Ktype.ColorMaskTex),
                    new Entry(Ktype.ColorMaskTex, Vtype.Image),
                    new Entry(Ktype.MainTex02AB, Ktype.MainTex02),
                    new Entry(Ktype.MainTex02, Vtype.Image, "0"),
                    new Entry(Ktype.ColorMask02AB, Ktype.ColorMask02Tex),
                    new Entry(Ktype.ColorMask02Tex, Vtype.Image, "0"),
                    new Entry(Ktype.PaintMaskAB, Ktype.PaintMask),
                    new Entry(Ktype.PaintMask, Vtype.Image, "0"),
                    new Entry(Ktype.NipHide, Vtype.Text, "1"),
                    new Entry(Ktype.KokanHide, Vtype.Text, "0"),
                    new Entry(Ktype.HalfUndress, Vtype.Text, "0"),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0/3"),
                    new Entry(Ktype.Detail, Vtype.Text, "0/1/2/3"),
                    new Entry(Ktype.Sort, Vtype.Text, "0")
                ],
                CatNo.co_gloves => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.StateType, Vtype.Text, "1"),
                    new Entry(Ktype.NailHide, Vtype.Text, "1"),
                    new Entry(Ktype.Sex, Vtype.Text, "3"),
                    new Entry(Ktype.MainTexAB, Ktype.MainTex),
                    new Entry(Ktype.MainTex, Vtype.Image),
                    new Entry(Ktype.ColorMaskAB, Ktype.ColorMaskTex),
                    new Entry(Ktype.ColorMaskTex, Vtype.Image),
                    new Entry(Ktype.MainTex02AB, Ktype.MainTex02),
                    new Entry(Ktype.MainTex02, Vtype.Image, "0"),
                    new Entry(Ktype.ColorMask02AB, Ktype.ColorMask02Tex),
                    new Entry(Ktype.ColorMask02Tex, Vtype.Image, "0"),
                    new Entry(Ktype.PaintMaskAB, Ktype.PaintMask),
                    new Entry(Ktype.PaintMask, Vtype.Image, "0"),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0"),
                    new Entry(Ktype.Detail, Vtype.Text, "0"),
                ],
                CatNo.co_panst => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.StateType, Vtype.Text, "1"),
                    new Entry(Ktype.Sex, Vtype.Text, "3"),
                    new Entry(Ktype.MainTexAB, Ktype.MainTex),
                    new Entry(Ktype.MainTex, Vtype.Image),
                    new Entry(Ktype.ColorMaskAB, Ktype.ColorMaskTex),
                    new Entry(Ktype.ColorMaskTex, Vtype.Image),
                    new Entry(Ktype.MainTex02AB, Ktype.MainTex02),
                    new Entry(Ktype.MainTex02, Vtype.Image, "0"),
                    new Entry(Ktype.ColorMask02AB, Ktype.ColorMask02Tex),
                    new Entry(Ktype.ColorMask02Tex, Vtype.Image, "0"),
                    new Entry(Ktype.PaintMaskAB, Ktype.PaintMask),
                    new Entry(Ktype.PaintMask, Vtype.Image, "0"),
                    new Entry(Ktype.KokanHide, Vtype.Text, "0"),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "99"),
                    new Entry(Ktype.Attribute, Vtype.Text, "99"),
                    new Entry(Ktype.Detail, Vtype.Text, "0/1/2/3")
                ],
                CatNo.co_shoes => [
                    new Entry(Ktype.Kind, Vtype.Text, "1"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.StateType, Vtype.Text, "1"),
                    new Entry(Ktype.ShoesType, Vtype.Text, "1"),
                    new Entry(Ktype.Sex, Vtype.Text, "1"),
                    new Entry(Ktype.MainTexAB, Ktype.MainTex),
                    new Entry(Ktype.MainTex, Vtype.Image),
                    new Entry(Ktype.ColorMaskAB, Ktype.ColorMaskTex),
                    new Entry(Ktype.ColorMaskTex, Vtype.Image),
                    new Entry(Ktype.MainTex02AB, Ktype.MainTex02),
                    new Entry(Ktype.MainTex02, Vtype.Image, "0"),
                    new Entry(Ktype.ColorMask02AB, Ktype.ColorMask02Tex),
                    new Entry(Ktype.ColorMask02Tex, Vtype.Image, "0"),
                    new Entry(Ktype.PaintMaskAB, Ktype.PaintMask),
                    new Entry(Ktype.PaintMask, Vtype.Image, "0"),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0"),
                    new Entry(Ktype.Detail, Vtype.Text, "0/1/2/3"),
                ],
                CatNo.co_shorts => [
                    new Entry(Ktype.Kind, Vtype.Text, "1"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.StateType, Vtype.Text, "0"),
                    new Entry(Ktype.Sex, Vtype.Text, "3"),
                    new Entry(Ktype.OverTopMaskAB, Ktype.OverTopMask),
                    new Entry(Ktype.OverTopMask, Vtype.Image, "0"),
                    new Entry(Ktype.OverBotMaskAB, Ktype.OverBotMask),
                    new Entry(Ktype.OverBotMask, Vtype.Image, "0"),
                    new Entry(Ktype.MainTexAB, Ktype.MainTex),
                    new Entry(Ktype.MainTex, Vtype.Image),
                    new Entry(Ktype.ColorMaskAB, Ktype.ColorMaskTex),
                    new Entry(Ktype.ColorMaskTex, Vtype.Image),
                    new Entry(Ktype.MainTex02AB, Ktype.MainTex02),
                    new Entry(Ktype.MainTex02, Vtype.Image, "0"),
                    new Entry(Ktype.ColorMask02AB, Ktype.ColorMask02Tex),
                    new Entry(Ktype.ColorMask02Tex, Vtype.Image, "0"),
                    new Entry(Ktype.PaintMaskAB, Ktype.PaintMask),
                    new Entry(Ktype.PaintMask, Vtype.Image, "0"),
                    new Entry(Ktype.KokanHide, Vtype.Text, "1"),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0/1/3"),
                    new Entry(Ktype.Detail, Vtype.Text, "0/1/2/3/4"),
                    new Entry(Ktype.Sort, Vtype.Text, "5"),
                ],
                CatNo.co_socks => [
                    new Entry(Ktype.Kind, Vtype.Text, "1"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.StateType, Vtype.Text, "1"),
                    new Entry(Ktype.SocksDent, Vtype.Text, "0"),
                    new Entry(Ktype.Sex, Vtype.Text, "3"),
                    new Entry(Ktype.MainTexAB, Ktype.MainTex),
                    new Entry(Ktype.MainTex, Vtype.Image),
                    new Entry(Ktype.ColorMaskAB, Ktype.ColorMaskTex),
                    new Entry(Ktype.ColorMaskTex, Vtype.Image),
                    new Entry(Ktype.MainTex02AB, Ktype.MainTex02),
                    new Entry(Ktype.MainTex02, Vtype.Image, "0"),
                    new Entry(Ktype.ColorMask02AB, Ktype.ColorMask02Tex),
                    new Entry(Ktype.ColorMask02Tex, Vtype.Image, "0"),
                    new Entry(Ktype.DentTexAB, Ktype.DentTex),
                    new Entry(Ktype.DentTex, Vtype.Image, "0"),
                    new Entry(Ktype.PaintMaskAB, Ktype.PaintMask),
                    new Entry(Ktype.PaintMask, Vtype.Image, "0"),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0/1/5/6"),
                    new Entry(Ktype.Detail, Vtype.Text, "0/1/2/3"),
                ],
                CatNo.co_top => [
                    new Entry(Ktype.Kind, Vtype.Text, "4"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.MainData),
                    new Entry(Ktype.MainData, Vtype.Asset),
                    new Entry(Ktype.NormalData, Vtype.Text, "0"),
                    new Entry(Ktype.StateType, Vtype.Text, "0"),
                    new Entry(Ktype.Coordinate, Vtype.Text, "0"),
                    new Entry(Ktype.NotBra, Vtype.Text, "0"),
                    new Entry(Ktype.NoShake, Vtype.Text, "0"),
                    new Entry(Ktype.Sex, Vtype.Text, "3"),
                    new Entry(Ktype.OverTopMaskAB, Ktype.OverTopMask),
                    new Entry(Ktype.OverTopMask, Vtype.Image, "0"),
                    new Entry(Ktype.OverBotMaskAB, Ktype.OverBotMask),
                    new Entry(Ktype.OverBotMask, Vtype.Image, "0"),
                    new Entry(Ktype.MainTexAB, Ktype.MainTex),
                    new Entry(Ktype.MainTex, Vtype.Image),
                    new Entry(Ktype.ColorMaskAB, Ktype.ColorMaskTex),
                    new Entry(Ktype.ColorMaskTex, Vtype.Image),
                    new Entry(Ktype.MainTex02AB, Ktype.MainTex02),
                    new Entry(Ktype.MainTex02, Vtype.Image, "0"),
                    new Entry(Ktype.ColorMask02AB, Ktype.ColorMask02Tex),
                    new Entry(Ktype.ColorMask02Tex, Vtype.Image, "0"),
                    new Entry(Ktype.MainTex03AB, Ktype.MainTex03),
                    new Entry(Ktype.MainTex03, Vtype.Image, "0"),
                    new Entry(Ktype.ColorMask03AB, Ktype.ColorMask03Tex),
                    new Entry(Ktype.ColorMask03Tex, Vtype.Image, "0"),
                    new Entry(Ktype.PaintMaskAB, Ktype.PaintMask),
                    new Entry(Ktype.PaintMask, Vtype.Image, "0"),
                    new Entry(Ktype.NipHide, Vtype.Text, "1"),
                    new Entry(Ktype.KokanHide, Vtype.Text, "0"),
                    new Entry(Ktype.HalfUndress, Vtype.Text, "0"),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0/1/3"),
                    new Entry(Ktype.Detail, Vtype.Text, "0/1/3"),
                ],
                CatNo.facepaint_layout => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.PosX, Vtype.Text),
                    new Entry(Ktype.PosY, Vtype.Text),
                    new Entry(Ktype.Rot, Vtype.Text, "0"),
                    new Entry(Ktype.Scale, Vtype.Text, "-2.5"),
                    new Entry(Ktype.CenterX, Vtype.Text, "0"),
                    new Entry(Ktype.MoveX, Vtype.Text, "0"),
                    new Entry(Ktype.CenterY, Vtype.Text, "0"),
                    new Entry(Ktype.MoveY, Vtype.Text, "0"),
                    new Entry(Ktype.CenterScale, Vtype.Text, "-2.5"),
                    new Entry(Ktype.AddScale, Vtype.Text, "3"),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na")
                ],
                CatNo.mole_layout => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.PosX, Vtype.Text),
                    new Entry(Ktype.PosY, Vtype.Text),
                    new Entry(Ktype.Rot, Vtype.Text, "0"),
                    new Entry(Ktype.Scale, Vtype.Text, "-0.8"),
                    new Entry(Ktype.CenterX, Vtype.Text, "0"),
                    new Entry(Ktype.MoveX, Vtype.Text, "0"),
                    new Entry(Ktype.CenterY, Vtype.Text, "0"),
                    new Entry(Ktype.MoveY, Vtype.Text, "0"),
                    new Entry(Ktype.CenterScale, Vtype.Text, "-0.8"),
                    new Entry(Ktype.AddScale, Vtype.Text, "1"),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na")
                ],
                CatNo.mt_body_detail => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainAB, Ktype.NormallMapDetail, Ktype.LineMask),
                    new Entry(Ktype.NormallMapDetail, Vtype.Image),
                    new Entry(Ktype.LineMask, Vtype.Image),
                    new Entry(Ktype.Sex, Vtype.Text, "3"),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0"),
                    new Entry(Ktype.Detail, Vtype.Text, "0")
                ],
                CatNo.mt_body_paint => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainAB, Ktype.PaintTex),
                    new Entry(Ktype.PaintTex, Vtype.Image),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0"),
                    new Entry(Ktype.Detail, Vtype.Text, "0")
                ],
                CatNo.mt_cheek => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainAB, Ktype.CheekTex),
                    new Entry(Ktype.CheekTex, Vtype.Image),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0/1/2"),
                    new Entry(Ktype.Detail, Vtype.Text, "0"),
                ],
                CatNo.mt_eye => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainAB, Ktype.EyeTex),
                    new Entry(Ktype.EyeTex, Vtype.Image),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0"),
                    new Entry(Ktype.Detail, Vtype.Text, "0"),
                ],
                CatNo.mt_eye_gradation => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.ColorMaskAB, Ktype.ColorMaskTex),
                    new Entry(Ktype.ColorMaskTex, Vtype.Image),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0"),
                    new Entry(Ktype.Detail, Vtype.Text, "0")
                ],
                CatNo.mt_eye_hi_up => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainAB, Ktype.EyeHiUpTex),
                    new Entry(Ktype.EyeHiUpTex, Vtype.Image),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0"),
                    new Entry(Ktype.Detail, Vtype.Text, "0"),
                ],
                CatNo.mt_eye_white => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainAB, Ktype.EyeWhiteTex),
                    new Entry(Ktype.EyeWhiteTex, Vtype.Image),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0"),
                    new Entry(Ktype.Detail, Vtype.Text, "0"),
                ],
                CatNo.mt_eyebrow => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainAB, Ktype.EyebrowTex),
                    new Entry(Ktype.EyebrowTex, Vtype.Image),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0"),
                    new Entry(Ktype.Detail, Vtype.Text, "0")
                ],
                CatNo.mt_eyelid => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainAB, Ktype.EyelidTex),
                    new Entry(Ktype.EyelidTex, Vtype.Image),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0"),
                    new Entry(Ktype.Detail, Vtype.Text, "0")
                ],
                CatNo.mt_eyeline_down => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainAB, Ktype.EyelineDownTex),
                    new Entry(Ktype.EyelineDownTex, Vtype.Image),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0"),
                    new Entry(Ktype.Detail, Vtype.Text, "0")
                ],
                CatNo.mt_eyeline_up => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainAB, Ktype.EyelineUpTex, Ktype.EyelineShadowTex),
                    new Entry(Ktype.EyelineUpTex, Vtype.Image),
                    new Entry(Ktype.EyelineShadowTex, Vtype.Image, "0"),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0"),
                    new Entry(Ktype.Detail, Vtype.Text, "0")
                ],
                CatNo.mt_eyepipil => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainAB, Ktype.EyepipilTex),
                    new Entry(Ktype.EyepipilTex, Vtype.Image),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0"),
                    new Entry(Ktype.Detail, Vtype.Text, "0")
                ],
                CatNo.mt_eyeshadow => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainAB, Ktype.EyeshadowTex),
                    new Entry(Ktype.EyeshadowTex, Vtype.Image),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0/1/2"),
                    new Entry(Ktype.Detail, Vtype.Text, "0")
                ],
                CatNo.mt_face_detail => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainAB, Ktype.NormallMapDetail, Ktype.LineMask),
                    new Entry(Ktype.NormallMapDetail, Vtype.Image, "0"),
                    new Entry(Ktype.LineMask, Vtype.Image),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "99"),
                    new Entry(Ktype.Attribute, Vtype.Text, "99"),
                    new Entry(Ktype.Detail, Vtype.Text, "99")
                ],
                CatNo.mt_face_paint => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainAB, Ktype.PaintTex),
                    new Entry(Ktype.PaintTex, Vtype.Image),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0"),
                    new Entry(Ktype.Detail, Vtype.Text, "0")
                ],
                CatNo.mt_hairgloss => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainTexAB, Ktype.MainTex),
                    new Entry(Ktype.MainTex, Vtype.Image),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Detail, Vtype.Text, "0/1/2/3/4/5")
                ],
                CatNo.mt_lip => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainAB, Ktype.LipTex),
                    new Entry(Ktype.LipTex, Vtype.Image),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "2/3"),
                    new Entry(Ktype.Detail, Vtype.Text, "0")
                ],
                CatNo.mt_lipline => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainAB, Ktype.LiplineTex),
                    new Entry(Ktype.LiplineTex, Vtype.Image),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "1/2/3/4"),
                    new Entry(Ktype.Detail, Vtype.Text, "0")
                ],
                CatNo.mt_mole => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainAB, Ktype.MoleTex),
                    new Entry(Ktype.MoleTex, Vtype.Image),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.MoleLayoutID, Vtype.Text, "0"),
                    new Entry(Ktype.Image, Vtype.Text, "99"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0"),
                    new Entry(Ktype.Detail, Vtype.Text, "0")
                ],
                CatNo.mt_nip => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainAB, Ktype.NipTex),
                    new Entry(Ktype.NipTex, Vtype.Image),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0"),
                    new Entry(Ktype.Detail, Vtype.Text, "0")
                ],
                CatNo.mt_nose => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainAB, Ktype.NoseTex),
                    new Entry(Ktype.NoseTex, Vtype.Image),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0"),
                    new Entry(Ktype.Detail, Vtype.Text, "0")
                ],
                CatNo.mt_pattern => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainTexAB, Ktype.MainTex),
                    new Entry(Ktype.MainTex, Vtype.Image),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0"),
                    new Entry(Ktype.Detail, Vtype.Text, "0")
                ],
                CatNo.mt_ramp => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainManifest, Vtype.Text, "abdata"),
                    new Entry(Ktype.MainAB, Ktype.LampTex),
                    new Entry(Ktype.MainTexAB, Ktype.MainTex),
                    new Entry(Ktype.MainTex, Vtype.Image, "0"),
                    new Entry(Ktype.MainTex02AB, Ktype.MainTex02),
                    new Entry(Ktype.MainTex02, Vtype.Image, "0"),
                    new Entry(Ktype.MainTex03AB, Ktype.MainTex03),
                    new Entry(Ktype.MainTex03, Vtype.Image, "0"),
                    new Entry(Ktype.ColorMaskAB, Ktype.ColorMaskTex),
                    new Entry(Ktype.ColorMaskTex, Vtype.Image, "0"),
                    new Entry(Ktype.ColorMask02AB, Ktype.ColorMask02Tex),
                    new Entry(Ktype.ColorMask02Tex, Vtype.Image, "0"),
                    new Entry(Ktype.LampTexAB, Vtype.Text, "0"),
                    new Entry(Ktype.LampTex, Vtype.Image),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                ],
                CatNo.mt_sunburn => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainAB, Ktype.SunburnTex),
                    new Entry(Ktype.SunburnTex, Vtype.Image),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "1"),
                    new Entry(Ktype.Detail, Vtype.Text, "0")
                ],
                CatNo.mt_underhair => [
                    new Entry(Ktype.Kind, Vtype.Text, "0"),
                    new Entry(Ktype.Possess, Vtype.Text, "1"),
                    new Entry(Ktype.Name, Vtype.Name),
                    new Entry(Ktype.MainAB, Ktype.UnderhairTex),
                    new Entry(Ktype.UnderhairTex, Vtype.Image),
                    new Entry(Ktype.ThumbAB, Ktype.ThumbTex),
                    new Entry(Ktype.ThumbTex, Vtype.Image, "thumb_na"),
                    new Entry(Ktype.Image, Vtype.Text, "0/1/2/3/4/5"),
                    new Entry(Ktype.Attribute, Vtype.Text, "0"),
                    new Entry(Ktype.Detail, Vtype.Text, "0")
                ],
                _ => []
            }
        }).Where(category => category.Entries.Length > 0);
    internal static void WrapMode(this string[] path, Texture2D t2d) =>
        (t2d.wrapModeU, t2d.wrapModeV, t2d.wrapModeW) = (path[0], path[^1]) switch
        {
            (_, "RepeatRepeatRepeat.png") =>
                (TextureWrapMode.Repeat, TextureWrapMode.Repeat, TextureWrapMode.Repeat),
            (_, "RepeatRepeatClamp.png") =>
                (TextureWrapMode.Repeat, TextureWrapMode.Repeat, TextureWrapMode.Clamp),
            (_, "RepeatRepeatMirror.png") =>
                (TextureWrapMode.Repeat, TextureWrapMode.Repeat, TextureWrapMode.Mirror),
            (_, "RepeatRepeatMirrorOnce.png") =>
                (TextureWrapMode.Repeat, TextureWrapMode.Repeat, TextureWrapMode.MirrorOnce),

            (_, "RepeatClampRepeat.png") =>
                (TextureWrapMode.Repeat, TextureWrapMode.Clamp, TextureWrapMode.Repeat),
            (_, "RepeatClampClamp.png") =>
                (TextureWrapMode.Repeat, TextureWrapMode.Clamp, TextureWrapMode.Clamp),
            (_, "RepeatClampMirror.png") =>
                (TextureWrapMode.Repeat, TextureWrapMode.Clamp, TextureWrapMode.Mirror),
            (_, "RepeatClampMirrorOnce.png") =>
                (TextureWrapMode.Repeat, TextureWrapMode.Clamp, TextureWrapMode.MirrorOnce),

            (_, "RepeatMirrorRepeat.png") =>
                (TextureWrapMode.Repeat, TextureWrapMode.Mirror, TextureWrapMode.Repeat),
            (_, "RepeatMirrorClamp.png") =>
                (TextureWrapMode.Repeat, TextureWrapMode.Mirror, TextureWrapMode.Clamp),
            (_, "RepeatMirrorMirror.png") =>
                (TextureWrapMode.Repeat, TextureWrapMode.Mirror, TextureWrapMode.Mirror),
            (_, "RepeatMirrorMirrorOnce.png") =>
                (TextureWrapMode.Repeat, TextureWrapMode.Mirror, TextureWrapMode.MirrorOnce),

            (_, "RepeatMirrorOnceRepeat.png") =>
                (TextureWrapMode.Repeat, TextureWrapMode.MirrorOnce, TextureWrapMode.Repeat),
            (_, "RepeatMirrorOnceClamp.png") =>
                (TextureWrapMode.Repeat, TextureWrapMode.MirrorOnce, TextureWrapMode.Clamp),
            (_, "RepeatMirrorOnceMirror.png") =>
                (TextureWrapMode.Repeat, TextureWrapMode.MirrorOnce, TextureWrapMode.Mirror),
            (_, "RepeatMirrorOnceMirrorOnce.png") =>
                (TextureWrapMode.Repeat, TextureWrapMode.MirrorOnce, TextureWrapMode.MirrorOnce),

            (_, "ClampRepeatRepeat.png") =>
                (TextureWrapMode.Clamp, TextureWrapMode.Repeat, TextureWrapMode.Repeat),
            (_, "ClampRepeatClamp.png") =>
                (TextureWrapMode.Clamp, TextureWrapMode.Repeat, TextureWrapMode.Clamp),
            (_, "ClampRepeatMirror.png") =>
                (TextureWrapMode.Clamp, TextureWrapMode.Repeat, TextureWrapMode.Mirror),
            (_, "ClampRepeatMirrorOnce.png") =>
                (TextureWrapMode.Clamp, TextureWrapMode.Repeat, TextureWrapMode.MirrorOnce),

            (_, "ClampClampRepeat.png") =>
                (TextureWrapMode.Clamp, TextureWrapMode.Clamp, TextureWrapMode.Repeat),
            (_, "ClampClampClamp.png") =>
                (TextureWrapMode.Clamp, TextureWrapMode.Clamp, TextureWrapMode.Clamp),
            (_, "ClampClampMirror.png") =>
                (TextureWrapMode.Clamp, TextureWrapMode.Clamp, TextureWrapMode.Mirror),
            (_, "ClampClampMirrorOnce.png") =>
                (TextureWrapMode.Clamp, TextureWrapMode.Clamp, TextureWrapMode.MirrorOnce),

            (_, "ClampMirrorRepeat.png") =>
                (TextureWrapMode.Clamp, TextureWrapMode.Mirror, TextureWrapMode.Repeat),
            (_, "ClampMirrorClamp.png") =>
                (TextureWrapMode.Clamp, TextureWrapMode.Mirror, TextureWrapMode.Clamp),
            (_, "ClampMirroMirror.png") =>
                (TextureWrapMode.Clamp, TextureWrapMode.Mirror, TextureWrapMode.Mirror),
            (_, "ClampMirroMirrorOnce.png") =>
                (TextureWrapMode.Clamp, TextureWrapMode.Mirror, TextureWrapMode.MirrorOnce),

            (_, "ClampMirrorOnceRepeat.png") =>
                (TextureWrapMode.Clamp, TextureWrapMode.MirrorOnce, TextureWrapMode.Repeat),
            (_, "ClampMirrorOnceClamp.png") =>
                (TextureWrapMode.Clamp, TextureWrapMode.MirrorOnce, TextureWrapMode.Clamp),
            (_, "ClampMirroOnceMirror.png") =>
                (TextureWrapMode.Clamp, TextureWrapMode.MirrorOnce, TextureWrapMode.Mirror),
            (_, "ClampMirroOnceMirrorOnce.png") =>
                (TextureWrapMode.Clamp, TextureWrapMode.MirrorOnce, TextureWrapMode.MirrorOnce),

            (_, "MirrorRepeatRepeat.png") =>
                (TextureWrapMode.Mirror, TextureWrapMode.Repeat, TextureWrapMode.Repeat),
            (_, "MirrorRepeatClamp.png") =>
                (TextureWrapMode.Mirror, TextureWrapMode.Repeat, TextureWrapMode.Clamp),
            (_, "MirrorRepeatMirror.png") =>
                (TextureWrapMode.Mirror, TextureWrapMode.Repeat, TextureWrapMode.Mirror),
            (_, "MirrorRepeatMirrorOnce.png") =>
                (TextureWrapMode.Mirror, TextureWrapMode.Repeat, TextureWrapMode.MirrorOnce),

            (_, "MirrorClampRepeat.png") =>
                (TextureWrapMode.Mirror, TextureWrapMode.Clamp, TextureWrapMode.Repeat),
            (_, "MirrorClampClamp.png") =>
                (TextureWrapMode.Mirror, TextureWrapMode.Clamp, TextureWrapMode.Clamp),
            (_, "MirrorClampMirror.png") =>
                (TextureWrapMode.Mirror, TextureWrapMode.Clamp, TextureWrapMode.Mirror),
            (_, "MirrorClampMirrorOnce.png") =>
                (TextureWrapMode.Mirror, TextureWrapMode.Clamp, TextureWrapMode.MirrorOnce),

            (_, "MirrorMirrorRepeat.png") =>
                (TextureWrapMode.Mirror, TextureWrapMode.Mirror, TextureWrapMode.Repeat),
            (_, "MirrorMirrorClamp.png") =>
                (TextureWrapMode.Mirror, TextureWrapMode.Mirror, TextureWrapMode.Clamp),
            (_, "MirrorMirroMirror.png") =>
                (TextureWrapMode.Mirror, TextureWrapMode.Mirror, TextureWrapMode.Mirror),
            (_, "MirrorMirroMirrorOnce.png") =>
                (TextureWrapMode.Mirror, TextureWrapMode.Mirror, TextureWrapMode.MirrorOnce),

            (_, "MirrorMirrorOnceRepeat.png") =>
                (TextureWrapMode.Mirror, TextureWrapMode.MirrorOnce, TextureWrapMode.Repeat),
            (_, "MirrorMirrorOnceClamp.png") =>
                (TextureWrapMode.Mirror, TextureWrapMode.MirrorOnce, TextureWrapMode.Clamp),
            (_, "MirrorMirroOnceMirror.png") =>
                (TextureWrapMode.Mirror, TextureWrapMode.MirrorOnce, TextureWrapMode.Mirror),
            (_, "MirrorMirroOnceMirrorOnce.png") =>
                (TextureWrapMode.Mirror, TextureWrapMode.MirrorOnce, TextureWrapMode.MirrorOnce),

            (_, "MirrorOnceRepeatRepeat.png") =>
                (TextureWrapMode.MirrorOnce, TextureWrapMode.Repeat, TextureWrapMode.Repeat),
            (_, "MirrorOnceRepeatClamp.png") =>
                (TextureWrapMode.MirrorOnce, TextureWrapMode.Repeat, TextureWrapMode.Clamp),
            (_, "MirrorOnceRepeatMirror.png") =>
                (TextureWrapMode.MirrorOnce, TextureWrapMode.Repeat, TextureWrapMode.Mirror),
            (_, "MirrorOnceRepeatMirrorOnce.png") =>
                (TextureWrapMode.MirrorOnce, TextureWrapMode.Repeat, TextureWrapMode.MirrorOnce),

            (_, "MirrorOnceClampRepeat.png") =>
                (TextureWrapMode.MirrorOnce, TextureWrapMode.Clamp, TextureWrapMode.Repeat),
            (_, "MirrorOnceClampClamp.png") =>
                (TextureWrapMode.MirrorOnce, TextureWrapMode.Clamp, TextureWrapMode.Clamp),
            (_, "MirrorOnceClampMirror.png") =>
                (TextureWrapMode.MirrorOnce, TextureWrapMode.Clamp, TextureWrapMode.Mirror),
            (_, "MirrorOnceClampMirrorOnce.png") =>
                (TextureWrapMode.MirrorOnce, TextureWrapMode.Clamp, TextureWrapMode.MirrorOnce),

            (_, "MirrorOnceMirrorRepeat.png") =>
                (TextureWrapMode.MirrorOnce, TextureWrapMode.Mirror, TextureWrapMode.Repeat),
            (_, "MirrorOnceMirrorClamp.png") =>
                (TextureWrapMode.MirrorOnce, TextureWrapMode.Mirror, TextureWrapMode.Clamp),
            (_, "MirrorOnceMirroMirror.png") =>
                (TextureWrapMode.MirrorOnce, TextureWrapMode.Mirror, TextureWrapMode.Mirror),
            (_, "MirrorOnceMirroMirrorOnce.png") =>
                (TextureWrapMode.MirrorOnce, TextureWrapMode.Mirror, TextureWrapMode.MirrorOnce),

            (_, "MirrorOnceMirrorOnceRepeat.png") =>
                (TextureWrapMode.MirrorOnce, TextureWrapMode.MirrorOnce, TextureWrapMode.Repeat),
            (_, "MirrorOnceMirrorOnceClamp.png") =>
                (TextureWrapMode.MirrorOnce, TextureWrapMode.MirrorOnce, TextureWrapMode.Clamp),
            (_, "MirrorOnceMirroOnceMirror.png") =>
                (TextureWrapMode.MirrorOnce, TextureWrapMode.MirrorOnce, TextureWrapMode.Mirror),
            (_, "MirrorOnceMirroOnceMirrorOnce.png") =>
                (TextureWrapMode.MirrorOnce, TextureWrapMode.MirrorOnce, TextureWrapMode.MirrorOnce),

            (_, "ThumbTex.png") => (TextureWrapMode.Clamp, TextureWrapMode.Clamp, TextureWrapMode.Clamp),
            ("bo_body", _) or
            ("bo_head", _) or
            ("co_bot", _) or
            ("co_bra", _) or
            ("co_gloves", _) or
            ("co_panst", _) or
            ("co_shoes", _) or
            ("co_shorts", _) or
            ("co_socks", _) or
            ("co_top", _) or
            ("mt_body_detail", _) or
            ("mt_eyeshadow", _) or
            ("mt_face_detail", _) or
            ("mt_sunburn", _) or
            ("mt_pattern", _) => (TextureWrapMode.Repeat, TextureWrapMode.Repeat, TextureWrapMode.Repeat),
            _ => (TextureWrapMode.Clamp, TextureWrapMode.Clamp, TextureWrapMode.Clamp)
        };
    }
}
