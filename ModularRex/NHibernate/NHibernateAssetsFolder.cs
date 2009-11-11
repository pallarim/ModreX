using System;
using System.Collections.Generic;
using System.Text;
using log4net;
using System.Reflection;
using OpenMetaverse;
using NHibernate;
using ModularRex.RexFramework;
using NHibernate.Criterion;

namespace ModularRex.NHibernate
{
    public class NHibernateAssetsFolder
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public bool Inizialized = false;

        public NHibernateManager manager;

        public void Initialise(string connect)
        {
            m_log.InfoFormat("[NHIBERNATE] Initializing NHibernateRexAssetFolder");
            Assembly assembly = GetType().Assembly;
            manager = new NHibernateManager(connect, "RexAssetFolder", assembly);
            Inizialized = true;
        }

        /// <summary>
        /// Save or update object to database
        /// </summary>
        /// <param name="obj">Object to save or update</param>
        public void Save(AssetFolder obj)
        {
            try
            {
                AssetFolder old = (AssetFolder)manager.Get(typeof(AssetFolder), obj.Id);
                if (old != null)
                {
                    m_log.InfoFormat("[NHIBERNATE] updating RexAssetFolder {0} {1}", obj.ParentPath, obj.Name);
                    manager.Update(obj);
                }
                else
                {
                    m_log.InfoFormat("[NHIBERNATE] saving RexAssetFolder {0} {1}", obj.ParentPath, obj.Name);
                    manager.Insert(obj);
                }
            }
            catch (Exception e)
            {
                m_log.Error("[NHIBERNATE]: Can't save: ", e);
            }
        }


        /// <summary>
        /// Gets the sub items.
        /// </summary>
        /// <param name="parentPath">The parent path where to find the items</param>
        /// <returns>List of items found in parent path or null if none found.</returns>
        public IList<AssetFolder> GetSubItems(string parentPath)
        {
            try
            {
                ICriteria criteria = manager.GetSession().CreateCriteria(typeof(AssetFolder));
                criteria.Add(Expression.Eq("ParentPath", parentPath));

                return criteria.List<AssetFolder>();
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[NHIBERNATE]: Failed to get sub items with parent path {0}. Exception {1} ", parentPath, e.ToString());
                return null;
            }
        }

        /// <summary>
        /// Gets the item.
        /// </summary>
        /// <param name="parentPath">The parent path of item</param>
        /// <param name="name">The name of item</param>
        /// <returns>The item or null if not found</returns>
        public AssetFolder GetItem(string parentPath, string name)
        {
            try
            {
                ICriteria criteria = manager.GetSession().CreateCriteria(typeof(AssetFolder));
                criteria.Add(Expression.Eq("ParentPath", parentPath));
                criteria.Add(Expression.Eq("Name", name));

                foreach (AssetFolder item in criteria.List<AssetFolder>())
                {
                    return item;
                }

                return null;
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[NHIBERNATE]: Failed to get item with parent path {0} and name {1}. Exception {2} ", parentPath, name, e.ToString());
                return null;
            }
        }

        /// <summary>
        /// Removes the item.
        /// </summary>
        /// <param name="parentPath">The parent path of item</param>
        /// <param name="name">The name of item</param>
        /// <returns>True if deleted properly, false if not</returns>
        public bool RemoveItem(string parentPath, string name)
        {
            AssetFolder item = GetItem(parentPath, name);
            if (item != null)
            {
                return manager.Delete(item);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Removes the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>True if deleted properly, false if not</returns>
        public bool RemoveItem(AssetFolder item)
        {
            if (item != null)
            {
                return manager.Delete(item);
            }
            else
            {
                return false;
            }
        }
    }
}
