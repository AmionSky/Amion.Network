using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Amion.Network
{
    /// <summary>
    /// Class for handling multiple connections. Also implements a listener for incoming connections.
    /// </summary>
    public class NetServer : NetShared, IDisposable
    {
        /// <summary>
        /// Currently active connections
        /// </summary>
        public ConcurrentDictionary<Guid, NetConnection> Connections;

        /// <summary>
        /// Called when the listener socket's status change
        /// </summary>
        public event EventHandler<ListenerStatusEventArgs> ListenerStatusChanged;

        /// <summary>
        /// Gets the port the listener running on. If fails returns 0.
        /// </summary>
        public int ListenerPort => (listener?.LocalEndPoint as IPEndPoint)?.Port ?? 0;

        private Socket listener;
        private Task accepterTask;

        /// <summary></summary>
        public NetServer()
        {
            Connections = new ConcurrentDictionary<Guid, NetConnection>();

            ConnectionStatusChanged += NetServer_ConnectionStatusChanged;
        }

        /// <summary>
        /// Starts/restarts the listener on a Local IP.
        /// </summary>
        /// <param name="listenerEndPoint">Listener IP end point. If null it uses LAN IP with any available port.</param>
        /// <param name="backlog">The maximum length of the pending connections queue.</param>
        public void StartListener(IPEndPoint listenerEndPoint = null, int backlog = 10)
        {
            //Stops the listener if there is any.
            StopListener();

            //Create a socket for the listener server.
            try { listener = new Socket(PreferredAddressFamily, SocketType.Stream, ProtocolType.Tcp); }
            catch (SocketException)
            {
                Log(NetUtility.Error(ECode.Server_LocalIPNotFound));
                StopListener();
                return;
            }
            
            Log("Listener socket created.");

            if (listenerEndPoint == null)
            {
                //Get local IP address
                IPAddress localIP = NetUtility.GetLocalIPAddress(PreferredAddressFamily);
                if (localIP == null)
                {
                    Log(NetUtility.Error(ECode.Server_FailedToCreateListenerSocket));
                    StopListener();
                    return;
                }

                listenerEndPoint = new IPEndPoint(localIP, 0);
            }

            //Bind the socket to the local IP address.
            try { listener.Bind(listenerEndPoint); }
            catch (Exception)
            {
                Log(NetUtility.Error(ECode.Server_FailedToBindListener));
                StopListener();
                return;
            }

            listenerEndPoint = (IPEndPoint)listener.LocalEndPoint;
            Log($"Listener socket bound to {listenerEndPoint.Address}:{listenerEndPoint.Port}");

            //Start the listening.
            listener.Listen(backlog);
            Log("Listener started listening.");

            //Create a task for accepting connections. Async socket stuff?
            Log("Waiting for incoming connections...");
            OnListenerStatusChanged(true);
            accepterTask = Task.Factory.StartNew(ConnectionAccepter, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Stops the listener.
        /// </summary>
        public void StopListener()
        {
            if (listener != null)
            {
                listener.Dispose();
                listener = null;

                if (accepterTask != null)
                {
                    accepterTask.Wait();
                    accepterTask = null;
                }
            }
        }

        /// <summary>
        /// Disconnects all connections.
        /// </summary>
        public void DisconnectAll()
        {
            foreach (var connection in Connections)
            {
                connection.Value?.Dispose();
            }
        }

        /// <summary>
        /// Sends an IsAlive message to all connection to verify if the sockets are still open.
        /// </summary>
        public void CheckConnections()
        {
            foreach (var connection in Connections)
            {
                connection.Value?.Send(new NetOutMessage(MessageType.IsAlive));
            }
        }

        /// <summary>
        /// Gets if the listener is running.
        /// </summary>
        public bool IsListenerRunning()
        {
            return (listener != null) ? listener.IsBound : false;
        }

        /// <summary>
        /// Gets the listener socket
        /// </summary>
        public Socket GetListener()
        {
            return listener;
        }

        /// <summary>
        /// Invokes ListenerStatusChanged event.
        /// </summary>
        /// <param name="isActive">Is the listener socket active</param>
        protected virtual void OnListenerStatusChanged(bool isActive)
        {
            ListenerStatusChanged?.Invoke(this, new ListenerStatusEventArgs(isActive));
        }

        // AccepterTask's action
        private void ConnectionAccepter()
        {
            while (listener != null && listener.IsBound)
            {
                Socket newConnection = null;
                try { newConnection = listener.Accept(); }
                catch (Exception) { break; }

                if (newConnection == null || !newConnection.Connected) break;

                newConnection.NoDelay = UseNoDelay;
                var netCon = new NetConnection(newConnection, OnConnectionStatusChanged);

                if (Connections.TryAdd(netCon.RemoteId, netCon))
                {
                    OnConnectionAdded(netCon);
                }
                else
                {
                    Log(NetUtility.Error(ECode.Server_FailedConnectionAdd));
                    netCon.Dispose();
                }
                
                if (AutoStartReceiver) netCon.StartReceiverTask();
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
                }
                else Log(NetUtility.Error(ECode.Server_FailedConnectionRemove));
            }
        }

        /// <summary>
        /// Shuts down worker thread and releases resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Helper for Dispose()
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopListener();
                DisconnectAll();
            }
        }
    }
}