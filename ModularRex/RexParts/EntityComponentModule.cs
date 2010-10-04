using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using ModularRex.NHibernate;

namespace ModularRex.RexParts
{
    public class EntityComponentModule : ISharedRegionModule
    {
        private List<Scene> m_scenes = new List<Scene>();
        private NHibernateECData m_db;
        private string m_db_connectionstring;

        #region ISharedRegionModule Members

        public void PostInitialise()
        {
        }

        #endregion

        #region IRegionModuleBase Members

        public string Name
        {
            get { return "EntityComponentModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void Initialise(Nini.Config.IConfigSource source)
        {
            //read configuration
            try
            {
                m_db_connectionstring = source.Configs["realXtend"].GetString("db_connectionstring", "SQLiteDialect;SQLite20Driver;Data Source=RexObjects.db;Version=3");
            }
            catch (Exception)
            {
                m_db_connectionstring = "SQLiteDialect;SQLite20Driver;Data Source=RexObjects.db;Version=3";
            }

            m_db = new NHibernateECData();
            m_db.Initialise(m_db_connectionstring);
        }

        public void Close()
        {
        }

        public void AddRegion(OpenSim.Region.Framework.Scenes.Scene scene)
        {
            m_scenes.Add(scene);
        }

        public void RemoveRegion(OpenSim.Region.Framework.Scenes.Scene scene)
        {
            m_scenes.Remove(scene);
        }

        public void RegionLoaded(OpenSim.Region.Framework.Scenes.Scene scene)
        {
        }

        #endregion
    }
}
