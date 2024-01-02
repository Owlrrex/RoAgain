using OwlLogging;
using SuperSimpleTcp;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mono.Nat;
using System.Linq;

namespace Server
{
    public abstract class CentralConnection
    {
        public Action<int> CharacterDisconnected;

        public abstract int Initialize(AServer parentServer, string port);

        public abstract int Send(Packet packet);

        public abstract void Receive(Packet packet, int senderId);

        public abstract void Update();

        public abstract int Shutdown();

        public abstract void DisconnectClient(ClientConnection connection);
    }

    public class CentralConnectionImpl : CentralConnection
    {
        private Dictionary<int, ClientConnection> _connectionsBySessionId = new();
        private Dictionary<int, string> _clientTargetsBySessionId = new();

        private int _nextSessionId = 1;
        private AServer _parentServer;

        private SimpleTcpServer _server;
        private byte[] _remainingData = new byte[0];
        private System.Collections.Concurrent.ConcurrentBag<Packet> _readyPackets = new();

        // This function handles the actual network-connection logic
        public override int Send(Packet packet)
        {
            string targetNetworkId = GetNetworkIdBySessionId(packet.SessionId);
            if (string.IsNullOrEmpty(targetNetworkId))
            {
                OwlLogger.LogError($"Can't send packet without valid connection: {packet.SerializeReflection()}", GameComponent.Network);
                return -1;
            }

            if(_server == null)
            {
                OwlLogger.LogError($"Can't send packet {packet} through uninitialized CentralConnection!", GameComponent.Network);
                return -2;
            }

            // TODO: Serialize packet properly
            byte[] serializedB = packet.SerializeJson();
            if (serializedB == null)
            {
                return -1;
            }

            _server.Send(targetNetworkId.ToString(), serializedB);

            return 0;
        }

        private string GetNetworkIdBySessionId(int sessionId)
        {
            if (!_clientTargetsBySessionId.ContainsKey(sessionId))
            {
                OwlLogger.LogError($"ServerSide CentralConnection tried to resolve NetworkId for invalid SessionId: {sessionId}", GameComponent.Network);
                return "";
            }
            return _clientTargetsBySessionId[sessionId];
        }

        public override void Receive(Packet packet, int senderId)
        {
            OwlLogger.Log($"ServerSide CentralConnection received Packet: {packet.SerializeReflection()}", GameComponent.Network, LogSeverity.VeryVerbose);

            // Maybe later: Further security checks to avoid people sending packets for other players

            int receivedSessionId = packet.SessionId;
            ClientConnection targetConnection = GetConnectionForSessionId(receivedSessionId);
            if (targetConnection == null)
            {
                OwlLogger.LogError($"Can't handle packet {packet}", GameComponent.Network);
                return;
            }

            targetConnection.Receive(packet);
        }

        private ClientConnection GetConnectionForSessionId(int sessionId)
        {
            if (!_connectionsBySessionId.ContainsKey(sessionId))
            {
                OwlLogger.LogError($"ServerSide CentralConnection tried to handle packet with invalid SessionId: {sessionId}", GameComponent.Network);
                return null;
            }
            return _connectionsBySessionId[sessionId];
        }

        private ClientConnection CreateNewConnection(string clientNetworkId)
        {
            ClientConnection newClientConnection = new ClientConnectionImpl();
            int newSessionId = GenerateNewSessionId();
            int connectionInitResult = newClientConnection.Initialize(this, newSessionId);
            if (connectionInitResult != 0)
            {
                OwlLogger.LogError($"Initializing new Connection for client {clientNetworkId} failed: {connectionInitResult}!", GameComponent.Network);
                return null;
            }
            int serverSetupResult = _parentServer.SetupWithNewClientConnection(newClientConnection);
            if(serverSetupResult != 0)
            {
                OwlLogger.LogError($"ServerSetup with new connection for client {clientNetworkId} failed: {serverSetupResult}!", GameComponent.Network);
                return null;
            }

            _connectionsBySessionId.Add(newSessionId, newClientConnection);
            _clientTargetsBySessionId.Add(newSessionId, clientNetworkId);

            return newClientConnection;
        }

        private int GenerateNewSessionId()
        {
            return _nextSessionId++;
        }

