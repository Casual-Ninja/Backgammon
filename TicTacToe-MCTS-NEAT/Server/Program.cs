using System;
using System.Collections.Generic;
using BackGammonUser;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using TicTacToe_MCTS_NEAT;
using System.Runtime.InteropServices;

namespace Server
{
    class Program
    {
        private const int portNumber = 12357;
        
        private static Dictionary<string, ServerUser> knownClients;

        private static List<ServerUser> onlineUsers = new List<ServerUser>();

        private static TcpListener tcpListener;

        static void Main(string[] args)
        {
            knownClients = new Dictionary<string, ServerUser>();

            string[] allUserPaths = SaveLoad.GetAllFilesInDirectory(ServerUser.DataPath);
            if (allUserPaths != null)
            {
                foreach (string s in allUserPaths)
                {
                    object user;
                    if (SaveLoad.LoadData<ServerUser>(s, out user))
                    {
                        ServerUser theUser = (ServerUser)user;
                        knownClients.Add(theUser.username, theUser);
                    }
                }
            }

            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = new IPAddress(new byte[] { 0, 0, 0, 0 });
            Console.WriteLine(ipAddress);
            tcpListener = new TcpListener(ipAddress, portNumber);

            AcceptClients();
        }

        private static void InteractWithClient(object client)
        {
            ServerUser theUser = (ServerUser)client;
            lock (onlineUsers)
            {
                onlineUsers.Add(theUser);
            }
            while (true)
            {
                Thread.Sleep(100); // check for messages and send messages to client every 0.1 seconds
                try
                {
                    if (theUser.SocketConnected(1000) == false)
                        throw new Exception("Connection Isn't valid anymore");
                    else
                        Console.WriteLine("Socket is connected");

                    theUser.CheckForMessages();
                    theUser.PushData();
                }
                catch (Exception exception)
                {
                    SaveLoad.SaveAllDataToDiskInThread();
                    lock (onlineUsers)
                    {
                        onlineUsers.Remove(theUser);
                        Console.WriteLine($"Removed user because:\n{exception}\nuser count: {onlineUsers.Count}");
                    }
                    break;
                }
            }
        }

        private static void AcceptClients()
        {
            tcpListener.Start();
            Console.WriteLine("Started lisntening...");

            Console.WriteLine("I am listening for connections on " +
            IPAddress.Parse(((IPEndPoint)tcpListener.LocalEndpoint).Address.ToString()) +
            " on port number " + ((IPEndPoint)tcpListener.LocalEndpoint).Port.ToString());
            while (true)
            {
                Thread.Sleep(100); // check for connection every 0.1 seconds
                if (tcpListener.Pending())
                {
                    Console.WriteLine("Found a pending request");
                    Socket client = tcpListener.AcceptSocket();
                    Console.WriteLine("Accepted Request");

                    //// Get the size of the uint to use to back the byte array
                    //int size = Marshal.SizeOf((uint)0);

                    //// Create the byte array
                    //byte[] keepAlive = new byte[size * 3];

                    //// Pack the byte array:
                    //// Turn keepalive on
                    //Buffer.BlockCopy(BitConverter.GetBytes((uint)1), 0, keepAlive, 0, size);
                    //// Set amount of time without activity before sending a keepalive to 5 seconds
                    //Buffer.BlockCopy(BitConverter.GetBytes((uint)5000), 0, keepAlive, size, size);
                    //// Set keepalive interval to 5 seconds
                    //Buffer.BlockCopy(BitConverter.GetBytes((uint)5000), 0, keepAlive, size * 2, size);

                    //// Set the keep-alive settings on the underlying Socket
                    //client.IOControl(IOControlCode.KeepAliveValues, keepAlive, null);
                    //client.SendTimeout = 2000;
                    //client.ReceiveTimeout = 2000;

                    Thread interactingThread = new Thread(InteractWithClient);
                    interactingThread.Start(new ServerUser(client, knownClients));
                }
            }
        }
    }
}