using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;
using GAME;
using MCTS;
using TicTacToe_MCTS_NEAT;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

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
        public string passwordSalt { get; protected set; }
        protected Socket socket;

        public bool IsConnected { get { return socket == null ? false : socket.Connected; } }

        protected void CloseSocket()
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
        /// Receives the information with the used protocol (message length prefix + type of message)
        /// </summary>
        /// <param name="information">The information that was received.</param>
        private void ReceiveInformation(out string information)
        {
            int amountRead = socket.Receive(lengthBuffer); // the length of the message
            if (amountRead == 0) // client disconnected
                throw new Exception("socket disconnected");

            string lengthString = GetStringFromEncodedData(lengthBuffer); // get the length in string format
            int lengthParsed;
            if (int.TryParse(lengthString, out lengthParsed)) // try to parse the length
            {
                if (informationBuffer == null || informationBuffer.Length != lengthParsed)
                    informationBuffer = new byte[lengthParsed];

                amountRead = socket.Receive(informationBuffer); // read the actual message
                if (amountRead == 0) // client disconnected (shouldn't happen here...)
                    throw new Exception("socket disconnected");

                information = GetStringFromEncodedData(informationBuffer); // decode the message
            }
            else // something went wrong! thats not a number, this can only happen if the client isn't using the correct protocol...
            {
                // i know this is client error because im using tcp so no way its sending problem
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

        public void CheckForMessages()
        {
            string information;

            ReceiveInformation(out information); // try to read a message

            if (information.Length >= 3) // there is no problem with the message it self
            {
                string messageType = information.Substring(0, 3); // the type of the message
                int typeValue;
                if (int.TryParse(messageType, out typeValue))
                {
                    try
                    {
                        MessageType type = (MessageType)typeValue;
                        if (information.Length == 3) // only sent the type of message, with no info about it (can happen with a few of them)
                            ParseMessage("", type);
                        else
                            ParseMessage(information.Substring(3, information.Length - 3), type); // move it to parsing
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
                Console.WriteLine("Information Wasn't in correct format: Length must be equal to, or higher than 3.");
            }
        }
    }

    public class ServerUser : User
    {
        public const string DataPath = "Data\\";
        public const string UserInformationPath = "Information";
        public const string UserPasswordPath = "Password";

        private const float TimeToSearch = 2000; // seconds to think per move
        private const int threadsToUse = 8; // 8 threads per move

        
        private HashSet<string> onlineUsers;

        public ServerUser(Socket socket, HashSet<string> onlineUsers)
        {
            if (rnd == null)
                rnd = new Random();

            this.socket = socket;

            lock (onlineUsers)
            {
                this.onlineUsers = onlineUsers;
            }

            AddDataToSend(EncryptionHandler.publicKey, MessageType.RSAEncryptionParamaters);
            PushData();
        }

        private bool IsCorrectLength(string username, string password)
        {
            if (username.Length < 5 || username.Length > 15 || password.Length < 5 || password.Length > 15)
                return true;
            return false;
        }

        private void LoggInToAccount(string accountInfo)
        {
            int firstChar = accountInfo.IndexOf(',');
            if (firstChar == -1)
            {
                AddDataToSend("Incorrect Format of account information", MessageType.AccountInformationError);
                return;
            }

            string[] accountInformationSplit = accountInfo.Split(',');

            if (accountInformationSplit.Length != 2) // if its not 2 then its in incorect format
            {
                AddDataToSend("Username or password cannot contain ','", MessageType.AccountInformationError);
                return;
            }

            string checkAccountName = accountInformationSplit[0];

            lock (onlineUsers)
            {
                if (onlineUsers.Contains(checkAccountName))
                {
                    AddDataToSend("Account already logged in!", MessageType.AccountInformationError);
                    return;
                }
            }

            UnicodeEncoding encoder = new UnicodeEncoding();
            string checkAccountPassWord = EncryptionHandler.RSADecrypt(accountInformationSplit[1]);
            Console.WriteLine("information: " + checkAccountName + " || " + checkAccountPassWord);

            if (IsCorrectLength(checkAccountName, checkAccountPassWord)) // password and name must be longer than 5
            {
                AddDataToSend("Password and Username length must be greater than 4", MessageType.AccountInformationError);
                return;
            }

            if (checkAccountPassWord.Contains(","))
            {
                AddDataToSend("Password cannot contain ','", MessageType.AccountInformationError);
                return;
            }

            SavingServerUser savedDataOfUser;
            object savedValue;
            if (SaveLoad.LoadData<SavingServerUser>(GetSpecificUserPath(checkAccountName), out savedValue))
            {
                savedDataOfUser = (SavingServerUser)savedValue;
                Console.WriteLine("Account exists...");

                // generate a 128-bit salt using a cryptographically strong random sequence of nonzero values
                this.passwordSalt = savedDataOfUser.salt;
                byte[] salt = Convert.FromBase64String(this.passwordSalt);
                Console.WriteLine($"Salt: {passwordSalt}");

                // derive a 256-bit subkey (use HMACSHA256 with 310,000 iterations)
                this.password = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                    password: checkAccountPassWord,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 310000,
                    numBytesRequested: 256 / 8));
                Console.WriteLine($"Hashed: {this.password}");

                if (savedDataOfUser.password == this.password)
                {
                    this.username = checkAccountName;
                    this.password = this.password;

                    this.information = new InformationContainer(savedDataOfUser.informationDict);

                    AddDataToSend("Logged in.", MessageType.AccountInformationOk);

                    lock (onlineUsers)
                    {
                        onlineUsers.Add(checkAccountName);
                    }
                }
                else // correct username but incorrect password
                {
                    Console.WriteLine($"Incorrect pasword {savedDataOfUser.password} != {this.password}");
                    AddDataToSend("Incorrect Username or Password", MessageType.AccountInformationError);
                }
            }
            else
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
                AddDataToSend("Username or password cannot contain ','", MessageType.AccountInformationError);
                return;
            }
            string checkAccountName = accountInformationSplit[0];
            string checkAccountPassWord = EncryptionHandler.RSADecrypt(accountInformationSplit[1]);
            if (IsCorrectLength(checkAccountName, checkAccountPassWord)) // password and name must be longer than 4
            {
                AddDataToSend("Password and Username Length must be between 5 and 15 characters.", MessageType.AccountInformationError);
                return;
            }
            
            if (SaveLoad.PathExists(GetSpecificUserPath(checkAccountName)))
            {
                AddDataToSend("Username already exists!", MessageType.AccountInformationError);
            }
            else
            {
                this.username = checkAccountName;

                // generate a 128-bit salt using a cryptographically strong random sequence of nonzero values
                byte[] salt = new byte[128 / 8];
                using (var rngCsp = new RNGCryptoServiceProvider())
                {
                    rngCsp.GetNonZeroBytes(salt);
                }
                this.passwordSalt = Convert.ToBase64String(salt);
                Console.WriteLine($"Salt: {passwordSalt}");

                // derive a 256-bit subkey (use HMACSHA256 with 310,000 iterations)
                string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                    password: checkAccountPassWord,
                    salt: salt,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 310000,
                    numBytesRequested: 256 / 8));
                Console.WriteLine($"Hashed: {hashed}");


                this.password = hashed;

                this.information = new InformationContainer();
                this.inGame = false;

                SavingServerUser newUserSave = new SavingServerUser(this);

                SaveAllUserData();

                AddDataToSend("", MessageType.AccountInformationOk);

                lock (onlineUsers)
                {
                    onlineUsers.Add(checkAccountName);
                }
            }
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
            GAME.Action bestMove = FindBestMoveInTime(threadsToUse);

            // send to the player the actual move the server made
            AddDataToSend(bestMove.ProtocolInformation(), MessageType.ChanceAction); 

            this.parentState = (BackGammonChoiceState)this.state.Move(this.parentState, bestMove);

            List<GAME.Action> diceOptions = parentState.GetLegalActions(null);
            this.state = (BackGammonChanceState)parentState.Move(null, diceOptions[rnd.Next(diceOptions.Count)]);
            
            if (this.parentState.IsGameOver()) // computer won!
            {
                ComputerWon();
            }
            else
            {
                AddDataToSend(this.state.ProtocolInformation(), MessageType.ChanceState);
                AddDataToSend("", MessageType.SwitchTurn);
                this.isPlayerTurn = true;
            }
        }

        private void ClientMadeMove(string moveString)
        {
            if (inGame == false) // make sure the game started
            {
                AddDataToSend("Not in game!", MessageType.MoveError);
                PushData();
                return;
            }
            if (this.isPlayerTurn == false) // is it the players turn?
                return;
            
            BackGammonChanceAction chanceAction = null; 
            // try reading the action made by the player
            try { chanceAction = BackGammonChanceAction.PorotocolInformation(moveString); }
            catch
            {   // failed to read the action, reply with error
                AddDataToSend("Move format not correct!", MessageType.MoveError);
                PushData();
                return;
            }

            bool legalMove = state.IsLegalMove(parentState, chanceAction); // is the move legal

            if (!legalMove) // it is not legal!
            {   
                // send to the player the actuall states as he probably has them wrong
                AddDataToSend(parentState.ProtocolInformation(), MessageType.ChoiceState);
                AddDataToSend(state.ProtocolInformation(), MessageType.ChanceState);
                // also send an error message
                AddDataToSend("Move is not legal!", MessageType.MoveError);
                PushData();
            }
            else
            {
                // make the move
                this.parentState = (BackGammonChoiceState)this.state.Move(this.parentState, chanceAction);
                // generate the new die
                this.state = new BackGammonChanceState(this.parentState.RandomPick(this.parentState.GetLegalActions(null), rnd));

                if (this.parentState.IsGameOver()) // player won!
                    PlayerWon();
                else
                {
                    AddDataToSend("", MessageType.MoveIsValid); // send to the player the move he made was valid
                    AddDataToSend("", MessageType.SwitchTurn); // now its the servers turn to play, so switch turn value
                    this.isPlayerTurn = false;
                    MakeComputerMove(); // do the ai move
                }
            }
        }

        private void StartGame()
        {
            this.inGame = true;
            this.parentState = new BackGammonChoiceState();

            this.state = new BackGammonChanceState(parentState.GetStartingAction(rnd));

            this.isPlayerTurn = rnd.Next(2) == 1 ? true : false;

            if (this.isPlayerTurn)
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

        private void LeaveGame()
        {
            this.inGame = false;
        }

        private string GetSpecificUserPath(string username)
        {
            return DataPath + username;
        }

        private void SaveAllUserData()
        {
            SavingServerUser dataToSave = new SavingServerUser(this);
            SaveLoad.SaveData(GetSpecificUserPath(username), dataToSave);
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
                    LeaveGame();
                    break;
                case MessageType.RequestData:
                    SendInformationToClient(message);
                    break;
                case MessageType.DisconnectFromServer:
                    LeaveGame();
                    CloseSocket();
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
        public string salt { get; set; }

        public Dictionary<InformationContainer.Information, double> informationDict { get; set; }

        public SavingServerUser(ServerUser user)
        {
            this.username = user.username;
            this.password = user.password;
            this.salt = user.passwordSalt;
            this.informationDict = user.information.Copy().informationDict;
        }

        public SavingServerUser(string username, string password, string salt, InformationContainer information)
        {
            this.username = username;
            this.password = password;
            this.salt = salt;
            this.informationDict = information.Copy().informationDict;
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