        public override int Initialize(AServer parentServer, string ipPort)
        {
            if (parentServer == null)
            {
                OwlLogger.LogError("Cannot initialize CentralConnection with null ParentServer!", GameComponent.Network);
                return -1;
            }

            if(_server != null)
            {
                OwlLogger.LogError($"Cannot initialize CentralConnection that's already initialized!", GameComponent.Network);
                return -2;
            }

            _parentServer = parentServer;

            NatUtility.DeviceFound += OnDeviceFound;

            NatUtility.StartDiscovery();

            _server = new(ipPort);
            // Setup settings here like this:
            // _server.Settings.AcceptInvalidCertificates = true;
            _server.Settings.StreamBufferSize = 8192;

            _server.Events.ClientConnected += OnClientConnected;
            _server.Events.ClientDisconnected += OnClientDisconnected;
            _server.Events.DataReceived += OnDataReceived;
            _server.Events.DataSent += OnDataSent; // Needed?

            _server.Start();

            return 0;
        }

        private void OnDeviceFound(object sender, DeviceEventArgs e)
        {
            if (e.Device.GetSpecificMapping(Protocol.Tcp, 13337).PublicPort == -1)
            {
                e.Device.CreatePortMap(new(Protocol.Tcp, 13337, 13337));
            }
        }

        private void OnClientConnected(object sender, ConnectionEventArgs args)
        {
            OwlLogger.Log($"Server CentralConnection received Connection from {args.IpPort} ({args.Reason})", GameComponent.Network, LogSeverity.Verbose);

            string clientId = args.IpPort;
            ClientConnection connection = CreateNewConnection(clientId);
            if (connection == null)
            {
                OwlLogger.LogError($"Failed to create Connection for ClientId {clientId}!", GameComponent.Network);
                return;
            }
            int sessionId = _nextSessionId - 1; // TODO: HACK only works for serial sessionIds, clean this up properly
            SessionCreationPacket packet = new()
            {
                SessionId = sessionId,
                AssignedSessionId = sessionId
            };
            Send(packet);
        }

        private void OnClientDisconnected(object sender, ConnectionEventArgs args)
        {
            OwlLogger.Log($"Server CentralConnection received Disconnection from {args.IpPort} ({args.Reason})", GameComponent.Network, LogSeverity.Verbose);

            // Get connection for IpPort
            ClientConnection connection = null;
            int sessionId = -1;
            foreach(KeyValuePair<int, string> kvp in _clientTargetsBySessionId)
            {
                if(kvp.Value == args.IpPort)
                {
                    sessionId = kvp.Key;
                    connection = _connectionsBySessionId[sessionId];
                    break;
                }
            }

            if(connection == null)
            {
                OwlLogger.LogError($"Connection for disconnected Client {args.IpPort} not found!", GameComponent.Network);
                return;
            }

            // Get EntityId for Connection
            int entityId = connection.CharacterId;

            // Cleanup connection internally
            _clientTargetsBySessionId.Remove(sessionId);
            _connectionsBySessionId.Remove(sessionId);

            if (entityId <= 0)
            {
                OwlLogger.Log($"Connection from {args.IpPort} disconnected before having a character logged in.", GameComponent.Network);
            }
            else
            {
                CharacterDisconnected?.Invoke(entityId);
            }
        }

        public override void DisconnectClient(ClientConnection connection)
        {
            int sessionId = -1;
            foreach(KeyValuePair<int, ClientConnection> kvp in _connectionsBySessionId)
            {
                if (kvp.Value == connection)
                {
                    sessionId = kvp.Key;
                    break;
                }
            }

            if(sessionId <= 0)
            {
                OwlLogger.LogError($"Tried to disconnect connection that couldn't be found in CentralConnection!", GameComponent.Network);
                return;
            }

            // errocheck
            string clientTarget = _clientTargetsBySessionId[sessionId];

            _server.DisconnectClient(clientTarget);
        }

        private void OnDataReceived(object sender, DataReceivedEventArgs args)
        {
            OwlLogger.Log($"Server CentralConnection received Data from {args.IpPort}: {System.Text.Encoding.UTF8.GetString(args.Data.Array, 0, args.Data.Count)}", GameComponent.Network, LogSeverity.VeryVerbose);

            List<byte> allData = new(_remainingData);
            allData.AddRange(args.Data);
            string[] completedPackets = Packet.SplitIntoPackets(allData.ToArray(), out _remainingData);
            foreach(string completedPacketStr in completedPackets)
            {
                Packet packet = Packet.DeserializeJson(completedPacketStr);
                _readyPackets.Add(packet);
            }
        }

        private void OnDataSent(object sender, DataSentEventArgs args)
        {
            OwlLogger.Log($"Server CentralConnection sent {args.BytesSent} Bytes to {args.IpPort}", GameComponent.Network, LogSeverity.VeryVerbose);
        }

        public override void Update()
        {
            Packet packet;
            bool canTake = _readyPackets.TryTake(out packet);
            while (canTake)
            {
                Receive(packet, 0); // Sender-Id only needed for Dummy-Server login handling
                if (_readyPackets.Count == 0)
                    break;
                canTake = _readyPackets.TryTake(out packet);
            }
        }

