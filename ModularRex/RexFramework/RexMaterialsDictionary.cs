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
    public class RexMaterialsDictionary : Dictionary<uint, RexMaterialsDictionaryItem>, ICloneable, IXmlSerializable
    {
        private RexObjectProperties MyPart;

        public void SetSceneObjectPart(RexObjectProperties vPart)
        {
            MyPart = vPart;
        }

        public void AddMaterial(uint index, UUID materialUUID)
        {
            lock (this)
            {
                if (this.ContainsKey(index))
                {
                    this[index].AssetID = materialUUID;
                }
                else
                {
                    this[index] = new RexMaterialsDictionaryItem();
                    this[index].AssetID = materialUUID;
                }
            }
            if (MyPart != null)
                MyPart.TriggerChangedRexObjectProperties();
        }

        public void AddMaterial(uint index, UUID materialUUID, string materialURL)
        {
            lock (this)
            {
                if (this.ContainsKey(index))
                {
                    this[index].AssetID = materialUUID;
                    this[index].AssetURL = materialURL;
                }
                else
                {
                    this[index] = new RexMaterialsDictionaryItem();
                    this[index].AssetID = materialUUID;
                    this[index].AssetURL = materialURL;
                }
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
                s = s + "idx:" + matindex + ",value:" + this[matindex].ToString() + "\n";

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

        public RexMaterialsDictionaryItem(KeyValuePair<uint, RexMaterialsDictionaryItem> e)
        {
            num = e.Key;
            assetId = e.Value.AssetID;
            assetUrl = e.Value.AssetURL;
        }

        private uint num = 0;
        public uint Num
        {
            get { return num; }
            set { num = value; }
        }

        private UUID assetId = UUID.Zero;
        /// <summary>
        /// The ID of the asset
        /// </summary>
        public UUID AssetID
        {
            get { return assetId; }
            set { assetId = value; }
        }

        private string assetUrl = String.Empty;
        /// <summary>
        /// The url of the asset where it is located
        /// </summary>
        public string AssetURL
        {
            get { return assetUrl; }
            set { assetUrl = value; }
        }

        private UUID rexObjectUUID;
        /// <summary>
        /// The UUID of the object to which this entry belongs to
        /// </summary>
        public UUID RexObjectUUID
        {
            get { return rexObjectUUID; }
            set { rexObjectUUID = value; }
        }

        public override string ToString()
        {
            return num.ToString() + ";" + assetId.ToString() + ";" + assetUrl;
        }
    }

}
