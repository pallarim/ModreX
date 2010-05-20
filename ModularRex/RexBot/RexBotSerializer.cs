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

namespace OpenSim.Region.Examples.RexBot
{
    class RexBotSerializer
    {
        public void ImportName(RexBot bot, XmlNode node)
        {
            foreach (XmlNode childNode in node.ChildNodes)
            {
                if (childNode.Name == "first_name")
                {
                    bot.FirstName = childNode.InnerText;
                }
                if (childNode.Name == "last_name")
                {
                    bot.LastName = childNode.InnerText;
                }
            }
        }

        /// <summary>
        /// Gets the name of the region.
        /// </summary>
        /// <param name="node">The node of bot</param>
        /// <returns>Region name or null if property is not set</returns>
        public string GetRegionName(XmlNode node)
        {
            string regionName = null;

            foreach (XmlNode childNode in node.ChildNodes)
            {
                if (childNode.Name == "region")
                {
                    regionName = childNode.InnerText;
                }
            }

            return regionName;
        }

        // imports rex bot config from xml node. uses strict parsing, throws exception from unknown elements.
        public void ImportRexBot(RexBot bot, XmlNode node)
        {
            foreach (XmlNode childNode in node.ChildNodes)
            {
                switch (childNode.Name)
                {
                    case "first_name":
                        bot.FirstName = childNode.InnerText;
                        break;

                    case "last_name":
                        bot.LastName = childNode.InnerText;
                        break;

                    case "storage_address":
                        bot.SetBotAppearance(childNode.InnerText);
                        break;

                    case "disable_walk":
                        bot.DisableWalk(Convert.ToBoolean(childNode.InnerText));
                        break;

                    case "movement_mod":
                        bot.SetMovementSpeedMod((float)Convert.ToDouble(childNode.InnerText));
                        break;

                    case "path":
                        parsePath(bot, childNode);
                        
                        break;

                    case "admin_mode":
                        bot.AdminMode = (Convert.ToBoolean(childNode.InnerText));
                        break;

//                    default:
//                        throw new System.Xml.XmlException("Unknown xml element: " + childNode.Name + ".");
                }
            }
        }

        private void parsePath(RexBot bot, XmlNode node)
        {
            string name = node.Attributes.GetNamedItem("name").Value;

            TravelMode mode = NavMeshInstance.DEFAULT_TRAVELMODE;
            int startNode = NavMeshInstance.DEFAULT_STARTNODE;
            bool random = NavMeshInstance.DEFAULT_RANDOM;
            bool reverse = NavMeshInstance.DEFAULT_REVERSE;
            bool allowU = NavMeshInstance.DEFAULT_ALLOWU;
            int timeOut = NavMeshInstance.DEFAULT_TIMEOUT;
            try
            {
                mode = NavMesh.ParseTravelMode(node.Attributes.GetNamedItem("mode").Value);
            }
            catch (Exception) { }
            try
            {
                startNode = Convert.ToInt32(node.Attributes.GetNamedItem("start_node").Value);
            }
            catch (Exception) {}
            try
            {
                random = Convert.ToBoolean(node.Attributes.GetNamedItem("random").Value);
            }
            catch (Exception) { }
            try
            {
                reverse = Convert.ToBoolean(node.Attributes.GetNamedItem("reverse").Value);
            }
            catch (Exception) {}
            try
            {
                allowU = Convert.ToBoolean(node.Attributes.GetNamedItem("allow_u").Value);
            }
            catch (Exception) { }
            try
            {
                timeOut = Convert.ToInt32(node.Attributes.GetNamedItem("timeout").Value);
            }
            catch (Exception) { }

            bot.SetPath(name, mode, startNode, random, reverse, allowU, timeOut);
        }
    }
}
