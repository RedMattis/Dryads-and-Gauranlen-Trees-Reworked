﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse.Sound;
using Verse;
using System.Reflection;
using HarmonyLib;
using Verse.AI;
using Dryads;
using KTrie;

namespace Dryad
{
    

    public class CompProperties_NewTreeConnection : CompProperties_TreeConnection
    {
        public List<ThingDef> harmonyBuildings = [];
        public List<TreeTier> gauTiers = [];
        public CompProperties_NewTreeConnection()
        {
            compClass = typeof(CompNewTreeConnection);
        }
    }

    //public class GauLevel
    //{
    //    public int dryadCount = 0;
    //    public string info = "";
    //    //public string info_GauDist = "";

    //    public GauLevel(CompNewTreeConnection tree, List<(Thing thing, float distance)> nearbyThings, float mapHarmony, int totalShrineWealth, float localHarmony, float harmonyEfficiency, float mechanoidDisruption, float totalHarmony, float otherGauranlenDist)
    //    {
            
    //        dryadBuffLevel = tier.dryadHediffSeverity;
    //         pawnBuffLevel = tier.connectedHediffSeverity;

            
    //        dryadCount+=stoneCount;
    //        turretCount+=stoneCount*2;


    //    }
    //}

    [HarmonyPatch]
    [StaticConstructorOnStartup]
    public class CompNewTreeConnection : CompTreeConnection
    {
        public FieldInfo dryadsFI = null;
        public FieldInfo spawnTickFI = null;
        public FieldInfo leafEffecterFI = null;

        protected int actualMaxDryads = 1;

        private TreeTierTracker currentTier = null;

        public int turretSpawnTick = 0;

        public int TurretSpawnTickCooldown => (int)(15000 * Main.settings.turretSpawnTime);

        //protected List<Thing> turrets = new();
        protected List<Thing> plants = new();
        public CompProperties_NewTreeConnection NewProps => (CompProperties_NewTreeConnection)props;
        // "dryads" is private in the base class, so we need to use reflection to access it
        public List<Pawn> Dryads
        {
            get
            {
                if (dryadsFI == null)
                {
                    dryadsFI = typeof(CompTreeConnection).GetField("dryads", BindingFlags.NonPublic | BindingFlags.Instance);
                }
                return (List<Pawn>)dryadsFI.GetValue(this);
            }
        }
        public int SpawnTick
        {
            get
            {
                if (spawnTickFI == null)
                {
                    spawnTickFI = typeof(CompTreeConnection).GetField("spawnTick", BindingFlags.NonPublic | BindingFlags.Instance);
                }
                return (int)spawnTickFI.GetValue(this);
            }

        }
        public Effecter LeafEffecter
        {
            get
            {
                if (leafEffecterFI == null)
                {
                    leafEffecterFI = typeof(CompTreeConnection).GetField("leafEffecter", BindingFlags.NonPublic | BindingFlags.Instance);
                }
                return (Effecter)leafEffecterFI.GetValue(this);
            }
            set => leafEffecterFI.SetValue(this, value);
        }

        public TreeTierTracker CurrentTier
        {
            get
            {
                currentTier ??= GetGauLevel();
                return currentTier;
            }
            set => currentTier = value;
        }

        public override void CompTick()
        {
            if (CurrentTier == null) RefreshConnection();
            if (Find.TickManager.TicksGame >= SpawnTick)
            {
                // Use reflection to run SpawnDryad();
                typeof(CompTreeConnection).GetMethod("SpawnDryad", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(this, null);
            }
            if (LeafEffecter == null)
            {
                LeafEffecter = EffecterDefOf.GauranlenLeavesBatch.Spawn();
                LeafEffecter.Trigger(parent, parent);
            }
            LeafEffecter?.EffectTick(parent, parent);
            if (!parent.IsHashIntervalTick(1000))
            {
                return;
            }
            if (Mode == GauranlenTreeModeDefOf.Gaumaker && Dryads.Count >= 3)
            {
                if (gaumakerPod == null && TryGetGaumakerCell(out var cell))
                {
                    gaumakerPod = GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.GaumakerCocoon), cell, parent.Map);
                }
            }
            else if (gaumakerPod != null && !gaumakerPod.Destroyed)
            {
                gaumakerPod.Destroy();
                gaumakerPod = null;
            }
            RefreshConnection();

            if (HasProductionMode && Mode != desiredMode)
            {
                FinalizeMode();
            }
        }

