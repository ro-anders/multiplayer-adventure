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

        private WebGameSetup setup = null;

        private bool starting = false;

        // Start is called before the first frame update
        public override void Start()
        {
            base.Start();
            setup = new WebGameSetup(transport);
        }

        // Update is called once per frame
        public override void Update()
        {
            base.Update();
            if (starting) {
                if (setup.IsReady) {
                    // Start the game
                    Debug.Log("Starting game " + (setup.GameNumber + 1)  + ".  This player is " 
                        + (setup.Slot + 1 ) + " of " + setup.NumPlayers);
                    bool[] useAi = { false, false, false };
                    gameEngine = new AdventureGame(this, setup.NumPlayers, setup.Slot, transport, 
                        setup.GameNumber, setup.FastDragons, setup.FearfulDragons, setup.HelpPopups, 
                        setup.MapGuides, false, useAi);
                    gameRenderable = true;
                    starting = false;
                }
            }
        }

        public override void Platform_GameChange(GAME_CHANGES change)
        {
            base.Platform_GameChange(change);
        }

        // This starts the game.
        public void PlayGame()
        {
            if (Application.isEditor) {
                bool[] useAi = { false, true, true };
                gameEngine = new AdventureGame(this, 3, 0, null, 1, false, false, false, false, false, useAi);
                gameRenderable = true;
                starting = false;
            } else {
                // Connect to the server
                setup.Connect();
                starting = true;
            }
        }

    }

}