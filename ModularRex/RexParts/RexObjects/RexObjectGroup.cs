using System.Xml;
using OpenSim.Region.Environment.Scenes;

namespace ModularRex.RexParts.RexObjects
{
    public class RexObjectGroup : SceneObjectGroup 
    {
        public void FromSceneObjectGroup(SceneObjectGroup origin)
        {
            // Dodgy, but hey it works!
            string xml = origin.ToXmlString2();
            SetFromXml(xml);
        }

        protected override SceneObjectPart CreatePartFromXml(XmlTextReader reader)
        {
            return RexObjectPart.FromXml(reader);
        }
    }
}
