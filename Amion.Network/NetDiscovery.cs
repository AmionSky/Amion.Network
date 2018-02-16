using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Amion.Network
{
    public class NetDiscovery : NetUtility, IDisposable
    {
        public int DiscoveryPort = 4356;
        public int ApplicationId = 42;
        public int ApprovalNumber = 111;
        public int MessageData = 0;

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

        public NetDiscovery(bool isServer, Action<EndPoint> responseAction, AddressFamily prefAddressFamily = AddressFamily.InterNetwork)
        {
            this.isServer = isServer;
            this.responseAction = responseAction;
            this.prefAddressFamily = prefAddressFamily;

            responseLoop = false;
            if (responseAction == null) this.responseAction = SendDiscoveryMessageVoid;

            CreateSocket();
        }

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

        public void StopResponseService()
        {
            DestroySocket();
            responseLoop = false;
            responseTask?.Wait();
            responseTask = null;
            CreateSocket();
        }

        public bool IsServiceRunning()
        {
            return responseLoop;
        }

        public bool SendDiscoveryMessage(EndPoint endPoint = null)
        {
            if (endPoint == null) endPoint = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);
            byte[] message = MessageEncoder(MessageData, MsgValidator(isServer));

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

        private byte[] MessageEncoder(int data, int validator)
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
                Error(ECode.Discovery_Msg_IncorrectAppId);
                return -1;
            }
            else if (ApprovalNumber != BitConverter.ToInt32(message, 4))
            {
                Error(ECode.Discovery_Msg_IncorrectAppNum);
                return -2;
            }
            else if (validator != BitConverter.ToInt32(message, 8))
            {
                Error(ECode.Discovery_Msg_FailedValidation);
                return -3;
            }
            else return BitConverter.ToInt32(message, 12);
        }


        /*
        public void StartServerResponseService(int listenerPort)
        {
            if (discoverySocket != null || responseTask != null) StopResponseService();

            discoverySocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            responseLoop = true;

            responseTask = Task.Run(() =>
            {
                try
                {
                    discoverySocket.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));
                    Log("Running discovery response service...");

                    EndPoint tempRemoteEP = null;
                    byte[] buffer = new byte[BufferSize];
                    byte[] message = MessageEncoder(listenerPort, MV_Server);

                    while (responseLoop)
                    {
                        //Server could crash here if there is another server
                        //on the network listening at the same port.
                        tempRemoteEP = new IPEndPoint(IPAddress.Any, 0);
                        discoverySocket.ReceiveFrom(buffer, ref tempRemoteEP);
                        int receivedPort = MessageDecoder(buffer, MV_Server);

                        if (receivedPort >= 0)
                        {
                            //Reply to client
                            discoverySocket.SendTo(message, tempRemoteEP);
                        }
                    }
                }
                catch (Exception)
                {
                    //Log(ex.Message);
                }
                finally
                {
                    discoverySocket?.Dispose();
                    discoverySocket = null;

                    Log("Discovery response service shut down.");
                }
            });
        }

        public void StartClientResponseService(Action<IPEndPoint> GotConnection)
        {
            if (discoverySocket != null || responseTask != null) StopResponseService();
            
            discoverySocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            responseLoop = true;

            responseTask = Task.Run(() =>
            {
                try
                {
                    discoverySocket.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort + 1));
                    Log("Running discovery response service...");
                    
                    EndPoint tempRemoteEP = null;
                    byte[] buffer = new byte[BufferSize];

                    while (responseLoop)
                    {
                        tempRemoteEP = new IPEndPoint(IPAddress.Any, 0);
                        discoverySocket.ReceiveFrom(buffer, ref tempRemoteEP);
                        int receivedPort = MessageDecoder(buffer, MV_Client);

                        if (receivedPort >= 0)
                        {
                            IPAddress remoteAddress = ((IPEndPoint)tempRemoteEP).Address;
                            GotConnection(new IPEndPoint(remoteAddress,receivedPort));
                        }
                    }
                }
                catch (Exception)
                {
                    //Log(ex.Message);
                }
                finally
                {
                    discoverySocket?.Dispose();
                    discoverySocket = null;

                    Log("Discovery response service shut down.");
                }
            });
        }

        

        public IPEndPoint StartClientHostDiscovery(out bool error, int timeout = 5000)
        {
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, timeout);

            IPEndPoint AllEndPoint = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);
            byte[] buffer = new byte[BufferSize];

            IPAddress remoteAddress = null;
            int remotePort = -1;

            error = false;

            try
            {
                //Send message to everyone on this network
                client.SendTo(MessageEncoder(timeout, MV_Server), AllEndPoint);
            }
            catch (Exception ex)
            {
                Log("Error in discovery search: " + ex.Message);

                client?.Dispose();

                error = true;
                return null;
            }
            
            EndPoint tempRemoteEP = null;

            while (remotePort < 0)
            {
                tempRemoteEP = new IPEndPoint(IPAddress.Any, 0);
                try { client.ReceiveFrom(buffer, ref tempRemoteEP); }
                catch (Exception ex) { Log(ex.Message); break; }
                remotePort = MessageDecoder(buffer, MV_Server);
            }

            //Get server IP and clean-up
            remoteAddress = ((IPEndPoint)tempRemoteEP).Address;
            client.Dispose();

            if (remoteAddress == null || remotePort < 0)
            {
                Log("No discovery host found.");
                return null;
            }
            else
            {
                Log($"Host: {remoteAddress}:{remotePort}");
                return new IPEndPoint(remoteAddress, remotePort);
            }
        }

        public void StartServerHostDiscovery(int listenerPort)
        {
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            IPEndPoint AllEndPoint = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort + 1);

            try
            {
                client.SendTo(MessageEncoder(listenerPort, MV_Client), AllEndPoint);
            }
            catch (Exception)
            {
                Log("Error in discovery search");
            }

            client?.Dispose();
        }
        */

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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