        public override int Shutdown()
        {
            foreach(var kvp in _connectionsBySessionId)
            {
                kvp.Value.Shutdown();
            }

            // Don't clear _clientTargetsBySessionId and _connectionsBySessionId - used by async OnDisconnect callback

            if (_server != null)
            {
                _server.Stop();
                _server.Dispose();
                _server = null;
            }

            _parentServer = null;
            _readyPackets.Clear();
            _remainingData = new byte[0];
            
            return 0;
        }
    }

    public class DummyCentralConnection : CentralConnection
    {
        private Dictionary<int, ClientConnection> _connectionsBySessionId = new();
        private Dictionary<int, int> _clientTargetsBySessionId = new();

        private int _nextSessionId = 1;
        private AServer _parentServer;

        // This function handles the actual network-connection logic
        public override int Send(Packet packet)
        {
            int targetNetworkId = GetNetworkIdBySessionId(packet.SessionId);
            if (targetNetworkId == -1)
            {
                OwlLogger.LogError($"Can't send packet without valid connection: {packet}", GameComponent.Network);
                return -1;
            }
            ADummyInternet.Instance.SendPacket(this, targetNetworkId, packet);
            return 0;
        }

        private int GetNetworkIdBySessionId(int sessionId)
        {
            if(!_clientTargetsBySessionId.ContainsKey(sessionId))
            {
                OwlLogger.LogError($"ServerSide DummyCentralConnection tried to resolve NetworkId for invalid SessionId: {sessionId}", GameComponent.Network);
                return -1;
            }
            return _clientTargetsBySessionId[sessionId];
        }

        public override void Receive(Packet packet, int senderId)
        {
            OwlLogger.Log($"ServerSide DummyCentralConnection received Packet: {packet.SerializeReflection()}", GameComponent.Network, LogSeverity.VeryVerbose);

            // Maybe later: Further security checks to avoid people sending packets for other players

            Type packetType = packet.GetType();

            // Handling the first packet that any client may send to the Server, which initializes a new connection
            if (packetType == typeof(LoginRequestPacket))
            {
                ClientConnection connection = CreateNewConnection(senderId);
                connection.Receive(packet);
                return;
            }

            int receivedSessionId = packet.SessionId;
            ClientConnection targetConnection = GetConnectionForSessionId(receivedSessionId);
            if(targetConnection == null)
            {
                OwlLogger.LogError($"Can't handle packet {packet}", GameComponent.Network);
                return;
            }

            targetConnection.Receive(packet);
        }

        private ClientConnection GetConnectionForSessionId(int sessionId)
        {
            if (!_connectionsBySessionId.ContainsKey(sessionId))
            {
                OwlLogger.LogError($"ServerSide DummyCentralConnection tried to handle packet with invalid SessionId: {sessionId}", GameComponent.Network);
                return null;
            }
            return _connectionsBySessionId[sessionId];
        }

        private ClientConnection CreateNewConnection(int clientNetworkId)
        {
            ClientConnection newClientConnection = new ClientConnectionImpl();
            int newSessionId = GenerateNewSessionId();
            int connectionInitResult = newClientConnection.Initialize(this, newSessionId);
            int serverSetupResult = _parentServer.SetupWithNewClientConnection(newClientConnection);
            _connectionsBySessionId.Add(newSessionId, newClientConnection);
            _clientTargetsBySessionId.Add(newSessionId, clientNetworkId);

            return newClientConnection;
        }

        private int GenerateNewSessionId()
        {
            return _nextSessionId++;
        }

        private void FakeNetworkDelay<T>(Action<T> function, T packet, float delaySeconds)
        {
            CoroutineRunner.StartNewCoroutine(FakeNetworkDelayInternal(function, packet, delaySeconds));
        }

        private IEnumerator FakeNetworkDelayInternal<T>(Action<T> function, T packet, float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            function(packet);
        }

        public override int Initialize(AServer parentServer, string port)
        {
            if(parentServer == null)
            {
                OwlLogger.LogError("Cannot initialize Central connection with null ParentServer!", GameComponent.Other);
                return -1;
            }

            _parentServer = parentServer;
            return ADummyInternet.Instance.OpenServer(this);
            
        }

        public override int Shutdown()
        {
            NatUtility.StopDiscovery();
            _parentServer = null;
            // Close Server: ADummyInternet.Instance.CloseServer(this);
            return 0;
        }

        public override void Update()
        {
            
        }

        public override void DisconnectClient(ClientConnection connection)
        {
            throw new NotImplementedException();
        }
    }
}