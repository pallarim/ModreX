using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Framework;
using OpenMetaverse;

namespace ModularRex.RexDBObjects
{
    public class RexUserAgentData : UserAgentData
    {
        public UUID currentRegion;

        public virtual UUID CurrentRegion
        {
            get { return currentRegion; }
            set { currentRegion = value; }
        }
    }
}
