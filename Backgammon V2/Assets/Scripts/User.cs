using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using GAME;
using System.Threading;
using UnityEngine;

namespace BackGammonUser
{
    public enum MessageType
    {
        ChanceState = 100,
        ChanceAction = 101,
        MoveIsValid = 102,
        ChoiceState = 200,
        ChoiceAction = 201,
        InformationContainer = 300,
        RequestData = 400,
        StartGame = 500,
        StopGame = 501,
        GameFinished = 502,
        SwitchTurn = 503,
        DisconnectFromServer = 600,
        LoggInToAcount = 700,
        CreateAcount = 702,
        AccountInformationError = 703,
        AccountInformationOk = 704,
        MoveError = 800
    }

    public class InformationContainer
    {
        private string[] information;

        public InformationContainer()
        {
            this.information = new string[Enum.GetNames(typeof(Information)).Length];
        }

        public string GetInformation(Information informationType)
        {
            return this.information[(int)informationType];
        }

        public void SetInformation(Information informationType, string information)
        {
            this.information[(int)informationType] = information;
        }


        public string GetAllInformation()
        {
            string allInformation = "";

            for (int i = 0; i < information.Length - 1; i++)
                allInformation += information[i] + ",";

            allInformation += information[information.Length - 1];

            return allInformation;
        }

        public enum Information
        {
            gamesPlayed,
            gamesWon,
            overAllScore
        }

        public override string ToString()
        {
            return GetAllInformation();
        }
    }

    public abstract class User
    {
        protected string username;
        protected string password;
        protected Socket socket;

        public bool IsConnected { get { return socket == null ? false : socket.Connected; } }

        public bool inGame { get; set; }
        public bool isPlayerTurn { get; set; }
        public BackGammonChanceState state { get; set; }
        public BackGammonChoiceState parentState { get; set; }
        public InformationContainer information { get; set; }

        private int bytesOfLengthRead = 0;
        private byte[] lengthBuffer = new byte[3];

        private int bytesOfInformationRead = 0;
        private byte[] informationBuffer = null;

        private string dataToSendNextPush = "";


        private string AddPreMessageInformation(string stringOfInfromation, MessageType messageType)
        {
            stringOfInfromation = (int)messageType + stringOfInfromation;
            int lengthInBytes = Encoding.UTF8.GetByteCount(stringOfInfromation);
            return $"{lengthInBytes}".PadLeft(3) + stringOfInfromation;
        }

