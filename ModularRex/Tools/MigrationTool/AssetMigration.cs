using System;
using System.Collections.Generic;
using System.Text;
using log4net;
using System.Reflection;
using Mono.Data.SqliteClient;
using OpenSim.Data;
using System.Data;
using ModularRex.RexFramework;
using OpenMetaverse;
using ModularRex.NHibernate;

namespace ModularRex.Tools.MigrationTool
{
    public class AssetMigration
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected string m_assetConnectionString = String.Empty;
        protected string m_rexConnectionString = String.Empty;

        private const string assetSelect = "select * from assets";

        public AssetMigration(string assetConnectionString, string rexConnectionString)
        {
            // default to something sensible
            if (assetConnectionString == "")
            {
                m_assetConnectionString = "URI=file:Asset.db,version=3";
            }
            else
            {
                m_assetConnectionString = assetConnectionString;
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
                SqliteConnection conn = new SqliteConnection(m_assetConnectionString);
                conn.Open();

                Assembly assem = GetType().Assembly;
                Migration m = new Migration(conn, assem, "AssetStore");

                if (m.Version == 0)
                {
                    //fetch all assets with mediaurl and construct RexAssetData objects
                    List<RexAssetData> rexAssets = new List<RexAssetData>();

                    using (SqliteCommand cmd = new SqliteCommand(assetSelect, conn))
                    {
                        using (IDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (((String)reader["MediaURL"]) != "")
                                {
                                    UUID id = new UUID((String) reader["UUID"]);
                                    string mediaUrl = (String)reader["MediaURL"];
                                    byte refreshRate = 0;
                                    object refRate = reader["RefreshRate"];
                                    if (refRate is byte)
                                    {
                                        refreshRate = (byte)refRate;
                                    }
                                    RexAssetData data = new RexAssetData(id, mediaUrl,refreshRate);
                                    rexAssets.Add(data);
                                }
                            }
                        }
                    }
                    conn.Close();

                    //Now add them to ModreX database
                    NHibernateRexAssetData rexAssetManager = new NHibernateRexAssetData();
                    rexAssetManager.Initialise(m_rexConnectionString);
                    foreach (RexAssetData data in rexAssets)
                    {
                        rexAssetManager.StoreObject(data);
                    }

                    //finally remove realXtend properties and update version number
                    conn.Open();
                    //TODO: remove realXtend properties
                    // this is not done yet because SQLite is missing drop column feature
                    m.Version = 1;
                }

                conn.Close();
                return true;
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[AssetStore] Migration failed. Reason: {0}", e);
                return false;
            }
        }
    }
}
