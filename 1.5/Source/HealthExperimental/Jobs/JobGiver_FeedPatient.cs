//using RimWorld;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Verse.AI;
//using Verse;
//using System.Security.Cryptography;

//namespace Dryad
//{
//    // Mostly stolen from Sarg's Animal Implants sow
//    public class JobGiver_FeedPatient : ThinkNode
//    {
//        public bool emergency;
//        //public int maxDistance = 9999;

//        public override ThinkNode DeepCopy(bool resolve = true)
//        {
//            return (JobGiver_FeedPatient)base.DeepCopy(resolve);
//        }

//        public override float GetPriority(Pawn pawn)
//        {
//            return 9f;
//        }

//        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
//        {
//            bool isBerryMaker = pawn.def == DryadDefs.Dryad_Berrymaker;

//            if (!isBerryMaker)
//            {
//                return ThinkResult.NoJob;
//            }
//            var hungryPawns = pawn.Map.mapPawns.SpawnedHungryPawns.Where(p => ShouldFeed(pawn, p, false)).ToList();
//            if (hungryPawns.Count > 0)
//            {
//                Pawn pawn2 = hungryPawns.RandomElement();
//                if (TryFindBestFoodSourceFor(pawn, pawn2, out var foodSource, out var foodDef))
//                {
//                    Job job = new(JobDefOf.FeedPatient, pawn2, foodSource)
//                    {
//                        //count = 1
//                    };
//                    Log.Message($"DEBUG: {pawn} found food for {pawn2}. ({foodSource}, {foodDef})");

                    
//                    pawn.jobs.ClearQueuedJobs(); // Clear job queue
//                    return new ThinkResult(job, this);
//                }
//                else
//                {
//                    Log.Message($"DEBUG: {pawn} failed to find food for {pawn2}. ({foodSource}, {foodDef})");
//                }
//            }

//            return ThinkResult.NoJob;
//        }

//        public bool ShouldFeed(Pawn pawn, Thing t, bool forced = false)
//        {
//            if (t is not Pawn pawn2 || pawn2 == pawn)
//            {
//                return false;
//            }
//            if (!pawn2.RaceProps.Humanlike && !pawn2.IsNonMutantAnimal)
//            {
//                return false;
//            }
//            if (pawn2.DevelopmentalStage.Baby())
//            {
//                return false;
//            }
//            if (!FeedPatientUtility.IsHungry(pawn2))
//            {
//                return false;
//            }
//            if (!FeedPatientUtility.ShouldBeFed(pawn2))
//            {
//                return false;
//            }
//            if (!pawn.CanReserve(t, 1, -1, null, forced))
//            {
//                return false;
//            }
//            if (pawn2.foodRestriction != null)
//            {
//                FoodPolicy currentRespectedRestriction = pawn2.foodRestriction.GetCurrentRespectedRestriction(pawn);
//                if (currentRespectedRestriction != null && currentRespectedRestriction.filter.AllowedDefCount == 0)
//                {
//                    JobFailReason.Is("NoFoodMatchingRestrictions".Translate());
//                    return false;
//                }
//            }
//            if (!TryFindBestFoodSourceFor(pawn, pawn2, out var _, out var _))
//            {
//                JobFailReason.Is("NoFood".Translate());
//                return false;
//            }
//            return true;
//        }

//        private bool TryFindBestFoodSourceFor(Pawn pawn, Pawn patient, out Thing foodSource, out ThingDef foodDef)
//        {
//            return FoodUtility.TryFindBestFoodSourceFor(pawn, patient, patient.needs.food.CurCategory == HungerCategory.Starving, out foodSource, out foodDef, canRefillDispenser: false, canUseInventory: true, canUsePackAnimalInventory: true, allowForbidden: false, allowCorpse: true, allowSociallyImproper: false, allowHarvest: false, forceScanWholeMap: false, ignoreReservations: false, calculateWantedStackCount: false, allowVenerated: true);
//        }

//        public bool AcceptableFood(Pawn eater, Pawn getter, Thing t, bool desperate, out IntVec3 intVec)
//        {
//            intVec = IntVec3.Invalid;
//            FoodPreferability minPref;
//            if (eater.NonHumanlikeOrWildMan())
//            {
//                minPref = FoodPreferability.NeverForNutrition;
//            }
//            else if (desperate)
//            {
//                minPref = FoodPreferability.DesperateOnly;
//            }
//            else
//            {
//                minPref = (((int)eater.needs.food.CurCategory >= 2) ? FoodPreferability.RawBad : FoodPreferability.MealAwful);
//                if (minPref == FoodPreferability.MealAwful && eater.genes != null && eater.genes.DontMindRawFood)
//                {
//                    minPref = FoodPreferability.RawBad;
//                }
//            }

//            if ((int)t.def.ingestible.preferability < (int)minPref || !eater.WillEat(t, getter, careIfNotAcceptableForTitle: true, false) || !t.def.IsNutritionGivingIngestible || !t.IngestibleNow || !(t is Corpse) || (!t.def.IsDrug) || !(t.IsForbidden(getter)) || (!desperate && t.IsNotFresh()) || t.IsDessicated())
//            {
//                return false;
//            }
//            int stackCount = 1;

//            if (!getter.CanReserve(t, 10, stackCount))
//            {
//                return false;
//            }

//            intVec = t.PositionHeld;
//            return true;
//        }



//    }

//}
