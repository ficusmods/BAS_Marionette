using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;

using UnityEditor;

namespace Marionette
{
    public class WardrobeCreator : EditorWindow
    {
        public static Dictionary<string, Dictionary<string, int>> occlusionLocations = new Dictionary<string, Dictionary<string, int>>()
      {
        {
          "Head",
          new Dictionary<string, int>()
          {
            {"All", -1},
          }
        },
        {
          "Torso",
          new Dictionary<string, int>()
          {
            {"All", -1},
            {"ChestUpper", 1 << 0},
            {"UpperArm", 1 << 1},
            {"Elbow", 1 << 2},
            {"Chest", 1 << 3},
            {"Forearm", 1 << 4},
            {"BreastGroup0", 1 << 5},
            {"BreastGroup1", 1 << 6},
            {"BreastGroup2", 1 << 7},
          }
        },
        {
          "HandLeft",
          new Dictionary<string, int>()
          {
            {"All", -1},
          }
        },
        {
          "HandRight",
          new Dictionary<string, int>()
          {
            {"All", -1},
          }
        },
        {
          "Legs",
          new Dictionary<string, int>()
          {
            {"All", -1},
            {"UpperLeg", 1 << 0}
          }
        },
        {
          "Feet",
          new Dictionary<string, int>()
          {
            {"All", -1},
          }
        },
      };

        [MenuItem("MarionetteSDK/WardrobeCreator")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(WardrobeCreator));
        }

        class WardrobeSetting
        {
            public string assetPrefabPath;
            public string occlusionID;
            public string assetOutputPath;
        }

        class ChannelSetting
        {
            public int channelIdx;
            public string channelName = "Head";
            public int layerIdx;
            public string layerName = "Helmet";
            public Dictionary<int, bool> fullyOccludedLayers = new Dictionary<int, bool>();
            public Dictionary<int, bool> partialOccludedLayers = new Dictionary<int, bool>();
            public Dictionary<string, bool> partialOccludedGroups = new Dictionary<string, bool>();
            public int partialOccludedMask;
        }

        class ChannelSettingSelection
        {
            public int idx = -1;
            public ChannelSetting channelSetting = null;
        }

        WardrobeSetting wardrobeSetting = new WardrobeSetting();
        List<ChannelSetting> channels = new List<ChannelSetting>();
        ChannelSettingSelection selectedChannel = new ChannelSettingSelection();
        Vector2 scrollPos;


        public int BoolMapToBitField(Dictionary<int, bool> boolMap)
        {
            int ret = 0;

            for (int i = 0; i < 32; i++)
            {
                bool toggled;
                if (!boolMap.TryGetValue(i, out toggled))
                {
                    toggled = false;
                }

                int currVal = 0;
                if (toggled)
                {
                    currVal = 1 << i;
                }

                ret |= currVal;
            }

            return ret;
        }

        public int PartialOcclusionMapToBitField(string channel, Dictionary<string, bool> partialOcclusionMap)
        {
            int ret = 0;

            string[] groupNames = occlusionLocations[channel].Keys.ToArray();
            for (int i = 0; i < groupNames.Length; i++)
            {
                bool toggled;
                if (!partialOcclusionMap.TryGetValue(groupNames[i], out toggled))
                {
                    toggled = false;
                }

                int currVal = 0;
                if (toggled)
                {
                    currVal = occlusionLocations[channel][groupNames[i]];
                }

                ret |= currVal;
            }

            return ret;
        }

        public void CreateWardrobeDataFromChannelSettings()
        {
            MarionetteWardrobeDataProxy wardrobeData = ScriptableObject.CreateInstance<MarionetteWardrobeDataProxy>();

            wardrobeData.assetPrefab = new AssetReference(AssetDatabase.AssetPathToGUID(wardrobeSetting.assetPrefabPath));
            wardrobeData.channels = new string[channels.Count];
            wardrobeData.layers = new int[channels.Count];
            wardrobeData.fullyOccludedLayers = new int[channels.Count];
            wardrobeData.partialOccludedLayers = new int[channels.Count];
            wardrobeData.partialOccludedMasks = new int[channels.Count];

            for (int i = 0; i < channels.Count; i++)
            {
                wardrobeData.channels[i] = channels[i].channelName;
                wardrobeData.layers[i] = channels[i].layerIdx;
                wardrobeData.fullyOccludedLayers[i] = BoolMapToBitField(channels[i].fullyOccludedLayers);
                wardrobeData.partialOccludedLayers[i] = BoolMapToBitField(channels[i].partialOccludedLayers);
                wardrobeData.partialOccludedMasks[i] = PartialOcclusionMapToBitField(channels[i].channelName, channels[i].partialOccludedGroups);
            }

            wardrobeData.occlusionID = wardrobeSetting.occlusionID;

            AssetDatabase.DeleteAsset(wardrobeSetting.assetOutputPath);
            AssetDatabase.CreateAsset(wardrobeData, wardrobeSetting.assetOutputPath);
        }

