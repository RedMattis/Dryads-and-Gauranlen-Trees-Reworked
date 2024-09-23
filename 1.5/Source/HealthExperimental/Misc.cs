using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Dryad
{
    public class PlaceWorker_Woody : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            if (!ModsConfig.IdeologyActive)
            {
                return;
            }
            CompProperties_NewTreeConnection compProperties_TreeConnection = (CompProperties_NewTreeConnection)def.CompDefFor<CompNewTreeConnection>();
            List<Thing> list = GauranlenUtility.BuildingsAffectingConnectionStrengthAt(center, Find.CurrentMap, compProperties_TreeConnection);
            GenDraw.DrawRadiusRing(center, compProperties_TreeConnection.radiusToBuildingForConnectionStrengthLoss, Color.white);
            if (list.NullOrEmpty())
            {
                return;
            }
            int num = 0;
            foreach (Thing item in list)
            {
                if (num++ > 10)
                {
                    break;
                }
                GenDraw.DrawLineBetween(GenThing.TrueCenter(center, Rot4.North, def.size, def.Altitude), item.TrueCenter(), SimpleColor.Red);
            }
        }
    }
}
