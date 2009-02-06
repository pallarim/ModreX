using OpenMetaverse;

namespace ModularRex.RexNetwork
{
    /// <summary>
    /// This client supports Rex style MediaURLs
    /// </summary>
    public interface IClientMediaURL
    {
        event ReceiveRexMediaURL OnReceiveRexMediaURL;
        void SendMediaURL(UUID assetId, string mediaURL, byte refreshRate);
    }
}
