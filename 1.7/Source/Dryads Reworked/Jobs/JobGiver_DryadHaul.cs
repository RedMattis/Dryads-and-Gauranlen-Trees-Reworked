using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;
using RimWorld.Planet;
using Verse.AI.Group;

namespace Dryad
{
    public class JobGiver_DryadHaul : ThinkNode_JobGiver
    {
        public int maxDistance = 9999;

        protected override Job TryGiveJob(Pawn pawn)
        {
            Thing haulable = GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling(), PathEndMode.OnCell, TraverseParms.For(pawn), maxDistance, Validator);
            if (haulable != null)
            {
                return HaulAIUtility.HaulToStorageJob(pawn, haulable, false);
            }
            return null;
            bool Validator(Thing t)
            {
                if (t.IsForbidden(pawn))
                {
                    return false;
                }
                if (!HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t, forced: false))
                {
                    return false;
                }
                if (pawn.carryTracker.MaxStackSpaceEver(t.def) <= 0)
                {
                    return false;
                }
                if (!StoreUtility.TryFindBestBetterStoreCellFor(t, pawn, pawn.Map, StoreUtility.CurrentStoragePriorityOf(t), pawn.Faction, out var _))
                {
                    return false;
                }
                return true;
            }
        }
    }

}
