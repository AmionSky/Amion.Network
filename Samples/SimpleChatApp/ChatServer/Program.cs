using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Amion.Network;

namespace ChatServer
{
    class Program
    {
        static NetServer server;
        static NetMessageHandler messageHandler;

        static void Main(string[] args)
        {
            Console.WriteLine("Amion.Network SimpleChat Server\n");

            Console.WriteLine("Type '!start' to start the server");
            Console.WriteLine("Type '!help' for all commands\n");

            //Create the network server
            server = new NetServer();

            //Subscribe to Connection Added/Removed event
            server.ConnectionAdded += Server_ConnectionAdded;
            server.ConnectionRemoved += Server_ConnectionRemoved;

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
                    case "!start":
                        {
                            Console.WriteLine("Starting the server");

                            //Start the server's listener socket on IP: 127.0.0.1:6695
                            server.StartListener(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 6695));

                            break;
                        }
                    case "!stop":
                        {
                            Console.WriteLine("Stopping the server");

                            server.StopListener();
                            server.DisconnectAll();

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
                            Console.WriteLine("Type '!stop' to stop the server");
                            Console.WriteLine("Type '!start' to start the server");
                            break;
                        }
                    default:
                        {
                            Console.WriteLine("Unknown command");
                            break;
                        }
                }
            } while (!exit);
        }

        // --------------------------------------------------
        //        Server's Connection Added / Removed
        // --------------------------------------------------
        private static void Server_ConnectionAdded(object sender, ConnectionAddedEventArgs e)
        {
            //Forward the connection's messages to the message handler
            //>> It is possible to directly add our MessageReceived method to it 
            //>> but then it would block the network message receiving thread
            e.Connection.RawMessageReceived += messageHandler.RawMessageReceived;

            //Write some info out
            Console.WriteLine($"Client connected! UniqueID: {e.Connection.RemoteId}, IP Address: {e.Connection.RemoteEndPoint}");
        }

        private static void Server_ConnectionRemoved(object sender, ConnectionRemovedEventArgs e)
        {
            //Write some info out
            Console.WriteLine($"Client disconnected! UniqueID: {e.RemoteId}");
        }

        // --------------------------------------------------
        //                Message Received
        // --------------------------------------------------
        private static void MessageHandler_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            //The clients send a string as chat message so we just read it and store it
            string chatMessage = e.Message.ReadString();

            //Display the chat message on the server
            Console.WriteLine($"{e.RemoteId}: {chatMessage}");

            //Create an outgoing network message for the chat message
            NetOutMessage netMessage = new NetOutMessage();
            netMessage.Write(chatMessage);
            netMessage.Finish();

            //Now transmit the message to the other clients
            foreach (var item in server.Connections)
            {
                //We don't want to send the message back to the client that sent it
                if (item.Key != e.RemoteId)
                {
                    //Send the message to a client
                    item.Value?.Send(netMessage);
                }
            }
        }
    }
}
