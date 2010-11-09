using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Xml.Serialization;
using System.Xml;

using OpenSim.Region.Framework.Scenes;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Framework;

using OpenMetaverse;
using log4net;

using ModularRex.RexFramework;
using System.Xml;
using System.Collections;

using OgreSceneImporter;

namespace NaaliSceneImporter
{
    public class NaaliEntity
    {
        public uint ImportId;
        public uint ParentId;

        public List<NaaliEntity> Children;

        public NaaliSceneData SceneData;
        public NaaliObjectData ObjectData;
        public string ComponentData;

        public NaaliEntity()
        {
            ImportId = 0;
            ParentId = 0;
            Children = new List<NaaliEntity>();
            
            SceneData = new NaaliSceneData();
            ObjectData = new NaaliObjectData();
            ComponentData = string.Empty;
        }

        public void SetImportId(string data)
        {
            ImportId = System.Convert.ToUInt32(data);
        }

        public void AddChild(NaaliEntity child)
        {
            Children.Add(child);
        }
    }

    public class NaaliObjectData
    {
        // Visuals
        public string MeshRef;
        public string CollisionMeshRef;
        public string SkeletonRef;
        public string ParticleRef;
        public List<string> Materials;
        public List<uint> MaterialTypes;
        
        // Script
        public string ServerScriptClass;

        // Sound
        public string SoundID;
        public float SoundVolume;
        public float SoundRadius;

        // Animation
        public string AnimationName;
        public float AnimationRate;

        // Drawing
        public uint DrawType;
        public float DrawDistance;
        public float LOD;
        public bool ScaleToPrim;
        public bool CastShadows;
        public bool LightCreatesShadows;
        public bool Visible;

        public NaaliObjectData()
        {
            MeshRef = string.Empty;
            CollisionMeshRef = string.Empty;
            SkeletonRef = string.Empty;
            ParticleRef = string.Empty;
            Materials = new List<string>();
            MaterialTypes = new List<uint>();
            
            ServerScriptClass = string.Empty;

            SoundID = string.Empty;
            SoundVolume = 0;
            SoundRadius = 0;

            AnimationName = string.Empty;
            AnimationRate = 0;

            DrawType = 1;
            DrawDistance = 0;
            LOD = 0;
            ScaleToPrim = false;
            CastShadows = false;
            LightCreatesShadows = false;
            Visible = true;
        }

        public void SetMaterials(string data)
        {
            if (data == string.Empty)
                return;
            foreach (string material in data.Split(';'))
                Materials.Add(material);
        }

        public void SetMaterialTypes(string data)
        {
            if (data == string.Empty)
                return;
            foreach (string materialType in data.Split(';'))
                MaterialTypes.Add(System.Convert.ToUInt32(materialType));
        }
    }

    public class NaaliSceneData
    {
        public Vector3 position;
        public Vector3 scale;
        public Quaternion orientation;

        public NaaliSceneData()
        {
            orientation = new Quaternion(0,0,0,0);
            position = Vector3.Zero;
            scale = Vector3.Zero;
        }

        public void SetPosition(string data)
        {
            string [] data_parts = data.Split(',');
            if (data_parts.Length >= 3)
            {
                position.X = (float)System.Convert.ToDouble(data_parts[0]);
                position.Y = (float)System.Convert.ToDouble(data_parts[1]);
                position.Z = (float)System.Convert.ToDouble(data_parts[2]);
            }
        }

        public void SetScale(string data)
        {
            string[] data_parts = data.Split(',');
            if (data_parts.Length >= 3)
            {
                scale.X = (float)System.Convert.ToDouble(data_parts[0]);
                scale.Y = (float)System.Convert.ToDouble(data_parts[1]);
                scale.Z = (float)System.Convert.ToDouble(data_parts[2]);
            }
        }

        public void SetOrientation(string data)
        {
            string[] data_parts = data.Split(',');
            if (data_parts.Length >= 4)
            {
                orientation.W = (float)System.Convert.ToDouble(data_parts[0]);
                orientation.X = (float)System.Convert.ToDouble(data_parts[1]);
                orientation.Y = (float)System.Convert.ToDouble(data_parts[2]);
                orientation.Z = (float)System.Convert.ToDouble(data_parts[3]);
            }
        }
    }

    public class NaaliSceneParser
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public List<NaaliEntity> ParseXml(string filename)
        {
            System.Xml.XmlDocument document = new XmlDocument();
            try
            {
                document.Load(filename);
            }
            catch (XmlException e)
            {
                m_log.ErrorFormat("[NAALISCENE]: Could not open XML file: {0}", e);
            }
            return ParseXmlDocument(document);
        }

        public List<NaaliEntity> ParseXml(byte[] data)
        {
            XmlDocument document = new XmlDocument();
            try
            {
                string s_data = System.Text.Encoding.ASCII.GetString(data).ToString();
                document.LoadXml(s_data);
            }
            catch (XmlException e)
            {
                m_log.ErrorFormat("[NAALISCENE]: Could not load XML data: {0}", e);
            }
            return ParseXmlDocument(document);
        }

