using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Marionette
{

    [DisallowMultipleComponent]
    public class MarionettePropertiesProxy : MonoBehaviour
    {
        public enum ColorProperty
        {
            None,
            HairColor,
            EyeColor,
            SkinColor
        };

        public ColorProperty watchedManikinProperties;
    }
}
