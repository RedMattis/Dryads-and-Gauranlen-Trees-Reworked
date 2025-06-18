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
    public class JobGiver_DryadHarvest: ThinkNode
    {
        public int maxDistance = 9999;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            var js = (JobGiver_DryadHarvest)base.DeepCopy(resolve);
            js.maxDistance = maxDistance;
            return js;
        }

        public override float GetPriority(Pawn pawn)
        {
            return 9f;
        }

        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            //bool isClawer = pawn.def == DryadDefs.Dryad_Clawer;
            //if (!isClawer)
            //{
            //    return ThinkResult.NoJob;
            //}

            int num = -999;
            TargetInfo targetInfo = TargetInfo.Invalid;
            WorkGiver_Scanner workGiver_Scanner = null;
            WorkGiver worker = DryadDefs.GrowerHarvest.Worker;
            if ((worker.def.priorityInType == num || !targetInfo.IsValid) && PawnCanUseWorkGiver(pawn, worker))
            {
                try
                {
                    Job job = worker.NonScanJob(pawn);
                    if (job != null)
                    {
                        return new ThinkResult(job, this, worker.def.tagToGive);
                    }
                    if (worker is WorkGiver_Scanner scanner)
                    {
                        if (scanner.def.scanThings)
                        {
                            Predicate<Thing> predicate = (Thing t) => !t.IsForbidden(pawn) && scanner.HasJobOnThing(pawn, t);
                            IEnumerable<Thing> globalWorkThings = scanner.PotentialWorkThingsGlobal(pawn);
                            Thing thing;
                            if (scanner.Prioritized)
                            {
                                IEnumerable<Thing> matchingWorkThings = globalWorkThings;
                                matchingWorkThings ??= pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
                                if (scanner.AllowUnreachable)
                                {
                                    IntVec3 position = pawn.Position;
                                    IEnumerable<Thing> searchSet = matchingWorkThings;
                                    Predicate<Thing> validator = predicate;
                                    thing = GenClosest.ClosestThing_Global(position, searchSet, maxDistance, validator, (Thing x) => scanner.GetPriority(pawn, x));
                                }
                                else
                                {
                                    IntVec3 position2 = pawn.Position;
                                    Map map = pawn.Map;
                                    IEnumerable<Thing> searchSet2 = matchingWorkThings;
                                    PathEndMode pathEndMode = scanner.PathEndMode;
                                    TraverseParms traverseParams = TraverseParms.For(pawn, scanner.MaxPathDanger(pawn));
                                    Predicate<Thing> validator2 = predicate;
                                    thing = GenClosest.ClosestThing_Global_Reachable(position2, map, searchSet2, pathEndMode, traverseParams, maxDistance, validator2, (Thing x) => scanner.GetPriority(pawn, x));
                                }
                            }
                            else if (scanner.AllowUnreachable)
                            {
                                IEnumerable<Thing> enumerable3 = globalWorkThings;
                                enumerable3 ??= pawn.Map.listerThings.ThingsMatching(scanner.PotentialWorkThingRequest);
                                IntVec3 position3 = pawn.Position;
                                IEnumerable<Thing> searchSet3 = enumerable3;
                                Predicate<Thing> validator3 = predicate;
                                thing = GenClosest.ClosestThing_Global(position3, searchSet3, maxDistance, validator3);
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
                            if (thing != null)
                            {
                                targetInfo = thing;
                                workGiver_Scanner = scanner;
                            }
                        }
                        if (scanner.def.scanCells)
                        {
                            IntVec3 position5 = pawn.Position;
                            float num2 = 99999f;
                            float num3 = float.MinValue;
                            bool prioritized = scanner.Prioritized;
                            bool allowUnreachable = scanner.AllowUnreachable;
                            Danger maxDanger = scanner.MaxPathDanger(pawn);
                            foreach (IntVec3 item in scanner.PotentialWorkCellsGlobal(pawn))
                            {
                                float dist = (item - pawn.Position).LengthHorizontal;
                                bool flag = false;
                                float num4 = (item - position5).LengthHorizontalSquared;
                                float num5 = 0f;
                                if (maxDistance < dist)
                                {
                                    continue;
                                }

                                if (num4 < num2 && !item.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, item))
                                {
                                    if (!allowUnreachable && !pawn.CanReach(item, scanner.PathEndMode, maxDanger))
                                    {
                                        continue;
                                    }
                                    flag = true;
                                }
                                if (flag)
                                {
                                    targetInfo = new TargetInfo(item, pawn.Map);
                                    workGiver_Scanner = scanner;
                                    num2 = num4;
                                    num3 = num5;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(string.Concat(pawn, " threw exception in WorkGiver ", worker.def.defName, ": ", ex.ToString()));
                }
                finally
                {
                }
                if (targetInfo.IsValid)
                {
                    Job job2 = ((!targetInfo.HasThing) ? workGiver_Scanner.JobOnCell(pawn, targetInfo.Cell) : workGiver_Scanner.JobOnThing(pawn, targetInfo.Thing));
                    if (job2 != null)
                    {
                        return new ThinkResult(job2, this, worker.def.tagToGive);
                    }
                    Log.ErrorOnce(string.Concat(workGiver_Scanner, " provided target ", targetInfo, " but yielded no actual job for pawn ", pawn, ". The CanGiveJob and JobOnX methods may not be synchronized."), 6112651);
                }
                num = worker.def.priorityInType;
            }
            return ThinkResult.NoJob;
        }

        private bool PawnCanUseWorkGiver(Pawn pawn, WorkGiver giver)
        {
            return giver.MissingRequiredCapacity(pawn) == null && !giver.ShouldSkip(pawn);
        }

    }

}
