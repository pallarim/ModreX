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
    public class NHibernateRexLegacyData
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public bool Inizialized = false;

        public NHibernateManager manager;

        public void Initialise(string connect)
        {
            m_log.InfoFormat("[NHIBERNATE] Initializing NHibernateRexLegacyData");
            Assembly assembly = GetType().Assembly;
            manager = new NHibernateManager(connect, "RexLegacyData", assembly);
            Inizialized = true;
        }

        /// <summary>
        /// Retrives all objects from the old rex tables
        /// </summary>
        /// <returns>All objects as a list, if none found or error while processing returns null</returns>
        public List<RexLegacyPrimData> LoadAllRexPrimData()
        {
            try
            {
                RexLegacyPrimData obj = new RexLegacyPrimData();
                ICriteria criteria = manager.GetSession().CreateCriteria(typeof(RexLegacyPrimData));

                List<RexLegacyPrimData> rexprimdata = (List<RexLegacyPrimData>)criteria.List<RexLegacyPrimData>();
                return rexprimdata;
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[NHIBERNATE]: Failed loading legacy RexPrimData. Exception {0} ", e);
                return new List<RexLegacyPrimData>();
            }
        }

        public List<RexLegacyPrimMaterialData> LoadAllRexPrimMaterialData()
        {
            try
            {
                RexLegacyPrimMaterialData obj = new RexLegacyPrimMaterialData();
                ICriteria criteria = manager.GetSession().CreateCriteria(typeof(RexLegacyPrimMaterialData));

                List<RexLegacyPrimMaterialData> rexprimmaterialdata = (List<RexLegacyPrimMaterialData>)criteria.List<RexLegacyPrimMaterialData>();
                return rexprimmaterialdata;
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[NHIBERNATE]: Failed loading legacy RexPrimMaterialData. Exception {0} ", e);
                return new List<RexLegacyPrimMaterialData>();
            }
        }
    }
}