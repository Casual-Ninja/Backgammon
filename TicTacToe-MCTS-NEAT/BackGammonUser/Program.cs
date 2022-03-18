using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using GAME;
using MCTS;

namespace BackGammonUser
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }

    public enum MessageType
    {
        ChanceState = 100,
        ChanceAction = 101,
        ChoiceState = 200,
        ChoiceAction = 201,
        InformationContainer = 300,
        RequestData = 400,
        StartGame = 500,
        StopGame = 501,
        GameFinished,
        DisconnectFromServer = 600,
        LoggInToAcount = 700,
        CreateAcount = 702,
        AccountInformationError = 703,
        AccountInformationOk = 704,
        MoveError = 800,
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
        protected Random rnd;
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
        private byte[] informationBuffer;


        private string AddPreMessageInformation(string stringOfInfromation, MessageType messageType)
        {
            stringOfInfromation = (int)messageType + stringOfInfromation;
            int lengthInBytes = Encoding.UTF8.GetByteCount(stringOfInfromation);
            return $"{lengthInBytes}".PadLeft(3) + stringOfInfromation;
        }

        private byte[] DataToSend(string stringOfInformation, MessageType messageType)
        {
            return Encoding.UTF8.GetBytes(AddPreMessageInformation(stringOfInformation, messageType));
        }

        private string GetStringFromData(byte[] data)
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

            if (bytesOfLengthRead != 3) // i haven't received all the length yet
            {
                int readLegnth = Math.Min(available, 3 - bytesOfLengthRead);
                bytesOfLengthRead += socket.Receive(lengthBuffer, bytesOfLengthRead, readLegnth, SocketFlags.None);
                available -= readLegnth;
            }

            if (bytesOfLengthRead == 3) // i received all the length
            {
                string lengthString = GetStringFromData(lengthBuffer);
                int lengthParsed;
                if (int.TryParse(lengthString, out lengthParsed)) // managed to parse the length
                {
                    if (informationBuffer.Length != lengthParsed)
                        informationBuffer = new byte[lengthParsed];

                    int readLength = Math.Min(available, lengthParsed - bytesOfInformationRead);
                    bytesOfInformationRead += socket.Receive(informationBuffer, bytesOfInformationRead, readLength, SocketFlags.None);


                    if (bytesOfInformationRead == lengthParsed) // i read all the data
                    {
                        information = GetStringFromData(informationBuffer);
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

        protected void SendData(string informationToSend, MessageType messageType)
        {
            socket.Send(DataToSend(informationToSend, messageType));
        }

        public virtual void CheckForMessages()
        {
            Console.WriteLine("Check");
            if (socket.Connected)
            {
                string information;
                try
                {
                    if (ReceiveInformation(out information))
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
                                    ParseMessage(information.Substring(3, information.Length - 3), type);
                                }
                                catch
                                {
                                    Console.WriteLine("Unkown Message Type.");
                                }

                            }
                        }
                        else
                        {
                            Console.WriteLine("Information Wasn't in correct format: Length must be equal or higher than 3.");
                        }
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Error in checking for messages:\n" + exception);
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

        public void LoginToServer()
        {
            socket.Connect(IPAddress.Loopback, 12357);
        }

        public void LoginToAccount()
        {
            SendData(username + "," + password, MessageType.LoggInToAcount);
        }

        public void CreateAccount()
        {
            SendData(username + ',' + password, MessageType.CreateAcount);
        }

        public void SendMoveToServer(GAME.Action action)
        {
            SendData(action.ProtocolInformation(), action.messageType);
        }

        public void GetLatestUserInformation()
        {
            SendData(MessageType.InformationContainer.ToString(), MessageType.RequestData);
        }

        public void StartNewGame()
        {
            SendData("", MessageType.StartGame);
        }

        public void QuitCurrentGame()
        {
            SendData("", MessageType.StopGame);
            inGame = false;
        }

        public void DisconnectFromServer()
        {
            SendData("", MessageType.DisconnectFromServer);
            socket.Close();
            inGame = false;
        }

        public void SetStartGameState(BackGammonChanceState state)
        {
            this.parentState = new BackGammonChoiceState(); // this is the starting position...
            this.state = state;

            if (state.Dice1 > state.Dice2)
                this.isPlayerTurn = true;
            else
                this.isPlayerTurn = false;
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
        private Dictionary<string, ServerUser> knownClients;
        private const float TimeToSearch = 1;
        private const int SimulationCount = 10000;

        public ServerUser(Socket socket, Dictionary<string, ServerUser> knownClients)
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
                SendData("Incorrect Username or Password", MessageType.AccountInformationError);
                return;
            }
            string checkAccountName = accountInformationSplit[0];
            string checkAccountPassWord = accountInformationSplit[1];
            if (checkAccountName.Length < 5 || checkAccountPassWord.Length < 5) // password and name must be longer than 0
            {
                SendData("Incorrect Username or Password", MessageType.AccountInformationError);
                return;
            }

            ServerUser savedDataOfUser;

            bool accountExists;

            lock (knownClients)
            {
                accountExists = knownClients.TryGetValue(checkAccountName, out savedDataOfUser);
            }
            if (accountExists)
            {
                if (savedDataOfUser.password == checkAccountPassWord)
                {
                    this.username = checkAccountName;
                    this.password = checkAccountPassWord;

                    this.information = savedDataOfUser.information;
                    this.state = (BackGammonChanceState)savedDataOfUser.state.Copy();
                    this.parentState = (BackGammonChoiceState)savedDataOfUser.parentState.Copy();
                    this.inGame = savedDataOfUser.inGame;

                    lock (knownClients)
                    {
                        knownClients[checkAccountName] = this;
                    }

                    SendData("Logged in.", MessageType.AccountInformationOk);
                }
                else // correct username but incorrect password
                {
                    SendData("Incorrect Username or Password", MessageType.AccountInformationError);
                }
            }
            else // not a saved account
            {
                SendData("Incorrect Username or Password", MessageType.AccountInformationError);
            }
        }

        private void CreateAccount(string accountInfo)
        {
            string[] accountInformationSplit = accountInfo.Split(',');
            if (accountInformationSplit.Length != 2) // if its not 2 then its in incorect format
            {
                SendData("Must be in format: Username,Password", MessageType.AccountInformationError);
                return;
            }
            string checkAccountName = accountInformationSplit[0];
            string checkAccountPassWord = accountInformationSplit[1];
            if (checkAccountName.Length < 5 || checkAccountPassWord.Length < 5) // password and name must be longer than 4
            {
                SendData("Password and Username Length must be greater than 4", MessageType.AccountInformationError);
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

                    this.knownClients.Add(this.username, this); // create this account

                    messageToSend = ("", MessageType.AccountInformationOk);
                }
            }

            SendData(messageToSend.Item1, messageToSend.Item2);
        }

        private State FindBestMoveInTime(int threadCount)
        {
            MCTSNode parentOfStart = new MCTSNode(parentState, MCTSNode.CHyperParam);

            MCTSNode startNode = new MCTSNode(state, parentOfStart);

            List<GAME.Action> legalActions = state.GetLegalActions(parentState);
            if (legalActions.Count > 1) // i actually have to think about the move
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                MCTSNode bestMove = startNode.BestActionInTimeMultiThreading(TimeToSearch, threadCount);

                sw.Stop();
                Console.WriteLine(sw.Elapsed);

                return bestMove.GetState();
            }
            else // only 1 options to do, so no need to think
            {
                return state.Move(parentState, legalActions[0]);
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
            this.inGame = false;
            SendData("", MessageType.GameFinished);
        }

        private void ComputerWon()
        {
            this.inGame = false;
            SendData("", MessageType.GameFinished);
        }

        private void MakeComputerMove()
        {
            // calculate the computer move
            State bestNextBoard = FindBestMoveInTime(1); // use only 1 thread for this

            // send the move to the player
            this.parentState = (BackGammonChoiceState)bestNextBoard;

            List<GAME.Action> diceOptions = parentState.GetLegalActions(null);
            this.state = (BackGammonChanceState)parentState.Move(null, diceOptions[rnd.Next(diceOptions.Count)]);

            SendData(this.parentState.ProtocolInformation(), MessageType.ChoiceState);
            SendData(this.state.ProtocolInformation(), MessageType.ChanceState);

            this.isPlayerTurn = true;

            if (this.parentState.IsGameOver()) // computer won!
            {
                ComputerWon();
            }
        }

        private void ClientMadeMove(string moveString)
        {
            if (inGame == false) // make sure the game started
            {
                SendData("Not in game!", MessageType.MoveError);
                return;
            }

            BackGammonChanceAction chanceAction = null;

            try
            {
                chanceAction = BackGammonChanceAction.PorotocolInformation(moveString);
            }
            catch
            {
                SendData("Move format not correct!", MessageType.MoveError);
            }

            bool legalMove = state.IsLegalMove(parentState, chanceAction);

            if (!legalMove)
            {
                SendData("Move is not legal!", MessageType.MoveError);
            }
            else
            {
                if (this.parentState.IsGameOver()) // player won!
                    PlayerWon();
                else
                    MakeComputerMove();
            }
        }

        private void StartGame()
        {
            this.inGame = true;
            this.parentState = new BackGammonChoiceState();

            this.state = new BackGammonChanceState(parentState.GetStartingAction(rnd));

            SendData(this.state.ProtocolInformation(), MessageType.StartGame); // send to the player the starting dice

            if (this.state.Dice1 > this.state.Dice2) // if 1 is bigger than 2, player starts
            {
                this.isPlayerTurn = true;
            }
            else
            {
                this.isPlayerTurn = false;

                MakeComputerMove();
            }
        }

        private void StopGame()
        {
            this.inGame = false;
        }

        protected override void ParseMessage(string message, MessageType messageType)
        {
            switch (messageType)
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
                    SendInformationToClient();
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

        public void SendInformationToClient()
        {
            SendData(information.GetAllInformation(), MessageType.InformationContainer);
        }

        public void SendNewStatesToClient(BackGammonChoiceState state, Dice dice)
        {
            SendData(state.ProtocolInformation(), state.messageType);
            SendData(dice.ProtocolInformation(), dice.messageType);
        }
    }
}
