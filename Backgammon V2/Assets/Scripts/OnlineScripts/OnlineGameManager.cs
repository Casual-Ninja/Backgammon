using UnityEngine;
using BackGammonUser;
using System.Collections;
using TMPro;
using GAME;
using UnityEngine.SceneManagement;

public class OnlineGameManager : MonoBehaviour
{
    [SerializeField] private BoardViewManager viewManager;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private GameObject endGameHolder;
    [SerializeField] private TextMeshProUGUI endGameText;
    [SerializeField] private string playerWonMessage, serverWonMessage;

    private static ClientUser user;

    public static void SetNewClientUser(ClientUser newUser) { user = newUser; }
    public static ClientUser GetUser() { return user; }

    private bool isInAccountPhase = true;

    IEnumerator Start()
    {
        //isInAccountPhase = true;

        //user = new ClientUser("", "");

        //user.LoginToServer();

        //// logg in to an account
        //yield return StartCoroutine(AccountPhase());
        
        // start a new game sesion
        user.StartNewGame();
        
        // Set up the start of the game
        yield return StartCoroutine(StartingGamePhase());
        
        // Play the game
        StartCoroutine(GamePhase());
    }

    private IEnumerator WaitForServerAnswer()
    {
        yield return new WaitForSecondsRealtime(0.1f);
    }

    private IEnumerator AccountPhase()
    {
        while (isInAccountPhase)
        {
            string answer = "C";

            (string, string) accountInfo = ("Erezzzz", "123456789");

            user.SetAccountInfo(accountInfo.Item1, accountInfo.Item2);

            if (answer == "C")
                user.CreateAccount();
            else
                user.LoginToAccount();

            (string, MessageType) messageOfLoggin;
            bool gotAnswer = false;

            while (gotAnswer == false)
            {
                yield return StartCoroutine(WaitForServerAnswer());

                if (user.TryGetMessage(out messageOfLoggin))
                {
                    print("Got message: " + messageOfLoggin);
                    if (messageOfLoggin.Item2 == MessageType.AccountInformationOk)
                    {
                        isInAccountPhase = false; // since i logged in succesfully
                        gotAnswer = true;
                        print("Succesfully did " + answer);
                    }
                    else if (messageOfLoggin.Item2 == MessageType.AccountInformationError)
                    {
                        gotAnswer = true;
                        print("Failed to do " + answer);
                    }
                    else
                    {
                        print(messageOfLoggin.Item1);
                    }
                }
            }
        }
    }

    private IEnumerator StartingGamePhase()
    {
        while (user.inGame == false) // while i haven't officialy started the game
        {
            yield return StartCoroutine(WaitForServerAnswer());

            (string, MessageType) message;
            if (user.TryGetMessage(out message))
            {
                if (message.Item2 == MessageType.StartGame)
                {
                    BackGammonChanceState chanceState = BackGammonChanceState.PorotocolInformation(message.Item1.Substring(0, 2));

                    user.SetStartGameState(chanceState);

                    viewManager.InitializeDeafualtPips();
                    yield return StartCoroutine(viewManager.SetDiceValues(user.state));

                    if (message.Item1[2] == '1')
                        user.isPlayerTurn = true;
                    else
                        user.isPlayerTurn = false;

                    print("Is player turn: " + user.isPlayerTurn);
                }
            }
        }
    }

    private void GameFinished(string gameFinishedMessage)
    {
        endGameHolder.SetActive(true);
        endGameText.text = gameFinishedMessage;
    }

    private IEnumerator GamePhase()
    {
        while (user.inGame)
        {
            print("In game phase...");

            BackGammonChanceAction actionInput = null;
            if (user.isPlayerTurn)
            {
                //actionInput = GetActionInput(user.state, user.parentState);
                actionInput = new BackGammonChanceAction();
                yield return StartCoroutine(inputManager.GetInput(user.parentState, user.state, actionInput)); // wait for input from player

                user.SendMoveToServer(actionInput);
            }

            bool waitForServerMessage = true;

            while (waitForServerMessage)
            {
                yield return StartCoroutine(WaitForServerAnswer());

                (string, MessageType) message;

                if (user.TryGetMessage(out message))
                {
                    print("Got message:\n" + message);
                    switch (message.Item2)
                    {
                        case MessageType.MoveIsValid: // the move i wanted to do is valid
                            user.parentState = (BackGammonChoiceState)user.state.Move(user.parentState, actionInput);
                            break;
                        case MessageType.ChanceAction: // server sent the move he wants to do, so read it, and show it
                            BackGammonChanceAction serverMove = BackGammonChanceAction.PorotocolInformation(message.Item1);
                            user.parentState = (BackGammonChoiceState)user.state.Move(user.parentState, serverMove);
                            print("Doing server move!");
                            yield return StartCoroutine(viewManager.DoMoves(serverMove, false));
                            break;
                        case MessageType.ChoiceState: // the new choice state of the board
                            user.parentState = BackGammonChoiceState.PorotocolInformation(message.Item1);
                            //viewManager.InitializeNewState(user.parentState);
                            break;
                        case MessageType.ChanceState: // the new chance state of the board
                            user.state = BackGammonChanceState.PorotocolInformation(message.Item1);
                            yield return StartCoroutine(viewManager.SetDiceValues(user.state));
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
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        
        // someone won (the opposite of current turn since the turn was switched)
        if (user.isPlayerTurn)
            GameFinished(playerWonMessage);
        else
            GameFinished(serverWonMessage);
    }

    public void ReturnToMainMenu()
    {
        StopAllCoroutines(); // stop all coroutines since they are supposed to work only in this scene...
        SceneManager.LoadScene(0);
    }
}