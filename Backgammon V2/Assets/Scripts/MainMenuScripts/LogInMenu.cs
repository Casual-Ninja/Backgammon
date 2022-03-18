using UnityEngine;
using TMPro;
using BackGammonUser;

public class LogInMenu : Menu
{
    [SerializeField] private Menu serverMenu, mainMenu;
    [SerializeField] private TextMeshProUGUI showClientText;
    [SerializeField] private TMP_InputField usernameTxt, passwordTxt;

    public Menu FinishedLoggingIn()
    {
        ClientUser theUser = OnlineGameManager.GetUser();

        (string, MessageType) message;
        if (theUser.TryGetMessage(out message))
        {
            print(message);
            switch (message.Item2)
            {
                case MessageType.DisconnectFromServer:
                    return mainMenu;
                case MessageType.AccountInformationError:
                    showClientText.text = message.Item1;
                    return this;
                case MessageType.AccountInformationOk:
                    return serverMenu;
                default:
                    break;
            }
        }
        return null;
    }

    private void TryLogin(string username, string password)
    {
        ClientUser theUser = OnlineGameManager.GetUser();

        theUser.SetAccountInfo(username, password);

        theUser.LoginToAccount();

        LoadingMenu loadingMenu = MenuManager.instance.GetLoadingMenu();
        loadingMenu.StartLoading("Tryin to log in", FinishedLoggingIn);
    }

    private void TryCreateAccount(string username, string password)
    {
        ClientUser theUser = OnlineGameManager.GetUser();

        theUser.SetAccountInfo(username, password);

        theUser.CreateAccount();

        LoadingMenu loadingMenu = MenuManager.instance.GetLoadingMenu();
        loadingMenu.StartLoading("Tryin to create account", FinishedLoggingIn);
    }

    public void PressedLogin()
    {
        if (usernameTxt.text != "" && passwordTxt.text != "")
            TryLogin(usernameTxt.text, passwordTxt.text);
    }

    public void PressedCreateAccount()
    {
        if (usernameTxt.text != "" && passwordTxt.text != "")
            TryCreateAccount(usernameTxt.text, passwordTxt.text);
    }
}
