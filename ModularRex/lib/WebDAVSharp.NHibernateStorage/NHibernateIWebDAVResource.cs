using System;
using System.Collections.Generic;
using System.Text;
using WebDAVSharp;
using log4net;
using System.Reflection;
using NHibernate;
using NHibernate.Criterion;

namespace WebDAVSharp.NHibernateStorage
{
    public class NHibernateIWebDAVResource
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public bool Inizialized = false;

        public NHibernateManager Manager;

        public void Initialise(string connect)
        {
            m_log.InfoFormat("[NHIBERNATE] Initializing NHibernateIWebDAVResource");
            Assembly assembly = GetType().Assembly;
            Manager = new NHibernateManager(connect, "");
            Inizialized = true;
        }

        public void Dispose() { }

        public bool SaveResource(IWebDAVResource resource)
        {
            try
            {
                IList<WebDAVProperty> properties = resource.CustomProperties;
                Manager.Insert(resource);
                if (properties != null &&
                    properties.Count > 0)
                {
                    foreach (WebDAVProperty prop in properties)
                    {
                        prop.ResourceId = resource.Id;
                        Manager.Insert(prop);
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the resource.
        /// </summary>
        /// <param name="path">The path to the resource.</param>
        /// <returns>The resource or null if not found</returns>
        public IWebDAVResource GetResource(string path)
        {
            ICriteria criteria = Manager.GetSession().CreateCriteria(typeof(IWebDAVResource));
            criteria.Add(Expression.Eq("Path", path));

            foreach(IWebDAVResource res in criteria.List())
            {
                if (res.Path == path)
                {
                    ICriteria cr2 = Manager.GetSession().CreateCriteria(typeof(WebDAVProperty));
                    criteria.Add(Expression.Eq("ResourceId", res.Id));
                    foreach (WebDAVProperty prop in cr2.List())
                        if(!res.CustomProperties.Contains(prop))
                            res.CustomProperties.Add(prop);
                    return res;
                }
            }

            return null;
        }

        public bool Remove(IWebDAVResource resProp)
        {
            if (resProp != null)
            {
                return Manager.Delete(resProp);
            }
            else
            {
                return false;
            }
        }

        public bool Remove(string path)
        {
            IWebDAVResource res = GetResource(path);
            if (res != null)
            {
                return Manager.Delete(res);
            }
            else
            {
                return false;
            }
        }
    }
}
