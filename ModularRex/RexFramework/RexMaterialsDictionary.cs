using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using log4net;
using OpenMetaverse;



namespace ModularRex.RexFramework
{
    /// <summary>
    /// Rex Material Dictionary
    /// May get unfolded as a seperate class
    /// </summary>
    public class RexMaterialsDictionary : Dictionary<uint, UUID>, ICloneable, IXmlSerializable
    {
        private RexObjectProperties MyPart;

        public void SetSceneObjectPart(RexObjectProperties vPart)
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
                MyPart.TriggerChangedRexObjectProperties();
        }

        public void DeleteMaterialByIndex(uint vIndex)
        {
            lock (this)
            {
                if (ContainsKey(vIndex))
                {
                    Remove(vIndex);
                    if (MyPart != null)
                        MyPart.TriggerChangedRexObjectProperties();
                }
            }
        }

        public void ClearMaterials()
        {
            lock (this)
            {
                Clear();
            }
            if (MyPart != null)
                MyPart.TriggerChangedRexObjectProperties();
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
                s = s + "idx:" + matindex + ",value:" + this[matindex] + "\n";

            return s;
        }
    }

    public class RexMaterialsDictionaryItem
    {
        private int id = 0;
        public int ID
        {
            get { return id; }
            set { id = value; }
        }

        public RexMaterialsDictionaryItem() { }

        public RexMaterialsDictionaryItem(KeyValuePair<uint, UUID> e)
        {
            num = e.Key;
            assetId = e.Value;
        }

        public KeyValuePair<uint,UUID> getDictionaryEntry()
        {
            return new KeyValuePair<uint, UUID>(num, assetId);
        }

        private uint num = 0;
        public uint Num
        {
            get { return num; }
            set { num = value; }
        }

        private UUID assetId = UUID.Zero;
        public UUID AssetID
        {
            get { return assetId; }
            set { assetId = value; }
        }

        private UUID rexObjectUUID;
        public UUID RexObjectUUID
        {
            get { return rexObjectUUID; }
            set { rexObjectUUID = value; }
        }
    }

}
