using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Dryad
{
    public class DryadConnectionHediff : HediffWithComps
    {
        public Dictionary<CompNewTreeConnection, float> compNewTreeConnections = new();

        public void UpdateConnection(CompNewTreeConnection compNewTreeConnection, float severity)
        {
            if (compNewTreeConnections.ContainsKey(compNewTreeConnection))
            {
                compNewTreeConnections[compNewTreeConnection] = severity;
            }
            else
            {
                compNewTreeConnections.Add(compNewTreeConnection, severity);
            }

            // Set severity to the highest value
            Severity = compNewTreeConnections.Values.Max();
        }
    }
}
