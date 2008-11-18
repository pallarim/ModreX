using OpenMetaverse;

namespace ModularRex.RexNetwork.RexAuthentication
{
    public static class RexAuthUtil
    {
        public static RexProfile GetByUUID(UUID id, string authserver)
        {
            return null;
        }

        public static RexProfile GetByAccount(string account)
        {
            return null;
        }

        public static bool Authorise(string account, UUID sessionID)
        {
            return true;
        }
    }
}
