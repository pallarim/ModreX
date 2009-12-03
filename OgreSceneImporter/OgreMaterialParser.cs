using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using OpenSim.Framework;

namespace OgreSceneImporter
{
    public class OgreMaterialParser
    {
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
            string line = reader.ReadLine();
            while (line != null)
            {
                if (line.StartsWith("material"))
                {
                    string[] matStrParts = line.Split(' ');
                    if (matStrParts.Length > 2)
                        Console.WriteLine("more than two parts in material name");
                    string materialName = matStrParts[1];
                    UUID materialID = UUID.Random();
                    materialUUIDs.Add(materialName, materialID);
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
                            if (tempLineParts.Length > 2)
                                Console.WriteLine("more than two parts in texture");
                            string textName = tempLineParts[1];
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

                    AssetBase asset = new AssetBase(materialID, materialName, 45); //45 is OgreMaterial asset type
                    asset.Data = Utils.StringToBytes(material.ToString());
                    m_scene.AssetService.Store(asset);
                }

                line = reader.ReadLine();
            }

            return true;
        }
    }
}
