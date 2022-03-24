using UnityEngine;
using TMPro;

public class InformationMenu : Menu
{
    [SerializeField] private TextMeshProUGUI informationText;
    [SerializeField] private ServerMenu mainServerMenu;

    public void LeaveInformationMenu()
    {
        print("Im clicking the button?");
        MenuManager.instance.OpenMenu(mainServerMenu);
    }

    public void SetNewInformation(string messageInfo)
    {
        // the message info is: nameOfThing:value,nameOfThing2:value...

        string newValue = "";
        for (int i = 0; i < messageInfo.Length; i++)
        {
            if (messageInfo[i] == ':')
                newValue += ": ";
            else if (messageInfo[i] == ',')
                newValue += "\n";
            else
                newValue += messageInfo[i];
        }
        informationText.text = newValue;
    }
}
