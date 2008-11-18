using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using OpenMetaverse;
using OpenSim.Region.Environment.Scenes;

namespace ModularRex.RexParts.RexObjects
{
    public class RexObjectPart : SceneObjectPart 
    {
        private UUID m_rexVisualMesh;
        private UUID m_rexCollisionMesh;
        private List<UUID> m_materials = new List<UUID>(10);

        public void ConvertFromSceneObjectPart(SceneObjectPart origin)
        {
            this.Acceleration = origin.Acceleration;
            this.AngularVelocity = origin.AngularVelocity;
            this.BaseMask = origin.BaseMask;
            this.Category = origin.Category;
            this.ClickAction = origin.ClickAction;
            //this.Color = origin.Color;
            this.CreationDate = origin.CreationDate;
            this.CreatorID = origin.CreatorID;
            this.Description = origin.Description;
           



        }

        public static RexObjectPart FromRexXml(XmlReader xmlReader)
        {
            XmlSerializer serializer = new XmlSerializer(typeof (RexObjectPart));
            RexObjectPart newobject = (RexObjectPart)serializer.Deserialize(xmlReader);
            return newobject;
        }

    }
}
