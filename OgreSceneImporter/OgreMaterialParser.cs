using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using OpenSim.Framework;
using log4net;
using System.Reflection;

namespace OgreSceneImporter
{
    public class OgreMaterialParser
    {
        private static readonly ILog m_log
            = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Scene m_scene = null;

        public OgreMaterialParser(Scene scene)
        {
            m_scene = scene;
        }

        public bool ParseAndSaveMaterial(string materialScript, out Dictionary<string, UUID> materialUUIDs, out Dictionary<string, UUID> textureUUIDs)
        {
            materialUUIDs = new Dictionary<string, UUID>();
            textureUUIDs = new Dictionary<string, UUID>();
            System.IO.StringReader reader = new System.IO.StringReader(materialScript);
            try
            {
                string line = reader.ReadLine();
                while (line != null)
                {
                    if (line.StartsWith("material"))
                    {
                        string[] matStrParts = line.Split(' ');
                        string materialName = String.Empty;
                        if (matStrParts.Length > 2)
                        {
                            if (matStrParts[1].StartsWith("\""))
                            {
                                //combine rest of the parts together
                                for (int i = 1; i < matStrParts.Length; i++)
                                {
                                    materialName += matStrParts[i];
                                    if (i < matStrParts.Length - 1)
                                        materialName += " ";
                                }
                                //and remove the quotes from it
                                materialName = materialName.Replace("\"", "");
                            }
                            else
                            {
                                m_log.ErrorFormat("[OGRESCENE]: Could not parse material name from malformed material file line \"{0}\"", line);
                                return false;
                            }
                        }
                        else
                        {
                            materialName = matStrParts[1];
                        }

                        UUID materialID = UUID.Random();
                        try
                        {
                            materialUUIDs.Add(materialName, materialID);
                        }
                        catch (Exception)
                        {
                            m_log.ErrorFormat("[OGRESCENE]: duplicate material \"{0}\"", materialName);
                        }

                        StringBuilder material = new StringBuilder();
                        material.AppendLine("material " + materialID.ToString());
                        int openBracets = 0;
                        do
                        {
                            line = reader.ReadLine();
                            if (line.StartsWith("{"))
                                openBracets++;
                            if (line.StartsWith("}"))
                                openBracets--;

                            string tempLine = line.TrimStart(' ', '\t');
                            if (tempLine.StartsWith("texture "))
                            {
                                string[] tempLineParts = tempLine.Split(' ');
                                string textName = String.Empty;
                                if (tempLineParts.Length > 2)
                                {
                                    if (tempLineParts[1].StartsWith("\""))
                                    {
                                        //combine rest of the parts together
                                        for (int i = 1; i < tempLineParts.Length; i++)
                                        {
                                            textName += tempLineParts[i];
                                            if (i < tempLineParts.Length - 1)
                                                textName += " ";
                                        }
                                        //and remove the quotes from it
                                        textName = textName.Replace("\"", "");
                                    }
                                    else
                                    {
                                        m_log.ErrorFormat("[OGRESCENE]: Could not parse texture name from malformed material file line \"{0}\"", line);
                                        return false;
                                    }
                                }
                                else
                                {
                                    textName = tempLineParts[1];
                                }

                                UUID textUUID;
                                if (!textureUUIDs.ContainsKey(textName))
                                {
                                    textUUID = UUID.Random();
                                    textureUUIDs.Add(textName, textUUID);
                                }
                                else
                                {
                                    textUUID = textureUUIDs[textName];
                                }

                                line = line.Replace(textName, textUUID.ToString());
                            }
                            material.AppendLine(line);
                        } while (!(line.StartsWith("}") && openBracets <= 0));

                        AssetBase asset = new AssetBase(materialID, materialName, 45, m_scene.RegionInfo.EstateSettings.EstateOwner.ToString()); //45 is OgreMaterial asset type
                        asset.Data = Utils.StringToBytes(material.ToString());
                        m_scene.AssetService.Store(asset);
                    }

                    line = reader.ReadLine();
                }
            }
            catch (Exception exp)
            {
                m_log.WarnFormat("Exception while parsing materials, closing filereader: {0}", exp.Message);
                reader.Close();
                throw;
            }

            return true;
        }
    }
}