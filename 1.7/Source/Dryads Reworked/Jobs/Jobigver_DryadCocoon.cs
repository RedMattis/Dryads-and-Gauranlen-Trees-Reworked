using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Dryad
{
    public class JobGiver_DryadCreateAndEnterCocoon : JobGiver_CreateAndEnterDryadHolder
    {
        public override JobDef JobDef => JobDefOf.CreateAndEnterCocoon;

        public override bool ExtraValidator(Pawn pawn, CompTreeConnection connectionComp)
        {
            if (connectionComp is CompNewTreeConnection newTreeConnection)
            {
                if (pawn?.kindDef == null) return true;
                if (newTreeConnection.DryadKind != pawn.kindDef && newTreeConnection.GreaterDryadKind != pawn.kindDef)
                {
                    return true;
                }
            }
            else
            {
                if (connectionComp.DryadKind != pawn.kindDef)
                {
                    return true;
                }
            }
            return base.ExtraValidator(pawn, connectionComp);
        }
    }

}
