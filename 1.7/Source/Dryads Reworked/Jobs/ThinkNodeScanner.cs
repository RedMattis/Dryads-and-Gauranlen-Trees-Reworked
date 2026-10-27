using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace Dryad
{
    public abstract class ThinkNode_Scanner : ThinkNode
    {
        public Thing RunThingScan(Pawn pawn, WorkGiver_Scanner scanner, Predicate<Thing> predicate, IEnumerable<Thing> globalWorkThings, float maxDistance)
        {
            Thing thing;
            if (scanner.Prioritized)
            {
                IEnumerable<Thing> matchingWorkThings = globalWorkThings;
                matchingWorkThings ??= pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
                if (scanner.AllowUnreachable)
                {
                    IntVec3 pawnPos = pawn.Position;
                    IEnumerable<Thing> searchSet = matchingWorkThings;
                    Predicate<Thing> validator = predicate;
                    thing = GenClosest.ClosestThing_Global(pawnPos, searchSet, maxDistance, validator, (Thing x) => scanner.GetPriority(pawn, x));
                }
                else
                {
                    IntVec3 pawnPos = pawn.Position;
                    Map map = pawn.Map;
                    IEnumerable<Thing> searchSet2 = matchingWorkThings;
                    PathEndMode pathEndMode = scanner.PathEndMode;
                    TraverseParms traverseParams = TraverseParms.For(pawn, scanner.MaxPathDanger(pawn));
                    Predicate<Thing> validator2 = predicate;
                    thing = GenClosest.ClosestThing_Global_Reachable(pawnPos, map, searchSet2, pathEndMode, traverseParams, maxDistance, validator2, (Thing x) => scanner.GetPriority(pawn, x));
                }
            }
            else if (scanner.AllowUnreachable)
            {
                IEnumerable<Thing> enumerable3 = globalWorkThings;
                enumerable3 ??= pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
                IntVec3 pawnPos = pawn.Position;
                IEnumerable<Thing> searchSet3 = enumerable3;
                Predicate<Thing> validator3 = predicate;
                thing = GenClosest.ClosestThing_Global(pawnPos, searchSet3, maxDistance, validator3);
            }
            else
            {
                IntVec3 position4 = pawn.Position;
                Map map2 = pawn.Map;
                ThingRequest potentialWorkThingRequest = scanner.PotentialWorkThingRequest;
                PathEndMode pathEndMode2 = scanner.PathEndMode;
                TraverseParms traverseParams2 = TraverseParms.For(pawn, scanner.MaxPathDanger(pawn));
                Predicate<Thing> validator4 = predicate;
                bool forceAllowGlobalSearch = globalWorkThings != null;
                thing = GenClosest.ClosestThingReachable(position4, map2, potentialWorkThingRequest, pathEndMode2, traverseParams2, maxDistance, validator4, globalWorkThings, 0, scanner.MaxRegionsToScanBeforeGlobalSearch, forceAllowGlobalSearch);
            }

            return thing;
        }
    }
}
