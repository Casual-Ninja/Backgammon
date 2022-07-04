using System;
using System.Collections.Generic;
using BackGammonUser;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TicTacToe_MCTS_NEAT;
using System.Security.Cryptography;

namespace Server
{
    class Program
    {
        private const int portNumber = 12357;

        private static HashSet<string> onlineUsers = new HashSet<string>();

        private static TcpListener tcpListener;

        static void Main(string[] args)
        {
            AcceptClients();
        }

        /// <summary>
        /// Receives messages and sends the replys, handles disconnecting of the client.
        /// </summary>
        /// <param name="client">The user to interact with.</param>
        private static void InteractWithClient(object client)
        {
            ServerUser theUser = (ServerUser)client;
            lock (onlineUsers) // add the client to a list of online users
            {
                onlineUsers.Add(theUser.username);
            }

            while (true)
            {
                try
                {
                    theUser.CheckForMessages(); // check for messages from the client
                    theUser.PushData(); // send the replys of the messages received
                }
                catch (Exception exception) // if i reach here, something went wrong (either client disconnected or there was error)
                {
                    SaveLoad.SaveAllDataToDiskInThread(); // save all the data accumalated to the disk
                    lock (onlineUsers) // remove that user from the list
                    {
                        onlineUsers.Remove(theUser.username);
                        Console.WriteLine($"Removed user because:\n{exception}\nuser count: {onlineUsers.Count}");
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Starts listening for connections, when finds an connection calls interact with it.
        /// </summary>
        private static void AcceptClients()
        {
            // Initialize rsa keys
            RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
            EncryptionHandler.Initialize(RSA);
            
            // start listening for connections on port {portNumber}
            IPAddress ipAddress = new IPAddress(new byte[] { 0, 0, 0, 0 });
            tcpListener = new TcpListener(ipAddress, portNumber);

            tcpListener.Start();
            Console.WriteLine("Started lisntening...");

            Console.WriteLine("I am listening for connections on " +
            IPAddress.Parse(((IPEndPoint)tcpListener.LocalEndpoint).Address.ToString()) + 
            " on port number " + ((IPEndPoint)tcpListener.LocalEndpoint).Port.ToString());

            GAME.BackGammonChanceAction.UseValues(new GAME.ChanceActionValues(2.453f, 3.466f, 16.1f));

            while (true)
            {
                Thread.Sleep(100); // check for connection every 0.1 seconds
                if (tcpListener.Pending()) // is there a pending connection?
                {
                    Console.WriteLine("Found a pending request");
                    Socket client = tcpListener.AcceptSocket(); // accept the connection
                    Console.WriteLine("Accepted Request");

                    // start interacting with that new client on a new thread
                    Thread interactingThread = new Thread(InteractWithClient);
                    interactingThread.Start(new ServerUser(client, onlineUsers));
                }
            }
        }
    }
}