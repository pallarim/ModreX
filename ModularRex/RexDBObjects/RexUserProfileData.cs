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
        public string Account;

        ///<summary>
        /// Users real name
        ///</summary>
        public string RealName;

        ///<summary>
        /// Session hash
        ///</summary>
        public string SessionHash;

        ///<summary>
        /// Avatar storage url
        ///</summary>
        public string AvatarStorageUrl;

        /// <summary>
        /// Skype url for user
        /// </summary>
        public string SkypeUrl;

        ///<summary>
        /// Grid url
        ///</summary>
        public string GridUrl;

        /// <summary>
        /// Authentication server URL
        /// </summary>
        public string AuthUrl;
    }
  
}
