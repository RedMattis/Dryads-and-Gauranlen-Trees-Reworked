﻿using RimWorld;
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
        protected Verb ExtinguishVerb => GunCompEq.AllVerbs.FirstOrDefault(v => v.verbProps.targetParams.canTargetFires);

        /// <summary>
        /// For the fire extinguish-sap ability
        /// </summary>
        /// <returns></returns>
        public override LocalTargetInfo TryFindNewTarget()
        {
            var target = base.TryFindNewTarget();
            targetingFire = false;
            if (target.IsValid && ExtinguishVerb != null)
            {
                return target;
            }
            targetingFire = true;

            var verb = ExtinguishVerb;

            int num = GenRadial.NumCellsInRadius(verb.verbProps.range);
            for (int i = 0; i < num; i++)
            {
                IntVec3 intVec = base.Position + GenRadial.RadialPattern[i];
                if (!GenSight.LineOfSight(base.Position, intVec, base.Map, skipFirstCell: true))
                {
                    continue;
                }
                List<Thing> thingList = intVec.GetThingList(base.Map);
                for (int j = 0; j < thingList.Count; j++)
                {
                    if (thingList[j] is Fire || thingList[j].HasAttachment(ThingDefOf.Fire))
                    {
                        return thingList[j].Position;
                    }
                }
            }
            return LocalTargetInfo.Invalid;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref targetingFire, "targetingFire", false);
        }
    }
}
