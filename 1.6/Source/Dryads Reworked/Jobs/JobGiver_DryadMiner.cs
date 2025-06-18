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
    public class JobGiver_DryadMine : ThinkNode_Scanner
    {
        public int maxDistance = 9999;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            return (JobGiver_DryadMine)base.DeepCopy(resolve);
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
            WorkGiver worker = DryadDefs.Mine.Worker;
            if ((worker.def.priorityInType == num || !targetInfo.IsValid) && pawn.Map != null && PawnCanUseWorkGiver(pawn, worker))
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
                            thing = RunThingScan(pawn, scanner, predicate, globalWorkThings, maxDistance);
                            if (thing != null)
                            {
                                targetInfo = thing;
                                workGiver_Scanner = scanner;
                            }
                        }
                        if (scanner.def.scanCells)
                        {
                            IntVec3 position5 = pawn.Position;
                            float max = maxDistance;
                            float min = float.MinValue;
                            bool prioritized = scanner.Prioritized;
                            bool allowUnreachable = scanner.AllowUnreachable;
                            Danger maxDanger = scanner.MaxPathDanger(pawn);
                            foreach (IntVec3 item in scanner.PotentialWorkCellsGlobal(pawn))
                            {
                                bool found = false;
                                float itemDistance = (item - position5).LengthHorizontalSquared;
                                float closestDistance = 0f;
                                if (prioritized)
                                {
                                    if (!item.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, item))
                                    {
                                        if (!allowUnreachable && !pawn.CanReach(item, scanner.PathEndMode, maxDanger))
                                        {
                                            continue;
                                        }
                                        closestDistance = scanner.GetPriority(pawn, item);
                                        if (closestDistance > min || (closestDistance == min && itemDistance < max))
                                        {
                                            found = true;
                                        }
                                    }
                                }
                                else if (itemDistance < max && !item.IsForbidden(pawn) && scanner.HasJobOnCell(pawn, item))
                                {
                                    if (!allowUnreachable && !pawn.CanReach(item, scanner.PathEndMode, maxDanger))
                                    {
                                        continue;
                                    }
                                    found = true;
                                }
                                if (found)
                                {
                                    targetInfo = new TargetInfo(item, pawn.Map);
                                    workGiver_Scanner = scanner;
                                    max = itemDistance;
                                    min = closestDistance;
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
