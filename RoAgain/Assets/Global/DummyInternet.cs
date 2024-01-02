
using System.Collections.Generic;
using Server;
using Client;
using OwlLogging;
using UnityEditor;
using System.Runtime.CompilerServices;

public class DummyInternet : ADummyInternet
{
    private static List<ServerConnection> _clientSideConnections = new();
    private static CentralConnection _serverSideConnection = null;

    public int Initialize()
    {
        if (Instance == this)
        {
            OwlLogger.Log("DummyInternet tried to re-register itself", GameComponent.Other);
            return 0;
        }

        if (Instance != null)
        {
            OwlLogger.LogError("DummyInternet is overwriting ADummyInternet instance!", GameComponent.Network);
        }
        Instance = this;
        return 0;
    }

    public override int OpenServer(object newServerObject)
    {
        if(newServerObject == null)
        {
            OwlLogger.LogError("Can't open null server!", GameComponent.Network);
            return -2;
        }

        if (newServerObject is not CentralConnection newServerConnection)
        {
            OwlLogger.LogError("Can't open server that's not a CentralConnection!", GameComponent.Network);
            return -3;
        }

        if (_serverSideConnection != null)
        {
            OwlLogger.LogError("Registering new Server when previous one is already open!", GameComponent.Network);
        }

        if(_serverSideConnection == newServerConnection)
        {
            OwlLogger.LogError("Server opens multiple times!", GameComponent.Network);
            return -1;
        }

        _serverSideConnection = newServerConnection;
        return 0;
    }

    public override int ConnectToServer(object newClientObject)
    {
        if(newClientObject == null)
        {
            OwlLogger.LogError("Can't connect with null client!", GameComponent.Network);
            return -2;
        }

        if (newClientObject is not ServerConnection newClientConnection)
        {
            OwlLogger.LogError("Can't connect to server with client that's not a ServerConnection!", GameComponent.Network);
            return -3;
        }

        if (_serverSideConnection == null)
        {
            OwlLogger.Log("Client tried to connect when no server is open", GameComponent.Network);
            return -1;
        }
        
        if(_clientSideConnections.Contains(newClientConnection))
        {
            OwlLogger.LogError("Client tries to connect to server multiple times!", GameComponent.Network);
            return -2;
        }

        _clientSideConnections.Add(newClientConnection);
        return 1; // returns senderId for server
    }

    public override int DisconnectFromServer(object clientsideObject)
    {
        if (clientsideObject == null)
        {
            OwlLogger.LogError("Can't disconnect with null client!", GameComponent.Network);
            return -2;
        }

        if (clientsideObject is not ServerConnection clientsideConnection)
        {
            OwlLogger.LogError("Can't disconnect to server with client that's not a ServerConnection!", GameComponent.Network);
            return -3;
        }

        if (_serverSideConnection == null)
        {
            OwlLogger.LogError("Client tried to disconnect when no server is open!", GameComponent.Network);
            return -1;
        }

        if(!_clientSideConnections.Contains(clientsideConnection))
        {
            OwlLogger.LogError("Client tried to disconnect when it's not connected!", GameComponent.Network);
            return -2;
        }

        _clientSideConnections.Remove(clientsideConnection);
        return 0;
    }

    public override void SendPacket(object sender, int receiverId, Packet packet)
    {
        int senderId = GetNetworkId(sender);
        if(senderId <= 0)
        {
            OwlLogger.LogError($"Sender {sender} tried to send packet {packet} but isn't registered to Network!", GameComponent.Network);
            return;
        }

        if(senderId == 1) // Server is sending
        {
            if (receiverId == 0)
                SendPacketToAllClients(packet, senderId);
            else
                SendPacketToClient(receiverId, senderId, packet);
            return;
        }

        _serverSideConnection.Receive(packet, senderId);
    }

    private void SendPacketToAllClients(Packet packet, int senderId)
    {
        foreach(ServerConnection client in _clientSideConnections)
        {
            client.Receive(packet);
        }
    }

    private void SendPacketToClient(int receiverId, int senderId, Packet packet)
    {
        object receiver = GetFromNetworkId(receiverId);
        if(receiver == null)
        {
            OwlLogger.LogError($"Cannot resolve receiverId {receiverId} for packet {packet}!", GameComponent.Network);
            return;
        }

        if (receiver is not ServerConnection target)
        {
            OwlLogger.LogError($"Tried to send packet to client {receiver} that's not a valid Connection!", GameComponent.Network);
            return;
        }

        target.Receive(packet);
    }

    private int GetNetworkId(object target)
    {
        if (target == null)
            return -1;

        if (target == _serverSideConnection)
            return 1;

        if (target is not ServerConnection connection)
            return -1;

        int index = _clientSideConnections.IndexOf(connection);
        if (index == -1)
            return -1;

        return index + 2;
    }

    private object GetFromNetworkId(int networkId)
    {
        if (networkId == 1)
            return _serverSideConnection;

        if(networkId > 1)
        {
            int index = networkId - 2;
            return _clientSideConnections[index];
        }

        OwlLogger.LogError($"Attempted to resolve invalid networkId {networkId}!", GameComponent.Network);
        return null;
    }
}
