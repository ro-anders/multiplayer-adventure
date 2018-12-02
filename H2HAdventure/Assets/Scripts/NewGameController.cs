﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Controller for the modal dialog that appears in the lobby scene.  Lets you specify 
// configuration of a new game.
public class NewGameController : MonoBehaviour {

    public LobbyController lobbyController;
    public Dropdown numPlayersDropdown;
    public Dropdown gameNumberDropdown;
    public Toggle diffAToggle;
    public Toggle diffBToggle;


    public void OnOkPressed() {
        NewGameInfo info = new NewGameInfo();
        info.numPlayers = numPlayersDropdown.value + 2;
        info.gameNumber = gameNumberDropdown.value;
        info.fastDragons = diffAToggle.isOn;
        info.dragonsRunFromSword = diffBToggle.isOn;
        lobbyController.SubmitNewGame(info);
        lobbyController.CloseNewGameDialog(true);
    }

    public void OnCancelPressed() {
        lobbyController.CloseNewGameDialog(false);
    }
}
