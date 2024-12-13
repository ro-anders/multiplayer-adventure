using System;
using System.IO.Compression;
using GameEngine;
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

        private WebGameSetup setup = null;

        private bool started = false;

        /** When playing in the editor we simulate waiting for the game server with a
            countdown timer. */
        private int fakeTimer = -1;

        /// <summary>
        /// Setup the game (which reads some game info from URL) and 
        /// connect to the backend game server.
        /// </summary>
        public override void Start()
        {
            base.Start();
            setup = new WebGameSetup(transport);
            if (Application.isEditor) {
                fakeTimer = 200;
            } else {
                setup.Connect();
            }
        }

        // Update is called once per frame
        public override void Update()
        {
            base.Update();

            // If we're in the editor handle faking some of the transitions
            // initiated by the game server.
            if (fakeTimer > 0) {
                --fakeTimer;
                if (fakeTimer == 0) {
                    if (setup.WebGameSetupState == WebGameSetup.WAITING_FOR_GAMEINFO) {
                        transport.fakeGameInfo();
                    }
                    if (setup.WebGameSetupState == WebGameSetup.WAITING_FOR_OTHERS) {
                        transport.fakeReceivedReady();
                    }
                }
            }
            
            if (!started) {
                switch (setup.WebGameSetupState) {
                    case WebGameSetup.WAITING_FOR_PLAYER:
                        if (!startPanel.gameObject.activeInHierarchy) {
                            startPanel.showGameInfo(setup);
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

        /// <summary>
        /// This marks the player as ready to play
        /// </summary>
        public void SetReady() {
            setup.SetReady();
            startPanel.markStartPressed();
            if (Application.isEditor) {
                fakeTimer = 200;
            }
        }

        // This starts the game.
        private void PlayGame()
        {
            // Start the game
            Debug.Log("Starting game " + (setup.GameNumber + 1)  + ".  This player is " 
                + (setup.Slot + 1 ) + " of " + setup.NumPlayers);
            if (Application.isEditor) {
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

    }

}