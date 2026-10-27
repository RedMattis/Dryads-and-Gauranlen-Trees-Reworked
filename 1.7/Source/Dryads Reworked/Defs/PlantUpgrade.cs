using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Dryad
{
    public class PlantUpgrade : Def
    {
        public static Dictionary<ThingDef, PlantUpgrade> plantLinks = null;
        public ThingDef plant;
        public ThingDef greaterPlant;
        public int cost = 3;

        public static (int greatCount, int baseCount, ThingDef upgrade) GetUpgradedVersion(ThingDef basePlant, int count)
        {
            if (basePlant == null || count == 0) return (0, 0, null);
            // Check if there are any defs of DryadGreaterLink at all.
            if (DefDatabase<PlantUpgrade>.AllDefs.Count() == 0) return (0, 0, null);

            plantLinks ??= DefDatabase<PlantUpgrade>.AllDefs.ToDictionary(r => r.plant);
            if (!plantLinks.TryGetValue(basePlant, out var link) || link == null) return (0, 0, null);

            int upgradeCount = count / link.cost;
            int baseCount = count % link.cost;
            if (baseCount == 0) { baseCount = link.cost; upgradeCount--; }
            return (upgradeCount, baseCount, link.greaterPlant);
        }
    }
}
