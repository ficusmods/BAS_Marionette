using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using UnityEngine;
using UnityEngine.AI;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;

using Chabuk.ManikinMono;
using ThunderRoad;

namespace Marionette
{
	public class MarionetteItemModuleWardrobe : ItemModuleWardrobe
	{
		[Serializable]
		public class MarionetteCreatureWardrobe
		{
			public string creatureName;
			public string adapterWardrobeDataAddress;

			[NonSerialized]
			public MarionetteWardrobeDataProxy proxyWardrobeData;

			public MarionetteCreatureWardrobe Clone()
			{
				return MemberwiseClone() as MarionetteCreatureWardrobe;
			}

			public override bool Equals(System.Object obj)
			{
				if (obj == null || !(obj is MarionetteCreatureWardrobe))
					return false;
				else
					return creatureName == (obj as MarionetteCreatureWardrobe).creatureName;
			}

			public override int GetHashCode()
			{
				return Animator.StringToHash(creatureName);
			}
		}

		// Key: Creature.name
		public List<MarionetteCreatureWardrobe> creatureWardrobes = new List<MarionetteCreatureWardrobe>();

		private static ManikinWardrobeData CreateWardrobeFromProxy(MarionetteWardrobeDataProxy proxyData)
        {
			ManikinWardrobeData ret = ScriptableObject.CreateInstance("ManikinWardrobeData") as ManikinWardrobeData;
			ret.assetPrefab = new AssetReferenceManikinPart(proxyData.assetPrefab.AssetGUID);
			ret.channels = proxyData.channels;
			ret.layers = proxyData.layers;
			ret.fullyOccludedLayers = proxyData.fullyOccludedLayers;
			ret.partialOccludedLayers = proxyData.partialOccludedLayers;
			ret.partialOccludedMasks = proxyData.partialOccludedMasks;
			ret.occlusionID = proxyData.occlusionID;
			ret.occlusionIDHash = Animator.StringToHash(proxyData.occlusionID);
			ret.tags = proxyData.tags;
			return ret;
		}

		private void AddToBaseWardrobe(MarionetteCreatureWardrobe adapterCreatureWardrobe)
		{
			CreatureWardrobe current = new CreatureWardrobe();

			current.creatureName = adapterCreatureWardrobe.creatureName;
			current.wardrobeDataAddress = "HANDLED_BY_ADAPTER: "+ adapterCreatureWardrobe.adapterWardrobeDataAddress;
			current.manikinWardrobeData = CreateWardrobeFromProxy(adapterCreatureWardrobe.proxyWardrobeData);
			wardrobes.Add(current);
		}

		public override IEnumerator LoadAddressableAssetsCoroutine(ItemData data)
		{
			foreach (MarionetteCreatureWardrobe currWardrobe in creatureWardrobes)
			{
				Logger.Basic("Loading addressable adapter wardrobe for creature: {0} - {1}", currWardrobe.creatureName, currWardrobe.adapterWardrobeDataAddress);
				if (currWardrobe.proxyWardrobeData != null)
				{
					Catalog.ReleaseAsset<MarionetteWardrobeDataProxy>(currWardrobe.proxyWardrobeData);
				}
				yield return Catalog.LoadAssetCoroutine<MarionetteWardrobeDataProxy>(currWardrobe.adapterWardrobeDataAddress,
					(MarionetteWardrobeDataProxy value) =>
					{
						currWardrobe.proxyWardrobeData = value;
						AddToBaseWardrobe(currWardrobe);
						Logger.Basic("Adapter wardrobe converted to Manikin wardrobe {0} - {1}", currWardrobe.creatureName, data.id);
					}, "Adapter wardrobe module " + data.id);
			}
		}

		public override void ReleaseAddressableAssets()
		{
			foreach (MarionetteCreatureWardrobe currWardrobe in creatureWardrobes)
			{
				if (currWardrobe.proxyWardrobeData != null)
				{
					Catalog.ReleaseAsset<MarionetteWardrobeDataProxy>(currWardrobe.proxyWardrobeData);
					currWardrobe.proxyWardrobeData = null;
				}
			}
		}
	}
}
