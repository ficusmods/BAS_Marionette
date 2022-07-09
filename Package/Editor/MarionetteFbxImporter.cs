using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;

using UnityEditor;
using NJ = Newtonsoft.Json;
using NJL = Newtonsoft.Json.Linq;

namespace Marionette
{
    public class FbxToProxyPartImporter : EditorWindow
    {
        
        GameObject rootObj = null;
        public string fbxPath = "FBX file";

        // Key: transform name
        private Dictionary<string, GameObject> lodParts;
        private List<GameObject>[] lodGroups; // LOD groups 0-9
        List<MarionetteSmrPartProxy> smrProxies = new List<MarionetteSmrPartProxy>();

        public enum RevealType
        {
            Flesh,
            Dent
        }

        class RevealSettings
        {
            public string name = "Material";
            public RevealType revealType = RevealType.Flesh;
            public Texture partBaseMap = null;
            public Texture partBumpMap = null;
            public Texture partMetallicGlossMap = null;
        }

        enum MeshPartMaterial
        {
            Custom,
            Flesh,
            Leather,
            Chainmail,
            Plate
        }

        class PartSettings
        {
            public ThunderRoad.RevealDecal.Type partType = ThunderRoad.RevealDecal.Type.Outfit;
            public bool addMeshPart = false;
            public MeshPartMaterial meshPartMaterialType = MeshPartMaterial.Custom;
            public bool addReveal = false;
            public List<RevealSettings> revealSettings = new List<RevealSettings>();
            public MarionettePropertiesProxy.ColorProperty watchedManikinProperties = MarionettePropertiesProxy.ColorProperty.None;
        }

        PartSettings partSettings = new PartSettings();

        [MenuItem("MarionetteSDK/FBX2ProxyParts")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(FbxToProxyPartImporter));
        }

