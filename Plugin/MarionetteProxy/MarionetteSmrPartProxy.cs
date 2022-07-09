using System;
using System.Collections.Generic;
using UnityEngine;

#if NON_UNITY_BUILD
using Chabuk.ManikinMono;
#endif

namespace Marionette
{
    [DisallowMultipleComponent]
    public class MarionetteSmrPartProxy : MonoBehaviour
    {
		public SkinnedMeshRenderer smr;

        [Serializable]
        public class BoneInfo
        {
            public string name;
            public bool weighted;
        }

        public string rootBoneName;
        public BoneInfo[] bones;

        [HideInInspector]
		public int rootBoneHash;

		[HideInInspector]
		public int[] boneNameHashes;

		[HideInInspector]
		public int[] weightedBoneNameHashes;

        public void RecalculateHashes()
        {
            rootBoneHash = Animator.StringToHash(rootBoneName);
            boneNameHashes = new int[bones.Length];
            List<int> weightedBones = new List<int>();
            for (int i=0; i < bones.Length; i++)
            {
                boneNameHashes[i] = Animator.StringToHash(bones[i].name);
                if(bones[i].weighted)
                {
                    weightedBones.Add(Animator.StringToHash(bones[i].name));
                }
            }
            weightedBoneNameHashes = weightedBones.ToArray();
        }


#if NON_UNITY_BUILD
        public void CreateProperties()
        {
            MarionettePropertiesProxy propertiesProxy = gameObject.GetComponent<MarionettePropertiesProxy>();
            if (propertiesProxy == null) return;

            ManikinProperties partProperties = gameObject.AddComponent<ManikinProperties>();
            FieldAccess.Read<ManikinProperties>(partProperties, "parentProperties", out ManikinProperties rigProperties);
            if (rigProperties == null) return;

            List<ManikinProperty> partPropertyList = new List<ManikinProperty>();

            HashSet<string> propertyHashes;
            if (!LUT.propertyMap.TryGetValue(propertiesProxy.watchedManikinProperties, out propertyHashes)) return;

            for (int i = 0; i < rigProperties.properties.Length; i++)
            {
                if (propertyHashes.Contains(rigProperties.properties[i].set.name))
                {
                    ManikinProperty currProperty = new ManikinProperty();
                    currProperty.apply = true;
                    currProperty.materialIndices = rigProperties.properties[i].materialIndices;
                    currProperty.propertyType = rigProperties.properties[i].propertyType;
                    currProperty.set = rigProperties.properties[i].set;
                    currProperty.values = rigProperties.properties[i].values;
                    partPropertyList.Add(currProperty);
                }
            }

            partProperties.properties = partPropertyList.ToArray();
            rigProperties.UpdateProperties();
        }

        private void Awake()
        {
            CreateProperties();
        }
#endif
    }
}