        public void DrawChannelSection()
        {
            EditorGUILayout.LabelField("Channels", GUI.skin.horizontalSlider);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Height(85), GUILayout.Width(position.width));
            List<ChannelSetting> newChannelSettings = new List<ChannelSetting>();
            for (int i = 0; i < channels.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                ChannelSetting channelSetting = channels[i];

                int originalIdx = channelSetting.channelIdx;


                string[] locationChannelNames = LUT.manikinLocations.Keys.ToArray();
                channelSetting.channelIdx = EditorGUILayout.Popup("Channel " + i, channelSetting.channelIdx, locationChannelNames);
                channelSetting.channelName = locationChannelNames[channelSetting.channelIdx];

                if (GUILayout.Button("Edit", GUILayout.Width(80)))
                {
                    selectedChannel.idx = i;
                    selectedChannel.channelSetting = channelSetting;
                }
                if (GUILayout.Button("Remove", GUILayout.Width(80)))
                {
                    if (selectedChannel.channelSetting == channelSetting)
                    {
                        selectedChannel = new ChannelSettingSelection();
                    }
                }
                else
                {
                    newChannelSettings.Add(channelSetting);
                }

                if (originalIdx != channelSetting.channelIdx)
                {
                    channelSetting.layerIdx = 0;
                    channelSetting.layerName = LUT.manikinLocations[channelSetting.channelName][channelSetting.layerIdx]; ;
                    channelSetting.fullyOccludedLayers = new Dictionary<int, bool>();
                    channelSetting.partialOccludedLayers = new Dictionary<int, bool>();
                    channelSetting.partialOccludedGroups = new Dictionary<string, bool>();
                }

                EditorGUILayout.EndHorizontal();
            }
            channels = newChannelSettings;
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("New Channel"))
            {
                ChannelSetting newChannel = new ChannelSetting();
                int channelIdx = channels.Count;
                newChannel.channelName = "Head";
                channels.Add(newChannel);
                selectedChannel = new ChannelSettingSelection();
                selectedChannel.idx = channelIdx;
                selectedChannel.channelSetting = newChannel;
            }

            EditorGUILayout.EndHorizontal();
        }

        public void DrawLayersSection()
        {
            EditorGUILayout.LabelField("Layer", GUI.skin.horizontalSlider);

            if (selectedChannel.channelSetting == null) return;

            ChannelSetting channelSetting = selectedChannel.channelSetting;
            int originalIdx = channelSetting.layerIdx;

            channelSetting.layerIdx = EditorGUILayout.Popup(
              "Layer for Channel " + selectedChannel.idx,
              channelSetting.layerIdx,
              LUT.manikinLocations[channelSetting.channelName]);

            channelSetting.layerName = LUT.manikinLocations[channelSetting.channelName][channelSetting.layerIdx];

            if (originalIdx != channelSetting.layerIdx)
            {
                channelSetting.fullyOccludedLayers = new Dictionary<int, bool>();
                channelSetting.partialOccludedLayers = new Dictionary<int, bool>();
                channelSetting.partialOccludedGroups = new Dictionary<string, bool>();
            }
        }

        public void DrawFullOcclusionSection()
        {
            EditorGUILayout.BeginVertical();

            var centeredStyle = GUI.skin.GetStyle("Label");
            centeredStyle.alignment = TextAnchor.UpperCenter;
            EditorGUILayout.LabelField("Full occlusion", centeredStyle, GUILayout.Width(150));

            ChannelSetting channelSetting = selectedChannel.channelSetting;

            string[] layerNames = LUT.manikinLocations[channelSetting.channelName];
            for (int i = 0; i < layerNames.Length; i++)
            {
                bool toggled;
                if (!channelSetting.fullyOccludedLayers.TryGetValue(i, out toggled))
                {
                    toggled = false;
                }
                channelSetting.fullyOccludedLayers[i] = EditorGUILayout.Toggle(layerNames[i], toggled, GUILayout.Width(150));
            }
            EditorGUILayout.EndVertical();
        }

        public void DrawPartialOcclusionSection()
        {

            EditorGUILayout.BeginVertical();

            var centeredStyle = GUI.skin.GetStyle("Label");
            centeredStyle.alignment = TextAnchor.UpperCenter;
            EditorGUILayout.LabelField("Partial occlusion", centeredStyle, GUILayout.Width(150));

            ChannelSetting channelSetting = selectedChannel.channelSetting;

            string[] layerNames = LUT.manikinLocations[channelSetting.channelName];
            for (int i = 0; i < layerNames.Length; i++)
            {
                bool toggled;
                if (!channelSetting.partialOccludedLayers.TryGetValue(i, out toggled))
                {
                    toggled = false;
                }
                channelSetting.partialOccludedLayers[i] = EditorGUILayout.Toggle(layerNames[i], toggled, GUILayout.Width(150));
            }

            EditorGUILayout.EndVertical();
        }

        public void DrawPartialOcclusionMaskSection()
        {
            ChannelSetting channelSetting = selectedChannel.channelSetting;

            EditorGUILayout.BeginVertical();
            var centeredStyle = GUI.skin.GetStyle("Label");
            centeredStyle.alignment = TextAnchor.UpperCenter;
            EditorGUILayout.LabelField("Occlusion groups", centeredStyle, GUILayout.Width(150));

            string[] groupNames = occlusionLocations[channelSetting.channelName].Keys.ToArray();
            for (int i = 0; i < groupNames.Length; i++)
            {
                bool toggled;
                if (!channelSetting.partialOccludedGroups.TryGetValue(groupNames[i], out toggled))
                {
                    toggled = false;
                }
                channelSetting.partialOccludedGroups[groupNames[i]] = EditorGUILayout.Toggle(groupNames[i], toggled, GUILayout.Width(150));
            }
            EditorGUILayout.EndVertical();
        }

        public void DrawOcclusionSection()
        {
            EditorGUILayout.LabelField("Occlusion", GUI.skin.horizontalSlider);
            if (selectedChannel.channelSetting == null) return;
            EditorGUILayout.BeginHorizontal();
            DrawFullOcclusionSection();
            DrawPartialOcclusionSection();
            DrawPartialOcclusionMaskSection();
            EditorGUILayout.EndHorizontal();
        }

        public void DrawAssetsAndIdsSection()
        {
            EditorGUILayout.LabelField("Occlusion", GUI.skin.horizontalSlider);

            wardrobeSetting.occlusionID = EditorGUILayout.TextField("occlusionID", wardrobeSetting.occlusionID);

            string assetPath = EditorGUILayout.TextField("Asset Path", wardrobeSetting.assetPrefabPath);
            UnityEngine.GameObject loadedAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (loadedAsset == null)
            {
                wardrobeSetting.assetPrefabPath = "";
                return;
            }

            if (loadedAsset.TryGetComponent<MarionetteGroupPartProxy>(out _))
            {
                wardrobeSetting.assetPrefabPath = assetPath;
            }
        }

        public void DrawCreateSection()
        {
            EditorGUILayout.LabelField("Create", GUI.skin.horizontalSlider);

            wardrobeSetting.assetOutputPath = EditorGUILayout.TextField("Output", wardrobeSetting.assetOutputPath);

            if (channels.Count == 0 || String.IsNullOrEmpty(wardrobeSetting.assetOutputPath)) return;

            if (GUILayout.Button("Create"))
            {
                CreateWardrobeDataFromChannelSettings();
            }
        }

        public void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            DrawChannelSection();
            DrawLayersSection();
            DrawOcclusionSection();
            DrawAssetsAndIdsSection();
            DrawCreateSection();
            EditorGUILayout.EndVertical();
        }
    }
}
