/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Xml;
using OpenMetaverse;


namespace OpenSim.Region.Examples.RexBot
{
    public class NavMeshSerializer
    {
        private static readonly log4net.ILog m_log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void Import(NavMesh mesh, XmlNode node)
        {
            mesh.Name = node.Attributes.GetNamedItem("name").Value;
            mesh.DefaultMode = NavMesh.ParseTravelMode(node.Attributes.GetNamedItem("default_mode").Value);

            foreach (XmlNode childNode in node.ChildNodes)
            {
                switch (childNode.Name)
                {
                    case "mesh_node":
                        Vector3 meshNodePos;
                        string pos = childNode.Attributes.GetNamedItem("p").Value;
                        try
                        {
                            meshNodePos = Vector3.Parse(pos);
                        }
                        catch (Exception e)
                        {
                            throw new System.Xml.XmlException("Failed to parse mesh_node position: " + pos + ". Reason: " + e.Message);
                        }
                        mesh.AddNode(meshNodePos);
                    break;

                    case "mesh_edge":
                        int e1 = Convert.ToInt32(childNode.Attributes.GetNamedItem("e1").Value);
                        int e2 = Convert.ToInt32(childNode.Attributes.GetNamedItem("e2").Value);
                        TravelMode mode = mesh.DefaultMode;
                        
                        try
                        {
                            mode = NavMesh.ParseTravelMode(childNode.Attributes.GetNamedItem("mode").Value);
                        } catch (Exception) {}

                        try
                        {
                            mesh.AddEdge(e1, e2, mode);
                        }
                        catch (System.ArgumentException)
                        {
                            m_log.Warn("[NavMeshSerializer]: Duplicate edge from: " + e1.ToString() + " to: " + e2.ToString() + ".");
                        }
                        break;

//                    default:
//                        throw new System.Xml.XmlException("Unknown xml element: " + childNode.Name + ".");
                }
            }
        }
    }
}
