using System;
using System.IO.Compression;
using GameEngine;
using TMPro;
using UnityEngine;

namespace GameScene
{

    /**
    * This is used for testing.  It has very little behavior beyond the base 
    * implementation but it is responsible for coordinating the remote players
    * before starting the game.
    */
    public class SimpleMplayerAdvView : UnityAdventureBase
    {
        public WebSocketTransport transport;

        public GameStartPanel startPanel = null;

        public TMP_Text roster; 

        public TMP_Text homeCastleWatermark;

        private WebGameSetup setup = null;


        private bool started = false;

        /// <summary>
        /// Setup the game (which reads some game info from URL) and 
        /// connect to the backend game server.
        /// </summary>
        public override void Start()
        {
            base.Start();
            setup = new WebGameSetup(transport);
            setup.Connect();
        }

        // Update is called once per frame
        public override void Update()
        {
            base.Update();
            
            if (!started) {
                switch (setup.WebGameSetupState) {
                    case WebGameSetup.WAITING_FOR_PLAYER:
                        if (!startPanel.gameObject.activeInHierarchy) {
                            startPanel.showGameInfo(setup);
                            showPlayersInRoster(setup.PlayerNames, setup.Slot);
                        }
                        break;
                    case WebGameSetup.GO:
                        startPanel.gameObject.SetActive(false);
                        PlayGame();
                        started = true;
                        break;
                }
            }
        }

        public override void Platform_GameChange(GAME_CHANGES change)
        {
            base.Platform_GameChange(change);
        }

        public override void Platform_ReportToServer(string message)
        {
            base.Platform_ReportToServer(message);
            // Map the string message to an int code.
            int message_index = Array.IndexOf(AdventureReports.ALL_REPORTS, message);
            if (message_index < 0) {
                throw new Exception("Unknown message \"" + message + "\".  Cannot send to server.");
            }
            transport.reportToServer(message_index);
        }

        /// <summary>
        /// This marks the player as ready to play
        /// </summary>
        public void SetReady() {
            setup.SetReady();
            startPanel.markStartPressed();
        }

        // This starts the game.
        private void PlayGame()
        {
            // Start the game
            Debug.Log("Starting game " + (setup.GameNumber + 1)  + ".  This player is " 
                + (setup.Slot + 1 ) + " of " + setup.NumPlayers);
            if (setup.DummyMode) {
                bool[] useAi = { false, true, true };
                gameEngine = new AdventureGame(this, setup.NumPlayers, setup.Slot, null, 
                    setup.GameNumber, setup.FastDragons, setup.FearfulDragons, 
                    false, false, false, useAi);
            } else {
                bool[] useAi = { false, false, false };
                gameEngine = new AdventureGame(this, setup.NumPlayers, setup.Slot, transport, 
                    setup.GameNumber, setup.FastDragons, setup.FearfulDragons, 
                    setup.HelpPopups, setup.MapGuides, false, useAi);
            }
            gameRenderable = true;
            started = true;
        }

        private void showPlayersInRoster(string[] playerNames, int currentPlayer) {
            roster.text = roster.text.Replace("Player1", (currentPlayer == 0 ? "<i>you</i>" : playerNames[0]));
            roster.text = roster.text.Replace("Player2", (currentPlayer == 1 ? "<i>you</i>" : playerNames[1]));
            if (playerNames.Length > 2) {
                roster.text = roster.text.Replace("Player3", (currentPlayer == 2 ? "<i>you</i>" : playerNames[2]));
            } else {
                // Strip off the last line
                roster.text = roster.text.Remove(roster.text.LastIndexOf(Environment.NewLine));
            }
            // Also set the watermark.  Gold=0, Copper=1, Jade=11
            homeCastleWatermark.text = "<sprite=" + (currentPlayer == 2 ? 11 : currentPlayer) + ">";
        }

    }

}