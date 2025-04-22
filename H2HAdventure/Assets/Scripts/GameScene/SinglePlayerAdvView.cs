using System;
using System.Web;
using GameEngine;
using GameScene;
using UnityEngine;

/**
 * View for running a single player view.  Has very little extra 
 * behavior beyond the base class except for a start/reset button.
 **/
public class SinglePlayerAdvView : UnityAdventureBase
{
    // If set to true will force it to a basic 
    // game (2 player Game 1 game where you
    // always start in the gold castle)
    private const bool DEMO = true;

    private System.Random randomGen = new System.Random();

    // The slot of the player.  -1 means to randomly generate.
    public int slot =-1;

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

    public int deduceExperienceLevel() {
        int defaultValue = 3;
        GameEngine.Logger.Debug("Reading URL");
        const string GAMECODE_PARAM = "gamecode";
        string urlstr = Application.absoluteURL;
        if ((urlstr == null) || (urlstr.Length == 0)) {
            return defaultValue;
        }
        Uri url = new Uri(urlstr);
        if ((url == null) || (url.Query == null) || (url.Query.Trim().Length == 0)) {
            return defaultValue;
        }
        string gamecode_str = HttpUtility.ParseQueryString(url.Query).Get(GAMECODE_PARAM);
        if (gamecode_str == null) {
            return defaultValue;
        }

        // Not dealing with hexadecimal, so parse into an int and then to a byte
        int gamecode_int = Int32.Parse(gamecode_str);
        // First bit of gamecode is map guides boolean
        bool map_guides = gamecode_int % 2 == 1;
        // Second bit is help popups boolean
        bool help_popups = gamecode_int/2 % 2 == 1;
        return (help_popups ? 1 : (map_guides ? 2 : 3) );
    }

    public void PlayGame()
    {
        // If we set DEMO, then game will be
        // a two player Game 1 game where you 
        // are always in the gold castle.
        // Otherwise will be a three player
        // Game 3 game in a random castle.

        // Randomly pick which player to play
        if (DEMO) {
            slot = 0;
        }
        int slot_to_play = (slot == -1 ? randomGen.Next(3) : slot);
        GameEngine.Logger.Debug("DEMO=" + DEMO + ", slot=" + slot + ", slot_to_play=" + slot_to_play);
        int num_players = (DEMO ? 2 : 3);
        int game_num = (DEMO ? 0 : 2);
        bool[] useAi = { true, true, num_players==3 };
        useAi[slot_to_play] = false;
        int experience_level = deduceExperienceLevel();
        gameEngine = new AdventureGame(this, num_players, slot_to_play, null, game_num,
            false, false, 
            experience_level <= 1, 
            experience_level <= 2,
            false, useAi);

        base.gameRenderable = true;
    }

}
