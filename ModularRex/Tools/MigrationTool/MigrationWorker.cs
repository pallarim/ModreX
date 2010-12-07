using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Framework;
using log4net;
using System.Reflection;
using Nini.Config;

namespace ModularRex.Tools.MigrationTool
{
    public class MigrationWorker
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected IConfigSource ApplicationSettings;

        protected string rexConnectionString = String.Empty;
        protected string regionConnectionString = String.Empty;
        protected string assetConnectionString = String.Empty;
        protected string inventoryConnectionString = String.Empty;
        protected string userprofileConnectionString = String.Empty;

        public MigrationWorker(IConfigSource appSettings)
        {
            ApplicationSettings = appSettings;
        }

        public void Start()
        {
            ReadConfigurations();

            //From this point on. Do the actual migrations work.
            m_log.Info("[MIGRATION]: Starting to migrate UserProfiles");
            UserProfileMigration user_m = new UserProfileMigration(userprofileConnectionString);
            if (!user_m.Convert())
            {
                return;
            }

            m_log.Info("[MIGRATION]: Starting to migrate Inventory");
            InventoryMigration inv_m = new InventoryMigration(inventoryConnectionString);
            if (!inv_m.Convert())
            {
                return;
            }

            m_log.Info("[MIGRATION]: Starting to migrate Assets");
            AssetMigration ass_m = new AssetMigration(assetConnectionString, rexConnectionString);
            if (!ass_m.Convert())
            {
                return;
            }

            m_log.Info("[MIGRATION]: Starting to migrate Region data");
            RegionMigration reg_m = new RegionMigration(regionConnectionString, rexConnectionString);
            if (!reg_m.Convert())
            {
                return;
            }
        }

        protected void ReadConfigurations()
        {
            if (ApplicationSettings.Configs != null)
            {
                //get region store connection string
                IConfig startupConfig = ApplicationSettings.Configs["Startup"];
                if (startupConfig != null)
                {
                    string storagePlugin = startupConfig.GetString("storage_plugin");
                    if (storagePlugin == "OpenSim.Data.SQLite.dll")
                    {
                        regionConnectionString = startupConfig.GetString("storage_connection_string", String.Empty);
                    }
                    else
                    {
                        m_log.Warn("[MIGRATION]: OpenSim.Data.SQLite.dll was not used as region store plugin. Trying with default configuration");
                    }
                }
                else
                {
                    m_log.Warn("[MIGRATION]: Did not find Startup section from configuration file. Using default region store connection string");
                }

                //get realXtend connection string
                IConfig rexConfig = ApplicationSettings.Configs["realXtend"];
                if (rexConfig != null)
                {
                    rexConnectionString = rexConfig.GetString("db_connectionstring", String.Empty);
                }
                else
                {
                    m_log.Warn("[MIGRATION]: Did not find realXtend section from configuration file. Using default rex connection string");
                }

                bool assetConfFound = false;

                //get inventory and userprofiles connection strings
                IConfig standaloneConfig = ApplicationSettings.Configs["StandAlone"];
                if (standaloneConfig != null)
                {
                    //get inventory connection string
                    string inventoryPlugin = standaloneConfig.GetString("inventory_plugin");
                    if (inventoryPlugin == "OpenSim.Data.SQLite.dll")
                    {
                        inventoryConnectionString = standaloneConfig.GetString("inventory_source", String.Empty);
                    }
                    else
                    {
                        m_log.Warn("[MIGRATION]: OpenSim.Data.SQLite.dll was not used as inventory plugin. Trying with default configuration");
                    }

                    //now get user profiles connection string
                    string userDatabasePlugin = standaloneConfig.GetString("userDatabase_plugin");
                    if (userDatabasePlugin == "OpenSim.Data.SQLite.dll")
                    {
                        userprofileConnectionString = standaloneConfig.GetString("user_source", String.Empty);
                    }
                    else
                    {
                        m_log.Warn("[MIGRATION]: OpenSim.Data.SQLite.dll was not used as userprofile plugin. Trying with default configuration");
                    }

                    //Check if assets configuration string is found from this section. This should be the case with 0.6.5
                    string assetPlugin = standaloneConfig.GetString("asset_plugin");
                    if (assetPlugin == "OpenSim.Data.SQLite.dll")
                    {
                        assetConnectionString = standaloneConfig.GetString("asset_source", String.Empty);
                        if (assetConnectionString != String.Empty) //get assets connection string
                        {
                            m_log.Info("[MIGRATION]: Found Assrt configuration under [StandAlone] section. Using 0.6.5 style configuration for assets");
                            assetConfFound = true;
                        }
                    }
                }
                else
                {
                    m_log.Warn("[MIGRATION]: Did nto find Standalone section from configuration file. Using defaults for inventory and userprofiles");
                }

                //Check if the version is 0.6.5 or some version later
                //In 0.6.5 version the asset configuration is in [StandAlone] section
                //In versions after 0.6.5 asset configuration is in [AssetService] section
                IConfig assetService = ApplicationSettings.Configs["AssetService"];
                if (!assetConfFound && assetService != null)
                {
                    string assetStorageProvider = assetService.GetString("StorageProvider");
                    if (assetStorageProvider == "OpenSim.Data.SQLite.dll")
                    {
                        assetConnectionString = assetService.GetString("ConnectionString", String.Empty);
                    }
                }
                else
                {
                    if (assetConnectionString == String.Empty)
                    {
                        m_log.Warn("[MIGRATION]: Did not find Asset configuration. Using default");
                    }
                }
            }
            else
            {
                m_log.Warn("[MIGRATION]: Did not find any configurations. Trying with defaults");
            }
        }
    }
}
