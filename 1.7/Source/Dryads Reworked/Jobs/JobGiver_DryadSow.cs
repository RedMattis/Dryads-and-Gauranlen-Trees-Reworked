using RimWorld;
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
    public class JobGiver_DryadSow : ThinkNode_Scanner
    {
        //public bool emergency;
        public int maxDistance = 9999;
        public bool plantTrees = true;
        public bool plantOnlyTrees = false;
        public bool plantMedical = true;
        public bool plantFood = true;
        public bool plantBeauty = true;
        public bool plantMisc = true;
        public bool plantDrugs = true;
        public int maxSkill = 99;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            var js = (JobGiver_DryadSow)base.DeepCopy(resolve);
            js.maxDistance = maxDistance;
            js.plantTrees = plantTrees;
            js.plantMedical = plantMedical;
            js.plantFood = plantFood;
            js.plantBeauty = plantBeauty;
            js.plantMisc = plantMisc;
            js.plantOnlyTrees = plantOnlyTrees;
            js.maxSkill = maxSkill;
            return js;
        }

        public override float GetPriority(Pawn pawn)
        {
            return 9f;
        }

        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
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
                            thing = RunThingScan(pawn, scanner, predicate, globalWorkThings, maxDistance);
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
                                bool isTree = thingDef?.plant?.IsTree == true;
                                int minSow = thingDef.plant?.sowMinSkill ?? 0;
                                if (minSow > maxSkill)
                                {
                                    continue;
                                }
                                if (plantOnlyTrees && !isTree)
                                {
                                    continue;
                                }
                                if (!plantTrees && isTree)
                                {
                                    continue;
                                }
                                bool isDrug = thingDef.plant?.drugForHarvestPurposes == true;
                                if (!plantDrugs && isDrug)
                                {
                                    continue;
                                }
                                if (!isDrug)
                                {
                                    if (!plantFood && (thingDef?.plant?.purpose == PlantPurpose.Food))
                                    {
                                        continue;
                                    }
                                    if (!plantMedical && (thingDef?.plant?.purpose == PlantPurpose.Health))
                                    {
                                        continue;
                                    }
                                    if (!plantBeauty && (thingDef?.plant?.purpose == PlantPurpose.Beauty))
                                    {
                                        continue;
                                    }
                                    if (!plantMisc && (thingDef?.plant?.purpose == PlantPurpose.Misc))
                                    {
                                        continue;
                                    }
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
    }

}