        private byte[] EncodeString(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        private string GetStringFromEncodedData(byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }

        /// <summary>
        /// Receives the information with the used protocol (message length prefix). A return value indicates whethe the reading was succesfull.
        /// </summary>
        /// <param name="information">The information that was received.</param>
        /// <returns>True if received all data succesfully.</returns>
        private bool ReceiveInformation(out string information)
        {
            int available = socket.Available;
            if (available == 0)
            {
                information = "";
                return false;
            }

            if (bytesOfLengthRead != 3) // i haven't received all the length yet
            {
                int readLegnth = Math.Min(available, 3 - bytesOfLengthRead);
                bytesOfLengthRead += socket.Receive(lengthBuffer, bytesOfLengthRead, readLegnth, SocketFlags.None);
                available -= readLegnth;
            }

            if (bytesOfLengthRead == 3) // i received all the length
            {
                string lengthString = GetStringFromEncodedData(lengthBuffer);
                int lengthParsed;
                if (int.TryParse(lengthString, out lengthParsed)) // managed to parse the length
                {
                    if (informationBuffer == null || informationBuffer.Length != lengthParsed)
                        informationBuffer = new byte[lengthParsed];

                    int readLength = Math.Min(available, lengthParsed - bytesOfInformationRead);
                    bytesOfInformationRead += socket.Receive(informationBuffer, bytesOfInformationRead, readLength, SocketFlags.None);


                    if (bytesOfInformationRead == lengthParsed) // i read all the data
                    {
                        information = GetStringFromEncodedData(informationBuffer);
                        bytesOfLengthRead = 0;
                        bytesOfInformationRead = 0;
                        return true;
                    }
                }
                else // something went wrong! thats not a number, this can only happen if the client isn't using the correct protocol...
                {
                    // i know this is client error because im using tcp so no way its sending problem
                    // don't know what to do... maybe just drop the connection with him?
                    throw new Exception("Didn't manage to parse the length!");
                }
            }
            information = "";
            return false;
        }

        protected abstract void ParseMessage(string message, MessageType messageType);

        protected void AddDataToSend(string informationToSend, MessageType messageType)
        {
            dataToSendNextPush += AddPreMessageInformation(informationToSend, messageType);
        }

        public void PushData()
        {
            try
            {
                if (dataToSendNextPush != "")
                {
                    byte[] encodedData = EncodeString(dataToSendNextPush);
                    socket.Send(encodedData);
                    dataToSendNextPush = "";
                }
            }
            catch (Exception exception)
            {
                //print("Error in sending data: " + exception);
            }
        }

        public virtual void CheckForMessages()
        {
            string information;
            while (ReceiveInformation(out information))
            {
                if (information.Length >= 3)
                {
                    string messageType = information.Substring(0, 3);
                    int typeValue;
                    if (int.TryParse(messageType, out typeValue))
                    {
                        try
                        {
                            MessageType type = (MessageType)typeValue;
                            if (information.Length == 3)
                                ParseMessage("", type);
                            else
                                ParseMessage(information.Substring(3, information.Length - 3), type);
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine(exception);
                            Console.WriteLine(information);
                            Console.WriteLine("Unkown Message Type: " + typeValue);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Information Wasn't in correct format: Length must be equal or higher than 3.");
                }
            }
        }
    }

    public class ClientUser : User
    {
        private Queue<(string, MessageType)> serverInformation = new Queue<(string, MessageType)>();

        public bool TryGetMessage(out (string, MessageType) message)
        {
            message = ("", 0);

            bool isWaiting;

            lock (serverInformation)
            {
                if (serverInformation.Count > 0)
                {
                    isWaiting = true;
                    message = serverInformation.Dequeue();
                }
                else
                {
                    isWaiting = false;
                    message = ("", 0);
                }
            }
            return isWaiting;
        }

        public ClientUser(string userName, string passWord)
        {
            this.username = userName;
            this.password = passWord;

            this.socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        }

        public string GetUsername() { return username; }

        public void SetAccountInfo(string username, string password)
        {
            this.username = username;
            this.password = password;
        }

        private void ManageSocketData(object mainThread)
        {
            Thread actualValue = (Thread)mainThread;
            while (actualValue.IsAlive)
            {
                Thread.Sleep(100);
                try
                {
                    PushData();
                    CheckForMessages();
                }
                catch
                {
                    DisconnectedFromServer();
                    break;
                }
            }
        }

        private void DisconnectedFromServer()
        {
            serverInformation.Enqueue(("", MessageType.DisconnectFromServer));
        }

        public void LoginToServer(Thread mainThread)
        {
            if (!socket.Connected)
            {
                socket.Connect(IPAddress.Loopback, 12357); // the port used is 12357

                Thread checkForMessagesThread = new Thread(ManageSocketData);
                checkForMessagesThread.Start(mainThread);
            }
        }

        public void LoginToAccount()
        {
            AddDataToSend(username + "," + password, MessageType.LoggInToAcount);
        }

        public void CreateAccount()
        {
            AddDataToSend(username + ',' + password, MessageType.CreateAcount);
        }

        public void SendMoveToServer(GAME.Action action)
        {
            AddDataToSend(action.ProtocolInformation(), action.messageType);
        }

        public void GetLatestUserInformation()
        {
            AddDataToSend(MessageType.InformationContainer.ToString(), MessageType.RequestData);
        }

        public void StartNewGame()
        {
            AddDataToSend("", MessageType.StartGame);
        }

        public void QuitCurrentGame()
        {
            AddDataToSend("", MessageType.StopGame);
            inGame = false;
        }

        public void DisconnectFromServer()
        {
            DisconnectedFromServer();
            PushData();
            socket.Close();
            inGame = false;
        }

        public void SetStartGameState(BackGammonChanceState state)
        {
            this.parentState = new BackGammonChoiceState(); // this is the starting position...
            this.state = state;
            this.inGame = true;
        }

        protected override void ParseMessage(string message, MessageType messageType)
        {
            lock (serverInformation)
            {
                serverInformation.Enqueue((message, messageType));
            }
        }
    }
}