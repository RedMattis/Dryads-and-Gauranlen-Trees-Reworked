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
    public class ThinkNode_ConditionalIsDef : ThinkNode_Conditional
    {
        private List<ThingDef> defList;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            ThinkNode_ConditionalIsDef obj = (ThinkNode_ConditionalIsDef)base.DeepCopy(resolve);
            obj.defList = new List<ThingDef>(defList);
            return obj;
        }

        protected override bool Satisfied(Pawn pawn)
        {
            if (defList == null)
            {
                return false;
            }
            return defList.Contains(pawn.def);
        }
    }

}