        public string GetProjFolder()
        {
            Type projectWindowUtilType = typeof(ProjectWindowUtil);
            MethodInfo getActiveFolderPath = projectWindowUtilType.GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);
            if (getActiveFolderPath == null) return null;
            object obj = getActiveFolderPath.Invoke(null, new object[0]);
            if (obj == null) return null;
            return obj.ToString();
        }

        public void InitializeObjectInfo()
        {
            lodParts = new Dictionary<string, GameObject>();
            lodGroups = new List<GameObject>[10];
            for(int i=0; i < lodGroups.Length; i++)
            {
                lodGroups[i] = new List<GameObject>();
            }

            foreach (Transform child in rootObj.transform)
            {
                Regex rgx = new Regex(@"_LOD([0-9])", RegexOptions.IgnoreCase);
                MatchCollection matches = rgx.Matches(child.name);
                if (matches.Count > 0)
                {
                    lodParts.Add(child.name, child.gameObject);
                    Int32.TryParse(matches[0].Groups[1].ToString(), out int lodGroup);
                    lodGroups[lodGroup].Add(child.gameObject);
                }
            }
        }

        public string GetPartsDataFilePath()
        {
            return fbxPath + ".parts";
        }

        public static string GetBinaryPath()
        {
            return Application.dataPath + "/MarionetteSDK/Bin/Fbx2PartsJson.exe";
        }

        public void GeneratePartsFile()
        {
            UnityEngine.Debug.Log("Generating parts data for object: " + rootObj.transform.name);
            Process.Start(GetBinaryPath(), fbxPath).WaitForExit();
        }
        
        public T ReCreateComponent<T>(GameObject obj) where T : MonoBehaviour
        {
          if (obj.TryGetComponent<T>(out T existingComponent))
          {
              GameObject.DestroyImmediate(existingComponent);
          }
          return obj.AddComponent<T>();
        }
        
        public bool TryRemoveComponent<T>(GameObject obj) where T : MonoBehaviour
        {
          if (obj.TryGetComponent<T>(out T existingComponent))
          {
              GameObject.DestroyImmediate(existingComponent);
              return true;
          }
          return false;
        }

        public SkinnedMeshRenderer GetHighestLodRenderer()
        {
            for (int i = lodGroups.Length - 1; i >= 0 ; i--)
            {
                if (lodGroups[i].Count > 0)
                {
                    return lodGroups[i][0].GetComponent<SkinnedMeshRenderer>();
                }
            }
            return null;
        }
        
        public MarionetteGroupPartProxy CreateGroupPartProxy(GameObject obj, List<MarionetteSmrPartProxy> smrProxies)
        {
          MarionetteGroupPartProxy ret = ReCreateComponent<MarionetteGroupPartProxy>(obj);
          ret.parts = smrProxies.ToArray();
          for(int i=0; i < lodGroups.Length; i++)
           {
               if (lodGroups[i].Count > 0)
               {
                   MarionetteGroupPartProxy.PartLOD partLod = new MarionetteGroupPartProxy.PartLOD();
                   partLod.renderers = new List<Renderer>();
                   foreach (GameObject smrPart in lodGroups[i])
                   {
                       partLod.renderers.Add(smrPart.GetComponent<SkinnedMeshRenderer>());
                   }
                   ret.partLODs.Add(partLod);
               }
           }
          return ret;
        }
        
        public List<MarionetteSmrPartProxy> CreateMarionetteSmrProxies(Dictionary<GameObject, NJL.JToken> smrProxyData)
        {
            List<MarionetteSmrPartProxy> ret = new List<MarionetteSmrPartProxy>();
            foreach (var pair in smrProxyData)
            {
                GameObject partObj = pair.Key;
                NJL.JToken jsonData = pair.Value;
                MarionetteSmrPartProxy smrProxy = ReCreateComponent<MarionetteSmrPartProxy>(partObj);
                smrProxy.smr = partObj.GetComponent<SkinnedMeshRenderer>();
                smrProxy.rootBoneName = jsonData["bones"][0]["name"].ToString();
                List<MarionetteSmrPartProxy.BoneInfo> partBones = new List<MarionetteSmrPartProxy.BoneInfo>();
                foreach (NJL.JToken jBone in (NJL.JArray)jsonData["bones"])
                {
                    MarionetteSmrPartProxy.BoneInfo boneInfo = new MarionetteSmrPartProxy.BoneInfo();
                    boneInfo.name = jBone["name"].ToString();
                    boneInfo.weighted = false;
                    if ((bool)jBone["hasWeights"].ToObject(typeof(bool)))
                    {
                        boneInfo.weighted = true;
                    }
                    partBones.Add(boneInfo);
                }
                smrProxy.bones = partBones.ToArray();
                smrProxy.RecalculateHashes();
                ret.Add(smrProxy);
            }
            return ret;
        }

        public bool CreateProxyObjects()
        {
            if (!System.IO.File.Exists(GetPartsDataFilePath()))
            {
                UnityEngine.Debug.LogError("Parts data file " + GetPartsDataFilePath() + " missing. The FBX file is probably badly structured." +
                    "Try executing Assets/MarionetteSDK/Bin/Fbx2PartsJson.exe <fbxPath> manually.");
                return false;
            }

            UnityEngine.Debug.Log("Generating parts for object: " + rootObj.transform.name);
            
            NJL.JArray j = NJL.JArray.Parse(File.ReadAllText(GetPartsDataFilePath()));
            if(!ValidatePartsData(j))
            {
                UnityEngine.Debug.LogError("Mismatching info in GameObject and parts data. Probably using wrong FBX file.");
                return false;
            }
            
            Dictionary<GameObject, NJL.JToken> smrProxyData = new Dictionary<GameObject, NJL.JToken>();
            foreach (NJL.JToken child in (NJL.JArray)j)
            {
              smrProxyData.Add(lodParts[child["name"].ToString()], child);
            }

            smrProxies = CreateMarionetteSmrProxies(smrProxyData);
            MarionetteGroupPartProxy groupProxy = CreateGroupPartProxy(rootObj, smrProxies);
            return true;
        }

        public bool ValidatePartsData(NJL.JArray j)
        {
            if(j.Count != lodParts.Count)
            {
                UnityEngine.Debug.Log("Mismatching LOD part count on GameObject and PartsData");
                return false;
            }
            
            foreach(NJL.JObject child in j)
            {
                if(!lodParts.ContainsKey(child["name"].ToString()))
                {
                    UnityEngine.Debug.Log(child["name"].ToString() + " missing from GameObject");
                    return false;
                }
            }
            return true;
        }

        public void CreateMeshPart()
        {
            ThunderRoad.MeshPart meshPart = ReCreateComponent<ThunderRoad.MeshPart>(rootObj);
            meshPart.skinnedMeshRenderer = GetHighestLodRenderer();
            switch (partSettings.meshPartMaterialType)
            {
                case MeshPartMaterial.Custom:
                    break;
                case MeshPartMaterial.Flesh:
                    meshPart.defaultPhysicMaterial = (PhysicMaterial)AssetDatabase.LoadAssetAtPath("Assets/SDK/PhysicMaterials/Flesh.physicMaterial", typeof(PhysicMaterial));
                    meshPart.idMap = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/MarionetteSDK/MaterialIDMaps/IdMapFlesh.png", typeof(Texture2D));
                    break;
                case MeshPartMaterial.Leather:
                    meshPart.defaultPhysicMaterial = (PhysicMaterial)AssetDatabase.LoadAssetAtPath("Assets/SDK/PhysicMaterials/Leather.physicMaterial", typeof(PhysicMaterial));
                    meshPart.idMap = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/MarionetteSDK/MaterialIDMaps/IdMapLeather.png", typeof(Texture2D));
                    break;
                case MeshPartMaterial.Chainmail:
                    meshPart.defaultPhysicMaterial = (PhysicMaterial)AssetDatabase.LoadAssetAtPath("Assets/SDK/PhysicMaterials/Chainmail.physicMaterial", typeof(PhysicMaterial));
                    meshPart.idMap = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/MarionetteSDK/MaterialIDMaps/IdMapChainmail.png", typeof(Texture2D));
                    break;
                case MeshPartMaterial.Plate:
                    meshPart.defaultPhysicMaterial = (PhysicMaterial)AssetDatabase.LoadAssetAtPath("Assets/SDK/PhysicMaterials/Plate.physicMaterial", typeof(PhysicMaterial));
                    meshPart.idMap = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/MarionetteSDK/MaterialIDMaps/IdMapPlate.png", typeof(Texture2D));
                    break;
                default:
                    break;
            }
        }

        public void CreateRevealDecals()
        {
            foreach(MarionetteSmrPartProxy smrProxy in smrProxies)
            {
                ThunderRoad.RevealDecal revealDecal = ReCreateComponent<ThunderRoad.RevealDecal>(smrProxy.gameObject);
                revealDecal.type = partSettings.partType;
            }
        }

        public void CreateRevealMaterials()
        {
            foreach (RevealSettings settings in partSettings.revealSettings)
            {
                if (String.IsNullOrEmpty(settings.name)) return;

                Texture baseMap = settings.partBaseMap;
                Texture bumpMap = settings.partBumpMap;
                Texture metallicGlossMap = settings.partMetallicGlossMap;
                Texture fleshReveal = (Texture2D)AssetDatabase.LoadMainAssetAtPath("Assets/Examples/Creatures/Source/Revealed_Flesh_c.png");
                Texture burnReveal = (Texture2D)AssetDatabase.LoadMainAssetAtPath("Assets/Examples/Weapons/Source/Revealed_Burn_c.png");
                Texture burnRevealNormal = (Texture2D)AssetDatabase.LoadMainAssetAtPath("Assets/Examples/Weapons/Source/Revealed_Burn_n.png");

                int flag = 1;
                flag &= (fleshReveal != null) ? 1 : 0;
                flag &= (burnReveal != null) ? 1 : 0;
                flag &= (burnRevealNormal != null) ? 1 : 0;
                if (flag == 0)
                {
                    UnityEngine.Debug.LogWarning("One of the needed reveal textures are missing. Your SDK might not be set up properly.");
                    return;
                }

                flag &= (baseMap != null) ? 1 : 0;
                flag &= (bumpMap != null) ? 1 : 0;
                
                if (flag == 0)
                {
                    UnityEngine.Debug.LogWarning(String.Format("Please assign the textures on {0}", settings.name));
                    return;
                }

                Material material = new Material(Shader.Find("ThunderRoad/Lit"));

                material.SetFloat("_UseReveal", 1.0f);
                material.SetFloat("_UseVertexOcclusion", 1.0f);
                material.SetFloat("_Smoothness", 0.5f);

                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                material.EnableKeyword("_NORMALMAP");
                material.EnableKeyword("_REVEALLAYERS");
                material.EnableKeyword("_VERTEXOCCLUSION_ON");

                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                material.renderQueue = 2000;
                material.SetOverrideTag("RenderType", "Opaque");

                material.SetTexture("_BaseMap", baseMap);
                material.SetTexture("_BumpMap", bumpMap);
                material.SetTexture("_MetallicGlossMap", metallicGlossMap);
                material.SetTexture("_Layer0", fleshReveal);
                material.SetTexture("_Layer0NormalMap", bumpMap);
                material.SetTexture("_Layer1", burnReveal);
                material.SetTexture("_Layer1NormalMap", burnRevealNormal);

                if (settings.revealType == RevealType.Dent)
                {
                    material.SetFloat("_Layer2Height", -10.0f);
                }
                else
                {
                    material.SetFloat("_Layer2Height", -3.0f);
                }


                string projFolder = GetProjFolder();
                if (projFolder != null)
                {
                    string output = String.Format("{0}/{1}.mat", projFolder, settings.name);
                    AssetDatabase.DeleteAsset(output);
                    AssetDatabase.CreateAsset(material, output);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        public void CreateMaterialInstances()
        {
            foreach (MarionetteSmrPartProxy smrProxy in smrProxies)
            {
                ThunderRoad.Plugins.MaterialInstance materialInstance = ReCreateComponent<ThunderRoad.Plugins.MaterialInstance>(smrProxy.gameObject);
            }
        }

        public void CreatePropertiesProxy()
        {
            if (partSettings.watchedManikinProperties == MarionettePropertiesProxy.ColorProperty.None) return;

            foreach (MarionetteSmrPartProxy smrProxy in smrProxies)
            {
                MarionettePropertiesProxy propertiesProxy = ReCreateComponent<MarionettePropertiesProxy>(smrProxy.gameObject);
                propertiesProxy.watchedManikinProperties = partSettings.watchedManikinProperties;
            }
        }

        public void RemoveAnyLodGroups()
        {
            LODGroup lodGroup;
            if (rootObj.TryGetComponent<LODGroup>(out lodGroup))
            {
                GameObject.DestroyImmediate(lodGroup);
            }
        }

        public void DrawObjectSelectorSection()
        {
            rootObj = (GameObject)EditorGUILayout.ObjectField(rootObj, typeof(GameObject), true);
        }

        public void DrawFbxSelectorSection()
        {
            fbxPath = GUILayout.TextField(fbxPath);
            if (GUILayout.Button("Select FBX"))
            {
                fbxPath = EditorUtility.OpenFilePanel("Select FBX file", "", "fbx");
            }
        }

        public void DrawCreateSection()
        {
            EditorGUILayout.LabelField("Occlusion", GUI.skin.horizontalSlider);
            if (GUILayout.Button("Generate parts"))
            {
                if (rootObj && System.IO.File.Exists(fbxPath))
                {
                    InitializeObjectInfo();
                    GeneratePartsFile();
                    if (CreateProxyObjects())
                    {
                        TryRemoveComponent<ThunderRoad.MeshPart>(rootObj);
                        foreach (MarionetteSmrPartProxy smrProxy in smrProxies)
                        {
                            TryRemoveComponent<ThunderRoad.RevealDecal>(smrProxy.gameObject);
                        }

                        if (partSettings.addMeshPart)
                        {
                            CreateMeshPart();
                        }
                        if (partSettings.addReveal)
                        {
                            CreateRevealDecals();
                            CreateRevealMaterials();
                        }
                        CreateMaterialInstances();
                        CreatePropertiesProxy();
                        RemoveAnyLodGroups();
                    }
                }
            }
        }


        Vector2 materialScrollPos;
        public void DrawPartSettingSection()
        {
            EditorGUILayout.LabelField("PartSettings", GUI.skin.horizontalSlider);
            partSettings.partType = (ThunderRoad.RevealDecal.Type)EditorGUILayout.EnumPopup("Part type:", partSettings.partType);
            partSettings.addMeshPart = EditorGUILayout.Toggle("MaterialCollision", partSettings.addMeshPart);
            if(partSettings.addMeshPart)
            {
                partSettings.meshPartMaterialType = (MeshPartMaterial)EditorGUILayout.EnumPopup("Material:", partSettings.meshPartMaterialType);
            }

            EditorGUILayout.LabelField("RevealSettings", GUI.skin.horizontalSlider);

            bool originalAddReveal = partSettings.addReveal;
            partSettings.addReveal = EditorGUILayout.Toggle("Reveal", partSettings.addReveal);
            if(partSettings.addReveal)
            {
                List<RevealSettings> newRevealSettings = new List<RevealSettings>();
                materialScrollPos = EditorGUILayout.BeginScrollView(materialScrollPos, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Height(300), GUILayout.Width(position.width));
                foreach (RevealSettings currSettings in partSettings.revealSettings)
                {
                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.BeginVertical();
                    currSettings.name = EditorGUILayout.TextField("Name", currSettings.name);
                    currSettings.revealType = (RevealType)EditorGUILayout.EnumPopup("Type", currSettings.revealType);
                    currSettings.partBaseMap = (Texture)EditorGUILayout.ObjectField("BaseMap", currSettings.partBaseMap, typeof(Texture), false);
                    currSettings.partBumpMap = (Texture)EditorGUILayout.ObjectField("BumpMap", currSettings.partBumpMap, typeof(Texture), false);
                    currSettings.partMetallicGlossMap = (Texture)EditorGUILayout.ObjectField("MetallicGlossMap", currSettings.partMetallicGlossMap, typeof(Texture), false);
                    EditorGUILayout.EndVertical();
                    if (!GUILayout.Button("Remove", GUILayout.Width(80)))
                    {
                        newRevealSettings.Add(currSettings);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("New Reveal Material") || (partSettings.addReveal && !originalAddReveal))
                {
                    RevealSettings newRevealEntry = new RevealSettings();
                    newRevealEntry.name = "Material " + newRevealSettings.Count;
                    newRevealSettings.Add(newRevealEntry);
                }
                partSettings.revealSettings = newRevealSettings;
            }
            else
            {
                partSettings.revealSettings = new List<RevealSettings>();
            }

            EditorGUILayout.LabelField("PropertySettings", GUI.skin.horizontalSlider);
            partSettings.watchedManikinProperties = (MarionettePropertiesProxy.ColorProperty)EditorGUILayout.EnumPopup("Color properties", partSettings.watchedManikinProperties);
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            DrawObjectSelectorSection();
            DrawFbxSelectorSection();
            DrawPartSettingSection();
            DrawCreateSection();
            EditorGUILayout.EndVertical();
        }
    }
}
