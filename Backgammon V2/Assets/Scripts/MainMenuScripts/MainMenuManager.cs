using UnityEngine;
using UnityEngine.SceneManagement;
using BackGammonUser;
using System.Threading;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private Menu startMenu, logInToAccountMenu, serverMenu;

    public static MainMenuManager instance;

    public Menu GetStartMenu() { return startMenu; }

    private static Thread mainThread = Thread.CurrentThread;

    private HelperSpace.ThreadIntClass isConnected = new HelperSpace.ThreadIntClass();

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        if (OnlineGameManager.GetUser() != null)
        {
            // i already connected to the server once in this session
            if (OnlineGameManager.GetUser().IsConnected)
            {
                MenuManager.instance.OpenMenu(serverMenu);
            }
        }
    }

    private void SetFinishedConnecting(int value)
    {
        lock (isConnected)
        {
            isConnected.value = value;
        }
    }

    public Menu FinishedConnecting()
    {
        int val = 0;
        lock (isConnected)
        {
            val = isConnected.value;
        }
        if (val == 0)
            return null;
        if (val == 1)
            return logInToAccountMenu;
        OnlineGameManager.GetUser().CloseSocket();
        return startMenu;
    }
    
    private void ConnectToServerInThread(object theUser)
    {
        ClientUser user = (ClientUser)theUser;
        SetFinishedConnecting(0);
        
        try
        {
            print("Trying to connect to server");
            lock (mainThread)
            {
                user.LoginToServer(mainThread);
            }
            SetFinishedConnecting(1);
        }
        catch
        {
            print("Failed to connect to server");
            SetFinishedConnecting(2);
        }
    }

    public void PressedLogIn()
    {
        if (OnlineGameManager.GetUser() != null)
            OnlineGameManager.GetUser().DisconnectFromServer();

        ClientUser newUser = new ClientUser("", "");
        OnlineGameManager.SetNewClientUser(newUser);

        LoadingMenu loadingMenu = MenuManager.instance.GetLoadingMenu();
        loadingMenu.StartLoading("Connecting to server", FinishedConnecting);

        lock (mainThread)
        {
            mainThread = Thread.CurrentThread;
        }
        Thread connectToServerThread = new Thread(ConnectToServerInThread);
        connectToServerThread.Start(newUser);
    }

# if UNITY_EDITOR
    private void OnApplicationQuit()
    {
        OnlineGameManager.GetUser().DisconnectFromServer();
        print("stopped playing");
    }
#endif
}
