using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marionette
{
    public class LUT
    {
        public static Dictionary<
            MarionettePropertiesProxy.ColorProperty,
            HashSet<string>
            > propertyMap = new Dictionary<MarionettePropertiesProxy.ColorProperty, HashSet<string>>
            {
                {
                    MarionettePropertiesProxy.ColorProperty.HairColor,
                    new HashSet<string>{
                        "HairColor",
                        "HairSecondaryColor",
                        "HairSpecularColor"
                    }
                },
                {
                    MarionettePropertiesProxy.ColorProperty.EyeColor,
                    new HashSet<string>{
                        "EyeIrisColor",
                        "EyeScleraColor"
                    }
                },
                {
                    MarionettePropertiesProxy.ColorProperty.SkinColor,
                    new HashSet<string>{
                        "SkinColor",
                    }
                }
            };

      public static Dictionary<string, string[]> manikinLocations = new Dictionary<string, string[]>
      {
        {
          "Head",
          new string[]
          {
            "Helmet",
            "Headband",
            "Nose",
            "EarringRight",
            "EarringLeft",
            "Beard",
            "Hair",
            "Tatoo",
            "EyesBrows",
            "EyesLashes",
            "Eyes",
            "Mouth",
            "Body"
          }
        },
        {
          "Torso",
          new string[]
          {
            "ShoulderRight",
            "ShoulderLeft",
            "Cloak",
            "Underwear",
            "Jacket",
            "Tatoo",
            "UnknownPart",
            "Body"
          }
        },
        {
          "HandLeft",
          new string[]
          {
            "Glove",
            "Wrist",
            "Ring",
            "Tatoo",
            "Body"
          }
        },
        {
          "HandRight",
          new string[]
          {
            "Glove",
            "Wrist",
            "Ring",
            "Tatoo",
            "Body"
          }
        },
        {
          "Legs",
          new string[]
          {
            "Skirt",
            "Armor",
            "Pants",
            "Underwear",
            "Tatoo",
            "Body"
          }
        },
        {
          "Feet",
          new string[]
          {
            "Boot",
            "Socket",
            "Tatoo",
            "Body"
          }
        },
        {
          "Global",
          new string[]
          {
            "Vfx"
          }
        }
      };
    }
}
