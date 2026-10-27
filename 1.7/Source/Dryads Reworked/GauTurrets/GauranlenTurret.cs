using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Dryad
{
    public class GauranlenTurret : Building_TurretGun
    {
        public bool targetingFire = false;
        private const int Building_TurretGun_TryStartShootSomethingIntervalTicks = 10;
        public const int TryTargetFireTick = Building_TurretGun_TryStartShootSomethingIntervalTicks * 30;


        protected override void BeginBurst()
        {
            if (targetingFire)
            {
                ExtinguishVerb.TryStartCastOn(CurrentTarget, preventFriendlyFire: true);
            }
            else
            {
                AttackVerb.TryStartCastOn(CurrentTarget, preventFriendlyFire: true);
            }
            OnAttackedTarget(CurrentTarget);
        }

        // Get first verb able to taget fires.
        private Verb _extinguishVerb = null;

        // Without a verb this will constantly check. Don't add this to a turret without this type of verb, the whole point of this class is to target fires.
        protected Verb ExtinguishVerb => _extinguishVerb ??= GunCompEq.AllVerbs.FirstOrDefault(v => v.verbProps.targetParams.canTargetFires);

        /// <summary>
        /// For the fire extinguish-sap ability
        /// </summary>
        /// <returns></returns>
        public override LocalTargetInfo TryFindNewTarget()
        {
            var target = base.TryFindNewTarget();
            targetingFire = false;
            if (target.IsValid)
            {
                return target;
            }
            if (ExtinguishVerb == null) return LocalTargetInfo.Invalid;
            targetingFire = true;

            var verb = ExtinguishVerb;
            
            TryGetPositionsWithFire(verb);
            if (positionsWithFire.Count > 0)
            {
                var targetPos = positionsWithFire.First();
                // Refreshing every 10 frames while there is a fire should be fine. TPS is probably not a concern when stuff is literally on fire.
                TryGetPositionsWithFire(verb, force: true);

                return targetPos;
            }

            return LocalTargetInfo.Invalid;
        }

        protected List<IntVec3> positionsWithFire = [];
        public int timeSinceLastCheck = 0;
        public const int FireCheckInterval = 10;
        private void TryGetPositionsWithFire(Verb verb, bool force=false)
        {
            if (force) timeSinceLastCheck = 99999;
            
            // Building_TurretGun_TryStartShootSomethingIntervalTicks
            if (timeSinceLastCheck < FireCheckInterval)
            {
                timeSinceLastCheck++;
                return;
            }
            timeSinceLastCheck = 0;
            int num = GenRadial.NumCellsInRadius(verb.verbProps.range);
            positionsWithFire.Clear();
            for (int i = 0; i < num; i++)
            {
                IntVec3 intVec = base.Position + GenRadial.RadialPattern[i];
                if (!GenSight.LineOfSight(base.Position, intVec, base.Map, skipFirstCell: true) && !ExtinguishVerb.ProjectileFliesOverhead())
                {
                    continue;
                }
                List<Thing> thingList = intVec.GetThingList(base.Map);
                for (int j = 0; j < thingList.Count; j++)
                {
                    if (thingList[j] is Fire || thingList[j].HasAttachment(ThingDefOf.Fire))
                    {
                        positionsWithFire.Add(thingList[j].Position);
                    }
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref targetingFire, "targetingFire", false);
        }
    }

}
