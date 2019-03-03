using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PromptNameController : MonoBehaviour {

    public GameObject thisPanel;
    public InputField nameInput;
    public LobbyController parent;

    void Start()
    {
        string prevName = PlayerPrefs.GetString(SessionInfo.PLAYER_NAME_PREF, "");
        if (!prevName.Equals(""))
        {
            nameInput.text = prevName;
        }
        
    }

    public void OnOkPressed() {
        if (nameInput.text.Trim() != "")
        {
            PlayerPrefs.SetString(SessionInfo.PLAYER_NAME_PREF, nameInput.text.Trim());
            parent.GotPlayerName(nameInput.text.Trim());
        }
    }
}
