using System;
using System.Collections.Generic;
using System.Text;
using log4net;
using System.Reflection;
using ModularRex.RexFramework;
using NHibernate;
using OpenMetaverse;
using NHibernate.Criterion;

namespace ModularRex.NHibernate
{
    public class NHibernateECData
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public bool Inizialized = false;
        private bool m_nullStorage = false;

        public NHibernateManager manager;

        public void Initialise(string connect)
        {
            m_log.InfoFormat("[NHIBERNATE] Initializing NHibernateECData");
            if (connect.ToLower() == "null")
            {
                m_nullStorage = true;
                Inizialized = true;
                return;
            }

            Assembly assembly = GetType().Assembly;
            manager = new NHibernateManager(connect, "ECData", assembly);
            Inizialized = true;
        }

        public bool StoreComponent(ECData component)
        {
            if (m_nullStorage)
                return false;

            ICriteria criteria = manager.GetSession().CreateCriteria(typeof(ECData));
            criteria.Add(Expression.Eq("EntityID", component.EntityID));
            criteria.Add(Expression.Eq("ComponentType", component.ComponentName));
            criteria.Add(Expression.Eq("ComponentName", component.ComponentName));

            IList<ECData> result = criteria.List<ECData>();
            if (result.Count == 0)
            {
                manager.Insert(component);
            }
            else if (result.Count == 1)
            {
                ECData data = result[0];
                data.Data = component.Data;
                data.DataIsString = component.DataIsString;
                manager.Update(data);
            }
            else
            {
                throw new Exception("Could not store component, because component with same EntityID, ComponentType and ComponentName already exists in database");
            }

            return true;
        }

        public IList<ECData> GetComponents(UUID entityId)
        {
            if (m_nullStorage)
                return new List<ECData>();

            ICriteria criteria = manager.GetSession().CreateCriteria(typeof(ECData));
            criteria.Add(Expression.Eq("EntityID", entityId));

            return criteria.List<ECData>();
        }
    }
}
