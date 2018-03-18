using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Amion.Network
{
    /// <summary>
    /// LAN discovery
    /// </summary>
    public class NetDiscovery : IDisposable
    {
        /// <summary>
        /// The port on which it performs the discovery.
        /// </summary>
        public int DiscoveryPort = 4356;

        /// <summary>
        /// Id of the application. Should be different for every app.
        /// </summary>
        public int ApplicationId = 42;

        /// <summary>
        /// Approval number of the application. Should be different for every app version which has different network logic.
        /// </summary>
        public int ApprovalNumber = 111;

        /// <summary>
        /// Extra 4 bytes of data to broadcast.
        /// </summary>
        public int MessageData = 0;

        /// <summary>
        /// Log issues on validation fail.
        /// </summary>
        public bool LogValidationIssues = false;

        /// <summary>
        /// Log action.
        /// </summary>
        public Action<string> Log = NetUtility.Log;

        private const int BufferSize = 16;
        private const int MV_Server = 792;
        private const int MV_Client = 348;

        private Socket discoverySocket;
        private AddressFamily prefAddressFamily;

        private Task responseTask;
        private bool responseLoop;

        private bool isServer;
        private Action<EndPoint> responseAction;

        private object serviceLock = new object();
        private object senderLock = new object();

        private int MsgValidator(bool isServer) => (isServer) ? MV_Server : MV_Client;

        /// <summary></summary>
        /// <param name="isServer">Is it for a server.</param>
        /// <param name="responseAction">Action to invoke on successful match. Pass null to just send response message.</param>
        /// <param name="prefAddressFamily">Preferred address family</param>
        public NetDiscovery(bool isServer, Action<EndPoint> responseAction, AddressFamily prefAddressFamily = AddressFamily.InterNetwork)
        {
            this.isServer = isServer;
            this.responseAction = responseAction;
            this.prefAddressFamily = prefAddressFamily;

            responseLoop = false;
            if (responseAction == null) this.responseAction = SendDiscoveryMessageVoid;

            CreateSocket();
        }

        /// <summary>
        /// Starts/Restarts the respose service
        /// </summary>
        public void StartResponseService()
        {
            lock (serviceLock)
            {
                if (responseTask != null) StopResponseService();

                try
                {
                    discoverySocket.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));

                    responseLoop = true;
                    responseTask = Task.Factory.StartNew(ResponseServiceLogic, TaskCreationOptions.LongRunning);
                }
                catch (Exception) { Log("Failed to start Discovery ResponseService"); }
            }
        }

        /// <summary>
        /// Stops the response service and recreates socket discovery socket.
        /// </summary>
        public void StopResponseService()
        {
            DestroySocket();
            responseLoop = false;
            responseTask?.Wait();
            responseTask = null;
            CreateSocket();
        }

        /// <summary>
        /// Returns true if the service thread is running.
        /// </summary>
        public bool IsServiceRunning()
        {
            return responseLoop;
        }

        /// <summary>
        /// Sends a discovery message.
        /// </summary>
        /// <param name="endPoint">Where to send. If null uses Broadcast and the DiscoveryPort.</param>
        /// <returns></returns>
        public bool SendDiscoveryMessage(EndPoint endPoint = null)
        {
            if (endPoint == null) endPoint = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);
            byte[] message = MessageEncoder(MsgValidator(isServer), MessageData);

            lock (senderLock)
            {
                try { discoverySocket.SendTo(message, endPoint); }
                catch { Log("Failed to send discovery message"); return false; }
            }

            return true;
        }

        private void SendDiscoveryMessageVoid(EndPoint endPoint = null) => SendDiscoveryMessage(endPoint);

        private void CreateSocket()
        {
            if (discoverySocket != null) return;

            discoverySocket = new Socket(prefAddressFamily, SocketType.Dgram, ProtocolType.Udp)
            {
                ExclusiveAddressUse = false
            };

            discoverySocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            discoverySocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        }

        private void DestroySocket()
        {
            if (discoverySocket == null) return;

            discoverySocket.Dispose();
            discoverySocket = null;
        }

        private void ResponseServiceLogic()
        {
            byte[] buffer = new byte[BufferSize];
            EndPoint tempRemoteEP = null;

            try
            {
                Log("Running discovery response service...");

                while (responseLoop)
                {
                    tempRemoteEP = new IPEndPoint(IPAddress.Any, 0);
                    discoverySocket.ReceiveFrom(buffer, ref tempRemoteEP);
                    int receivedPort = MessageDecoder(buffer, MsgValidator(!isServer));

                    if (receivedPort >= 0)
                    {
                        if (receivedPort == 0) responseAction(null);
                        else
                        {
                            IPAddress remoteAddress = ((IPEndPoint)tempRemoteEP).Address;
                            responseAction(new IPEndPoint(remoteAddress, receivedPort));
                        }
                    }
                }
            }
            catch (Exception) { }

            Log("Discovery response service shut down.");
        }

        private byte[] MessageEncoder(int validator, int data)
        {
            byte[] message = new byte[16];
            byte[][] rawMessage = new byte[4][];

            rawMessage[0] = BitConverter.GetBytes(ApplicationId);
            rawMessage[1] = BitConverter.GetBytes(ApprovalNumber);
            rawMessage[2] = BitConverter.GetBytes(validator);
            rawMessage[3] = BitConverter.GetBytes(data);

            for (int i = 0; i < 16; i++)
            {
                int s = i / 4;
                message[i] = rawMessage[s][i - s * 4];
            }

            return message;
        }

        private int MessageDecoder(byte[] message, int validator)
        {
            if (ApplicationId != BitConverter.ToInt32(message, 0))
            {
                if (LogValidationIssues) Log(NetUtility.Error(ECode.Discovery_Msg_IncorrectAppId));
                return -1;
            }
            else if (ApprovalNumber != BitConverter.ToInt32(message, 4))
            {
                if (LogValidationIssues) Log(NetUtility.Error(ECode.Discovery_Msg_IncorrectAppNum));
                return -2;
            }
            else if (validator != BitConverter.ToInt32(message, 8))
            {
                if (LogValidationIssues) Log(NetUtility.Error(ECode.Discovery_Msg_FailedValidation));
                return -3;
            }
            else return BitConverter.ToInt32(message, 12);
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
                if (discoverySocket != null)
                {
                    discoverySocket.Dispose();
                    discoverySocket = null;
                }
            }
        }
    }
}