using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Region.Environment.Scenes;

namespace ModularRex.RexParts.RexObjects
{
    public class RexObjectGroup : SceneObjectGroup 
    {
        public void FromSceneObjectGroup(SceneObjectGroup origin)
        {
            string xml = origin.ToXmlString2();
            
        }
    }
}
