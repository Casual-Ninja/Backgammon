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
        
        private static Dictionary<string, SavingServerUser> knownClients;

        private static HashSet<string> onlineUsers = new HashSet<string>();

        private static TcpListener tcpListener;

        static void Main(string[] args)
        {
            knownClients = new Dictionary<string, SavingServerUser>();

            string[] allUserPaths = SaveLoad.GetAllFilesInDirectory(ServerUser.DataPath);
            
            if (allUserPaths != null)
            {
                foreach (string s in allUserPaths)
                {
                    Console.WriteLine(s);
                    object user;
                    if (SaveLoad.LoadData<SavingServerUser>(s, out user))
                    {
                        SavingServerUser theUser = (SavingServerUser)user;
                        knownClients.Add(theUser.username, theUser);

                        Console.WriteLine(theUser);
                    }
                    else
                        Console.WriteLine("Didn't manage to read");
                }
            }

            AcceptClients();
        }

        private static void InteractWithClient(object client)
        {
            ServerUser theUser = (ServerUser)client;
            lock (onlineUsers)
            {
                onlineUsers.Add(theUser.username);
            }
            while (true)
            {
                try
                {
                    theUser.CheckForMessages();
                    theUser.PushData();
                }
                catch (Exception exception)
                {
                    SaveLoad.SaveAllDataToDiskInThread();
                    lock (onlineUsers)
                    {
                        onlineUsers.Remove(theUser.username);
                        Console.WriteLine($"Removed user because:\n{exception}\nuser count: {onlineUsers.Count}");
                    }
                    break;
                }
            }
        }

        private static void AcceptClients()
        {
            RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
            EncryptionHandler.Initialize(RSA);

            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = new IPAddress(new byte[] { 0, 0, 0, 0 });
            Console.WriteLine(ipAddress);
            tcpListener = new TcpListener(ipAddress, portNumber);

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

                    Thread interactingThread = new Thread(InteractWithClient);
                    interactingThread.Start(new ServerUser(client, knownClients, onlineUsers));
                }
            }
        }
    }
}