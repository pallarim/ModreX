using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;

namespace ModularRex.RexFramework
{
    public class ECData
    {
        private UUID m_entity_id = UUID.Zero;
        private string m_component_type = String.Empty;
        private string m_component_name = String.Empty;
        private byte[] m_data = null;
        private bool m_data_is_string = false;

        public ECData() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ECData"/> class.
        /// </summary>
        /// <param name="entityId">The entity id.</param>
        /// <param name="componentName">Name of the component.</param>
        /// <param name="data">The data of the component.</param>
        /// <param name="dataIsString">if set to <c>true</c> <c>data</c> is string.</param>
        public ECData(UUID entityId, string componentType, string componentName, byte[] data, bool dataIsString)
        {
            m_entity_id = entityId;
            m_component_name = componentName;
            m_data = data;
            m_data_is_string = dataIsString;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ECData"/> class.
        /// </summary>
        /// <param name="entityId">The entity id.</param>
        /// <param name="componentName">Name of the component.</param>
        /// <param name="data">The data as base64 string.</param>
        public ECData(UUID entityId, string componentType, string componentName, string data)
        {
            m_entity_id = entityId;
            m_component_name = componentName;
            m_data = Convert.FromBase64String(data);
            m_data_is_string = true;
        }

        /// <summary>
        /// Gets or sets the entity ID.
        /// </summary>
        /// <value>The entity ID to whom this component belongs to</value>
        public virtual UUID EntityID
        {
            get { return m_entity_id; }
            set { m_entity_id = value; }
        }

        /// <summary>
        /// Gets or sets the type of the component.
        /// </summary>
        /// <value>The type of the component.</value>
        public virtual string ComponentType
        {
            get { return m_component_type; }
            set { m_component_type = value; }
        }

        /// <summary>
        /// Gets or sets the name of the component.
        /// </summary>
        /// <value>The name of the component.</value>
        public virtual string ComponentName
        {
            get { return m_component_name; }
            set { m_component_name = value; }
        }

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data of the Component</value>
        public virtual byte[] Data
        {
            get { return m_data; }
            set { m_data = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether <c>Data</c> is string. If <c>Data</c> is string,
        /// it should be convertable from byte array back to string with method <c>ToBase64String</c>
        /// in <c>Convert</c> class.
        /// </summary>
        /// <value><c>true</c> if <c>Data</c> is string otherwise, <c>false</c>.</value>
        public virtual bool DataIsString
        {
            get { return m_data_is_string; }
            set { m_data_is_string = value; }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            ECData t = obj as ECData;
            if (t == null)
                return false;

            if (t.EntityID == this.EntityID &&
                t.ComponentType == this.ComponentType &&
                t.ComponentName == this.ComponentName &&
                t.Data == this.Data &&
                t.DataIsString == this.DataIsString)
                return true;
            else
                return false;
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash += this.EntityID.GetHashCode();
            hash += (null == this.ComponentType ? 0 : this.ComponentType.GetHashCode());
            hash += (null == this.ComponentName ? 0 : this.ComponentName.GetHashCode());
            hash += (null == this.Data ? 0 : this.Data.GetHashCode());
            hash += this.DataIsString.GetHashCode();
            return hash;
        }
    }
}
