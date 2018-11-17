using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PromptNameController : MonoBehaviour {

    public GameObject thisPanel;
    public InputField nameInput;
    public LobbyController parent;

    public void OnOkPressed() {
        parent.ThisPlayerName = nameInput.text;
        thisPanel.SetActive(false);
    }
}
