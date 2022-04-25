using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using GAME;
using System.Threading;
using UnityEngine;
using System.Security.Cryptography;

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
        MoveError = 800,
        RSAEncryptionParamaters = 900
    }

    public class InformationContainer
    {
        public Dictionary<Information, double> informationDict { get; }

        public InformationContainer()
        {
            this.informationDict = new Dictionary<Information, double>();
        }

        public InformationContainer(Dictionary<Information, double> dict)
        {
            Dictionary<Information, double> copy = new Dictionary<Information, double>();

            foreach (KeyValuePair<Information, double> pair in dict)
                copy.Add(pair.Key, pair.Value);

            this.informationDict = copy;
        }

        public double GetInformation(Information informationType)
        {
            return informationDict[informationType];
        }

        public void SetInformation(Information informationType, double information)
        {
            if (informationDict.ContainsKey(informationType))
                informationDict[informationType] = information;
            else
                informationDict.Add(informationType, information);
        }

        public void AddToValue(Information informationType, double changeValue)
        {
            if (informationDict.ContainsKey(informationType))
                informationDict[informationType] += changeValue;
            else
                informationDict.Add(informationType, changeValue);
        }

        public string GetAllInformation()
        {
            string allInformation = "";

            foreach (KeyValuePair<Information, double> pair in informationDict)
            {
                allInformation += Enum.GetName(typeof(Information), (int)pair.Key) + ":" + pair.Value + ",";
            }
            if (allInformation != "")
                allInformation.Substring(0, allInformation.Length - 1); // removes the ',' at the end
            return allInformation;
        }

        public InformationContainer Copy()
        {
            Dictionary<Information, double> copy = new Dictionary<Information, double>();

            foreach (KeyValuePair<Information, double> pair in informationDict)
                copy.Add(pair.Key, pair.Value);

            return new InformationContainer(copy);
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
        public string username { get; protected set; }
        public string password { get; protected set; }
        protected Socket socket;

        public bool IsConnected { get { return socket == null ? false : socket.Connected; } }

        public void CloseSocket()
        {
            if (socket != null)
                socket.Close();
        }

        public bool inGame { get; set; }
        public bool isPlayerTurn { get; set; }
        public BackGammonChanceState state { get; set; }
        public BackGammonChoiceState parentState { get; set; }
        public InformationContainer information { get; set; }
        
        private byte[] lengthBuffer = new byte[3];
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
        private void ReceiveInformation(out string information)
        {
            int amountRead = socket.Receive(lengthBuffer);
            if (amountRead == 0)
                throw new Exception("socket disconnected");

            string lengthString = GetStringFromEncodedData(lengthBuffer);
            int lengthParsed;
            if (int.TryParse(lengthString, out lengthParsed)) // managed to parse the length
            {
                if (informationBuffer == null || informationBuffer.Length != lengthParsed)
                    informationBuffer = new byte[lengthParsed];

                amountRead = socket.Receive(informationBuffer);
                if (amountRead == 0)
                    throw new Exception("socket disconnected");

                information = GetStringFromEncodedData(informationBuffer);
            }
            else // something went wrong! thats not a number, this can only happen if the client isn't using the correct protocol...
            {
                // i know this is client error because im using tcp so no way its sending problem
                // don't know what to do... maybe just drop the connection with him?
                throw new Exception("Didn't manage to parse the length!");
            }
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
                Console.WriteLine("Error in sending data: " + exception);
            }
        }

        public virtual void CheckForMessages()
        {
            string information;
            ReceiveInformation(out information);
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

        private void ManageCheckingForMessages(object mainThread)
        {
            Thread actualValue = (Thread)mainThread;
            while (actualValue.IsAlive)
            {
                Thread.Sleep(100);
                try
                {
                    CheckForMessages();
                }
                catch (Exception exception)
                {
                    Debug.Log(exception);
                    DisconnectedFromServer();
                    break;
                }
            }
        }

        private void ManageSocketData(object mainThread)
        {
            Thread actualValue = (Thread)mainThread;

            Thread checkForMessagesThread = new Thread(ManageCheckingForMessages);
            checkForMessagesThread.Start(Thread.CurrentThread);

            while (actualValue.IsAlive)
            {
                Thread.Sleep(100);
                try
                {
                    PushData();
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
            Debug.Log("adding disconnected from server");
            serverInformation.Enqueue(("", MessageType.DisconnectFromServer));
        }

        public void LoginToServer(IPAddress serverAdderss, Thread mainThread)
        {
            if (!socket.Connected)
            {
                socket.Connect(serverAdderss, 12357); // the port used is 12357

                Thread checkForMessagesThread = new Thread(ManageSocketData);
                checkForMessagesThread.Start(mainThread);
            }
        }

        public void LoginToAccount()
        {
            string encryptedPassword = EncryptionHandler.RSAEncrypt(password);
            AddDataToSend(username + "," + encryptedPassword, MessageType.LoggInToAcount);
        }

        public void CreateAccount()
        {
            AddDataToSend(username + ',' + EncryptionHandler.RSAEncrypt(password), MessageType.CreateAcount);
        }

        public void SendMoveToServer(GAME.Action action)
        {
            AddDataToSend(action.ProtocolInformation(), action.messageType);
        }

        public void GetLatestUserInformation()
        {
            AddDataToSend(((int)MessageType.InformationContainer).ToString(), MessageType.RequestData);
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
            PushData();
            DisconnectedFromServer();
            CloseSocket();
            inGame = false;
        }

        public void SetStartGameState(BackGammonChanceState state)
        {
            this.parentState = new BackGammonChoiceState(); // this is the starting position...
            this.state = state;
            this.inGame = true;
        }

        public void SetStartGameState(BackGammonChanceState state, BackGammonChoiceState choiceState)
        {
            this.parentState = choiceState; // this is the starting position...
            this.state = state;
            this.inGame = true;
        }

        protected override void ParseMessage(string message, MessageType messageType)
        {
            lock (serverInformation)
            {
                if (messageType == MessageType.RSAEncryptionParamaters)
                {
                    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                    rsa.FromXmlString(message);
                    EncryptionHandler.Initialize(rsa);
                }
                else
                    serverInformation.Enqueue((message, messageType));

            }
        }
    }
}