using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Dryad
{
    public class GauranlenTracker : GameComponent
    {
        public HashSet<CompGauranlenConnection> gauPlants = [];
        public HashSet<CompNewTreeConnection> allTrees = [];
        public GauranlenTracker(Game game) { }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (Find.TickManager.TicksGame % 15000 == 0)
            {
                // Remove any null or destroyed entries
                gauPlants.RemoveWhere(p => p == null || p.parent == null || p.parent.Destroyed);
                allTrees.RemoveWhere(t => t == null || t.parent == null || t.parent.Destroyed);
                foreach (var map in Find.Maps)
                {
                    foreach (var building in map.listerBuildings.allBuildingsColonist)
                    {
                        if (building.GetComp<CompGauranlenConnection>() is CompGauranlenConnection plant)
                        {
                            gauPlants.Add(plant);
                        }
                        // Check if the building has the CompNewTreeConnection comp
                        if (building.GetComp<CompNewTreeConnection>() is CompNewTreeConnection tree)
                        {
                            allTrees.Add(tree);
                        }
                    }
                }
                // Iterrate the turrets and make sure they are listed in their parent.
                for (int i = gauPlants.Count - 1; i >= 0; i--)
                {
                    var plant = gauPlants.ElementAt(i);
                    // Remove if not supportable by parent.
                    if (plant.parentTree != null && !plant.ParentTreeComp.CanSupportPlant(plant.parent.def, additional: false))
                    {
                        plant.ParentTreeComp.RemovePlant(plant.parent);
                        plant.SetParentTree(null);
                    }
                    plant.parentTree ??= GetClosestTreeWithFreeSlotForPlant(plant);
                    if (plant.parentTree == null)
                    {
                        plant.parent.Destroy();
                        gauPlants.Remove(plant);
                    }
                }
            }
        }

        public ThingWithComps GetClosestTreeWithFreeSlotForPlant(CompGauranlenConnection plant)
        {
            // Created a sorted list of trees by distance to the turret.
            var pos = plant.parent.Position;
            var sortedTrees = allTrees
                .Where(t => t.parent.Position.DistanceTo(pos) <= plant.Props.maxDistanceFromTree)
                .OrderBy(t => t.parent.Position.DistanceTo(pos));
            foreach (var tree in sortedTrees)
            {
                if (tree.CanSupportPlant(plant.parent.def, additional:true))
                {
                    tree.AddPlant(plant.parent);
                    return tree.parent;
                }
            }
            return null;
        }
    }
}
