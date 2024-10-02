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
    public class JobGiver_DryadTend : ThinkNode
    {
        public bool emergency;
        //public int maxDistance = 9999;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            var jt = (JobGiver_DryadTend)base.DeepCopy(resolve);
            jt.emergency = emergency;
            return jt;
        }

        public override float GetPriority(Pawn pawn)
        {
            return 11f;
        }

        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
            //bool isMedicineMaker = pawn.def == DryadDefs.Dryad_Medicinemaker;

            //if (!(isMedicineMaker))
            //{
            //    return ThinkResult.NoJob;
            //}
            var tendablePawns = pawn.Map.mapPawns.SpawnedPawnsWithAnyHediff.Where(p => ShouldTendPawn(pawn, p, false)).ToList();
            if (tendablePawns.Count > 0)
            {
                Pawn pawn2 = tendablePawns.RandomElement();
                Job job = new(JobDefOf.TendPatient, pawn2)
                {
                    //count = 1
                };
                return new ThinkResult(job, this);
            }

            return ThinkResult.NoJob;
        }

        public bool ShouldTendPawn(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is not Pawn patient || patient.GetPosture() == PawnPosture.Standing || //!GoodLayingStatusForTend(pawn2, pawn) ||
                !HealthAIUtility.ShouldBeTendedNowByPlayer(patient) || patient.IsForbidden(pawn) ||
                !pawn.CanReserve(patient, 1, -1, null, forced) || (patient.IsMutant && !patient.mutant.Def.entitledToMedicalCare) ||
                (patient.InAggroMentalState && !patient.health.hediffSet.HasHediff(HediffDefOf.Scaria)))
            {
                return false;
            }
            return true;
        }
        public static bool GoodLayingStatusForTend(Pawn patient, Pawn doctor)
        {
            if (patient == doctor)
            {
                return true;
            }
            if (patient.RaceProps.Humanlike)
            {
                return patient.InBed();
            }
            return patient.GetPosture() != PawnPosture.Standing;
        }
    }

}
