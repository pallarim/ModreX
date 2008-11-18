using OpenMetaverse;

namespace ModularRex.RexNetwork.RexAuthentication
{
    public class RexProfile
    {
        private string m_firstName;
        private string m_lastName;
        private string m_account;
        private string m_avatarServer;
        private string m_authServer;
        private UUID m_uuid;

        /// <summary>
        /// Returns the full User Name of the avatar.
        /// IE: Joe Smith
        /// </summary>
        public string Name
        {
            get { return m_firstName + " " + m_lastName; }
        }

        /// <summary>
        /// Returns the first part of an avatar username
        /// IE: Joe
        /// </summary>
        public string FirstName
        {
            get { return m_firstName; }
            set { m_firstName = value; }
        }

        /// <summary>
        /// Returns the last part of an avatar username
        /// IE: Smith
        /// </summary>
        public string LastName
        {
            get { return m_lastName; }
            set { m_lastName = value; }
        }

        public string AccountName
        {
            get { return m_account; }
            set { m_account = value; }
        }

        public string AccountHost
        {
            get { return m_authServer; }
            set { m_authServer = value; }
        }

        public string Account
        {
            get { return m_account + "@" + m_authServer; }
        }

        public string AvatarServer
        {
            get { return m_avatarServer; }
            set { m_avatarServer = value; }
        }

        public UUID ID
        {
            get { return m_uuid; }
            set { m_uuid = value; }
        }
    }
}
