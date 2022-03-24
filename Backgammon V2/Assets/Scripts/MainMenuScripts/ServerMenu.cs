using BackGammonUser;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerMenu : Menu
{
    [SerializeField] private InformationMenu informationMenu;

    public void StartGameButton(int sceneIndex)
    {
        if (OnlineGameManager.GetUser().IsConnected)
            SceneManager.LoadScene(sceneIndex);
        else
            MenuManager.instance.OpenMenu(MainMenuManager.instance.GetStartMenu());
    }

    public Menu FinishedGettingInformation()
    {
        ClientUser theUser = OnlineGameManager.GetUser();

        (string, MessageType) message;
        if (theUser.TryGetMessage(out message))
        {
            print(message);
            switch (message.Item2)
            {
                case MessageType.DisconnectFromServer:
                    return MainMenuManager.instance.GetStartMenu();
                case MessageType.InformationContainer: // i got all the information from the server
                    informationMenu.SetNewInformation(message.Item1);
                    return informationMenu;
                default:
                    break;
            }
        }
        return null;
    }

    public void SeeInformationButton()
    {
        OnlineGameManager.GetUser().GetLatestUserInformation();
        LoadingMenu loadingMenu = MenuManager.instance.GetLoadingMenu();
        loadingMenu.StartLoading("Getting Information", FinishedGettingInformation);
    }
}
