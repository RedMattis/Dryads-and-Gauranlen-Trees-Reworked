using Dryads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Verse;

namespace Dryad
{
    public abstract class PatchOp_IfSettings : PatchOperation
    {
        protected readonly PatchOperation match;
        protected readonly PatchOperation nomatch;
        abstract protected bool ShouldApply();
        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (ShouldApply())
            {
                if (match != null)
                {
                    return match.Apply(xml);
                }
            }
            else if (nomatch != null)
            {
                return nomatch.Apply(xml);
            }
            return true;
        }
    }
    public class PatchOp_IfNoAwakened : PatchOp_IfSettings { protected override bool ShouldApply() => Main.settings.noAwakendDryads; }
}
