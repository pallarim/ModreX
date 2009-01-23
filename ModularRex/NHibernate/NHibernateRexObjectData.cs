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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using OpenMetaverse;
using log4net;
using NHibernate;
using NHibernate.Criterion;
using OpenSim.Framework;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Scenes;
using Environment = NHibernate.Cfg.Environment;
using ModularRex.RexFramework;

namespace OpenSim.Data.NHibernate
{
    /// <summary>
    /// A RegionData Interface to the NHibernate database
    /// </summary>
    public class NHibernateRexObjectData
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public bool Inizialized = false;

        public NHibernateManager manager;

        public void Initialise(string connect)
        {
            m_log.InfoFormat("[NHIBERNATE] Initializing NHibernateRexObjectData");
            Assembly assembly = GetType().Assembly;
            manager = new NHibernateManager(connect, "RexObjectData", assembly);
            //manager.configuration.AddAssembly("ModularRex.NHibernate");
            Inizialized = true;
        }

        public void Dispose() { }

        private void SaveOrUpdate(RexObjectProperties p)
        {
            try
            {
                IList<RexMaterialsDictionaryItem> templist = p.RexMaterialDictionaryItems;
                if (p.RexMaterialDictionaryItems == null)
                {
                    p.RexMaterialDictionaryItems = new List<RexMaterialsDictionaryItem>();
                }
                int i;
                ISession session = manager.GetSession();
                ICriteria criteria = session.CreateCriteria(typeof(RexObjectProperties)).Add(Restrictions.Eq("ParentObjectID", p.ParentObjectID)).SetProjection(Projections.Count("ParentObjectID"));
                if (session == null || criteria == null)
                {
                    i = 0;
                }
                else
                {
                    object tmp = criteria.UniqueResult();
                    if (tmp is int && tmp != null)
                    {
                        i = (int)tmp;
                    }
                    else
                    {
                        i = 0;
                    }
                }
                session.Close();
                if (i != 0)
                {
                    m_log.InfoFormat("[NHIBERNATE] updating RexObjectProperties {0}", p.ParentObjectID);
                    manager.Update(p);
                    if (templist != null && templist.Count > 0)
                    {
                        try
                        {
                            foreach (RexMaterialsDictionaryItem item in templist)
                            {
                                m_log.Debug("hibernate item id = " + item.ID);
                                if (item.ID == 0)
                                {
                                    session = manager.GetSession();
                                    ICriteria criteria2 = session.CreateCriteria(typeof(RexMaterialsDictionaryItem));
                                    criteria2.Add(Restrictions.Eq("RexObjectUUID", p.ParentObjectID));
                                    criteria2.Add(Restrictions.Eq("Num", item.Num));
                                    criteria2.SetMaxResults(1);
                                    List<RexMaterialsDictionaryItem> list = (List<RexMaterialsDictionaryItem>)criteria2.List<RexMaterialsDictionaryItem>();
                                    session.Close();
                                    if (list.Count == 0)
                                    {
                                        item.RexObjectUUID = p.ParentObjectID;
                                        manager.Save(item);
                                    }
                                    else
                                    {
                                        list[0].AssetID = item.AssetID;
                                        manager.Update(list[0]);
                                    }

                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            m_log.Error("[NHibernate]: Exception was thrown while processing RexObjectMaterials" + ex);
                        }
                        
                    }
                    else m_log.Debug("nhibernate templist = null\n#!%$£");
                }
                else
                {
                    m_log.InfoFormat("[NHIBERNATE] saving RexObjectProperties {0}", p.ParentObjectID);
                    p.RexMaterialDictionaryItems = new List<RexMaterialsDictionaryItem>();
                    manager.Save(p);
                }
            }
            catch (Exception e)
            {
                m_log.Error("[NHIBERNATE] issue saving RexObjectProperties: "+ e.Message + e.Source + e.StackTrace);
            }
        }

        /// <summary>
        /// Adds an object into region storage
        /// </summary>
        /// <param name="obj">the object</param>
        /// <param name="regionUUID">the region UUID</param>
        public void StoreObject(RexObjectProperties obj)
        {
            try
            {
                //foreach (SceneObjectPart part in obj.Children.Values)
                //{
                    m_log.InfoFormat("Storing part {0}", obj.ParentObjectID);
                    SaveOrUpdate(obj);
                //}
            }
            catch (Exception e)
            {
                m_log.Error("Can't save: ", e);
            }
        }

        public RexObjectProperties LoadObject(UUID uuid)
        {
            try
            {
                RexObjectProperties obj = new RexObjectProperties();
                ICriteria criteria = manager.GetSession().CreateCriteria(typeof(RexObjectProperties));
                criteria.Add(Expression.Eq("ParentObjectID", uuid));
                criteria.AddOrder(Order.Asc("ParentObjectID"));

                foreach (RexObjectProperties p in criteria.List())
                {
                    // root part
                    if (p.ParentObjectID == uuid)
                    {
                        return p;
                    }
                }

                return obj;
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[NHIBERNATE]: Failed loading RexObject with id {0}. Exception {1} ", uuid, e.ToString());
                return null;
            }
        }

        /// <summary>
        /// Removes an object from region storage
        /// </summary>
        /// <param name="obj">the object</param>
        /// <param name="regionUUID">the region UUID</param>
        public void RemoveObject(UUID obj)
        {
            RexObjectProperties g = LoadObject(obj);
            manager.Delete(g);

            m_log.InfoFormat("[REGION DB]: Removing obj: {0}", obj.Guid);
        }

        public void Shutdown()
        {
            //session.Flush();
        }
    }
}
