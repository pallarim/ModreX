using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Framework;
using log4net;
using System.Reflection;

namespace ModularRex.Tools.MigrationTool
{
    public class MigrationWorker
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected ConfigSettings ApplicationSettings;

        public MigrationWorker(ConfigSettings appSettings)
        {
            ApplicationSettings = appSettings;
        }

        public void Start()
        {
            //TODO: read connection strings from configuration instead of using defaults

            m_log.Info("[MIGRATION]: Starting to migrate UserProfiles");
            UserProfileMigration user_m = new UserProfileMigration(String.Empty); //TODO: change to actual configuration
            if (!user_m.Convert())
            {
                return;
            }

            m_log.Info("[MIGRATION]: Starting to migrate Inventory");
            InventoryMigration inv_m = new InventoryMigration(String.Empty); //TODO: change to actual configuration
            if (!inv_m.Convert())
            {
                return;
            }

            m_log.Info("[MIGRATION]: Starting to migrate Assets");
            AssetMigration ass_m = new AssetMigration(String.Empty, String.Empty); //TODO: change to actual configuration
            if (!ass_m.Convert())
            {
                return;
            }

            m_log.Info("[MIGRATION]: Starting to migrate Region data");
            RegionMigration reg_m = new RegionMigration(String.Empty); //TODO: change to actual configuration
            if (!reg_m.Convert())
            {
                return;
            }
        }
    }
}
