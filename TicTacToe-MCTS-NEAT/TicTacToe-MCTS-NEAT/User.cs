using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using GAME;
using MCTS;
using System.Threading;
using TicTacToe_MCTS_NEAT;
using System.Text.Json.Serialization;

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
                allInformation = allInformation.Substring(0, allInformation.Length - 1); // removes the ',' at the end
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
        protected Random rnd;
        public string username { get; protected set; }
        public string password { get; protected set; }
        protected Socket socket;

        public bool IsConnected { get { return socket == null ? false : socket.Connected; } }

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
            //005hello
            int amountRead = socket.Receive(lengthBuffer);
            if (amountRead == 0) // client disconnected
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
            if (rnd == null)
                rnd = new Random();

            this.username = userName;
            this.password = passWord;

            this.socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        }

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
                    Console.WriteLine("Disconnecting from server because of socket error");
                    DisconnectedFromServer();
                    break;
                }
            }
        }

        private void DisconnectedFromServer()
        {
            serverInformation.Enqueue(("", MessageType.DisconnectFromServer));
        }

        public void LoginToServer()
        {
            if (!socket.Connected)
            {
                socket.Connect(IPAddress.Loopback, 12357); // the port used is 12357

                Thread checkForMessagesThread = new Thread(ManageSocketData);
                checkForMessagesThread.Start(Thread.CurrentThread);
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
            AddDataToSend("", MessageType.DisconnectFromServer);
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

    public class ServerUser : User
    {
        public const string DataPath = "Data\\";
        public const string UserInformationPath = "Information";
        public const string UserPasswordPath = "Password";

        private const float TimeToSearch = 1000; // seconds to think per move
        private const int SimulationCount = 10000; // roll outs per move
        private const int threadsToUse = 8; // 8 threads per move


        private Dictionary<string, SavingServerUser> knownClients;

        public ServerUser(Socket socket, Dictionary<string, SavingServerUser> knownClients)
        {
            if (rnd == null)
                rnd = new Random();

            this.socket = socket;
            lock (knownClients)
            {
                this.knownClients = knownClients;
            }
        }

        private void LoggInToAccount(string accountInfo)
        {
            string[] accountInformationSplit = accountInfo.Split(',');
            if (accountInformationSplit.Length != 2) // if its not 2 then its in incorect format
            {
                AddDataToSend("Incorrect Username or Password", MessageType.AccountInformationError);
                return;
            }
            string checkAccountName = accountInformationSplit[0];
            string checkAccountPassWord = accountInformationSplit[1];
            if (checkAccountName.Length < 5 || checkAccountPassWord.Length < 5) // password and name must be longer than 0
            {
                AddDataToSend("Incorrect Username or Password", MessageType.AccountInformationError);
                return;
            }

            SavingServerUser savedDataOfUser;

            bool accountExists;

            lock (knownClients)
            {
                accountExists = knownClients.TryGetValue(checkAccountName, out savedDataOfUser);
            }
            if (accountExists)
            {
                Console.WriteLine("Account exists...");
                if (savedDataOfUser.password == checkAccountPassWord)
                {
                    this.username = checkAccountName;
                    this.password = checkAccountPassWord;

                    this.information = new InformationContainer(savedDataOfUser.informationDict);

                    AddDataToSend("Logged in.", MessageType.AccountInformationOk);
                }
                else // correct username but incorrect password
                {
                    Console.WriteLine($"Incorrect pasword {savedDataOfUser.password} != {checkAccountPassWord}");
                    AddDataToSend("Incorrect Username or Password", MessageType.AccountInformationError);
                }
            }
            else // not a saved account
            {
                AddDataToSend("Incorrect Username or Password", MessageType.AccountInformationError);
            }
        }

        private void CreateAccount(string accountInfo)
        {
            Console.WriteLine("In create acount method");
            string[] accountInformationSplit = accountInfo.Split(',');
            if (accountInformationSplit.Length != 2) // if its not 2 then its in incorect format
            {
                AddDataToSend("Must be in format: Username,Password", MessageType.AccountInformationError);
                return;
            }
            string checkAccountName = accountInformationSplit[0];
            string checkAccountPassWord = accountInformationSplit[1];
            if (checkAccountName.Length < 5 || checkAccountPassWord.Length < 5 || checkAccountName.Length > 15 || checkAccountPassWord.Length > 15) // password and name must be longer than 4
            {
                AddDataToSend("Password and Username Length must be between 5 and 15 characters.", MessageType.AccountInformationError);
                return;
            }

            (string, MessageType) messageToSend;

            lock (knownClients)
            {
                if (knownClients.ContainsKey(checkAccountName)) // does this username already exist?
                {
                    messageToSend = ("Username already exists!", MessageType.AccountInformationError);
                }
                else
                {
                    this.username = checkAccountName;
                    this.password = checkAccountPassWord;

                    this.information = new InformationContainer();
                    this.inGame = false;

                    SavingServerUser newUserSave = new SavingServerUser(this);

                    this.knownClients.Add(this.username, newUserSave); // create this account

                    SaveAllUserData();

                    messageToSend = ("", MessageType.AccountInformationOk);
                }
            }

            AddDataToSend(messageToSend.Item1, messageToSend.Item2);
        }

        private GAME.Action FindBestMoveInTime(int threadCount)
        {
            MCTSNode parentOfStart = new MCTSNode(parentState, MCTSNode.CHyperParam);

            MCTSNode startNode = new MCTSNode(state, parentOfStart);

            List<GAME.Action> legalActions = state.GetLegalActions(parentState);
            if (legalActions.Count > 1) // i actually have to think about the move
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                
                MCTSNode bestMove = startNode.BestActionInTimeMultiThreading(TimeToSearch, threadCount);

                int bestMoveIndex = startNode.GetIndexOfChild(bestMove);

                sw.Stop();
                Console.WriteLine(sw.Elapsed);

                return legalActions[bestMoveIndex];
            }
            else // only 1 options to do, so no need to think
            {
                return legalActions[0];
            }
        }

        private State FindBestMoveInSimulations(int threadCount, params int[] seeds)
        {
            MCTSNode parentOfStart = new MCTSNode(parentState, MCTSNode.CHyperParam);

            MCTSNode startNode = new MCTSNode(state, parentOfStart);

            List<GAME.Action> legalActions = state.GetLegalActions(parentState);
            if (legalActions.Count > 1) // i actually have to think about the move
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                MCTSNode bestMove = startNode.BestActionMultiThreading(SimulationCount, threadCount, seeds);

                sw.Stop();
                Console.WriteLine(sw.Elapsed);

                return bestMove.GetState();
            }
            else // only 1 options to do, so no need to think
            {
                return state.Move(parentState, legalActions[0]);
            }
        }

        private void PlayerWon()
        {
            StopGame();
        }

        private void ComputerWon()
        {
            StopGame();
        }

        private void MakeComputerMove()
        {
            // send to the player the new die the server got
            AddDataToSend(this.state.ProtocolInformation(), MessageType.ChanceState);

            PushData(); // push the data before starting to calculate the move

            // calculate the computer move
            GAME.Action bestMove = FindBestMoveInTime(threadsToUse); // use only 1 thread for this

            AddDataToSend(bestMove.ProtocolInformation(), MessageType.ChanceAction); // send to the player the actual move the server made

            // send the move to the player
            this.parentState = (BackGammonChoiceState)this.state.Move(this.parentState, bestMove);

            List<GAME.Action> diceOptions = parentState.GetLegalActions(null);
            this.state = (BackGammonChanceState)parentState.Move(null, diceOptions[rnd.Next(diceOptions.Count)]);

            //AddDataToSend(this.parentState.ProtocolInformation(), MessageType.ChoiceState);
            AddDataToSend(this.state.ProtocolInformation(), MessageType.ChanceState);
            
            if (this.parentState.IsGameOver()) // computer won!
            {
                ComputerWon();
            }
            else
            {
                AddDataToSend("", MessageType.SwitchTurn);
                this.isPlayerTurn = true;
            }
        }

        private void ClientMadeMove(string moveString)
        {
            if (inGame == false) // make sure the game started
            {
                AddDataToSend("Not in game!", MessageType.MoveError);
                return;
            }
            if (this.isPlayerTurn == false)
                return;

            BackGammonChanceAction chanceAction = null;

            try
            {
                chanceAction = BackGammonChanceAction.PorotocolInformation(moveString);
            }
            catch
            {
                AddDataToSend("Move format not correct!", MessageType.MoveError);
                return;
            }

            bool legalMove = state.IsLegalMove(parentState, chanceAction);

            if (!legalMove)
            {
                AddDataToSend(parentState.ProtocolInformation(), MessageType.ChoiceState);
                AddDataToSend("Move is not legal!", MessageType.MoveError);
            }
            else
            {
                this.parentState = (BackGammonChoiceState)this.state.Move(this.parentState, chanceAction);
                this.state = new BackGammonChanceState(this.parentState.RandomPick(this.parentState.GetLegalActions(null), rnd));

                if (this.parentState.IsGameOver()) // player won!
                    PlayerWon();
                else
                {
                    AddDataToSend("", MessageType.MoveIsValid); // send to the player the move he made was valid
                    AddDataToSend("", MessageType.SwitchTurn); // now its the servers turn to play, so switch turn value
                    this.isPlayerTurn = false;
                    MakeComputerMove();
                }
            }
        }

        private void StartGame()
        {
            this.inGame = true;
            this.parentState = new BackGammonChoiceState();

            this.state = new BackGammonChanceState(parentState.GetStartingAction(rnd));

            this.isPlayerTurn = rnd.Next(2) == 1 ? true : false;

            if (this.isPlayerTurn) // if 1 is bigger than 2, player starts
            {
                AddDataToSend(this.state.ProtocolInformation() + '1', MessageType.StartGame); // send to the player the starting dice
            }
            else
            {
                AddDataToSend(this.state.ProtocolInformation() + '0', MessageType.StartGame); // send to the player the starting dice

                MakeComputerMove();
            }
        }

        private void StopGame()
        {
            this.inGame = false;
            AddDataToSend("", MessageType.GameFinished);

            information.AddToValue(InformationContainer.Information.gamesPlayed, 1);
            if (this.isPlayerTurn) // did the player win the game?
            {
                information.AddToValue(InformationContainer.Information.gamesWon, 1);

                int scoreOfWin = this.parentState.ActualGameScoreResult();
                information.AddToValue(InformationContainer.Information.overAllScore, scoreOfWin);
            }

            SaveAllUserData();
        }

        private string GetSpecificUserPath()
        {
            return DataPath + username;
        }

        private void SaveAllUserData()
        {
            SavingServerUser dataToSave = new SavingServerUser(this);
            SaveLoad.SaveData(GetSpecificUserPath(), dataToSave);
        }

        protected override void ParseMessage(string message, MessageType messageType)
        {
            Console.WriteLine(message + " " + messageType);
            switch(messageType)
            {
                case MessageType.LoggInToAcount:
                    LoggInToAccount(message);
                    break;
                case MessageType.CreateAcount:
                    CreateAccount(message);
                    break;
                case MessageType.ChanceAction:
                    ClientMadeMove(message);
                    break;
                case MessageType.StartGame:
                    StartGame();
                    break;
                case MessageType.StopGame:
                    StopGame();
                    break;
                case MessageType.RequestData:
                    SendInformationToClient(message);
                    break;
                case MessageType.DisconnectFromServer:
                    StopGame();
                    this.socket.Close();
                    break;
                default:
                    Console.WriteLine("Client sent unknown Message type");
                    break;
            }
        }

        public void SendInformationToClient(string message)
        {
            int value;
            if (int.TryParse(message, out value))
            {
                try
                {
                    MessageType dataTypeToSend = (MessageType)value;

                    switch (dataTypeToSend)
                    {
                        case MessageType.InformationContainer:
                            AddDataToSend(information.GetAllInformation(), MessageType.InformationContainer);
                            break;
                        case MessageType.ChoiceState:
                            if (this.inGame)
                            {
                                AddDataToSend(parentState.ProtocolInformation(), MessageType.ChoiceState);
                                AddDataToSend(state.ProtocolInformation(), MessageType.ChanceState);
                            }
                            break;
                        default:
                            Console.WriteLine("Client sent message with unknown data type to send: " + value);
                            break;
                    }
                }
                catch
                {
                    Console.WriteLine("Client sent message with no known Message Type: " + value);
                }
            }
            else
            {
                Console.WriteLine("Client sent message with wrong format: " + message);
            }
        }
    }
    
    [Serializable]
    public class SavingServerUser
    {
        public string username { get; set; }
        public string password { get; set; }

        public Dictionary<InformationContainer.Information, double> informationDict { get; set; }

        public SavingServerUser(ServerUser user)
        {
            this.username = user.username;
            this.password = user.password;
            this.informationDict = user.information.Copy().informationDict;
        }

        [JsonConstructor]
        public SavingServerUser(string username, string password, Dictionary<InformationContainer.Information, double> informationDict)
        {
            this.username = username;
            this.password = password;
            if (informationDict == null)
                this.informationDict = new Dictionary<InformationContainer.Information, double>();
            else
                this.informationDict = informationDict;
        }

        public override string ToString()
        {
            return $"name: {username} password: {password} information:\n{informationDict}";
        }
    }
}