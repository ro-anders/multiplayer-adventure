using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Controller for the modal dialog that appears in the lobby scene.  Lets you specify 
// configuration of a new game.
public class NewGameController : MonoBehaviour {

    public LobbyController lobbyController;
    public Dropdown numPlayersDropdown;
    public Dropdown gameNumberDropdown;
    public Toggle diff1Toggle;
    public Toggle diff2Toggle;


    public void OnOkPressed() {
        NewGameInfo info = new NewGameInfo();
        info.numPlayers = numPlayersDropdown.value + 2;
        info.gameNumber = gameNumberDropdown.value;
        info.fastDragons = diff1Toggle.isOn;
        info.dragonsRunFromSword = diff2Toggle.isOn;
        lobbyController.SubmitNewGame(info);
        lobbyController.CloseNewGameDialog(true);
    }

    public void OnCancelPressed() {
        lobbyController.CloseNewGameDialog(false);
    }

    public void OnGameTypeChanged()
    {
        if (gameNumberDropdown.value == 6)
        {
            numPlayersDropdown.value = 1;
            numPlayersDropdown.interactable = false;
            diff1Toggle.isOn = true;
            diff1Toggle.interactable = false;
            diff1Toggle.GetComponentInChildren<Text>().text = "Respawning, fast dragons"; 
            diff2Toggle.isOn = true;
            diff2Toggle.interactable = false;
            diff2Toggle.GetComponentInChildren<Text>().text = "Dragons much smarter with sword";
        }
        else
        {
            numPlayersDropdown.interactable = true;
            diff1Toggle.interactable = true;
            diff1Toggle.GetComponentInChildren<Text>().text = "Fast dragons";
            diff2Toggle.interactable = true;
            diff2Toggle.GetComponentInChildren<Text>().text = "Dragons run from sword";
        }
    }


}
