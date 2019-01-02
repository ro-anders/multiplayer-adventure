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
    public IntroPanelController introPanel;

    private UnityTransport xport;

    private const int DRAW_AREA_WIDTH = 320;
    private const int DRAW_AREA_HEIGHT = 256;

    private AdventureGame gameEngine;

    private PlayerSync localPlayer;

    private bool gameStarted = false;
    private int numPlayersReady = 0;

    // We hold a big buffer of rectangles as compactly as possible, so we
    // convert a Rectange (red, green, blue, x, y, width, length) and store
    // them serially in a 1D array
    private const short RECTSIZE = 7;
    private const int RECTS_START_SIZE = 3000;
    private short[] rectsToDisplay = new short[RECTS_START_SIZE * RECTSIZE];
    int rectsBufferSize = RECTS_START_SIZE;
    int numRects = 0;
    bool painting = false;
    bool displaying = false;

    void Start() {
        xport = this.gameObject.GetComponent<UnityTransport>();
        introPanel.Show();
        if (SessionInfo.NetworkSetup == SessionInfo.Network.NONE) {
            StartGame();
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
        DisplayRectangles();
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
        introPanel.Hide();
        AdventureSetup(localPlayer == null ? 0 : localPlayer.getSlot());
        gameStarted = true;
    }

    public void AdventureSetup(int inLocalPlayerSlot) {
        Debug.Log("Starting game.");
        GameInLobby game = SessionInfo.GameToPlay;
        UnityTransport xportToUse = (SessionInfo.NetworkSetup == SessionInfo.Network.NONE ? null : xport);
        gameEngine = new AdventureGame(this, game.numPlayers, inLocalPlayerSlot, xportToUse, game.gameNumber, false, false);
    }

    public void AdventureUpdate() {
        // Pretty sure this is single threaded and paint and display loops can't
        // overlap, but put this in to check that.
        if (displaying)
        {
            Debug.Log("AdventureUpdate called while in the middle of display loop");
        }
        painting = true;
        numRects = 0;
        gameEngine.Adventure_Run();
        painting = false;
    }

    public void Platform_PaintPixel(int r, int g, int b, int x, int y, int width, int height)
    {
        if (numRects >= rectsBufferSize)
        {
            int newBufferSize = 2 * rectsBufferSize;
            short[] newArray = new short[newBufferSize];
            rectsToDisplay.CopyTo(newArray, 0);
            rectsToDisplay = newArray;
            rectsBufferSize = newBufferSize;
        }
        int at = numRects * RECTSIZE;
        rectsToDisplay[at] = (short)r;
        rectsToDisplay[at + 1] = (short)g;
        rectsToDisplay[at + 2] = (short)b;
        rectsToDisplay[at + 3] = (short)x;
        rectsToDisplay[at + 4] = (short)y;
        rectsToDisplay[at + 5] = (short)width;
        rectsToDisplay[at + 6] = (short)height;
        ++numRects;
    }

    public void DisplayRectangles()
    {
        // Pretty sure this is single threaded and paint and display loops can't
        // overlap, but put this in to check that.
        if (painting)
        {
            Debug.Log("DisplayRectangles called while in the middle of paint loop");
        }
        if (numRects >= 0)
        {
            displaying = true;
            screenRenderer.StartUpdate();
            for (int ctr = 0; ctr < numRects; ++ctr)
            {
                int at = ctr * RECTSIZE;
                Color color = new Color(rectsToDisplay[at] / 256.0f, rectsToDisplay[at+1] / 256.0f, rectsToDisplay[at + 2] / 256.0f);
                for (int i = 0; i < rectsToDisplay[at + 5]; ++i)
                    for (int j = 0; j < rectsToDisplay[at + 6]; ++j)
                    {
                        int xi = rectsToDisplay[at + 3] + i;
                        int yj = rectsToDisplay[at + 4] + j;
                        if ((xi >= 0) && (xi < DRAW_AREA_WIDTH) && (yj >= 0) && (yj < DRAW_AREA_HEIGHT))
                        {

                            screenRenderer.SetPixel(xi, yj, color);
                        }
                    }
            }
            screenRenderer.EndUpdate();
            numRects = 0;
            displaying = false;
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
