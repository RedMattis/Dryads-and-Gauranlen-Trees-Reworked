using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Dryad
{
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
}
