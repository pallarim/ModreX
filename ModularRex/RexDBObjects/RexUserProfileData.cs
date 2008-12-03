using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenSim.Framework;

namespace ModularRex.RexDBObjects
{
    /// <summary>
    /// Information about a particular user known to the userserver
    /// </summary>
    public class RexUserProfileData : UserProfileData
    {
        
        ///<summary>
        /// Users account
        ///</summary>
        public string account;

        ///<summary>
        /// Users real name
        ///</summary>
        public string realname;

        ///<summary>
        /// Session hash
        ///</summary>
        public string sessionHash;

        ///<summary>
        /// Avatar storage url
        ///</summary>
        public string avatarStorageUrl;

        /// <summary>
        /// Skype url for user
        /// </summary>
        public string skypeUrl;

        ///<summary>
        /// Grid url
        ///</summary>
        public string gridUrl;

    }
  
}
