using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Dryad
{
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
            else return (0, 0);
        }
        public IEnumerable<(int count, ThingDef thingDef)> GetPlantsToSpawn(List<(Building thing, float distance)> thingsFound)
        {
            var plantsToSpawn = plants.Select(pt => (pt.CountToSpawn(thingsFound, "plants"), pt.plantDef)).ToList();
            var merged = plantsToSpawn.GroupBy(p => p.plantDef).Select(g => (count: g.Sum(p => p.Item1), plantDef: g.Key)).ToList();

            foreach ((int count, ThingDef plantDef) in merged)
            {
                var (greatCount, baseCount, upgrade) = PlantUpgrade.GetUpgradedVersion(plantDef, count);
                if (upgrade != null)
                {
                    yield return (baseCount, plantDef);
                    if (greatCount > 0)
                        yield return (greatCount, upgrade);
                }
                else
                {
                    yield return (count, plantDef);
                }
            }
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

        public int CountToSpawn(List<(Building thing, float distance)> thingsFound, string caller = "")
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
