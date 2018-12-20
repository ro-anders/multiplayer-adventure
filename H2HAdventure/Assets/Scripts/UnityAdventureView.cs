using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameEngine;

public class Rectangle
{
    public Rectangle(int r, int g, int b, int x, int y, int width, int height)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;

    }
    public readonly int r;
    public readonly int g;
    public readonly int b;
    public readonly int x;
    public readonly int y;
    public readonly int width;
    public readonly int height;
}

public class UnityAdventureView : MonoBehaviour, AdventureView
{
    public AdventureAudio adv_audio;
    public AdventureDirectional adv_input;
    public RenderTextureDrawer screenRenderer;

    private UnityTransport xport;

    private const int DRAW_AREA_WIDTH = 320;
    private const int DRAW_AREA_HEIGHT = 256;

    private AdventureGame gameEngine;

    private PlayerSync localPlayer;

    private bool gameStarted = false;
    private int numPlayersReady = 0;

    private List<Rectangle>[] rectsToDisplay = new List<Rectangle>[2];
    private int displayThisOne = -1;
    private int displaying = -1;
    private int paintInThisOne = -1;
    private int maxNumRects = 0;

    void Start() {
        rectsToDisplay[0] = new List<Rectangle>();
        rectsToDisplay[1] = new List<Rectangle>();

        xport = this.gameObject.GetComponent<UnityTransport>();
        if (SessionInfo.NetworkSetup == SessionInfo.Network.NONE) {
            AdventureSetup(0);
            gameStarted = true;
        }
    }

    // Update is called 60 times per second
    void FixedUpdate()
    {
        if (gameStarted)
        {
            AdventureUpdate();
        }
    }

    // Update is called up to 60 times per second, but may be slower depending
    // on device and load
    void Update()
    {
        PaintRectangles();
    }

    public UnityTransport RegisterNewPlayer(PlayerSync newPlayer)
    {
        if (newPlayer.isLocalPlayer)
        {
            localPlayer = newPlayer;
        }
        xport.registerSync(newPlayer);
        if (newPlayer.isServer)
        {
            ++numPlayersReady;
            if (numPlayersReady >= SessionInfo.GameToPlay.numPlayers)
            {
                StartCoroutine(SignalStartGame());
            }
        }
        return xport;
    }

    private IEnumerator SignalStartGame()
    {
        const float GAME_START_BANNER_TIME = 3f;
        yield return new WaitForSeconds(GAME_START_BANNER_TIME);
        localPlayer.RpcStartGame();
    }



    public void StartGame()
    {
        AdventureSetup(localPlayer.getSlot());
        gameStarted = true;
    }

    public void AdventureSetup(int inLocalPlayerSlot) {
        Debug.Log("Starting game.");
        GameInLobby game = SessionInfo.GameToPlay;
        UnityTransport xportToUse = (SessionInfo.NetworkSetup == SessionInfo.Network.NONE ? null : xport);
        gameEngine = new AdventureGame(this, game.numPlayers, inLocalPlayerSlot, xportToUse, game.gameNumber, false, false);
    }

    public void AdventureUpdate() {
        // Setup which list of rectangles to fill
        paintInThisOne = (displayThisOne == -1 ? 0 :
          (displaying >= 0 ? 1-displaying : 1-displayThisOne));

        gameEngine.Adventure_Run();

        if (rectsToDisplay[paintInThisOne].Count > maxNumRects)
        {
            maxNumRects = rectsToDisplay[paintInThisOne].Count;
            Debug.Log("Painted " + maxNumRects + " rectangles.");
        }
        displayThisOne = paintInThisOne;
        paintInThisOne = -1;

    }

    public void Platform_PaintPixel(int r, int g, int b, int x, int y, int width, int height)
    {
        rectsToDisplay[paintInThisOne].Add(new Rectangle(r, g, b, x, y, width, height));
    }

    public void PaintRectangles()
    {
        if (displayThisOne >= 0)
        {
            displaying = displayThisOne;
            List<Rectangle> displayRects = rectsToDisplay[displaying];
            screenRenderer.StartUpdate();
            for (int ctr = 0; ctr < displayRects.Count; ++ctr)
            {
                Rectangle rect = displayRects[ctr];
                Color color = new Color(rect.r / 256.0f, rect.g / 256.0f, rect.b / 256.0f);
                for (int i = 0; i < rect.width; ++i)
                    for (int j = 0; j < rect.height; ++j)
                    {
                        int xi = rect.x + i;
                        int yj = rect.y + j;
                        if ((xi >= 0) && (xi < DRAW_AREA_WIDTH) && (yj >= 0) && (yj < DRAW_AREA_HEIGHT))
                        {

                            screenRenderer.SetPixel(rect.x + i, rect.y + j, color);
                        }
                    }
            }
            screenRenderer.EndUpdate();
            displayRects.Clear();
            // If the other list of rects has been filled while we were painting
            // we setup to use that next time.  Otherwise wait for a list to be filled.
            if (displaying == displayThisOne)
            {
                displayThisOne = -1;
            }
            displaying = -1;
        }
    }

    public void Platform_ReadJoystick(ref bool joyLeft, ref bool joyUp, ref bool joyRight, ref bool joyDown, ref bool joyFire) {
        if (adv_input != null)
        {
            adv_input.getDirection(ref joyLeft, ref joyUp, ref joyRight, ref joyDown);
            joyFire = adv_input.getDropButton();
        }
    }

    public void Platform_ReadConsoleSwitches(ref bool reset) {
        if (adv_input != null)
        {
            reset = adv_input.getResetButton();
        }
    }

    public void Platform_MakeSound(SOUND sound, float volume) {
        if (adv_audio != null)
        {
            adv_audio.play(sound, volume);
        }
    }

    public void Platform_ReportToServer(string message) {
        Debug.Log("Message to server: " + message);
    }


    public void Platform_DisplayStatus(string message, int durationSecs) {
        Debug.Log("Message for player: " + message);
    }


}
