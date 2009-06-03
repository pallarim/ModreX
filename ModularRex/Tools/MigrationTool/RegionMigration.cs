using System;
using System.Collections.Generic;
using System.Text;
using Mono.Data.SqliteClient;
using System.Reflection;
using OpenSim.Data;
using log4net;

namespace ModularRex.Tools.MigrationTool
{
    class RegionMigration
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected string m_connectionString = String.Empty;

        private const string addAuthbyerID = "ALTER TABLE land ADD COLUMN AuthbuyerID varchar(36) NOT NULL default  '00000000-0000-0000-0000-000000000000'";

        public RegionMigration(string connectionString)
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

                if (version <= 9)
                {
                    if (version == 0)
                    {
                        //Add new field to Land table
                        SqliteCommand addAuthbyerIDCmd = new SqliteCommand(addAuthbyerID, conn);
                        addAuthbyerIDCmd.ExecuteNonQuery();

                        //Change migration to version 1
                        m.Version = 1;
                    }

                    //Run migrations up to 9
                    m.Update();

                    //Skip over 10. Change version to 10
                    //This skips adding of the ClickAction since that already exists in 0.4 database
                    m.Version = 10;
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
    }
}
