using OpenMetaverse;

namespace ModularRex.RexNetwork
{
    public delegate void ReceiveRexMediaURL(IClientMediaURL remoteClient, UUID agentID, UUID itemID, string mediaURL, byte refreshRate);

    /// <summary>
    /// This client supports Rex style MediaURLs
    /// </summary>
    public interface IClientMediaURL
    {
        event ReceiveRexMediaURL OnReceiveRexMediaURL;
        void SendMediaURL(UUID assetId, string mediaURL, byte refreshRate);
    }
}
