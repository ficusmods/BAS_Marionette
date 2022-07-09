using System;
using System.Collections.Generic;
using UnityEngine;

using ThunderRoad;

#if NON_UNITY_BUILD
using Chabuk.ManikinMono;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

namespace Marionette
{
    [DisallowMultipleComponent]
    public class MarionetteGroupPartProxy : MonoBehaviour
    {
		[Serializable]
		public struct PartLOD
		{
			public List<Renderer> renderers;
		}

		public List<PartLOD> partLODs = new List<PartLOD>();

        public bool copyLastLodToAnySuperiorLOD = true;
        public MarionetteSmrPartProxy[] parts;

        [HideInInspector]
        public int versionMajor = Config.versionMajor;
        [HideInInspector]
        public int versionMinor = Config.versionMinor;

	}
}
