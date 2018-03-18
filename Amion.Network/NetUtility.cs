using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Amion.Network
{
    /// <summary>
    /// Error code enum.
    /// </summary>
    public enum ECode
    {
        /// <summary>Server: Failed to add connection.</summary>
        Server_FailedConnectionAdd,
        /// <summary>Server: Failed to remove connection.</summary>
        Server_FailedConnectionRemove,
        /// <summary>Server: Failed to find a local IP address.</summary>
        Server_LocalIPNotFound,
        /// <summary>Server: Failed to bind the listener socket.</summary>
        Server_FailedToBindListener,
        /// <summary>Server: Failed to create the listener socket.</summary>
        Server_FailedToCreateListenerSocket,
    }

    /// <summary>
    /// Miscellaneous static methods used by classes.
    /// </summary>
    public static class NetUtility
    {
        /// <summary>
        /// Log an error.
        /// </summary>
        /// <param name="code">Error code enum</param>
        public static string Error(ECode code)
        {
            return $"Error #{((int)code).ToString("0000")}: {code}";
        }

        /// <summary>
        /// Log to console.
        /// </summary>
        /// <param name="text">Text to write</param>
        public static void Log(string text)
        {
            Console.WriteLine(text);
        }

        /// <summary>
        /// Gets first active LAN IP address.
        /// </summary>
        /// <param name="addressFamily">Address family to search for</param>
        /// <returns>LAN IP address</returns>
        public static IPAddress GetLocalIPAddress(AddressFamily addressFamily)
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