        private void RefreshConnection()
        {
            var tierTracker = GetGauLevel();
            var tier = tierTracker.tier;
            actualMaxDryads = tierTracker.dryadCount;

            if (actualMaxDryads > 4) ConnectionStrength = Mathf.Min(1, 0.90f + (actualMaxDryads - 4) * 0.01f);
            if (actualMaxDryads == 4) ConnectionStrength = 0.90f;
            if (actualMaxDryads == 3) ConnectionStrength = 0.75f;
            if (actualMaxDryads == 2) ConnectionStrength = 0.5f;
            if (actualMaxDryads == 1) ConnectionStrength = 0.25f;
            if (actualMaxDryads == 0) ConnectionStrength = 0f;
            DesiredConnectionStrength = 0;

            // Fetch all dryads.
            foreach (var dryad in Dryads)
            {
                var dryadHediff = dryad.health.hediffSet.GetFirstHediffOfDef(tier.dryadHediff);
                if (dryadHediff == null)
                {
                    dryad.health.AddHediff(tier.dryadHediff);
                }
                else
                {
                    dryadHediff.Severity = tier.dryadHediffSeverity;
                }
            }

            if (ConnectedPawn != null)
            {
                var connectedPawnHediff = ConnectedPawn.health.hediffSet.GetFirstHediffOfDef(tier.connectedPawnHediff);
                if (connectedPawnHediff == null)
                {
                    connectedPawnHediff = ConnectedPawn.health.AddHediff(tier.connectedPawnHediff);
                    try
                    {
                        var dryadConnectionHediff = (DryadConnectionHediff)connectedPawnHediff;
                        dryadConnectionHediff.UpdateConnection(this, tier.connectedHediffSeverity);
                    }
                    catch { }
                }
                else
                {
                    var dryadConnectionHediff = (DryadConnectionHediff)connectedPawnHediff;
                    dryadConnectionHediff.UpdateConnection(this, tier.connectedHediffSeverity);
                }
            }
            SpawnPlants(tierTracker);
        }

