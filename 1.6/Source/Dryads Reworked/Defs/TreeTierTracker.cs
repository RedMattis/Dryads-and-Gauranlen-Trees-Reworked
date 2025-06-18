using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Dryad
{
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

    }
}
