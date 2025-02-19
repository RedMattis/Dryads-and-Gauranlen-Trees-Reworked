using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Dryad
{
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

    
}
