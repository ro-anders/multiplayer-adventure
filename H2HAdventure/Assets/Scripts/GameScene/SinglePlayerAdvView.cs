using System;
using GameEngine;
using UnityEngine;

/**
 * Name is a misnomer, this is not limited to single player, but this
   is a view that launches H2H adventure without all the ancillaries
   like a chat panel or player roster.  Uses a "mode" variable
   to define exactly what simple game it is launching.
   **/
public class SinglePlayerAdvView : UnityAdventureBase
{

    private System.Random randomGen = new System.Random();

    // What simple game are we launching.  Can be 
    // "single" - one player vs. the AI
    // "testmulti" - runs multiplayer on local machine with hardcoded settings
    // "multi" - runs multiplayer with hardcoded settings
    public string mode;

    // The transport to use in the simple game
    public Transport xport;

    // The slot of the player.  -1 means to randomly generate.
    public int slot = -1;

    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    public override void Update()
    {
        base.Update();
    }

    public override void Platform_GameChange(GAME_CHANGES change)
    {
        base.Platform_GameChange(change);
    }

    public void PlayGame()
    {
        // Randomly pick which player to play
        if (slot == -1) {
            slot = randomGen.Next(3);
        }
        if (mode == "single") {
            bool[] useAi = { true, true, true };
            useAi[slot] = false;
            gameEngine = new AdventureGame(this, 3, slot, null, 0,
                false, false, false, false, false, useAi);
        } else {
            bool[] useAi = { false, false, false };
            gameEngine = new AdventureGame(this, 2, slot, xport, 0,
                false, false, false, false, false, useAi);
        }
        gameRenderable = true;
    }

}
