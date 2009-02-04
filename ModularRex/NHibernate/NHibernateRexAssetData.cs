using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using log4net;
using NHibernate;
using NHibernate.Criterion;
using OpenSim.Framework;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Scenes;
using Environment = NHibernate.Cfg.Environment;
using ModularRex.RexFramework;
using OpenSim.Data.NHibernate;

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

        public void StoreObject(RexAssetData obj)
        {
            try
            {
                RexAssetData old = (RexAssetData)manager.Load(typeof(RexAssetData), obj.AssetID);
                if (old != null)
                {
                    m_log.InfoFormat("[NHIBERNATE] updating RexAssetData {0}", obj.AssetID);
                    manager.Update(obj);
                }
                else
                {
                    m_log.InfoFormat("[NHIBERNATE] saving RexAssetData {0}", obj.AssetID);
                    manager.Save(obj);
                }
            }
            catch (Exception e)
            {
                m_log.Error("[NHIBERNATE]: Can't save: ", e);
            }
        }

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
