using System;
using System.Collections.Generic;
using System.Text;
using Mono.Data.SqliteClient;
using System.Reflection;
using OpenSim.Data;
using log4net;
using ModularRex.RexFramework;
using OpenMetaverse;
using ModularRex.NHibernate;

namespace ModularRex.Tools.MigrationTool
{
    class RegionMigration
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected string m_connectionString = String.Empty;
        protected string m_rexConnectionString = String.Empty;

        private const string addAuthbyerID = "ALTER TABLE land ADD COLUMN AuthbuyerID varchar(36) NOT NULL default  '00000000-0000-0000-0000-000000000000'";

        public RegionMigration(string connectionString, string rexConnectionString)
        {
            // default to something sensible
            if (connectionString == "")
            {
                m_connectionString = "URI=file:OpenSim.db,version=3";
            }
            else
            {
                m_connectionString = connectionString;
            }

            if (rexConnectionString == String.Empty)
            {
                m_rexConnectionString = "SQLiteDialect;SQLite20Driver;Data Source=RexObjects.db;Version=3";
            }
            else
            {
                m_rexConnectionString = rexConnectionString;
            }
        }

        public bool Convert()
        {
            try
            {
                SqliteConnection conn = new SqliteConnection(m_connectionString);
                conn.Open();

                Assembly assem = GetType().Assembly;
                Migration m = new Migration(conn, assem, "RegionStore");

                int version = m.Version;

                if (version <= 14)
                {
                    if (version == 0)
                    {
                        //read rex tables and add to rex database
                        m_log.Info("[regionstore] converting rex tables to rexobjectproperties");
                        if (!ConvertLegacyRexDataToModreX())
                        {
                            conn.Close();
                            return false;
                        }

                        m_log.Info("[RegionStore] Update region migrations");
                        //Add new field to Land table
                        SqliteCommand addAuthbyerIDCmd = new SqliteCommand(addAuthbyerID, conn);
                        addAuthbyerIDCmd.ExecuteNonQuery();

                        //Change migration to version 1
                        m.Version = 1;
                    }

                    //Run migrations up to 9
                    //Note: this run migrations only to point nine since only those files exist in application resources.
                    m.Update();

                    //Skip over 10. Change version to 10
                    //This skips adding of the ClickAction since that already exists in 0.4 database
                    //m.Version = 10;
                }

                conn.Close();
                return true;
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[RegionStore] Migration failed. Reason: {0}", e);
                return false;
            }
        }

        protected bool ConvertLegacyRexDataToModreX()
        {
            NHibernateRexLegacyData legacydata = new NHibernateRexLegacyData();

            //convert connnection string to NHibernate style
            //parse string like this: "URI=file:OpenSim.db,version=3"
            //and convert it to like this: "SQLiteDialect;SQLite20Driver;Data Source=RexObjects.db;Version=3"
            string arg1 = String.Empty;
            string arg2 = String.Empty;

            string[] components = m_connectionString.Split(',');
            if (components[0].StartsWith("URI=file:"))
            {
                arg1 = components[0].Substring(9);
            }
            else
            {
                m_log.ErrorFormat("[MODREXOBJECTS]: Error parseing connection string {0}", m_connectionString);
                return false;
            }
            if (components[1].StartsWith("version="))
            {
                arg2 = components[1].Substring(8);
            }
            else
            {
                m_log.ErrorFormat("[MODREXOBJECTS]: Error parseing connection string {0}", m_connectionString);
                return false;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("SQLiteDialect;SQLite20Driver;Data Source={0};Version={1}", arg1, arg2);

            legacydata.Initialise(sb.ToString());
            if (!legacydata.Inizialized)
            {
                m_log.Info("[MODREXOBJECTS]: Legacy database failed to initialize.");
                return false;
            }

            List<RexLegacyPrimData> rexprimdata = legacydata.LoadAllRexPrimData();
            m_log.Info("[MODREXOBJECTS]: Legacy rexprimdata objects loaded:" + rexprimdata.Count.ToString());

            List<RexLegacyPrimMaterialData> rexprimmaterialdata = legacydata.LoadAllRexPrimMaterialData();
            m_log.Info("[MODREXOBJECTS]: Legacy rexprimmaterialdata objects loaded:" + rexprimmaterialdata.Count.ToString());

            Dictionary<UUID, RexObjectProperties> rexObjects = new Dictionary<UUID, RexObjectProperties>();

            foreach (RexLegacyPrimData prim in rexprimdata)
            {
                RexObjectProperties p = new RexObjectProperties();
                p.SetRexPrimDataFromLegacyData(prim);
                rexObjects.Add(prim.UUID, p);
            }

            foreach (RexLegacyPrimMaterialData primmat in rexprimmaterialdata)
            {
                if (rexObjects.ContainsKey(primmat.UUID) && rexObjects[primmat.UUID] != null)
                {
                    rexObjects[primmat.UUID].RexMaterials.AddMaterial((uint)primmat.MaterialIndex, primmat.MaterialUUID);
                }
                else
                {
                    m_log.WarnFormat("[MODREXOBJECTS]: Could not find RexObjectData for prim {0} while adding materials. Creating", primmat.UUID);
                    RexObjectProperties p = new RexObjectProperties();
                    p.ParentObjectID = primmat.UUID;
                    p.RexMaterials.AddMaterial((uint)primmat.MaterialIndex, primmat.MaterialUUID);
                    rexObjects.Add(primmat.UUID, p);
                }
            }

            //Add rex object to database
            NHibernateRexObjectData rexObjectManager = new NHibernateRexObjectData();
            rexObjectManager.Initialise(m_rexConnectionString);
            foreach (KeyValuePair<UUID, RexObjectProperties> data in rexObjects)
            {
                rexObjectManager.StoreObject(data.Value);
            }

            return true;
        }
    }
}
