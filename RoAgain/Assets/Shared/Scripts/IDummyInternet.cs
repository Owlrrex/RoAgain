public abstract class ADummyInternet
{
    public static ADummyInternet Instance;

    public abstract int OpenServer(object newServerObject);
    public abstract int ConnectToServer(object newClientObject);
    public abstract int DisconnectFromServer(object clientsideObject);
    public abstract void SendPacket(object sender, int receiverId, Packet packet);
}
