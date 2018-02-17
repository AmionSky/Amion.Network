using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Amion.Network
{
    public enum ECode
    {
        Server_FailedConnectionAdd,
        Server_FailedConnectionRemove,
        Server_LocalIPNotFound,
        Server_FailedToBindListener,
    }

    public abstract class NetUtility
    {
        protected static void Error(ECode code)
        {
            Log($"Error #{((int)code).ToString("0000")}: {code}");
        }

        protected static void Log(string text)
        {
            Console.WriteLine(text);
        }

        protected static IPAddress GetLocalIPAddress(AddressFamily addressFamily)
        {
            return NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault
            (
                ni => ni.OperationalStatus == OperationalStatus.Up
                && ni.GetIPProperties().GatewayAddresses.FirstOrDefault() != null
            )
            ?.GetIPProperties().UnicastAddresses.FirstOrDefault
            (
                ip => ip.Address.AddressFamily == addressFamily
            )?.Address;
        }
    }
}