using System;
using GameEngine;
using UnityEngine;

public class SimpleMplayerAdvView : UnityAdventureBase
{

    private System.Random randomGen = new System.Random();

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
        bool[] useAi = { true, true, true };
        int slot = randomGen.Next(3);
        useAi[slot] = false;
        gameEngine = new AdventureGame(this, 3, slot, null, 0,
            false, false, false, false, false, useAi);
        gameRenderable = true;
    }

}
