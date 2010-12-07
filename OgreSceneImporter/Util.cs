using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Framework.Servers.HttpServer;
using System.IO;
using System.Xml.Serialization;
using OgreSceneImporter.UploadSceneDB;

namespace OgreSceneImporter
{
    public static class Util
    {
        public static byte[] GetBytesFromRequestBody(OSHttpRequest httpRequest)
        {
            byte[] data = new byte[httpRequest.ContentLength];
            int r;
            int offset = 0;
            while ((r = httpRequest.InputStream.Read(data, offset, data.Length - offset)) > 0)
                offset += r;
            return data;
        }

        public static string GetDataFromRequestBody(OSHttpRequest httpRequest)
        {
            byte[] data = GetBytesFromRequestBody(httpRequest);

            byte[] content = ReadContent(data);
            string retData;
            System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
            retData = enc.GetString(content);
            int end = retData.IndexOf('\r');
            if (end != -1)
            {
                retData = retData.Remove(end);
            }
            return retData;
        }

        public static byte[] ReadContent(byte[] data)
        {
            MemoryStream mstream = new MemoryStream(data);
            System.IO.BinaryReader reader = new BinaryReader(mstream);
            //reader.BaseStream.Position
            byte[] startBytes;
            if (mstream.Length > 500)
            {
                startBytes = reader.ReadBytes(500);
            }
            else
            {
                startBytes = reader.ReadBytes((int)mstream.Length);
            }
            string startString = System.Text.Encoding.ASCII.GetString(startBytes).ToString();

            // find 2 linefeeds in a row, marks the end of headers
            int startIndex = 0;
            bool crlf = false;
            int LF_index = startString.IndexOf("\n", 0);
            int CRLF_index = startString.IndexOf("\r\n", 0);
            if (CRLF_index != -1)
            {
                crlf = true;
            }

            if (crlf)
            {
                startIndex = startString.IndexOf("\r\n\r\n", 38);
            }
            else
            {
                startIndex = startString.IndexOf("\n\n", 36);
            }

            int contentStartIndex;
            if (crlf)
                contentStartIndex = startIndex + 4;
            else
                contentStartIndex = startIndex + 2;
            int contentEndIndex = (int)mstream.Length - 38; // 38 = length of "--uuid--"
            int byteCount = contentEndIndex - contentStartIndex;
            byte[] fileBuffer = new byte[byteCount];

            Buffer.BlockCopy(data, contentStartIndex, fileBuffer, 0, byteCount);

            return fileBuffer;
        }

        public static byte[] ConstructResponceBytesFromDictionary<T, Y>(SerializableDictionary<T, Y> dictionary)
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(SerializableDictionary<T, Y>));
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            StringWriter sw = new StringWriter(sb);
            serializer.Serialize(sw, dictionary);
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            return encoding.GetBytes(sb.ToString());
        }

        public static byte[] CreateErrorResponse(string error)
        {
            SerializableDictionary<string, string> errorMessage = new SerializableDictionary<string, string>();
            errorMessage.Add("error", error);
            return ConstructResponceBytesFromDictionary(errorMessage);
        }

        public static SceneAsset GetWithNameFromList(string name, List<SceneAsset> meshes)
        {
            foreach (SceneAsset sa in meshes)
            {
                if (sa.Name == name)
                {
                    return sa;
                }
            }
            return null;
        }
    }

    [XmlRoot("dictionary")]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
    {

        #region IXmlSerializable Members

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }


        public void ReadXml(System.Xml.XmlReader reader)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));


            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();


            if (wasEmpty)
                return;


            while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
            {
                reader.ReadStartElement("item");


                reader.ReadStartElement("key");
                TKey key = (TKey)keySerializer.Deserialize(reader);
                reader.ReadEndElement();


                reader.ReadStartElement("value");
                TValue value = (TValue)valueSerializer.Deserialize(reader);

                reader.ReadEndElement();

                this.Add(key, value);


                reader.ReadEndElement();
                reader.MoveToContent();

            }
            reader.ReadEndElement();
        }


        public void WriteXml(System.Xml.XmlWriter writer)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));
            foreach (TKey key in this.Keys)
            {
                writer.WriteStartElement("item");


                writer.WriteStartElement("key");
                keySerializer.Serialize(writer, key);
                writer.WriteEndElement();

                writer.WriteStartElement("value");
                TValue value = this[key];
                valueSerializer.Serialize(writer, value);
                writer.WriteEndElement();

                writer.WriteEndElement();

            }

        }

        #endregion
    }
}
