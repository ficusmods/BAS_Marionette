using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;
using ThunderRoad;
using Chabuk.ManikinMono;

using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Marionette
{
    [HarmonyPatch(typeof(UnityEngine.ResourceManagement.ResourceManager))]
    [HarmonyPatch("ProvideResource")]
    [HarmonyPatch(new Type[] { typeof(IResourceLocation), typeof(Type), typeof(bool) })]
    public static class UnityResourceLoadPatch
    {
        public static bool IsPartVersionCompatible(MarionetteGroupPartProxy part)
        {
            return (part.versionMajor == Config.versionMajor);
        }

        public static void CreateManikinPartFromProxy(MarionetteGroupPartProxy rootPart)
        {
            ManikinPart[] parts = new ManikinPart[rootPart.parts.Length];
            List<ManikinGroupPart.PartLOD> partLODs = new List<ManikinGroupPart.PartLOD>();

            for (int i = 0; i < rootPart.parts.Length; i++)
            {
                int success = 1;

                MarionetteSmrPartProxy proxySmrPart = rootPart.parts[i];
                ManikinSmrPart manikinSmrPart = proxySmrPart.gameObject.AddComponent<ManikinSmrPart>();
                success &= FieldAccess.Write(manikinSmrPart, "smr", proxySmrPart.smr) ? 1 : 0;
                success &= FieldAccess.Write(manikinSmrPart, "rootBoneHash", proxySmrPart.rootBoneHash) ? 1 : 0;
                success &= FieldAccess.Write(manikinSmrPart, "boneNameHashes", proxySmrPart.boneNameHashes) ? 1 : 0;
                success &= FieldAccess.Write(manikinSmrPart, "weightedBoneNameHashes", proxySmrPart.weightedBoneNameHashes) ? 1 : 0;

                if (success == 1)
                {
                    ManikinGroupPart.PartLOD partLOD = new ManikinGroupPart.PartLOD();
                    partLOD.renderers = new List<Renderer>();
                    partLOD.renderers.Add(proxySmrPart.smr);
                    partLODs.Add(partLOD);
                    parts[i] = manikinSmrPart;
                    Logger.Basic("ManikinSmrPart created from adapter proxy: {0}", proxySmrPart.name);
                }
                else
                {
                    Logger.Basic("Something went wrong while handling proxy: {0}", proxySmrPart.name);
                }
            }

            ManikinGroupPart manikinRootPart = rootPart.gameObject.AddComponent<ManikinGroupPart>();
            if (FieldAccess.Write(manikinRootPart, "parts", parts))
            {
                manikinRootPart.copyLastLodToAnySuperiorLOD = rootPart.copyLastLodToAnySuperiorLOD;
                manikinRootPart.partLODs = partLODs;

                Logger.Basic("ManikinGroupPart created from adapter proxy: {0}", rootPart.name);
            }
            else
            {
                Logger.Basic("Something went wrong during handling {0}", rootPart.name);
            }
        }

        public static void TryCreateManikinPart(GameObject obj)
        {
            if (obj.TryGetComponent<ManikinPart>(out _)) return;
            MarionetteGroupPartProxy rootPart;
            if (!obj.TryGetComponent<MarionetteGroupPartProxy>(out rootPart)) return;

            if (IsPartVersionCompatible(rootPart))
            {
                Logger.Basic("Creating Manikin part from proxy: {0}", rootPart.name);
                CreateManikinPartFromProxy(rootPart);
            }
            else
            {
                Logger.Basic("Part version incompatible: {0} [v{1}.{2}]", rootPart.name, rootPart.versionMajor, rootPart.versionMinor);
            }
        }

        static void Postfix(ref AsyncOperationHandle __result)
        {
            __result.Completed += Load_Completed;
        }

        private static void Load_Completed(AsyncOperationHandle obj)
        {
            if (obj.Status != AsyncOperationStatus.Failed)
            {
                if (obj.Result.GetType().IsSubclassOf(typeof(GameObject)) || obj.Result.GetType() == typeof(GameObject))
                {
                    TryCreateManikinPart((GameObject)obj.Result);
                }
            }
        }
    }
}
