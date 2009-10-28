using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using log4net;
using NHibernate;
using NHibernate.Criterion;
using OpenSim.Framework;
using Environment = NHibernate.Cfg.Environment;
using ModularRex.RexFramework;

namespace ModularRex.NHibernate
{
    public class NHibernateRexAssetData
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public bool Inizialized = false;

        public NHibernateManager manager;

        public void Initialise(string connect)
        {
            m_log.InfoFormat("[NHIBERNATE] Initializing NHibernateRexObjectData");
            Assembly assembly = GetType().Assembly;
            manager = new NHibernateManager(connect, "RexAssetData", assembly);
            Inizialized = true;
        }

        public void Dispose() { }

        /// <summary>
        /// Save or update object to database
        /// </summary>
        /// <param name="obj">Object to save or update</param>
        public void StoreObject(RexAssetData obj)
        {
            try
            {
                RexAssetData old = (RexAssetData)manager.Get(typeof(RexAssetData), obj.AssetID);
                if (old != null)
                {
                    m_log.InfoFormat("[NHIBERNATE] updating RexAssetData {0}", obj.AssetID);
                    manager.Update(obj);
                }
                else
                {
                    m_log.InfoFormat("[NHIBERNATE] saving RexAssetData {0}", obj.AssetID);
                    manager.Insert(obj);
                }
            }
            catch (Exception e)
            {
                m_log.Error("[NHIBERNATE]: Can't save: ", e);
            }
        }

        /// <summary>
        /// Loads object from the database
        /// </summary>
        /// <param name="uuid">UUID of the object</param>
        /// <returns>Returns RexAssetData if the object is found with ID, returns null if not found.</returns>
        public RexAssetData LoadObject(UUID uuid)
        {
            try
            {
                RexAssetData obj = new RexAssetData();
                ICriteria criteria = manager.GetSession().CreateCriteria(typeof(RexAssetData));
                criteria.Add(Expression.Eq("AssetID", uuid));
                criteria.AddOrder(Order.Asc("AssetID"));

                foreach (RexAssetData p in criteria.List())
                {
                    if (p.AssetID == uuid)
                    {
                        return p;
                    }
                }

                return obj;
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[NHIBERNATE]: Failed loading RexAssetData with id {0}. Exception {1} ", uuid, e.ToString());
                return null;
            }
        }

        /// <summary>
        /// Retrives all objects from the RexAssetData table
        /// </summary>
        /// <returns>All objects as a list, if none found or error while processing returns null</returns>
        public List<RexAssetData> LoadAllObjects()
        {
            try
            {
                RexAssetData obj = new RexAssetData();
                ICriteria criteria = manager.GetSession().CreateCriteria(typeof(RexAssetData));

                List<RexAssetData> assets = (List<RexAssetData>)criteria.List<RexAssetData>();
                return assets;
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[NHIBERNATE]: Failed loading RexAssetData. Exception {0} ", e);
                return new List<RexAssetData>();
            }
        }

        /// <summary>
        /// Removes object from the database
        /// </summary>
        /// <param name="obj">UUID of the object to remove</param>
        public void RemoveObject(UUID obj)
        {
            RexAssetData g = LoadObject(obj);
            manager.Delete(g);

            m_log.InfoFormat("[NHIBERNATE]: Removing obj: {0}", obj.Guid);
        }

        public void Shutdown()
        {
            //session.Flush();
        }
    }
}
