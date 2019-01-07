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
}
