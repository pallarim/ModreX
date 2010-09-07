using System;
using System.Collections.Generic;
using System.Text;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using log4net;
using System.Reflection;
using NHibernate.Criterion;

using NHibernate.Dialect;  
using NHibernate.Dialect.Function;  
using NHibernate.Engine;  
using NHibernate.Id;  
using NHibernate.Mapping;  
using NHibernate.Util;  
using System.Data.Common;  

using Environment = NHibernate.Cfg.Environment;



namespace OgreSceneImporter.UploadSceneDB
{

    


    class NHibernateManager
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private string dialect;
        private Configuration configuration;
        private ISessionFactory sessionFactory;
        

        #region Initialization


        static List<string> GetAllClass(string nameSpace, string classname)
        {
            //create an Assembly and use its GetExecutingAssembly Method
            //http://msdn2.microsoft.com/en-us/library/system.reflection.assembly.getexecutingassembly.aspx
            Assembly asm = Assembly.GetExecutingAssembly();
            //create a list for the namespaces
            List<string> namespaceList = new List<string>();
            //create a list that will hold all the classes
            //the suplied namespace is executing
            List<string> returnList = new List<string>();
            //loop through all the "Types" in the Assembly using
            //the GetType method:
            //http://msdn2.microsoft.com/en-us/library/system.reflection.assembly.gettypes.aspx
            foreach (Type type in asm.GetTypes())
            {
                

                if (type.Namespace == nameSpace)
                    namespaceList.Add(type.Name);
            }
            //now loop through all the classes returned above and add
            //them to our classesName list
            foreach (String className in namespaceList)
                returnList.Add(className);
            //return the list
            return returnList;
        }     

        /// <summary>
        /// Initiate NHibernate Manager
        /// </summary>
        /// <param name="connect">NHibernate dialect, driver and connection string separated by ';'</param>
        /// <param name="store">Name of the store</param>
        public NHibernateManager(string connect, string store)//, ISceneStorage scenestorage)
        {
            try
            {
                ParseConnectionString(connect);

                //To create sql file uncomment code below and write the name of the file (rewrites database, history will be erased)
                //SchemaExport exp = new SchemaExport(configuration);
                //exp.SetOutputFile("db_creation.sql");
                //exp.Create(false, true);

                // The above will sweep the db empty and creates the tables,
                // this update checks that the tables are there but wont erase the data
                SchemaUpdate update = new SchemaUpdate(configuration);
                update.Execute(false, true);

                sessionFactory = configuration.BuildSessionFactory();                
                //bool tableTest = TestTables();
                
            }
            catch (MappingException mapE)
            {
                if (mapE.InnerException != null)
                    Console.WriteLine("[NHIBERNATE]: Mapping not valid: {0}, {1}, {2}", mapE.Message, mapE.StackTrace, mapE.InnerException.ToString());
                else
                    m_log.ErrorFormat("[NHIBERNATE]: Mapping not valid: {0}, {1}", mapE.Message, mapE.StackTrace);
            }
            catch (HibernateException hibE)
            {
                Console.WriteLine("[NHIBERNATE]: HibernateException: {0}, {1}", hibE.Message, hibE.StackTrace);
            }
            catch (TypeInitializationException tiE)
            {
                Console.WriteLine("[NHIBERNATE]: TypeInitializationException: {0}, {1}", tiE.Message, tiE.StackTrace);
            }
        }

        public bool CreateDBTables()
        {
            try
            {
                // try creating tables
                SchemaExport exp = new SchemaExport(configuration);
                exp.SetOutputFile("db_creation.sql");
                exp.Create(false, true);
                return true;
            }
            catch (Exception ex)
            {
                m_log.Debug("[NHIBERNATE]: Failed to create uploadscene databases:" + ex.GetType().Name + " " + ex.Message);
                return false;
            }
        }

        //public bool TestTables()
        //{
        //    // try to make query to db
        //    try
        //    {
        //        ICriteria criteria = GetSession().CreateCriteria(typeof(UploadScene));
        //        criteria.Add(Expression.Eq("Name", "*"));
        //        System.Collections.IList list = criteria.List();
        //        return true;
        //    }
        //    catch (Exception exp)
        //    {
        //        m_log.DebugFormat("[OGRESCENE]: Error testing upload scene database tables: {0}, {1}", exp.Message, exp.StackTrace);
        //        return false;
        //        //throw;
        //    }
        //}

        /// <summary>
        /// Parses the connection string and creates the NHibernate configuration
        /// </summary>
        /// <param name="connect">NHibernate dialect, driver and connection string separated by ';'</param>
        private void ParseConnectionString(string connect)
        {
            // Split out the dialect, driver, and connect string
            char[] split = { ';' };
            string[] parts = connect.Split(split, 3);
            if (parts.Length != 3)
            {
                // TODO: make this a real exception type
                throw new Exception("Malformed Inventory connection string '" + connect + "'");
            }

            dialect = parts[0];

            // NHibernate setup
            configuration = new Configuration();
            configuration.SetProperty(Environment.ConnectionProvider,
                            "NHibernate.Connection.DriverConnectionProvider");
            configuration.SetProperty(Environment.Dialect,
                            "NHibernate.Dialect." + dialect);
            configuration.SetProperty(Environment.ConnectionDriver,
                            "NHibernate.Driver." + parts[1]);
            configuration.SetProperty(Environment.ConnectionString, parts[2]);

            //configuration.SetProperty("hbm2ddl.auto", "create");
            //configuration.Configure();

            //configuration.SetProperty(Environment.ShowSql, "true");
            //configuration.SetProperty(Environment.GenerateStatistics, "false");
            //configuration.AddAssembly("WebDAVSharp.NHibernateStorage");
            configuration.AddAssembly("OgreSceneImporter");

        }

        #endregion


        /// <summary>
        /// Inserts given object to database.
        /// Uses stateless session for efficiency.
        /// </summary>
        /// <param name="obj">Object to be insterted.</param>
        /// <returns>Identifier of the object. Useful for situations when NHibernate generates the identifier.</returns>
        public object Insert(object obj)
        {
            try
            {
                using (IStatelessSession session = sessionFactory.OpenStatelessSession())
                {
                    using (ITransaction transaction = session.BeginTransaction())
                    {
                        Object identifier = session.Insert(obj);
                        transaction.Commit();
                        return identifier;
                    }
                }
            }
            catch (Exception e)
            {
                m_log.Error("[NHIBERNATE] issue inserting object ", e);
                return null;
            }
        }

        public object Update(object obj)
        {
            try
            {
                using (IStatelessSession session = sessionFactory.OpenStatelessSession())
                {
                    using (ITransaction transaction = session.BeginTransaction())
                    {
                        session.Update(obj);
                        transaction.Commit();
                        return obj;
                    }
                }                
            }
            catch (Exception e)
            {
                m_log.Error("[NHIBERNATE] issue updating object ", e);
                return null;
            }
        }

        public bool Delete(object obj)
        {
            try
            {
                using (IStatelessSession session = sessionFactory.OpenStatelessSession())
                {
                    using (ITransaction transaction = session.BeginTransaction())
                    {
                        session.Delete(obj);
                        transaction.Commit();
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                m_log.Error("[NHIBERNATE] issue deleting object ", e);
                return false;
            }
        }

        /// <summary>
        /// Returns statefull session which can be used to execute custom nhibernate or sql queries.
        /// </summary>
        /// <returns>Statefull session</returns>
        public ISession GetSession()
        {
            return sessionFactory.OpenSession();
        }


    }


}
