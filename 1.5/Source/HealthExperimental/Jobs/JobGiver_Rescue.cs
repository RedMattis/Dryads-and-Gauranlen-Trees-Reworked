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
    public class JobGiver_DryadRescueNearby : ThinkNode_JobGiver
    {
        private float radius = 30f;

        private float minDistFromEnemy = 15f;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            JobGiver_DryadRescueNearby obj = (JobGiver_DryadRescueNearby)base.DeepCopy(resolve);
            obj.radius = radius;
            obj.minDistFromEnemy = minDistFromEnemy;
            return obj;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            Pawn otherPawn = (Pawn)GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.OnCell, TraverseParms.For(pawn), radius, Validator);
            if (otherPawn == null)
            {
                return null;
            }
            Building_Bed building_Bed = RestUtility.FindBedFor(otherPawn, pawn, checkSocialProperness: false, ignoreOtherReservations: false, otherPawn.GuestStatus);
            if (building_Bed == null || !otherPawn.CanReserve(building_Bed))
            {
                return null;
            }
            Job job = JobMaker.MakeJob(JobDefOf.Rescue, otherPawn, building_Bed);
            job.count = 1;
            return job;
            bool Validator(Thing t)
            {
                Pawn patient = (Pawn)t;
                if (!CanRescueNow(pawn, patient))
                {
                    return false;
                }
                return true;
            }
        }

        public bool CanRescueNow(Pawn rescuer, Pawn patient, bool forced = false)
        {
            if (!forced && patient.Faction != rescuer.Faction)
            {
                return false;
            }

            if (!WantsToBeRescued(patient))
            {
                return false;
            }

            if (!forced && patient.IsForbidden(rescuer))
            {
                return false;
            }

            if (!forced && GenAI.EnemyIsNear(patient, minDistFromEnemy))
            {
                return false;
            }

            if (!rescuer.CanReserveAndReach(patient, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, forced))
            {
                return false;
            }

            return true;
        }

        public bool WantsToBeRescued(Pawn pawn)
        {
            if (!pawn.Downed)
            {
                return false;
            }

            if (pawn.InBed() || pawn.IsCharging())
            {
                return false;
            }

            if (CaravanFormingUtility.IsFormingCaravanOrDownedPawnToBeTakenByCaravan(pawn))
            {
                return false;
            }

            if (pawn.IsMutant && !pawn.mutant.Def.entitledToMedicalCare)
            {
                return false;
            }

            if (pawn.InMentalState && !pawn.health.hediffSet.HasHediff(HediffDefOf.Scaria))
            {
                return false;
            }

            if (pawn.ShouldBeSlaughtered())
            {
                return false;
            }

            if (pawn.TryGetLord(out var lord))
            {
                return false;
            }

            if (LifeStageUtility.AlwaysDowned(pawn))
            {
                return false;
            }

            return true;
        }
    }


}
