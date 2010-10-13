using System;
using System.Collections.Generic;
using System.Text;
using Mono.Data.SqliteClient;
using System.Reflection;
using OpenSim.Data;
using log4net;
using System.Data;

namespace ModularRex.Tools.MigrationTool
{
    public class InventoryMigration
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected string m_connectionString = String.Empty;

        private const string addSalePrice = "ALTER TABLE inventoryitems ADD COLUMN salePrice integer default 99";
        private const string addSaleType = "ALTER TABLE inventoryitems ADD COLUMN saleType integer default 0";
        private const string addCreationDate = "ALTER TABLE inventoryitems ADD COLUMN creationDate integer default 2000";
        private const string addGroupID = "ALTER TABLE inventoryitems ADD COLUMN groupID varchar(255) default '00000000-0000-0000-0000-000000000000'";
        private const string addGroupOwned = "ALTER TABLE inventoryitems ADD COLUMN groupOwned integer default 0";
        private const string addFlags = "ALTER TABLE inventoryitems ADD COLUMN flags integer default 0";

        public InventoryMigration(string connectionString)
        {
            if (connectionString == String.Empty)
            {
                m_connectionString = "URI=file:inventoryStore.db,version=3";
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
                Migration m = new Migration(conn, assem, "InventoryStore");
                if (m.Version == 0)
                {
                    //Apply all changes to db
                    SqliteCommand addSalePriceCmd = new SqliteCommand(addSalePrice, conn);
                    addSalePriceCmd.ExecuteNonQuery();

                    SqliteCommand addSaleTypeCmd = new SqliteCommand(addSaleType, conn);
                    addSaleTypeCmd.ExecuteNonQuery();

                    SqliteCommand addCreationDateCmd = new SqliteCommand(addCreationDate, conn);
                    addCreationDateCmd.ExecuteNonQuery();

                    SqliteCommand addGroupIDCmd = new SqliteCommand(addGroupID, conn);
                    addGroupIDCmd.ExecuteNonQuery();

                    SqliteCommand addGroupOwnedCmd = new SqliteCommand(addGroupOwned, conn);
                    addGroupOwnedCmd.ExecuteNonQuery();

                    SqliteCommand addFlagsCmd = new SqliteCommand(addFlags, conn);
                    addFlagsCmd.ExecuteNonQuery();

                    //then change version number
                    m.Version = 1;
                }
                return true;
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[InventoryStore] Migration failed. Reason: {0}", e);
                return false;
            }
        }
    }
}
