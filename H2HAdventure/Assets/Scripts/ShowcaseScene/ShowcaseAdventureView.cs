using System.Collections;
using System.Collections.Generic;
using GameEngine;
using UnityEngine;
using UnityEngine.UI;

public class ShowcaseAdventureView : UnityAdventureBase
{

    public ShowcaseTransport xport;

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

    public void PlayGame(ProposedGame game, int thisPlayerSlot)
    {
        gameEngine = new AdventureGame(this, game.numPlayers, thisPlayerSlot, xport,
            game.gameNumber, game.diff1 == 0, game.diff2 == 0,
            SessionInfo.ThisPlayerInfo.needsPopupHelp, SessionInfo.ThisPlayerInfo.needsMazeGuides);
        gameRenderable = true;
    }

}
