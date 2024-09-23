using RimWorld;
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

namespace Dryad
{
    [DefOf]
    public static class DryadDefs
    {
        public static ThingDef Dryad_Medicinemaker;
        public static ThingDef Dryad_Berrymaker;
        public static ThingDef Dryad_Woodmaker;
        public static ThingDef Plant_Healroot;
        public static ThingDef Plant_Strawberry;
        public static WorkGiverDef GrowerSow;
        public static HediffDef Dryad_Hediff;
        public static HediffDef Dryad_ConnectedHediff;
    }

    public class JobGiverCasteDuty : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {

            // Check if pawn is a Dryad_Medicinemaker
            if (pawn.def == DryadDefs.Dryad_Berrymaker)
            {

            }

            return null;
        }
    }

    public class CompProperties_NewTreeConnection : CompProperties_TreeConnection
    {
        public List<ThingDef> harmonyBuildings = new();
        public CompProperties_NewTreeConnection()
        {
            compClass = typeof(CompNewTreeConnection);
        }
    }

    public class GauLevel
    {
        const int maxLocalHarmony = 10;

        public float mapHarmony = 0;
        public int totalShrineWealth = 0;
        public int dryadCount = 0;
        public float localHarmony = 0;
        public float harmonyEfficiency = 0;
        public float mechanoidDisruption = 0;
        public float totalHarmony = 0;
        public float buffLevel = 0.10f;
        public string info = "";
        public string info_GauDist = "";

        public GauLevel(float mapHarmony, int totalShrineWealth, float localHarmony, float harmonyEfficiency, float mechanoidDisruption, float totalHarmony, float otherGauranlenDist, int stoneCount)
        {
            const int gDistClose = 10;
            const int gDistMid = 16;
            this.mapHarmony = mapHarmony;
            this.totalShrineWealth = totalShrineWealth;
            this.localHarmony = localHarmony;
            this.harmonyEfficiency = harmonyEfficiency;
            this.mechanoidDisruption = mechanoidDisruption;
            this.totalHarmony = totalHarmony;

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

            dryadCount = localHarmony switch
            {
                >= maxLocalHarmony => 4,
                > maxLocalHarmony / 2 => 3,
                > maxLocalHarmony / 4 => 2,
                _ => 1
            };

            var txtClr = ColorLibrary.Orange;
            info = "Dryad_MaxLevelMaybe".Translate().Colorize(ColorLibrary.Blue); ;

            if (dryadCount != 4)
            {
                info = "Dryad_NeedMoreHarmony".Translate(localHarmony.ToString("F1")).Resolve().Colorize(txtClr);
            }
            if (totalHarmony < -10)
            {
                info = "Dryad_NeedMapHarmony".Translate(totalHarmony.ToString("F0"), (-10).ToString("F0")).Colorize(ColorLibrary.RedReadable);
                buffLevel = 0.01f;
                dryadCount -= 1;
            }

            if (dryadCount == 4)
            {
                if (totalShrineWealth < 4000)
                {
                    info = "Dryad_ShrineWealthNeed".Translate(totalShrineWealth.ToString("F0"), 4000.ToString("F0")).Resolve().Colorize(txtClr);
                    dryadCount = 3;
                }
                else if (mapHarmony < 20)
                {
                    info = "Dryad_NeedMapHarmony".Translate(mapHarmony.ToString("F0"), 20.ToString("F0")).Resolve().Colorize(txtClr);
                    dryadCount = 3;
                }
                else if (totalShrineWealth > 15000 && mapHarmony > 40 && otherGauranlenDist > gDistMid)
                {
                    info = "Dryad_MaxLevelYES".Translate().Colorize(ColorLibrary.Green);
                    buffLevel = 1;
                }
                else if (totalShrineWealth > 6000 && mapHarmony > 20 && otherGauranlenDist > gDistClose)
                {
                    info = "Dryad_MaxLevel".Translate().Colorize(ColorLibrary.Green);
                    buffLevel = 0.75f;
                }
                else
                {
                    buffLevel = 0.5f;
                }
            }

            if (dryadCount == 3)
            {
                if (totalShrineWealth < 2100)
                {
                    info = "Dryad_ShrineWealthNeed".Translate(totalShrineWealth.ToString("F0"), 2000.ToString("F0")).Resolve().Colorize(txtClr);
                    dryadCount = 2;
                }
                else if (mapHarmony < 10)
                {
                    info = "Dryad_NeedMapHarmony".Translate(mapHarmony.ToString("F0"), 10f.ToString("F0")).Resolve().Colorize(txtClr);
                    dryadCount = 2;
                }
                else
                {
                    buffLevel = 0.5f;
                }
            }
            dryadCount+=stoneCount;

            if (otherGauranlenClose) info_GauDist = "Dryad_GauDistClose".Translate(otherGauranlenDist.ToString("F0")).Colorize(ColorLibrary.Orange);
            else if (otherGauranlenMid) info_GauDist = "Dryad_GauDistNear".Translate(otherGauranlenDist.ToString("F0")).Colorize(ColorLibrary.YellowGreen);
            else info_GauDist = "Dryad_GauDistFar".Translate(otherGauranlenDist.ToString("F0")).Colorize(ColorLibrary.Green);
        }
    }

    [HarmonyPatch]
    [StaticConstructorOnStartup]
    public class CompNewTreeConnection : CompTreeConnection
    {
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
        public FieldInfo dryadsFI = null;
        public FieldInfo spawnTickFI = null;
        public FieldInfo leafEffecterFI = null;

        protected int actualMaxDryads = 1;

        public override void CompTick()
        {
            if (!ModsConfig.IdeologyActive)
            {
                return;
            }
            if (Find.TickManager.TicksGame >= SpawnTick)
            {
                // Use reflection to run SpawnDryad();
                typeof(CompTreeConnection).GetMethod("SpawnDryad", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(this, null);
            }
            if (LeafEffecter == null)
            {
                // leafEffecter = EffecterDefOf.GauranlenLeavesBatch.Spawn();
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
            var gauLevel = GetGauLevel();
            actualMaxDryads = gauLevel.dryadCount;

            if (actualMaxDryads > 4) ConnectionStrength = 1f;
            if (actualMaxDryads == 4) ConnectionStrength = 0.98f;
            if (actualMaxDryads == 3) ConnectionStrength = 0.75f;
            if (actualMaxDryads == 2) ConnectionStrength = 0.5f;
            if (actualMaxDryads == 1) ConnectionStrength = 0.25f;
            if (actualMaxDryads == 0) ConnectionStrength = 0f;
            DesiredConnectionStrength = 0;

            // Fetch all dryads.
            foreach (var dryad in Dryads)
            {
                var dryadHediff = dryad.health.hediffSet.GetFirstHediffOfDef(DryadDefs.Dryad_Hediff);
                if (dryadHediff == null)
                {
                    dryad.health.AddHediff(DryadDefs.Dryad_Hediff);
                }
                else
                {
                    dryadHediff.Severity = gauLevel.buffLevel;
                }
            }

            if (ConnectedPawn != null)
            {
                var connectedPawnHediff = ConnectedPawn.health.hediffSet.GetFirstHediffOfDef(DryadDefs.Dryad_ConnectedHediff);
                if (connectedPawnHediff == null)
                {
                    connectedPawnHediff = ConnectedPawn.health.AddHediff(DryadDefs.Dryad_ConnectedHediff);
                    try
                    {
                        var dryadConnectionHediff = (DryadConnectionHediff)connectedPawnHediff;
                        dryadConnectionHediff.UpdateConnection(this, gauLevel.buffLevel);
                    } catch { }
                }
                else
                {
                    var dryadConnectionHediff = (DryadConnectionHediff)connectedPawnHediff;
                    dryadConnectionHediff.UpdateConnection(this, gauLevel.buffLevel);
                }
            }
        }

        private GauLevel GetGauLevel()
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

            // Grab first 4 shrines, or less if there are less than 4
            float totalShrineWealth = nearBuildings.Where(b => hb.Contains(b.def)).OrderByDescending(b => b.MarketValue).Take(3).Sum(b => b.MarketValue);

            // Check if any of the buildings nearby is the AnimusStone
            int animaStoneCount = veryNearBuildings.Sum(b => b.def == ThingDefOf.AnimusStone ? 1 : 0);

            totalShrineWealth *= (1 + animaStoneCount);

            return new GauLevel(mapHarmony, (int)totalShrineWealth, localHarmony, harmonyEfficiency, mechanoidDisruption, totalHarmony, otherGauranlenDist, animaStoneCount);

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
                var gauLevel = GetGauLevel();
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
                text += "\n" + gauLevel.info;
                text += "\n" + gauLevel.info_GauDist;
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
            dryad.health.AddHediff(DryadDefs.Dryad_Hediff);
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
        }
    }
}
