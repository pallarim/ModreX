using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using OpenSim.Region.Environment.Scenes;
using OpenMetaverse;

namespace ModularRex.RexParts.RexObjects
{
    public class RexMaterialsDictionary : Dictionary<uint, UUID>, ICloneable, IXmlSerializable
    {
        private RexObjectPart MyPart = null;

        public void SetSceneObjectPart(RexObjectPart vPart)
        {
            MyPart = vPart;
        }

        public void AddMaterial(uint vIndex, UUID vMaterialUUID)
        {
            lock (this)
            {
                this[vIndex] = vMaterialUUID;
            }
            if (MyPart != null)
                MyPart.ScheduleRexPrimUpdate(true);
        }

        public void DeleteMaterialByIndex(uint vIndex)
        {
            lock (this)
            {
                if (this.ContainsKey(vIndex))
                {
                    this.Remove(vIndex);
                    if (MyPart != null)
                        MyPart.ScheduleRexPrimUpdate(true);
                }
            }
        }

        public void ClearMaterials()
        {
            lock (this)
            {
                this.Clear();
            }
            if (MyPart != null)
                MyPart.ScheduleRexPrimUpdate(true);
        }


        public void ReadXml(XmlReader reader)
        {

        }

        public void WriteXml(XmlWriter writer)
        {

        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public Object Clone()
        {
            RexMaterialsDictionary clone = new RexMaterialsDictionary();
            lock (this)
            {
                foreach (uint matindex in Keys)
                    clone.Add(matindex, this[matindex]);
            }
            return clone;
        }

        public override string ToString()
        {
            string s = String.Empty;

            foreach (uint matindex in Keys)
                s = s + "idx:" + matindex.ToString() + ",value:" + this[matindex].ToString() + "\n";

            return s;
        }
    }
}
