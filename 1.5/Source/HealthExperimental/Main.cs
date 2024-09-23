
using Verse;
using HarmonyLib;
using UnityEngine;
using System.Collections;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse.Noise;
using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using RimWorld.QuestGen;
using System.Text;

namespace Dryads
{
    [StaticConstructorOnStartup]
    internal class Main : Mod
    {

        public Main(ModContentPack content) : base(content)
        {
            ApplyHarmonyPatches();
        }

        static void ApplyHarmonyPatches()
        {
            var harmony = new Harmony("RedMattis.Dryads");
            harmony.PatchAll();
        }
    }
    

}
