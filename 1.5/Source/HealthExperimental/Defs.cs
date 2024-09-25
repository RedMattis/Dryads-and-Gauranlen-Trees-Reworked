using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Dryad
{
    [DefOf]
    public static class DryadDefs
    {
        public static ThingDef Dryad_Medicinemaker;
        public static ThingDef Dryad_Berrymaker;
        public static ThingDef Dryad_Woodmaker;
        public static ThingDef Plant_Healroot;
        public static ThingDef Plant_Strawberry;
        public static WorkGiverDef GrowerSow;
        //public static HediffDef Dryad_Hediff;
        //public static HediffDef Dryad_ConnectedHediff;

        //public static ThingDef Dryad_Thorncaster;
        //public static ThingDef Dryad_Bulb;
    }

    public class PlantSpawnRules : Def
    {
        public static Dictionary<ThingDef, PlantSpawnRules> plantRules = null;
        public ThingDef thingDef;
        public ThingDef spawnOn = null;

        public static PlantSpawnRules GetRulesForPlant(ThingDef plantDef)
        {
            plantRules ??= DefDatabase<PlantSpawnRules>.AllDefs.ToDictionary(r => r.thingDef);
            return plantRules.TryGetValue(plantDef, out var rules) ? rules : null;
        }
    }

    public class TreeTierTracker
    {
        public List<(int count, ThingDef thingDef)> plants = [];
        public int dryadCount = 0;
        public string info = "";
        public TreeTier tier = null;

        public TreeTierTracker() { }
        public TreeTierTracker(TreeTier tier, List<(Building thing, float distance)> thingsNearby, string levelUpInfo)
        {
            this.tier = tier;
            plants = tier.GetPlantsToSpawn(thingsNearby).ToList();
            dryadCount = tier.GetDryadCount(thingsNearby);
            info = levelUpInfo;
        }

        //public void ExposeData()
        //{
        //    Scribe_Collections.Look(ref plants, "plants", LookMode.Value, LookMode.Def);
        //    Scribe_Values.Look(ref dryadCount, "dryadCount", 0);
        //    Scribe_Defs.Look(ref tier, "tier");
        //    Scribe_Values.Look(ref info, "info", "");
        //}
    }

    public class TreeTier : Def
    {
        public Color tierColor = ColorLibrary.Orange;
        public float localHarmony = -99;
        public float globalHarmony = -99;
        public float localWealth = -1;
        public float gauranlenSpacing = -1;

        public HediffDef connectedPawnHediff;
        public float connectedHediffSeverity = 0.1f;
        public HediffDef dryadHediff;
        public float dryadHediffSeverity = 0.1f;

        public int dryadCount = 1;
        public int dryadMaxCount = 5;
        public List<ThingCountToSpawn> plants = [];
        public List<ThingCountToSpawn> dryadPerThing = [];

        public int GetDryadCount(List<(Building thing, float distance)> thingsFound) => dryadCount + dryadPerThing.Sum(pt => pt.CountToSpawn(thingsFound));

        public IEnumerable<(int count, ThingDef thingDef)> GetPlantsToSpawn(List<(Building thing, float distance)> thingsFound)
        {
            var plantsToSpawn = plants.Select(pt => (pt.CountToSpawn(thingsFound), pt.plantDef));
            // Merge identical plants, sum up their count.
            return plantsToSpawn.GroupBy(p => p.plantDef).Select(g => (g.Sum(p => p.Item1), g.Key));
        }

        // Check if the tree is valid for this tier (enough Harmony, etc.)
        public bool IsValidFor(CompNewTreeConnection tree, float localHarmony, float globalHarmony, float localWealth, float gauranlenSpacing, ref string info)
        {
            //if (info.NullOrEmpty()) info = "Dryad_MaxLevelYES".Translate().Colorize(ColorLibrary.Blue);
            if (localHarmony < this.localHarmony)
            {
                info = "Dryad_NeedMoreHarmony".Translate(localHarmony.ToString("F1"), this.localHarmony.ToString("F1")).Resolve().Colorize(tierColor);
                return false;
            }
            if (globalHarmony < this.globalHarmony)
            {
                info = "Dryad_NeedMapHarmony".Translate(globalHarmony.ToString("F0"), this.globalHarmony.ToString("F0")).Resolve().Colorize(tierColor);
                return false;
            }
            if (localWealth < this.localWealth)
            {
                info = "Dryad_ShrineWealthNeed".Translate(localWealth.ToString("F0"), this.localWealth.ToString("F0")).Resolve().Colorize(tierColor);
                return false;
            }
            if (gauranlenSpacing < this.gauranlenSpacing)
            {
                info = "Dryad_GauranlenSpacing".Translate(gauranlenSpacing.ToString("F0"), this.gauranlenSpacing.ToString("F0")).Resolve().Colorize(tierColor);
                return false;
            }
            return true;
        }

    }

    public class ThingCountToSpawn
    {
        public ThingDef multiplyByNearbyThingCount = null;
        public float distanceToMThing = 14; // Distance from thing. If any.
        public ThingDef plantDef;
        public int count = 1;
        public int max = 99;

        public int CountToSpawn(List<(Building thing, float distance)> thingsFound)
        {
            if (multiplyByNearbyThingCount == null)
            {
                return count;
            }

            int fromThingCount = thingsFound.Where(t => t.thing.def == multiplyByNearbyThingCount && t.distance <= distanceToMThing).Count() * count;
            return Math.Min(fromThingCount, max);
        }
    }
}