        const int maxSpawnDist = 7;
        private void SpawnPlants(TreeTierTracker tierTracker)
        {
            plants.RemoveAll(t => t == null || t.Destroyed || !t.Spawned);

            if (turretSpawnTick > Find.TickManager.TicksGame) return;

            foreach(var (pToSpawn, plantDef) in tierTracker.plants.InRandomOrder())
            {
                int plantNumberSpawned = plants.Count(p => p.def == plantDef);
                if (plantNumberSpawned == pToSpawn) continue;
                if (plantNumberSpawned > pToSpawn)
                {
                    var p = plants.First(p => p.def == plantDef);
                    try { p.Destroy(); } catch { }
                    plants.Remove(p);
                    continue;
                }

                var plantRules = PlantSpawnRules.GetRulesForPlant(plantDef);
                if (plantRules == null)
                {
                    Log.Error($"PlantSpawnRules not found for {plantDef.defName}");
                    continue;
                }
                if (CellFinder.TryFindRandomCellNear(parent.Position, parent.Map, maxSpawnDist, (IntVec3 c)
                    => c.GetThingList(parent.Map).Any(t =>
                    // If not spawnOn is not defined check if the place is empty.
                    (plantRules.spawnOn == null && c.GetEdifice(parent.Map) == null) ||
                    // If spawnOn is defined check if the place is the right thing.
                    c.GetThingList(parent.Map).Any(t => t.def == plantRules.spawnOn)
                    ), out var cell))
                {
                    var plantThing = GenSpawn.Spawn(plantDef, cell, parent.Map);
                    plants.Add(plantThing);
                    plantThing.SetFaction(Faction.OfPlayer);
                    turretSpawnTick = Find.TickManager.TicksGame + Rand.Range(TurretSpawnTickCooldown/2, (int)(TurretSpawnTickCooldown*1.5f));
                    return;
                }
            }

            // Shorter cooldown in case there was nothing to place, or it failed to place.
            turretSpawnTick = Find.TickManager.TicksGame + Rand.Range(TurretSpawnTickCooldown / 4, (int)(TurretSpawnTickCooldown));
        }


        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            foreach (var turret in plants.Where(t=>t != null && t.Destroyed == false))
            {
                turret.Destroy();
            }
            base.PostDestroy(mode, previousMap);
        }

        private TreeTierTracker GetGauLevel()
        {
            // Get all buildings withn range of the tree
            var otherGau = parent.Map.listerThings.ThingsInGroup(ThingRequestGroup.DryadSpawner);
            var allBuildings = parent.Map.listerBuildings.allBuildingsColonist;
            var nearBuildings = allBuildings.Where(b => b.Position.InHorDistOf(parent.Position, 10));
            var veryNearBuildings = allBuildings.Where(b => b.Position.InHorDistOf(parent.Position, 4));

            // Get the harmony from all buildings
            var hb = NewProps.harmonyBuildings;
            float localHarmony = nearBuildings.Where(b => hb.Contains(b.def)).Sum(HarmonyFromBuilding);
            float mapHarmony = allBuildings.Where(b => hb.Contains(b.def)).Sum(HarmonyFromBuilding);
            float harmonyDisruption = nearBuildings.Where(b => b.def.building.artificialForMeditationPurposes).Count();
            float mechanoidDisruption = parent.Map.mapPawns.AllPawns.Where(p => p.Faction == Faction.OfPlayer && p.RaceProps.IsMechanoid).Sum(p => (int)Mathf.Ceil(p.BodySize));
            mechanoidDisruption *= Main.settings.mechPenaltyScale;
            
            float totalHarmony = mapHarmony - mechanoidDisruption;
            mechanoidDisruption -= totalHarmony;
            if (mechanoidDisruption > 0)
            {
                harmonyDisruption -= mechanoidDisruption * 2;
            }
            else mechanoidDisruption = 0; // For the tooltip.

            float harmonyEfficiency = harmonyDisruption switch
            {
                <= 0 => 1,
                < 5 => 0.75f,
                < 10 => 0.60f,
                < 25 => 0.5f,
                < 50 => 0.4f,
                _ => 0.3f
            };

            // Check if another gauranlen tree is nearby
            var otherTrees = otherGau.Where(b => b.def == parent.def && b != parent);

            // Get the closest distance (if any other trees are around).
            float otherGauranlenDist = 99999;
            if (otherTrees.Any())
            {
                otherGauranlenDist = otherTrees.Min(b => b.Position.DistanceTo(parent.Position));
            }

            // Grab first 3 shrines, or less if there are less than 3
            float totalShrineWealth = nearBuildings.Where(b => hb.Contains(b.def)).OrderByDescending(b => b.MarketValue).Take(3).Sum(b => b.MarketValue);

            // Check if any of the buildings nearby is the AnimusStone
            int animaStoneCount = veryNearBuildings.Sum(b => b.def == ThingDefOf.AnimusStone ? 1 : 0);

            totalShrineWealth *= (1 + animaStoneCount);

            const int gDistClose = 9;
            const int gDistMid = 14;
            localHarmony *= harmonyEfficiency;

            bool otherGauranlenClose = otherGauranlenDist < gDistClose;
            bool otherGauranlenMid = otherGauranlenDist < gDistMid;
            if (otherGauranlenClose)
            {
                localHarmony *= 0.75f;
            }
            else if (otherGauranlenMid)
            {
                localHarmony *= 0.9f;
            }
            string info = "";
            if (NewProps.gauTiers.Count == 0)
            {
                Log.Error("No tiers defined for " + parent.def.defName);
                return null;
            }
            var tier = NewProps.gauTiers.First(t => t.IsValidFor(this, localHarmony, totalHarmony, totalShrineWealth, otherGauranlenDist, ref info));

            if (tier == null)
            {
                Log.Error("No valid tier found for " + parent.def.defName);
                return null;
            }
            List<(Building thing, float distance)> thingsNearby = allBuildings.Select(b => (b, b.Position.DistanceTo(parent.Position))).ToList();

            var tracker = new TreeTierTracker(tier, thingsNearby, info);
            CurrentTier = tracker;
            return tracker;
        }

        public float HarmonyFromBuilding(Thing building)
        {
            bool isAnimaStone = building.def == ThingDefOf.AnimusStone;
            if (isAnimaStone)
            {
                return 5;
            }
            float focusPower = building.def.statBases.GetStatValueFromList(StatDefOf.MeditationFocusStrength, 0);
            int power = focusPower switch
            {
                > 0.29f => 2,
                _ => 1
            };
            // Check if building is made from Jade.
            if (building.Stuff == ThingDefOf.Jade && power <= 4)
            {
                power *= power > 1 ? 2 : 3;
            }
            return power;
        }

        private bool TryGetGaumakerCell(out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (CellFinder.TryFindRandomCellNear(parent.Position, parent.Map, 3, (IntVec3 c) => GauranlenUtility.CocoonAndPodCellValidator(c, parent.Map, ThingDefOf.Plant_PodGauranlen), out cell) || CellFinder.TryFindRandomCellNear(parent.Position, parent.Map, 3, (IntVec3 c) => GauranlenUtility.CocoonAndPodCellValidator(c, parent.Map, ThingDefOf.Plant_TreeGauranlen), out cell))
            {
                return true;
            }
            return false;
        }


        public override string CompInspectStringExtra()
        {
            string text = "";
            string text2 = string.Empty;
            if (Dryads.Count < MaxDryads)
            {
                text2 = "SpawningDryadIn".Translate(NamedArgumentUtility.Named(Props.pawnKind, "DRYAD"), (SpawnTick - Find.TickManager.TicksGame).ToStringTicksToPeriod().Named("TIME")).Resolve();
            }
            text = ((!ConnectionTorn) ? (text + "ConnectedPawn".Translate().Resolve() + ": " + (Connected ? ConnectedPawn.NameFullColored : "Nobody".Translate().CapitalizeFirst()).Resolve()) : (text + "ConnectionTorn".Translate(UntornInDurationTicks.ToStringTicksToPeriod()).Resolve()));
            if (Connected)
            {
                if (Mode != null)
                {
                    text += string.Concat("\n", "GauranlenTreeMode".Translate(), ": ") + Mode.LabelCap;
                }
                if (HasProductionMode && Mode != desiredMode)
                {
                    text +="\n" + "WaitingForConnectorToChangeCaste".Translate(ConnectedPawn.Named("CONNECTEDPAWN")).Resolve();
                }
                if (!text2.NullOrEmpty())
                {
                    text +="\n" + text2;
                }
                var tierTracker = GetGauLevel();
                if (MaxDryads > 0)
                {
                    text = string.Concat(text, " ", "DryadPlural".Translate(), $" ({Dryads.Count}/{MaxDryads})");
                    if (Dryads.Count > 0)
                    {
                        text +=": " + Dryads.Select((Pawn x) => x.NameShortColored.Resolve()).ToCommaList().CapitalizeFirst();
                    }
                }
                else
                {
                    text += "\n" + "Dryad_NotEnoughHarmony".Translate().Colorize(ColorLibrary.RedReadable);
                }
                text += "\n" + tierTracker.info;
                if (!HasProductionMode)
                {
                    text +="\n" + "AlertGauranlenTreeWithoutDryadTypeLabel".Translate().Colorize(ColorLibrary.RedReadable);
                }
                if (Mode == GauranlenTreeModeDefOf.Gaumaker && MaxDryads < 3)
                {
                    text +="\n" + "ConnectionStrengthTooWeakForGaumakerPod".Translate().Colorize(ColorLibrary.RedReadable);
                }
                string text3 = AffectingBuildingsDescription("ConnectionStrengthAffectedBy");
                if (!text3.NullOrEmpty())
                {
                    text +="\n" + text3;
                }
                if (plants.Count > 0)
                {
                    foreach ((int maxCount, ThingDef plant) in tierTracker.plants)
                    {
                        int currentPlantCount = plants.Count(p => p.def == plant);
                        text += "\n" + "Dryad_GauPlantNum".Translate(currentPlantCount, maxCount, plant.label);
                    }
                }
            }
            else if (!text2.NullOrEmpty())
            {
                text +="\n" + text2;
            }
            return text;
        }

        [HarmonyPatch(typeof(CompTreeConnection), "ResetDryad")]
        [HarmonyPostfix]
        public static void ResetDryadPostFix(CompTreeConnection __instance, Pawn dryad)
        {
            if (dryad == null) return;
            //dryad.health.AddHediff(DryadDefs.Dryad_Hediff);
            foreach (TrainableDef allDef in DefDatabase<TrainableDef>.AllDefs)
            {
                // Hax it so that dryads can be trained to rescue even if not combat trained.
                if (allDef.defName == "Rescue" &&
                    dryad?.def?.race?.trainableTags?.Contains("Help") == true && dryad?.def?.race?.trainableTags?.Contains("Combat") != true)
                {
                    dryad.training.SetWantedRecursive(allDef, checkOn: true);
                    dryad.training.Train(allDef, __instance.ConnectedPawn, complete: true);
                    if (allDef == TrainableDefOf.Release)
                    {
                        dryad.playerSettings.followDrafted = true;
                    }
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref actualMaxDryads, "actualMaxDryads", 1);
            Scribe_Values.Look(ref turretSpawnTick, "turretSpawnTick", 0);
            //Scribe_Deep.Look(ref currentTier, "currentTier");

            Scribe_Collections.Look(ref plants, "turrets", LookMode.Reference);
            //Scribe_Collections.Look(ref flowers, "flowers", LookMode.Reference);
            
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                // Remove all missing/destroyed turrets.
                plants.RemoveAll((Thing x) => x?.Destroyed ?? true);
            }
        }
    }
}
