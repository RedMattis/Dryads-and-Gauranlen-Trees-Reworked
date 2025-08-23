using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Dryad
{
    public class CompProperties_GauranLenConnection : CompProperties
    {
        public int maxDistanceFromTree = 16;
        public CompProperties_GauranLenConnection()
        {
            compClass = typeof(CompGauranlenConnection);
        }
    }
    public class CompGauranlenConnection : ThingComp
    {
        public CompProperties_GauranLenConnection Props => (CompProperties_GauranLenConnection)props;
        public ThingWithComps parentTree = null;

        private CompNewTreeConnection _parentTreeComp = null;
        public CompNewTreeConnection ParentTreeComp => _parentTreeComp ??= parentTree.GetComp<CompNewTreeConnection>();

        public void SetParentTree(ThingWithComps tree)
        {
            parentTree = tree;
            _parentTreeComp = null;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();

            if (parentTree != null)
            {
                GenDraw.DrawLineBetween(parentTree.TrueCenter(), parent.TrueCenter(), SimpleColor.Green);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref parentTree, "parentTree");
        }
    }
}
