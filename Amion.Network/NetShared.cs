using System;
using System.Net.Sockets;

namespace Amion.Network
{
    /// <summary>
    /// Base class for NetServer and NetClient.
    /// </summary>
    public abstract class NetShared : NetUtility
    {
        /// <summary>
        /// Preferred address family. Recommended to leave as default.
        /// </summary>
        public AddressFamily PreferredAddressFamily = AddressFamily.InterNetwork;

        /// <summary>
        /// Use TCP no-delay (Nagle algorithm)
        /// </summary>
        public bool UseNoDelay = false;

        /// <summary>
        /// Automatically start the message receiver. Don't use it if you didn't set up listening for raw incoming messages on ConnectionAdded event.
        /// </summary>
        public bool AutoStartReceiver = true;

        /// <summary>
        /// Called when established a new connection.
        /// </summary>
        public event EventHandler<ConnectionAddedEventArgs> ConnectionAdded;

        /// <summary>
        /// Called after disconnecting.
        /// </summary>
        public event EventHandler<ConnectionRemovedEventArgs> ConnectionRemoved;

        /// <summary>
        /// Called when a status of a NetConnection changed.
        /// Use ConnectionAdded / ConnectionRemoved for program logic.
        /// Called before ConnectionAdded. Called around at the same time with ConnectionRemoved.
        /// </summary>
        public event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;

        #region Events

        /// <summary>
        /// Invokes ConnectionAdded event.
        /// </summary>
        protected virtual void OnConnectionAdded(ConnectionAddedEventArgs e)
        {
            ConnectionAdded?.Invoke(this, e);
        }
        /// <summary>
        /// Invokes ConnectionAdded event.
        /// </summary>
        /// <param name="netConnection">The new connection</param>
        protected virtual void OnConnectionAdded(NetConnection netConnection)
        {
            ConnectionAdded?.Invoke(this, new ConnectionAddedEventArgs(netConnection));
        }

        /// <summary>
        /// Invokes ConnectionRemoved event.
        /// </summary>
        protected virtual void OnConnectionRemoved(ConnectionRemovedEventArgs e)
        {
            ConnectionRemoved?.Invoke(this, e);
        }
        /// <summary>
        /// Invokes ConnectionRemoved event.
        /// </summary>
        /// <param name="remoteId">The remote ID of the removed connection</param>
        protected virtual void OnConnectionRemoved(Guid remoteId)
        {
            ConnectionRemoved?.Invoke(this, new ConnectionRemovedEventArgs(remoteId));
        }

        /// <summary>
        /// Invokes ConnectionStatusChanged event.
        /// </summary>
        protected virtual void OnConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs e)
        {
            ConnectionStatusChanged?.Invoke(sender, e);
        }
        /// <summary>
        /// Invokes ConnectionStatusChanged event.
        /// </summary>
        /// <param name="sender">NetConnection</param>
        /// <param name="status">The current status of the connection</param>
        /// <param name="remoteId">The remote ID of the connection</param>
        protected virtual void OnConnectionStatusChanged(object sender, NetConnectionStatus status, Guid remoteId)
        {
            ConnectionStatusChanged?.Invoke(sender, new ConnectionStatusChangedEventArgs(status, remoteId));
        }

        #endregion
    }
}