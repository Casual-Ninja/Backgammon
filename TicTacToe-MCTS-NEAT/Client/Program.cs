using System;
using BackGammonUser;
using System.Threading;
using GAME;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            ClientGameManager manager = new ClientGameManager();
        }
    }
    
    class ClientGameManager
    {
        private ClientUser user;
        private bool isInAccountPhase = true;

        public ClientGameManager()
        {
            isInAccountPhase = true;

            user = new ClientUser("", "");

            user.LoginToServer();

            Console.WriteLine("Account phase");
            // logg in to an account
            AccountPhase();


            Console.WriteLine("Creating game phase");
            // start a new game sesion
            user.StartNewGame();


            Console.WriteLine("Starting new game phase");
            // Set up the start of the game
            StartingGamePhase();

            Console.WriteLine("Actual game phase");
            // Play the game
            GamePhase();

            // Game finished
            Console.WriteLine("Game finished!!!!");
        }

        private (string, string) GetUserInformation()
        {
            Console.WriteLine("Enter your username");
            string userName = Console.ReadLine();
            Console.WriteLine("Enter your password");
            string passWord = Console.ReadLine();

            return (userName, passWord);
        }

        private void AccountPhase()
        {
            while (isInAccountPhase)
            {
                Console.WriteLine("Create Account / Logg In To Account : C / L");

                string answer = Console.ReadLine();

                (string, string) accountInfo = GetUserInformation();

                user.SetAccountInfo(accountInfo.Item1, accountInfo.Item2);

                if (answer == "C")
                    user.CreateAccount();
                else
                    user.LoginToAccount();

                (string, MessageType) messageOfLoggin;
                bool gotAnswer = false;

                while (gotAnswer == false)
                {
                    Thread.Sleep(100); // every 0.1 sec
                    if (user.TryGetMessage(out messageOfLoggin))
                    {
                        Console.WriteLine("Got message: " + messageOfLoggin);
                        if (messageOfLoggin.Item2 == MessageType.AccountInformationOk)
                        {
                            isInAccountPhase = false; // since i logged in succesfully
                            gotAnswer = true;
                            Console.WriteLine("Succesfully did " + answer);
                        }
                        else if (messageOfLoggin.Item2 == MessageType.AccountInformationError)
                        {
                            gotAnswer = true;
                            Console.WriteLine("Failed to do " + answer);
                        }
                        else
                        {
                            Console.WriteLine(messageOfLoggin.Item1);
                        }
                    }
                }
            }
        }

        private void StartingGamePhase()
        {
            while (user.inGame == false) // while i haven't officialy started the game
            {
                (string, MessageType) message;
                if (user.TryGetMessage(out message))
                {
                    Console.WriteLine(message);
                    if (message.Item2 == MessageType.StartGame)
                    {
                        BackGammonChanceState chanceState = BackGammonChanceState.PorotocolInformation(message.Item1.Substring(0, 2));

                        user.SetStartGameState(chanceState);

                        Console.WriteLine(user.parentState);

                        Console.WriteLine(user.state);

                        if (message.Item1[2] == '1')
                            user.isPlayerTurn = true;
                        else
                            user.isPlayerTurn = false;
                    }
                }
            }
        }

        private static BackGammonChanceAction GetActionInput(BackGammonChanceState currentState, State prevState)
        {
            BackGammonChanceAction action = new BackGammonChanceAction();

            int actionLength = 0;
            
            bool first = true;
            while (currentState.IsLegalMove(prevState, action) == false)
            {
                if (first)
                {
                    first = false;
                    actionLength = ((BackGammonChanceAction)currentState.GetLegalActions(prevState)[0]).Count;
                }
                else
                {
                    Console.WriteLine("Cannot do that action...");
                    action = new BackGammonChanceAction();
                }
                
                for (int i = 0; i < actionLength; i++)
                {
                    while (true)
                    {
                        Console.WriteLine("Enter action " + i);
                        string input = Console.ReadLine();

                        string[] split = input.Split('/');

                        if (split.Length == 2)
                        {
                            int indexFrom;
                            if (int.TryParse(split[0], out indexFrom))
                            {
                                int indexTo;

                                if (int.TryParse(split[1], out indexTo))
                                {
                                    action.AddToAction(((sbyte)(indexFrom), (sbyte)(indexTo)));
                                    break;
                                }
                            }

                        }
                        else
                        {
                            if (input == "legal")
                            {
                                Console.WriteLine("Legal Actions:");
                                foreach (GAME.Action act in currentState.GetLegalActions(prevState))
                                    Console.WriteLine(act);
                            }
                        }
                        Console.WriteLine("Try to input action again");
                    }
                }
            }

            return action;
        }

        private void GamePhase()
        {
            while (user.inGame)
            {
                BackGammonChanceAction actionInput = null;
                if (user.isPlayerTurn)
                {
                    actionInput = GetActionInput(user.state, user.parentState);
                    user.SendMoveToServer(actionInput);
                }

                bool waitForServerMessage = true;

                while (waitForServerMessage)
                {
                    (string, MessageType) message;

                    if (user.TryGetMessage(out message))
                    {
                        switch (message.Item2)
                        {
                            case MessageType.MoveIsValid: // the move i wanted to do is valid
                                user.parentState = (BackGammonChoiceState)user.state.Move(user.parentState, actionInput);
                                BackGammonChoiceState copyOfBoard = (BackGammonChoiceState)user.parentState.Copy();
                                copyOfBoard.RotateBoard();
                                Console.WriteLine(copyOfBoard); // show the result of my own move
                                break;
                            case MessageType.ChanceAction: // server sent the move he wants to do, so read it, and write it
                                BackGammonChanceAction serverMove = BackGammonChanceAction.PorotocolInformation(message.Item1);
                                Console.WriteLine("Server move: " + serverMove.ToString());
                                break;
                            case MessageType.ChoiceState: // the new choice state of the board
                                user.parentState = BackGammonChoiceState.PorotocolInformation(message.Item1);
                                Console.WriteLine(user.parentState);
                                break;
                            case MessageType.ChanceState: // the new chance state of the board
                                user.state = BackGammonChanceState.PorotocolInformation(message.Item1);
                                Console.WriteLine(user.state);
                                break;
                            case MessageType.SwitchTurn: // change the turn value
                                user.isPlayerTurn = !user.isPlayerTurn;
                                waitForServerMessage = false;
                                break;
                            case MessageType.GameFinished: // the game finished, you can know who won based on user.isPlayerTurn
                                user.inGame = false;
                                waitForServerMessage = false;
                                break;
                            case MessageType.MoveError: // The move i sent wasn't correct for some reason...
                                waitForServerMessage = false;
                                Console.WriteLine("Move error:\n" + message.Item1);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }
    }
}
