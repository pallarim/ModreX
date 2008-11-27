using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;

namespace ModularRex.RexParts
{
    // Interface for script engine.
    public interface RexScriptAccessInterface
    {
        bool GetAvatarStartLocation(out Vector3 vLoc, out Vector3 vLookAt);
    }


    // Static class used for getting values from the script to server .net code.
    // At the moment supports only one engine -> static.
    public class RexScriptAccess
    {
        public static RexScriptAccessInterface MyScriptAccess = null;

        public static bool GetAvatarStartLocation(out Vector3 vLoc, out Vector3 vLookAt)
        {
            vLoc = new Vector3(0, 0, 0);
            vLookAt = new Vector3(0, 0, 0);

            if (MyScriptAccess != null)
                return MyScriptAccess.GetAvatarStartLocation(out vLoc, out vLookAt);
            else
                return false;
        }
    }
}