        private List<NaaliEntity> ParseXmlDocument(XmlDocument document)
        {
            // We are interested in this combination 
            // when iterating the components of an entity
            string searchComponentType = "EC_DynamicComponent";
            string searchComponentName = "RexPrimExportData";

            List<NaaliEntity> entities = new List<NaaliEntity>();

            // Iterate all entitys
            XmlNodeList entityNodes = document.GetElementsByTagName("entity");
            foreach (XmlNode parseEntity in entityNodes)
            {
                if (parseEntity.HasChildNodes == false)
                    continue;

                string iid = parseEntity.Attributes["id"].Value;
                uint iiiddd = System.Convert.ToUInt32(parseEntity.Attributes["id"].Value);

                NaaliEntity entity = new NaaliEntity();
                entity.SetImportId(parseEntity.Attributes["id"].Value);
                entity.ComponentData += "<entity id=\"REPLACE_ENTITY_LOCAL_ID\">";

                // Iterate entity components
                int componentCount = 0;
                bool export_component_found = false;
                foreach (XmlNode component in parseEntity.ChildNodes)
                {
                    // Get component type and possible name
                    string component_type = component.Attributes["type"].Value;
                    string component_name = string.Empty;
                    if (component.Attributes["name"] != null)
                        component_name = component.Attributes["name"].Value;

                    // Parse NaaliSceneExportData component
                    if (component_type == searchComponentType && component_name == searchComponentName && component.HasChildNodes)
                    {
                        export_component_found = true;
                        foreach (XmlNode attribute in component.ChildNodes)
                        {
                            string name = attribute.Attributes["name"].Value;
                            string value = attribute.Attributes["value"].Value;
                            if (name == string.Empty)
                                continue;

                            switch (name)
                            {
                                case "Position":
                                    entity.SceneData.SetPosition(value);
                                    break;
                                case "Scale":
                                    entity.SceneData.SetScale(value);
                                    break;
                                case "Orientation":
                                    entity.SceneData.SetOrientation(value);
                                    break;
                                case "Parent":
                                    entity.ParentId = System.Convert.ToUInt32(value);
                                    break;
                                case "MeshRef":
                                    entity.ObjectData.MeshRef = value;
                                    break;
                                case "CollisionMeshRef":
                                    entity.ObjectData.CollisionMeshRef = value;
                                    break;
                                case "SkeletonRef":
                                    entity.ObjectData.SkeletonRef = value;
                                    break;
                                case "Materials":
                                    entity.ObjectData.SetMaterials(value);
                                    break;
                                case "MaterialTypes":
                                    entity.ObjectData.SetMaterialTypes(value);
                                    break;
                                case "ParticleRef":
                                    entity.ObjectData.ParticleRef = value;
                                    break;
                                case "DrawType":
                                    entity.ObjectData.DrawType = System.Convert.ToUInt32(value);
                                    break;
                                case "DrawDistance":
                                    entity.ObjectData.DrawDistance = (float)System.Convert.ToDouble(value);
                                    break;
                                case "ScaleToPrim":
                                    entity.ObjectData.ScaleToPrim = System.Convert.ToBoolean(value);
                                    break;
                                case "CastShadows":
                                    entity.ObjectData.CastShadows = System.Convert.ToBoolean(value);
                                    break;
                                case "LightCreatesShadows":
                                    entity.ObjectData.LightCreatesShadows = System.Convert.ToBoolean(value);
                                    break;
                                case "SoundID":
                                    entity.ObjectData.SoundID = value;
                                    break;
                                case "SoundVolume":
                                    entity.ObjectData.SoundVolume = (float)System.Convert.ToDouble(value);
                                    break;
                                case "SoundRadius":
                                    entity.ObjectData.SoundRadius = (float)System.Convert.ToDouble(value);
                                    break;
                                case "LOD":
                                    entity.ObjectData.LOD = (float)System.Convert.ToDouble(value);
                                    break;
                                case "Visible":
                                    entity.ObjectData.Visible = System.Convert.ToBoolean(value);
                                    break;
                                case "AnimationName":
                                    entity.ObjectData.AnimationName = value;
                                    break;
                                case "AnimationRate":
                                    entity.ObjectData.AnimationRate = (float)System.Convert.ToDouble(value);
                                    break;
                                case "ServerScriptClass":
                                    entity.ObjectData.ServerScriptClass = value;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    else
                    {
                        // Handle all other components
                        entity.ComponentData += component.OuterXml;
                        componentCount++;
                    }
                }

                if (export_component_found)
                {
                    // Debug prints
                    m_log.Debug("[NAALISCENE]: Found object with import data");
                    m_log.DebugFormat("[NAALISCENE]: >> Import id: {0}", entity.ImportId.ToString());
                    m_log.DebugFormat("[NAALISCENE]: >> Entity component count: {0}", componentCount.ToString());

                    // Finalize ec data
                    if (componentCount > 0)
                        entity.ComponentData += "</entity>";
                    else
                        entity.ComponentData = string.Empty;

                    // Try to find parent entity
                    bool parentFound = false;
                    foreach (NaaliEntity iterEntity in entities)
                    {
                        if (iterEntity.ImportId == entity.ParentId)
                        {
                            iterEntity.AddChild(entity);
                            parentFound = true;
                            m_log.Debug("[NAALISCENE]: >> Entity is a child, parent found");
                            break;
                        }
                    }

                    // Add entity to return list if parent was not found
                    if (!parentFound)
                        entities.Add(entity);                    
                }
                else
                {
                    // Inform when skipping objects, something wrong with the xml for this entity
                    m_log.Info("[NAALISCENE]: Skipping object, import data missing");
                    m_log.InfoFormat("[NAALISCENE]: >> Import id: {0}", entity.ImportId.ToString());
                    m_log.InfoFormat("[NAALISCENE]: >> Reason: could not find {0} component", searchComponentName.ToString());
                }
            }

            m_log.InfoFormat("[NAALISCENE]: XML parsing results: {0} entities found.", entities.Count.ToString());
            return entities;
        }
    }
}