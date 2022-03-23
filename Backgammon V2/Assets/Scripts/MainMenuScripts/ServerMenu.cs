using UnityEngine.SceneManagement;

public class ServerMenu : Menu
{
    public void StartGameButton(int sceneIndex)
    {
        if (OnlineGameManager.GetUser().IsConnected)
            SceneManager.LoadScene(sceneIndex);
        else
            MenuManager.instance.OpenMenu(MainMenuManager.instance.GetStartMenu());
    }
}
