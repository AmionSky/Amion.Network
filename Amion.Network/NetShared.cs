using System;
using System.Net.Sockets;

namespace Amion.Network
{
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

        protected virtual void OnConnectionAdded(ConnectionAddedEventArgs e)
        {
            ConnectionAdded?.Invoke(this, e);
        }
        protected virtual void OnConnectionAdded(NetConnection netConnection)
        {
            ConnectionAdded?.Invoke(this, new ConnectionAddedEventArgs(netConnection));
        }

        protected virtual void OnConnectionRemoved(ConnectionRemovedEventArgs e)
        {
            ConnectionRemoved?.Invoke(this, e);
        }
        protected virtual void OnConnectionRemoved(Guid remoteId)
        {
            ConnectionRemoved?.Invoke(this, new ConnectionRemovedEventArgs(remoteId));
        }

        protected virtual void OnConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs e)
        {
            ConnectionStatusChanged?.Invoke(sender, e);
        }
        protected virtual void OnConnectionStatusChanged(object sender, NetConnectionStatus status, Guid remoteId)
        {
            ConnectionStatusChanged?.Invoke(sender, new ConnectionStatusChangedEventArgs(status, remoteId));
        }

        #endregion
    }
}