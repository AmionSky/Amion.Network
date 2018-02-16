using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Amion.Network
{
    public class NetServer : NetShared, IDisposable
    {
        
        public ConcurrentDictionary<Guid, NetConnection> Connections;

        public event EventHandler<ListenerStatusEventArgs> ListenerStatusChanged;

        private Socket listener;
        private Task accepterTask;

        public int ListenerPort => (listener?.LocalEndPoint as IPEndPoint)?.Port ?? 0;

        public NetServer()
        {
            Connections = new ConcurrentDictionary<Guid, NetConnection>();

            ConnectionStatusChanged += NetServer_ConnectionStatusChanged;
        }

        public void StartListener(int backlog = 10, int listenerPort = 0)
        {
            //Stops the listener if there is any.
            StopListener();

            //Create a socket for the listener server.
            listener = new Socket(PreferredAddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Log("Listener socket created.");

            //Get local IP address
            IPAddress localIP = GetLocalIPAddress(PreferredAddressFamily);
            if (localIP == null)
            {
                Error(ECode.Server_LocalIPNotFound);
                Log("Listener startup aborted!");
                return;
            }

            //Bind the socket to the local IP address.
            try { listener.Bind(new IPEndPoint(localIP, listenerPort)); }
            catch { Error(ECode.Server_FailedToBindListener); return; }
            IPEndPoint ipEndPoint = (IPEndPoint)listener.LocalEndPoint;
            Log($"Listener socket bound to {ipEndPoint.Address}:{ipEndPoint.Port}");

            //Start the listening.
            listener.Listen(backlog);
            Log("Listener started listening.");

            //Create a task for accepting connections. Async socket stuff?
            Log("Waiting for incoming connections...");
            OnListenerStatusChanged(true);
            accepterTask = Task.Factory.StartNew(ConnectionAccepter, TaskCreationOptions.LongRunning);
        }

        public void StopListener()
        {
            listener?.Dispose();
            listener = null;
            accepterTask?.Wait();
        }

        public void DisconnectAll()
        {
            foreach (var connection in Connections)
            {
                connection.Value?.Dispose();
            }
        }

        public bool IsListenerRunning()
        {
            return (listener != null) ? listener.IsBound : false;
        }

        public Socket GetListener()
        {
            return listener;
        }

        protected virtual void OnListenerStatusChanged(bool isActive)
        {
            ListenerStatusChanged?.Invoke(this, new ListenerStatusEventArgs(isActive));
        }

        private void ConnectionsCheck(object state)
        {
            foreach (var connection in Connections)
            {
                connection.Value?.Send(new NetOutMessage(MessageType.IsAlive));
            }
        }

        private void ConnectionAccepter()
        {
            while (listener != null && listener.IsBound)
            {
                Socket newConnection = null;
                try { newConnection = listener.Accept(); }
                catch (Exception) { break; }

                if (newConnection == null || !newConnection.Connected) break; ;

                newConnection.NoDelay = UseNoDelay;
                var netCon = new NetConnection(newConnection);

                if (Connections.TryAdd(netCon.RemoteId, netCon))
                {
                    OnConnectionAdded(netCon);
                    netCon.StatusChanged += OnConnectionStatusChanged;
                    OnConnectionStatusChanged(netCon, netCon.Status, netCon.RemoteId);
                }
                else
                {
                    Error(ECode.Server_FailedConnectionAdd);
                    netCon.Dispose();
                }
                
                if (netCon.Status != NetConnectionStatus.Disconnected && AutoStartReceiver)
                {
                    netCon.StartReceiverTask();
                }
            }

            Log("Connection accepter shut down.");
            OnListenerStatusChanged(false);
        }

        // Events handling
        private void NetServer_ConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs e)
        {
            if (e.Status == NetConnectionStatus.Disconnected)
            {
                if (Connections.TryRemove(e.RemoteId, out var connection))
                {
                    if (connection != null)
                    {
                        connection.StatusChanged -= OnConnectionStatusChanged;
                    }
                    
                    OnConnectionRemoved(e.RemoteId);
                    Log("Connection removed: Guid: " + e.RemoteId);
                }
                else Error(ECode.Server_FailedConnectionRemove);
            }
        }

        // Other
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisconnectAll();

                if (listener != null)
                {
                    listener.Dispose();
                    listener = null;
                }
            }
        }
    }
}