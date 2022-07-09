using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

using ThunderRoad;
using UnityEngine;
using HarmonyLib;

namespace Marionette
{
    public class LoadModule : LevelModule
    {
        public override IEnumerator OnLoadCoroutine()
        {
            Harmony harmony = new Harmony("ficus.MarionetteAdapter.patch");
            harmony.PatchAll();

            return base.OnLoadCoroutine();
        }
    }
}
