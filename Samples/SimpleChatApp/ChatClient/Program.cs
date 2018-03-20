using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Amion.Network;

namespace ChatClient
{
    class Program
    {
        static NetClient client;
        static NetMessageHandler messageHandler;

        static void Main(string[] args)
        {
            Console.WriteLine("Amion.Network SimpleChat Client\n");

            Console.WriteLine("Type '!connect' to connect to the server");
            Console.WriteLine("Type '!help' for all commands\n");

            //Create the network client
            client = new NetClient();

            //Subscribe to Connection Added/Removed event
            client.ConnectionAdded += Client_ConnectionAdded;
            client.ConnectionRemoved += Client_ConnectionRemoved;

            //Create a message handler for incoming network messages
            messageHandler = new NetMessageHandler();

            //Subscribe to the MessageReceived event where we will handle all incoming network messages
            messageHandler.MessageReceived += MessageHandler_MessageReceived;

            //Start the message processor thread
            messageHandler.StartMessageProcessor();

            //Begin Loop to keep the program running
            bool exit = false;
            do
            {
                string command = Console.ReadLine();

                switch (command)
                {
                    case "!connect":
                        {
                            Console.WriteLine("Connecting to the server");

                            //Connect to IP: 127.0.0.1:6695
                            client.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6695));

                            break;
                        }
                    case "!disconnect":
                        {
                            Console.WriteLine("Disconnecting from the server");
                            client.Disconnect();
                            break;
                        }
                    case "!exit":
                        {
                            Console.WriteLine("Exiting the program");
                            exit = true;
                            break;
                        }
                    case "!help":
                        {
                            Console.WriteLine("Type '!exit' to quit the program");
                            Console.WriteLine("Type '!disconnect' to disconnect from the server");
                            Console.WriteLine("Type '!connect' to connect to the server");
                            break;
                        }
                    default:
                        {
                            //Create an outgoing network message for the chat message
                            NetOutMessage netMessage = new NetOutMessage();
                            netMessage.Write(command);
                            netMessage.Finish();

                            //Send the message if possible
                            client.Connection?.Send(netMessage);

                            break;
                        }
                }
            } while (!exit);
        }

        // --------------------------------------------------
        //        Client's Connection Added / Removed
        // --------------------------------------------------
        private static void Client_ConnectionAdded(object sender, ConnectionAddedEventArgs e)
        {
            //Forward the connection's messages to the message handler
            //>> It is possible to directly add our MessageReceived method to it 
            //>> but then it would block the network message receiving thread
            e.Connection.RawMessageReceived += messageHandler.RawMessageReceived;

            //Write some info out
            Console.WriteLine("Connected to server!");
        }

        private static void Client_ConnectionRemoved(object sender, ConnectionRemovedEventArgs e)
        {
            //Write some info out
            Console.WriteLine("Disconnected from server!");
        }

        // --------------------------------------------------
        //                Message Received
        // --------------------------------------------------
        private static void MessageHandler_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            //Read the chat message
            string chatMessage = e.Message.ReadString();

            //Display the chat message
            Console.WriteLine($"{e.RemoteId}: {chatMessage}");
        }
    }
}
