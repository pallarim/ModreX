using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using ModularRex.RexFramework;
using OpenMetaverse;

namespace ModularRex.RexParts
{
    public class RexObjectPropertiesManager : IEnumerable<RexObjectProperties>
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    
        private Dictionary<UUID,RexObjectProperties> m_objs = new Dictionary<UUID, RexObjectProperties>();
    
        public void Add(UUID id,RexObjectProperties rexobj)
        {
            lock (m_objs)
            {
                try
                {
                    m_objs.Add(id, rexobj);
                }
                catch (Exception e)
                {
                    m_log.ErrorFormat("Add RexObjectProperties failed: {0}", e.Message);
                }
            }
        }


        public void InsertOrReplace(RexObjectProperties rexobj)
        {
            lock (m_objs)
            {
                try
                {
                    m_objs[rexobj.ParentObjectID] = rexobj;
                }
                catch (Exception e)
                {
                    m_log.ErrorFormat("Insert or Replace RexObjectProperties failed: {0}", e.Message);
                }
            }
        }

        public void Clear()
        {
            lock (m_objs)
            {
                m_objs.Clear();
            }
        }

        public int Count
        {
            get
            {
                lock (m_objs)
                {
                    return m_objs.Count;
                }
            }
        }

        public bool ContainsKey(UUID id)
        {
            lock (m_objs)
            {
                try
                {
                    return m_objs.ContainsKey(id);
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Remove(UUID id)
        {
            lock (m_objs)
            {
                try
                {
                    return m_objs.Remove(id);
                }
                catch (Exception e)
                {
                    m_log.ErrorFormat("Remove RexObjectProperties failed for {0}", id, e);
                    return false;
                }
            }
        }

        public List<RexObjectProperties> GetAllRexObjectProperties()
        {
            lock (m_objs)
            {
                return new List<RexObjectProperties>(m_objs.Values);
            }
        }

        public RexObjectProperties this[UUID id]
        {
            get
            {
                lock (m_objs)
                {
                    try
                    {
                        return m_objs[id];
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
            set
            {
                InsertOrReplace(value);
            }
        }

        public bool TryGetValue(UUID key, out RexObjectProperties obj)
        {
            lock (m_objs)
            {
                return m_objs.TryGetValue(key, out obj);
            }
        }

        public IEnumerator<RexObjectProperties> GetEnumerator()
        {
            return GetAllRexObjectProperties().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
