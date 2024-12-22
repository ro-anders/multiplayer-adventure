using System;
using GameEngine;
using UnityEngine;

/**
 * View for running a single player view.  Has very little extra 
 * behavior beyond the base class except for a start/reset button.
 **/
public class SinglePlayerAdvView : UnityAdventureBase
{

    private System.Random randomGen = new System.Random();

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
        int slot_to_play = (slot == -1 ? randomGen.Next(3) : slot);
        bool[] useAi = { true, true, true };
        useAi[slot_to_play] = false;
        gameEngine = new AdventureGame(this, 3, slot_to_play, null, 2,
            false, false, false, false, false, useAi);

        base.gameRenderable = true;
    }

}
