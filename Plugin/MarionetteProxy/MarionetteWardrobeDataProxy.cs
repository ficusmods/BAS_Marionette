using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Marionette
{
    [CreateAssetMenu(menuName = "ManikinAdapter/MarionetteWardrobeDataProxy")]
    public class MarionetteWardrobeDataProxy : ScriptableObject
    {
        public AssetReference assetPrefab;

        public string[] channels;
        public int[] layers;
        public int[] fullyOccludedLayers;
        public int[] partialOccludedLayers;
        public int[] partialOccludedMasks;

        public string occlusionID;

        public string[] tags;
    }
}
