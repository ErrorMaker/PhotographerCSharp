using Photographer.Network.Protocol;

namespace Photographer.Network
{
    public interface IHConnection
    {
        HotelEndPoint RemoteEndPoint { get; }

        int SendToServer(byte[] data);
        int SendToServer(HMessage packet);

        int SendToClient(byte[] data);
        int SendToClient(HMessage packet);
    }
}