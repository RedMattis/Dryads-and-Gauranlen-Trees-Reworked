﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;

namespace Dryad
{
    // Mostly stolen from Sarg's Animal Implants sow
    public class JobGiver_Sow : ThinkNode
    {
        public bool emergency;
        public int maxDistance = 9999;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            return (JobGiver_Sow)base.DeepCopy(resolve);
        }

        public override float GetPriority(Pawn pawn)
        {
            return 9f;
        }

        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            bool isBerryMaker = pawn.def == DryadDefs.Dryad_Berrymaker;
            bool isMedicineMaker = pawn.def == DryadDefs.Dryad_Medicinemaker;
            bool isWoodMaker = pawn.def == DryadDefs.Dryad_Woodmaker;

            if (!(isBerryMaker || isMedicineMaker || isWoodMaker))
            {
                return ThinkResult.NoJob;
            }

            int num = -999;
            TargetInfo targetInfo = TargetInfo.Invalid;
            WorkGiver_Scanner workGiver_Scanner = null;
            WorkGiver_GrowerSow workGiver_GrowerSow = (WorkGiver_GrowerSow)DryadDefs.GrowerSow.Worker;
            if ((workGiver_GrowerSow.def.priorityInType == num || !targetInfo.IsValid) && PawnCanUseWorkGiver(pawn, workGiver_GrowerSow))
            {
                try
                {
                    Job job = workGiver_GrowerSow.NonScanJob(pawn);
                    if (job != null)
                    {
                        return new ThinkResult(job, this, workGiver_GrowerSow.def.tagToGive);
                    }
                    WorkGiver_Scanner scanner = workGiver_GrowerSow;
                    if (scanner != null)
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
                            if (thing != null)
                            {
                                targetInfo = thing;
                                workGiver_Scanner = scanner;
                            }
                        }
                        if (scanner.def.scanCells)
                        {
                            IntVec3 pawnPos = pawn.Position;
                            float num2 = 99999f;
                            float num3 = float.MinValue;
                            bool prioritized = scanner.Prioritized;
                            bool allowUnreachable = scanner.AllowUnreachable;
                            Danger maxDanger = scanner.MaxPathDanger(pawn);
                            foreach (IntVec3 item in scanner.PotentialWorkCellsGlobal(pawn))
                            {
                                ThingDef thingDef = CalculateWantedPlantDef(item, pawn.Map);

                                if (thingDef == null)
                                {
                                    continue;
                                }
                                // Get distance to thing.
                                float distance = (item - pawn.Position).LengthHorizontal;
                                if (distance > maxDistance)
                                {
                                    continue;
                                }

                                int minSow = thingDef.plant?.sowMinSkill ?? 0;
                                if (isBerryMaker && (minSow > 9 || (thingDef?.plant?.purpose != PlantPurpose.Food)))
                                {
                                    continue;
                                }
                                if (isWoodMaker && (minSow > 9 || thingDef?.plant?.IsTree != true))
                                {
                                    continue;
                                }
                                // Check if Healing Plant.
                                if (isMedicineMaker && (thingDef?.plant?.purpose != PlantPurpose.Health))
                                {
                                    continue;
                                }
                                bool flag = false;
                                float num4 = (item - pawnPos).LengthHorizontalSquared;
                                float num5 = 0f;
                                if (prioritized)
                                {
                                    if (!item.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, item))
                                    {
                                        if (!allowUnreachable && !pawn.CanReach(item, scanner.PathEndMode, maxDanger))
                                        {
                                            continue;
                                        }
                                        num5 = scanner.GetPriority(pawn, item);
                                        if (num5 > num3 || (num5 == num3 && num4 < num2))
                                        {
                                            flag = true;
                                        }
                                    }
                                }
                                else if (num4 < num2 && !item.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, item))
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
                    Log.Error(string.Concat(pawn, " threw exception in WorkGiver ", workGiver_GrowerSow.def.defName, ": ", ex.ToString()));
                }
                finally
                {
                }
                if (targetInfo.IsValid)
                {
                    Job job2 = ((!targetInfo.HasThing) ? workGiver_Scanner.JobOnCell(pawn, targetInfo.Cell) : workGiver_Scanner.JobOnThing(pawn, targetInfo.Thing));
                    //job2.checkOverrideOnExpire = true;
                    if (job2 != null)
                    {
                        return new ThinkResult(job2, this, workGiver_GrowerSow.def.tagToGive);
                    }
                    Log.ErrorOnce(string.Concat(workGiver_Scanner, " provided target ", targetInfo, " but yielded no actual job for pawn ", pawn, ". The CanGiveJob and JobOnX methods may not be synchronized."), 6112651);
                }
                num = workGiver_GrowerSow.def.priorityInType;
            }
            return ThinkResult.NoJob;
        }

        private bool PawnCanUseWorkGiver(Pawn pawn, WorkGiver giver)
        {
            return giver.MissingRequiredCapacity(pawn) == null && !giver.ShouldSkip(pawn);
        }

        public static ThingDef CalculateWantedPlantDef(IntVec3 c, Map map)
        {
            return c.GetPlantToGrowSettable(map)?.GetPlantDefToGrow();
        }

        private Job GiverTryGiveJobPrioritized(Pawn pawn, WorkGiver giver, IntVec3 cell)
        {
            if (!PawnCanUseWorkGiver(pawn, giver))
            {
                return null;
            }
            try
            {
                Job job = giver.NonScanJob(pawn);
                if (job != null)
                {
                    return job;
                }
                WorkGiver_Scanner scanner = giver as WorkGiver_Scanner;
                if (scanner != null)
                {
                    if (giver.def.scanThings)
                    {
                        Predicate<Thing> predicate = (Thing t) => !t.IsForbidden(pawn) && scanner.HasJobOnThing(pawn, t);
                        List<Thing> thingList = cell.GetThingList(pawn.Map);
                        for (int i = 0; i < thingList.Count; i++)
                        {
                            Thing thing = thingList[i];
                            if (scanner.PotentialWorkThingRequest.Accepts(thing) && predicate(thing))
                            {
                                return scanner.JobOnThing(pawn, thing);
                            }
                        }
                    }
                    if (giver.def.scanCells && !cell.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, cell))
                    {
                        return scanner.JobOnCell(pawn, cell);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(string.Concat(pawn, " threw exception in GiverTryGiveJobTargeted on WorkGiver ", giver.def.defName, ": ", ex.ToString()));
            }
            return null;
        }
    }

}
