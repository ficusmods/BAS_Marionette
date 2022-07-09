using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEditor;

namespace Marionette
{
    [CustomEditor(typeof(MarionetteWardrobeDataProxy))]
    [CanEditMultipleObjects]
    public class MarionetteWardrobeDataProxyViewer : Editor
    {
        [Serializable]
        public class WardrobeData
        {
            public string channel;
            public string layer;
            public string[] fullyOccludedLayers;
            public string[] partialOccludedLayers;
            public int partialOcclusionMask;
            public int occlusionID;
        }

        [Serializable]
        public class WardrobeSettings
        {
            public AssetReference assetPrefab;
            public WardrobeData[] channels;
            public string occlusionID;
        }

        public WardrobeSettings wardrobeSettings;

        public static List<int> LayerBitFieldToLayerIdxList(int field)
        {
            List<int> ret = new List<int>();

            int bits = 0;
            if (field == -1)
            {
                bits = 31;
            }
            else
            {
                bits = ((int)Mathf.Log((float)field, 2)) + 1;
            }
            for (int i = 0; i < bits; i++)
            {
                int mask = 1 << i;
                if ((field & mask) != 0)
                {
                    ret.Add(i);
                }
            }

            return ret;
        }

        public static string LayerIdxToName(string channel, int layer)
        {
            string ret = "NOT_FOUND";

            string[] layerNames;
            if (LUT.manikinLocations.TryGetValue(channel, out layerNames))
            {
                if (layer < layerNames.Length)
                {
                    ret = String.Format("{0} ({1})", layerNames[layer], layer);
                }
                else
                {
                    ret = String.Format("LAYER_OVERFLOW ({0})", layer);
                }
            }

            return ret;
        }

        public static List<string> LayerBitFieldToLayerNames(string channel, int field)
        {
            List<string> ret = new List<string>();

            var indices = LayerBitFieldToLayerIdxList(field);
            foreach (int idx in indices)
            {
                ret.Add(LayerIdxToName(channel, idx));
            }

            return ret;
        }

        void OnEnable()
        {
            MarionetteWardrobeDataProxy wardrobeProxy = (MarionetteWardrobeDataProxy)target;
            UnityEngine.Debug.Log("Drawing wardrobe " + wardrobeProxy.assetPrefab.ToString());

            wardrobeSettings = new WardrobeSettings();
            WardrobeData[] channels = new WardrobeData[wardrobeProxy.channels.Length];

            for (int i = 0; i < wardrobeProxy.channels.Length; i++)
            {
                channels[i] = new WardrobeData();
                channels[i].channel = wardrobeProxy.channels[i];
                channels[i].layer = LayerIdxToName(wardrobeProxy.channels[i], wardrobeProxy.layers[i]);
                channels[i].fullyOccludedLayers = LayerBitFieldToLayerNames(wardrobeProxy.channels[i], wardrobeProxy.fullyOccludedLayers[i]).ToArray();
                channels[i].partialOccludedLayers = LayerBitFieldToLayerNames(wardrobeProxy.channels[i], wardrobeProxy.partialOccludedLayers[i]).ToArray();
                channels[i].partialOcclusionMask = wardrobeProxy.partialOccludedMasks[i];
            }

            wardrobeSettings.assetPrefab = wardrobeProxy.assetPrefab;
            wardrobeSettings.channels = channels;
            wardrobeSettings.occlusionID = wardrobeProxy.occlusionID;
        }

        public override void OnInspectorGUI()
        {
            SerializedObject serializedWardrobeProxy = new UnityEditor.SerializedObject(this);
            SerializedProperty serializedWardrobe = serializedWardrobeProxy.FindProperty("wardrobeSettings");
            EditorGUILayout.PropertyField(serializedWardrobe);
        }
    }
}
