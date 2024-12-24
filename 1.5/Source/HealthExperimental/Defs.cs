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
        public static ThingDef Dryad_Clawer;
        public static ThingDef Plant_Healroot;
        public static ThingDef Plant_Strawberry;
        public static WorkGiverDef GrowerSow;
        public static WorkGiverDef GrowerHarvest;
        public static WorkGiverDef Mine;
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

    public class DryadGreaterLink : Def
    {
        public static Dictionary<PawnKindDef, DryadGreaterLink> dryadLinks = null;
        public PawnKindDef dryad;
        public PawnKindDef greaterDryad;
        public int cost = 2;

        public static DryadGreaterLink GetGreaterVersionOf(PawnKindDef baseDryad)
        {
            if (baseDryad == null) return null;
            // Check if there are any defs of DryadGreaterLink at all.
            if (DefDatabase<DryadGreaterLink>.AllDefs.Count() == 0) return null;

            dryadLinks ??= DefDatabase<DryadGreaterLink>.AllDefs.ToDictionary(r => r.dryad);
            return dryadLinks.TryGetValue(baseDryad, out var link) ? link : null;
        }
    }

    public class TreeTierTracker
    {
        public List<(int count, ThingDef thingDef)> plants = [];
        public int dryadCount = 0;
        public int greaterDryadCount = 0;
        public string info = "";
        public TreeTier tier = null;

        public TreeTierTracker() { }
        public TreeTierTracker(CompNewTreeConnection treeComp, TreeTier tier, List<(Building thing, float distance)> thingsNearby, string levelUpInfo)
        {
            this.tier = tier;
            plants = tier.GetPlantsToSpawn(thingsNearby).ToList();
            dryadCount = tier.GetDryadCount(thingsNearby, treeComp);
            (greaterDryadCount, int greaterCost) = tier.GetGreaterDryadData(thingsNearby, treeComp);
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
        public int animusStoneCount = 0;

        public bool hidden = false;

        public HediffDef connectedPawnHediff;
        public float connectedHediffSeverity = 0.1f;
        public HediffDef dryadHediff;
        public float dryadHediffSeverity = 0.1f;

        protected int dryadCount = 1;
        protected int greaterDryadCount = 0;
        protected int dryadMaxCount = 5;
        public List<ThingCountToSpawn> plants = [];
        public List<ThingCountToSpawn> dryadPerThing = [];
        public List<ThingCountToSpawn> greaterDryadPerThing = [];

        public int GetDryadCount(List<(Building thing, float distance)> thingsFound, CompNewTreeConnection treeComp)
        {
            (int greaterAmount, int greaterCost) = GetGreaterDryadData(thingsFound, treeComp);
            int amount = dryadCount + dryadPerThing.Sum(pt => pt.CountToSpawn(thingsFound, "dryads"));
            amount -= (-greaterAmount * (greaterCost - 1)); // cost - 1 Because it already counts as one.
            amount = Mathf.Min(dryadMaxCount, amount);
            return amount;
        }
        public (int amount, int cost) GetGreaterDryadData(List<(Building thing, float distance)> thingsFound, CompNewTreeConnection treeComp)
        {
            if (treeComp?.DryadKind == null)
            {
                return (0, 0);
            }
            if (DryadGreaterLink.GetGreaterVersionOf(treeComp.DryadKind) is DryadGreaterLink link)
            {
                return (Math.Min(greaterDryadCount + greaterDryadPerThing.Sum(pt => pt.CountToSpawn(thingsFound, "greater_dryads")), dryadMaxCount), link.cost);
            }
            else return (0,0);
        }
        public IEnumerable<(int count, ThingDef thingDef)> GetPlantsToSpawn(List<(Building thing, float distance)> thingsFound)
        {
            var plantsToSpawn = plants.Select(pt => (pt.CountToSpawn(thingsFound, "plants"), pt.plantDef));
            // Merge identical plants, sum up their count.
            return plantsToSpawn.GroupBy(p => p.plantDef).Select(g => (g.Sum(p => p.Item1), g.Key));
        }

        public bool New_IsValidFor(CompNewTreeConnection tree, float localHarmony, float globalHarmony, float localWealth, float gauranlenSpacing, int animusStoneCount, ref string info)
        {
            if (animusStoneCount < this.animusStoneCount)
            {
                info = "Dryad_NeedMoreAnimus".Translate(animusStoneCount, this.animusStoneCount).Resolve().Colorize(tierColor);
                if (hidden) info = "";
                return false;
            }

            return IsValidFor(tree, localHarmony, globalHarmony, localWealth, gauranlenSpacing, ref info);
        }

        // Check if the tree is valid for this tier (enough Harmony, etc.)
        public bool IsValidFor(CompNewTreeConnection tree, float localHarmony, float globalHarmony, float localWealth, float gauranlenSpacing, ref string info)
        {

            //if (info.NullOrEmpty()) info = "Dryad_MaxLevelYES".Translate().Colorize(ColorLibrary.Blue);
            if (localHarmony < this.localHarmony)
            {
                info = "Dryad_NeedMoreHarmony".Translate(localHarmony.ToString("F1"), this.localHarmony.ToString("F1")).Resolve().Colorize(tierColor);
                if (hidden) info = "";
                return false;
            }
            if (globalHarmony < this.globalHarmony)
            {
                info = "Dryad_NeedMapHarmony".Translate(globalHarmony.ToString("F0"), this.globalHarmony.ToString("F0")).Resolve().Colorize(tierColor);
                if (hidden) info = "";
                return false;
            }
            if (localWealth < this.localWealth)
            {
                info = "Dryad_ShrineWealthNeed".Translate(localWealth.ToString("F0"), this.localWealth.ToString("F0")).Resolve().Colorize(tierColor);
                if (hidden) info = "";
                return false;
            }
            if (gauranlenSpacing < this.gauranlenSpacing)
            {
                info = "Dryad_GauranlenSpacing".Translate(gauranlenSpacing.ToString("F0"), this.gauranlenSpacing.ToString("F0")).Resolve().Colorize(tierColor);
                if (hidden) info = "";
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
        private float count = 1;
        public int max = 99;

        public int CountToSpawn(List<(Building thing, float distance)> thingsFound, string caller="")
        {
            if (multiplyByNearbyThingCount == null)
            {
                return Mathf.FloorToInt(count);
            }

            float fromThingCount = thingsFound.Where(t => t.thing.def == multiplyByNearbyThingCount && t.distance <= distanceToMThing).Count() * count;
            return Mathf.FloorToInt(Math.Min(fromThingCount, max));
        }
    }
}
