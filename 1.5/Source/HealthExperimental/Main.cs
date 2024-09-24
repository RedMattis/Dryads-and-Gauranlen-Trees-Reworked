
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

        public static DryadSettings settings;
        public Main(ModContentPack content) : base(content)
        {
            settings = GetSettings<DryadSettings>();
            ApplyHarmonyPatches();
            
        }

        public override string SettingsCategory()
        {
            return "Dryad_DryadsReworkedSettings".Translate();
        }

        static void ApplyHarmonyPatches()
        {
            var harmony = new Harmony("RedMattis.Dryads");
            harmony.PatchAll();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);

            var listingStandard = new Listing_Standard();
            var scrollView = new Rect(0, 0, inRect.width - 16, inRect.height + 100);
            var scrollPosition = Vector2.zero;

            Widgets.BeginScrollView(inRect, ref scrollPosition, scrollView);
            listingStandard.Begin(scrollView);

            CreateSettingsSlider(listingStandard, "Mech Harmony Reduction:", ref settings.mechPenaltyScale);

            CreateSettingsSlider(listingStandard, "Turret Spawn Time Multiplier:", ref settings.turretSpawnTime);


            // Add a "Reset to Default" button
            if (listingStandard.ButtonText("Reset to Default"))
            {
                settings.ResetToDefault();
            }

            listingStandard.End();
            Widgets.EndScrollView();
        }

        private static void CreateSettingsSlider(Listing_Standard listingStandard, string labelName, ref float value)
        {
            // Define a total rect for one row of slider and label
            Rect fullRow = listingStandard.GetRect(Text.LineHeight);

            // Divide the row into segments for the label, the slider, and the value text
            float labelWidth = fullRow.width * 0.5f;  // 50% for label
            float sliderWidth = fullRow.width * 0.35f; // 35% for slider
            float valueWidth = fullRow.width * 0.15f;  // 15% for value display

            Rect labelRect = new(fullRow.x, fullRow.y, labelWidth, fullRow.height);
            Rect sliderRect = new(labelRect.xMax, fullRow.y, sliderWidth, fullRow.height);
            Rect valueRect = new(sliderRect.xMax, fullRow.y, valueWidth, fullRow.height);

            // Draw the label, slider, and value on the respective Rects
            Widgets.Label(labelRect, labelName);
            value = Widgets.HorizontalSlider(sliderRect, value, 0f, 4f, true);
            Widgets.Label(valueRect, $"{value * 100:F0}%");
        }
    }
    
    public class DryadSettings : ModSettings
    {
        public const float defaultMechPenaltyScale = 1f;
        public float mechPenaltyScale = defaultMechPenaltyScale;

        public const float defaultTurretSpawnTime = 1;
        public float turretSpawnTime = defaultTurretSpawnTime;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref mechPenaltyScale, "mechPenalty", defaultMechPenaltyScale);
            Scribe_Values.Look(ref turretSpawnTime, "turretSpawnTime", defaultTurretSpawnTime);
            base.ExposeData();
        }

        public void ResetToDefault()
        {
            mechPenaltyScale = defaultMechPenaltyScale;
            turretSpawnTime = defaultTurretSpawnTime;
        }
    }

}
