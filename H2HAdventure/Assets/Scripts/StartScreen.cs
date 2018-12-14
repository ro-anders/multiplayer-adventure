using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScreen : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
    public void OnPlayClicked() {
        SessionInfo.NetworkSetup = SessionInfo.Network.MATCHMAKER;
        SceneManager.LoadScene("Lobby");
    }

    public void OnTestClicked()
    {
        SessionInfo.NetworkSetup = SessionInfo.Network.ALL_LOCAL;
        SceneManager.LoadScene("Lobby");
    }

    public void OnTestSingleClicked()
    {
        SessionInfo.NetworkSetup = SessionInfo.Network.NONE;
        SessionInfo.ThisPlayerId = 12;
        SessionInfo.ThisPlayerName = "adam";
        SessionInfo.GameToPlay = new GameInLobby();
        SessionInfo.GameToPlay.gameId = 11;
        SessionInfo.GameToPlay.playerOne = SessionInfo.ThisPlayerId;
        SessionInfo.GameToPlay.playerOneName = SessionInfo.ThisPlayerName;
        SessionInfo.GameToPlay.playerTwo = 13;
        SessionInfo.GameToPlay.playerTwoName = "ben";
        SessionInfo.GameToPlay.playerThree = 14;
        SessionInfo.GameToPlay.playerThreeName = "calvin";
        SessionInfo.GameToPlay.numPlayers = 3;
        SessionInfo.GameToPlay.gameNumber = 1;
        SceneManager.LoadScene(LobbyController.GAME_SCENE);
    }
}
