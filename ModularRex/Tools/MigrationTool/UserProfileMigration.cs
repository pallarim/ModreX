using System;
using System.Collections.Generic;
using System.Text;
using Mono.Data.SqliteClient;
using System.Data;
using System.Reflection;
using OpenSim.Data;
using log4net;

namespace ModularRex.Tools.MigrationTool
{
    public class UserProfileMigration
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected string m_connectionString = String.Empty;

        public UserProfileMigration(string connectionString)
        {
            // default to something sensible
            if (connectionString == "")
            {
                m_connectionString = "URI=file:userprofiles.db,version=3";
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
                Migration m = new Migration(conn, assem, "UserStore");

                if (m.Version == 0)
                {
                    m.Version = 1;
                }

                conn.Close();
                return true;
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[UserStore] Migration failed. Reason: {0}", e);
                return false;
            }
        }
    }
}
