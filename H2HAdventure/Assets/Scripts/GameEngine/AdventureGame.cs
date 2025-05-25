// Adventure: Revisited
// C++ Version Copyright � 2006 Peter Hirschberg
// peter@peterhirschberg.com
// http://peterhirschberg.com
//
// Big thanks to Joel D. Park and others for annotating the original Adventure decompiled assembly code.
// I relied heavily and deliberately on that commented code.
//
// Original Adventure� game Copyright � 1980 ATARI, INC.
// Any trademarks referenced herein are the property of their respective holders.
// 
// Original game written by Warren Robinett. Warren, you rock.
#undef DEBUG_EASTEREGG

using System;
using System.Collections;
using System.Collections.Generic;


namespace GameEngine
{

    public class AdventureGame
    {
        private const int ADVENTURE_SCREEN_WIDTH = Adv.ADVENTURE_SCREEN_BWIDTH;
        private const int ADVENTURE_SCREEN_HEIGHT = Adv.ADVENTURE_SCREEN_BHEIGHT;
        private const int ADVENTURE_OVERSCAN = Adv.ADVENTURE_OVERSCAN_BHEIGHT;
        private const int ADVENTURE_TOTAL_SCREEN_HEIGHT = Adv.ADVENTURE_TOTAL_SCREEN_HEIGHT;
        private const double ADVENTURE_FRAME_PERIOD = Adv.ADVENTURE_FRAME_PERIOD;
        private const int ADVENTURE_MAX_NAME_LENGTH = Adv.ADVENTURE_MAX_NAME_LENGTH;
        private const int MAX_OBJECTS = 32;                      // Should be plenty
        private const int MAX_DISPLAYABLE_OBJECTS = 2;             // The 2600 only has 2 Player (sprite) objects. Accuracy will be compromised if this is changed!

        // finite state machine values
        private const int GAMESTATE_GAMESELECT = 0;
        private const int GAMESTATE_ACTIVE_1 = 1;
        private const int GAMESTATE_ACTIVE_2 = 2;
        private const int GAMESTATE_ACTIVE_3 = 3;
        private const int GAMESTATE_WIN = 4;

        // local game state vars
        private const int GAMEOPTION_PRIVATE_MAGNETS = 1;
        private const int GAMEOPTION_UNLOCK_GATES_FROM_INSIDE = 2;
        private const int GAMEOPTION_NO_HIDE_KEY_IN_CASTLE = 4;



        private AdventureView view;

        private int winFlashTimer;
        private int frameNumber;
        private int winningRoom = -1; // The room number of the castle of the winning player.  -1 if the game is not won yet.
        private bool displayWinningRoom; // At the end of the game we show the player who won.
        private readonly int numPlayers;
        private readonly int gameMapLayout;                               // The board setup.  Level 1 = 0, Levels 2 & 3 = 1, Gauntlet = 2
        private int gameState = GAMESTATE_GAMESELECT;
        private int flashColorHue;
        private int flashColorLum;
        private int displayListIndex;
        private bool joyLeft, joyUp, joyRight, joyDown, joyFire;
        private bool joystickDisabled = false; // Only used when scripting and testing ai

        /** Keep track of when the reset switch is pressed.  This boolean is whether the 
         * reset switch was being pressed at the time we last checked (not to be confused
         * with whether it is being pressed right now). */
        private bool switchReset;
        private bool useMazeGuides;
        private PlayerRecorder playerRecorder;

        private int turnsSinceTimeCheck;
        private readonly int[] missedChecks = { 0, 0, 0 };

        private Sync sync;
        private readonly Transport transport;
        private readonly int thisPlayer;
        private BALL thisBall;

        private readonly OBJECT[] surrounds;

        private Ai.AiNav ai;
        private Ai.AiPlayer[] aiPlayers = { null, null, null };

        private Random randomGen = new Random();

        /** We wait a few seconds between when the game comes up connected and when the game actually starts.
         This is the countdown timer. */
        private int timeToStartGame;

        private static int numDragons = 3;
        private static Dragon[] dragons = new Dragon[0];
        private static Bat bat = null;
        private Portcullis[] ports;


        /** There are a bunch of game modes
         * 0-2 are competitive version of the original three
         * 3-5 are cooperative version of the original three
         * 6 is a role playing cooperative version that Glynn requested       
         * 7 is a version which I call The Gauntlet. */
        private int gameMode;
        private readonly bool isCooperative;

        private readonly ROOM[] roomDefs;
        private Map gameMap;
        private Board gameBoard;
        private PopupMgr popupMgr;
        private int lastPopupTime = int.MinValue;

        // This holds all the switches for whether to turn on or off different game options
        // It is a bitwise or of each game option
        private readonly int gameOptions = GAMEOPTION_NO_HIDE_KEY_IN_CASTLE;

        public AdventureGame(AdventureView inView, int inNumPlayers, int inThisPlayer,
            Transport inTransport, int inGameNum, bool leftDifficultyOn, bool rightDifficultyOn,
            bool inUseHelpPopups, bool inUseMazeGuides, bool raceCompleted, bool[] useAi)
        {
            view = inView;

            numPlayers = inNumPlayers;
            thisPlayer = inThisPlayer;
            gameMode = inGameNum;
            isCooperative = (gameMode > Adv.GAME_MODE_3);
            useMazeGuides = inUseMazeGuides;
            timeToStartGame = 60 * 3;
            frameNumber = 0;
            playerRecorder = new PlayerRecorder(PlayerRecorder.GLOBAL_PLAYER_RECORDER_MODE);

            // The map for game 3 is the same as 2.
            gameMapLayout = (gameMode == Adv.GAME_MODE_GAUNTLET ? Map.MAP_LAYOUT_SMALL :
                (gameMode == Adv.GAME_MODE_1 || gameMode == Adv.GAME_MODE_C_1 ? Map.MAP_LAYOUT_SMALL :
                Map.MAP_LAYOUT_BIG));
            gameMap = new Map(numPlayers, gameMapLayout, isCooperative, useMazeGuides);
            roomDefs = gameMap.roomDefs;
            gameBoard = new Board(gameMap, view);
            if (inUseHelpPopups)
            {
                popupMgr = new PopupMgr(gameBoard);
            }
            EasterEgg.setup(view, gameBoard, raceCompleted);

            surrounds = new OBJECT[numPlayers];
            for (int ctr = 0; ctr < numPlayers; ++ctr)
            {
                surrounds[ctr] = new OBJECT("surround" + ctr, objectGfxSurround, new byte[0], 0, COLOR.ORANGE, OBJECT.RandomizedLocations.FIXED_LOCATION, 0x07);
            }

            Dragon.Difficulty difficulty = (gameMode == Adv.GAME_MODE_1 || gameMode == Adv.GAME_MODE_C_1 ?
                                             (leftDifficultyOn ? Dragon.Difficulty.EASY : Dragon.Difficulty.TRIVIAL) :
                                             (leftDifficultyOn ? Dragon.Difficulty.HARD : Dragon.Difficulty.MODERATE));
            Dragon.setRunFromSword(rightDifficultyOn);

            Dragon.setDifficulty(difficulty);
            if (gameMode == Adv.GAME_MODE_ROLE_PLAY)
            {
                Dragon.setSuperDragons();
            }
            dragons = new Dragon[numDragons];
            dragons[0] = new Dragon("grindle", 0, COLOR.LIMEGREEN, 2, greenDragonMatrix, popupMgr);
            dragons[1] = new Dragon("yorgle", 1, COLOR.YELLOW, 2, yellowDragonMatrix, popupMgr);
            dragons[2] = new Dragon("rhindle", 2, COLOR.RED, 3, redDragonMatrix, popupMgr);
            bat = new Bat(COLOR.BLACK);

            OBJECT goldKey = new OBJECT("gold key", objectGfxKey, new byte[0], 0, COLOR.YELLOW, OBJECT.RandomizedLocations.OUT_IN_OPEN);
            OBJECT copperKey = new OBJECT("coppey key", objectGfxKey, new byte[0], 0, COLOR.COPPER, OBJECT.RandomizedLocations.OUT_IN_OPEN);
            OBJECT jadeKey = new OBJECT("jade key", objectGfxKey, new byte[0], 0, COLOR.JADE, OBJECT.RandomizedLocations.OUT_IN_OPEN);
            OBJECT whiteKey = new OBJECT("white key", objectGfxKey, new byte[0], 0, COLOR.WHITE);
            OBJECT blackKey = new OBJECT("black key", objectGfxKey, new byte[0], 0, COLOR.BLACK);
            OBJECT[] crystalKeys = new OBJECT[3];
            for (int ctr = 0; ctr < 3; ++ctr)
            {
                crystalKeys[ctr] = new OBJECT("crystal key", objectGfxKey, new byte[0], 0, COLOR.CRYSTAL, OBJECT.RandomizedLocations.FIXED_LOCATION);
                crystalKeys[ctr].setPrivateToPlayer(ctr);
            }

            ports = new Portcullis[6];
            ports[0] = new Portcullis("gold gate", Map.GOLD_CASTLE, gameMap.getRoom(Map.GOLD_FOYER), Ai.NavZone.GOLD_CASTLE, goldKey); // Gold
            ports[1] = new Portcullis("white gate", Map.WHITE_CASTLE, gameMap.getRoom(Map.RED_MAZE_1), Ai.NavZone.WHITE_CASTLE_1, whiteKey); // White
            addAllRoomsToPort(ports[1], Map.RED_MAZE_3, Map.RED_MAZE_1);
            ports[2] = new Portcullis("black gate", Map.BLACK_CASTLE, gameMap.getRoom(Map.BLACK_FOYER), Ai.NavZone.BLACK_CASTLE, blackKey); // Black
            addAllRoomsToPort(ports[2], Map.BLACK_MAZE_1, Map.BLACK_MAZE_ENTRY);
            ports[2].addRoom(gameMap.getRoom(Map.BLACK_FOYER));
            ports[2].addRoom(gameMap.getRoom(Map.BLACK_INNERMOST_ROOM));
            ports[3] = new CrystalPortcullis(gameMap.getRoom(Map.CRYSTAL_FOYER), crystalKeys);
            ports[4] = new Portcullis("copper gate", Map.COPPER_CASTLE, gameMap.getRoom(Map.COPPER_FOYER), Ai.NavZone.COPPER_CASTLE, copperKey);
            ports[5] = new Portcullis("jade gate", Map.JADE_CASTLE, gameMap.getRoom(Map.JADE_FOYER), Ai.NavZone.JADE_CASTLE, jadeKey);
            gameMap.addCastles(ports);


            // Setup the number.  Unlike other objects we need to position the number immediately.
            OBJECT number = new OBJECT("number", objectGfxNum, numberStates, 0, COLOR.LIMEGREEN, OBJECT.RandomizedLocations.FIXED_LOCATION);
            gameBoard.addObject(Board.OBJECT_NUMBER, number);
            number.init(Map.NUMBER_ROOM, 0x50, 0x40);

            // Setup the rest of the objects
            gameBoard.addObject(Board.OBJECT_YELLOW_PORT, ports[0]);
            gameBoard.addObject(Board.OBJECT_COPPER_PORT, ports[4]);
            gameBoard.addObject(Board.OBJECT_JADE_PORT, ports[5]);
            gameBoard.addObject(Board.OBJECT_WHITE_PORT, ports[1]);
            gameBoard.addObject(Board.OBJECT_BLACK_PORT, ports[2]);
            gameBoard.addObject(Board.OBJECT_CRYSTAL_PORT, ports[3]);
            gameBoard.addObject(Board.OBJECT_NAME, new OBJECT("easter egg message", objectGfxAuthor, new byte[0], 0, COLOR.FLASH, OBJECT.RandomizedLocations.FIXED_LOCATION));
            gameBoard.addObject(Board.OBJECT_REDDRAGON, dragons[2]);
            gameBoard.addObject(Board.OBJECT_YELLOWDRAGON, dragons[1]);
            gameBoard.addObject(Board.OBJECT_GREENDRAGON, dragons[0]);
            gameBoard.addObject(Board.OBJECT_SWORD, new OBJECT("sword", objectGfxSword, new byte[0], 0, COLOR.YELLOW));
            gameBoard.addObject(Board.OBJECT_BRIDGE, new Bridge());
            gameBoard.addObject(Board.OBJECT_YELLOWKEY, goldKey);
            gameBoard.addObject(Board.OBJECT_COPPERKEY, copperKey);
            gameBoard.addObject(Board.OBJECT_JADEKEY, jadeKey);
            gameBoard.addObject(Board.OBJECT_WHITEKEY, whiteKey);
            gameBoard.addObject(Board.OBJECT_BLACKKEY, blackKey);
            gameBoard.addObject(Board.OBJECT_CRYSTALKEY1, crystalKeys[0]);
            gameBoard.addObject(Board.OBJECT_CRYSTALKEY2, crystalKeys[1]);
            gameBoard.addObject(Board.OBJECT_CRYSTALKEY3, crystalKeys[2]);
            gameBoard.addObject(Board.OBJECT_BAT, bat);
            gameBoard.addObject(Board.OBJECT_DOT, new OBJECT("dot", objectGfxDot, new byte[0], 0, COLOR.LTGRAY, OBJECT.RandomizedLocations.FIXED_LOCATION));
            gameBoard.addObject(Board.OBJECT_CHALISE, new OBJECT("chalise", objectGfxChallise, new byte[0], 0, COLOR.FLASH));
            gameBoard.addObject(Board.OBJECT_EASTEREGG, new OBJECT("easteregg", objectGfxEasterEgg, new byte[0], 0, COLOR.FLASH,
                                                             OBJECT.RandomizedLocations.OPEN_OR_IN_CASTLE, 0x03));
            gameBoard.addObject(Board.OBJECT_MAGNET, new Magnet());

            // Setup the players
            bool useAltIcons = (gameMode == Adv.GAME_MODE_ROLE_PLAY);
            gameBoard.addPlayer(new BALL(0, ports[0], useAltIcons), thisPlayer == 0);
            Portcullis p2Home = (isCooperative ? ports[0] : ports[4]);
            gameBoard.addPlayer(new BALL(1, p2Home, useAltIcons), thisPlayer == 1);
            if (numPlayers > 2)
            {
                Portcullis p3Home = (isCooperative ? ports[0] : ports[5]);
                gameBoard.addPlayer(new BALL(2, p3Home, useAltIcons), thisPlayer == 2);
            }
            thisBall = gameBoard.getPlayer(thisPlayer);
            bool willUseAi = useAi[0] || useAi[1] || useAi[2];
            if (willUseAi)
            {
                ai = new Ai.AiNav(gameMap);
            }
            for (int ctr = 0; ctr < numPlayers; ++ctr)
            {
                if (useAi[ctr])
                {
                    aiPlayers[ctr] = new Ai.AiPlayer(ai, gameBoard, ctr);
                }
            }
            joystickDisabled = joystickDisabled && useAi[thisPlayer];

            if (gameMode == Adv.GAME_MODE_ROLE_PLAY)
            {
                setupRolePlay();
            }

            // Setup the transport
            transport = inTransport;
            sync = new Sync(numPlayers, thisPlayer, transport);

            // Need to have the transport setup before we setup the objects,
            // because we may be broadcasting randomized locations to other machines
            SetupRoomObjects();

            ResetPlayers();

            if (popupMgr != null)
            {
                popupMgr.SetupPopups();
            }
        }

        public void PrintDisplay(int thisPlayerRoom)
        {
            // If we are playing back and episode, we may track a different player
            if ((PlayerRecorder.GLOBAL_PLAYER_RECORDER_MODE == PlayerRecorder.Modes.PLAYBACK) &&
                (PlayerRecorder.PLAYBACK_PLAYER_VIEW >= 0))
            {
                thisPlayerRoom = gameBoard.getPlayer(PlayerRecorder.PLAYBACK_PLAYER_VIEW).room;
            }
            // get the playfield data
            int displayedRoom = (displayWinningRoom ? winningRoom : thisPlayerRoom);

            ROOM currentRoom = roomDefs[displayedRoom];

            // get the playfield color
            COLOR color = ((gameState == GAMESTATE_WIN) && (winFlashTimer > 0)) ? GetFlashColor() : COLOR.table(currentRoom.color);
            COLOR colorBackground = COLOR.table(COLOR.LTGRAY);

            // Fill the entire backbuffer with the playfield background color before we draw anything else
            view.Platform_PaintPixel(colorBackground.r, colorBackground.g, colorBackground.b, 0, 0, ADVENTURE_SCREEN_WIDTH, ADVENTURE_TOTAL_SCREEN_HEIGHT);

            // paint the surround under the playfield layer
            for (int ctr = 0; ctr < numPlayers; ++ctr)
            {
                if ((surrounds[ctr].room == displayedRoom) && (surrounds[ctr].state == 0))
                {
                    DrawObject(surrounds[ctr]);
                }
            }

            // each cell is 8 x 32
            const int cell_width = Map.WALL_WIDTH;
            const int cell_height = Map.WALL_HEIGHT;

            // draw the playfield
            for (int wy = 0; wy < Map.MAX_WALL_Y; ++wy)
            {
                for (int wx = 0; wx < Map.MAX_WALL_X; ++wx)
                {
                    if (currentRoom.walls[wx, wy])
                    {
                        view.Platform_PaintPixel(color.r, color.g, color.b, wx * cell_width, wy * cell_height, cell_width, cell_height);
                    }
                }
            }

            // Draw the guide
            if (useMazeGuides)
            {
                Guide guide = gameMap.Guide;
                IEnumerator<Guide.Line> lines = guide.GetLines(displayedRoom);
                while (lines.MoveNext())
                {
                    Guide.Line nextLine = lines.Current;
                    COLOR lineColor = nextLine.Color;
                    Guide.Point first = null;
                    IEnumerator<Guide.Point> points = nextLine.GetEnumerator();
                    int ctr = 0;
                    while (points.MoveNext())
                    {
                        Guide.Point next = points.Current;
                        if (first != null)
                        {
                            int x = (first.x < next.x ? first.x : next.x);
                            int y = (first.y < next.y ? first.y : next.y);
                            int width = (first.x < next.x ? next.x - first.x : first.x - next.x);
                            int height = (first.y < next.y ? next.y - first.y : first.y - next.y);
                            view.Platform_PaintPixel(lineColor.r, lineColor.g, lineColor.b, x - 1, y - 1, width + 2, height + 2);
                        }
                        first = next;
                        ++ctr;
                    }
                }
                IEnumerator<Guide.Marker> markers = guide.GetMarkers(displayedRoom);
                while (markers.MoveNext())
                {
                    Guide.Marker nextMarker = markers.Current;
                    DrawGraphic(nextMarker.X, nextMarker.Y, nextMarker.Gfx, nextMarker.Color, 1);
                }
            }

            //
            // Draw the balls
            //
            COLOR defaultColor = COLOR.table(roomDefs[displayedRoom].color);

            for (int i = 0; i < numPlayers; ++i)
            {
                BALL player = gameBoard.getPlayer(i);
                if (player.displayedRoom == displayedRoom)
                {
                    COLOR ballColor = (player.isGlowing() ? GetFlashColor() : defaultColor);
                    DrawBall(player, ballColor);
                }
            }

            //
            // Draw any objects in the room
            //
            ClearDrawnObjects();
            MarkDrawnObjects(displayedRoom);
            for (int ctr=0; ctr<numPlayers; ++ctr)
            {
                BALL nextBall = gameBoard.getPlayer(ctr);
                if (nextBall.displayedRoom != displayedRoom)
                {
                    MarkDrawnObjects(nextBall.displayedRoom);
                }
            }
            DrawObjectsAndThinWalls(displayedRoom);
        }

        COLOR GetFlashColor()
        {
            float r = 0, g = 0, b = 0;
            float h = flashColorHue / (360.0f / 3);
            if (h < 1)
            {
                r = h * 255;
                g = 0;
                b = (1 - h) * 255;
            }
            else if (h < 2)
            {
                h -= 1;
                r = (1 - h) * 255;
                g = h * 255;
                b = 0;
            }
            else
            {
                h -= 2;
                r = 0;
                g = (1 - h) * 255;
                b = h * 255;
            }

            int color_r = (flashColorLum > r ? flashColorLum : (int)r);
            int color_g = (flashColorLum > r ? flashColorLum : (int)g);
            int color_b = (flashColorLum > r ? flashColorLum : (int)b);

            return new COLOR(color_r, color_g, color_b);
        }

        private void AdvanceFlashColor()
        {
            flashColorHue += 2;
            if (flashColorHue >= 360)
                flashColorHue -= 360;

            flashColorLum += 11;
            if (flashColorLum > 200)
                flashColorLum = 0;

        }

        private void setupRolePlay()
        {
            gameBoard.getObject(Board.OBJECT_SWORD).setPrivateToPlayer(0);
            gameBoard.getObject(Board.OBJECT_YELLOWKEY).setPrivateToPlayer(1);
            gameBoard.getObject(Board.OBJECT_WHITEKEY).setPrivateToPlayer(1);
            gameBoard.getObject(Board.OBJECT_BLACKKEY).setPrivateToPlayer(1);
            gameBoard.getObject(Board.OBJECT_MAGNET).setPrivateToPlayer(1);
            gameBoard.getObject(Board.OBJECT_BRIDGE).setPrivateToPlayer(2);
            gameBoard.getObject(Board.OBJECT_CHALISE).setPrivateToPlayer(2);
            gameBoard.getObject(Board.OBJECT_DOT).setPrivateToPlayer(2);
        }


        void addAllRoomsToPort(Portcullis port, int firstRoom, int lastRoom)
        {
            for (int nextKey = firstRoom; nextKey <= lastRoom; ++nextKey)
            {
                ROOM nextRoom = gameMap.getRoom(nextKey);
                port.addRoom(nextRoom);
            }
        }

        private void ResetPlayers()
        {
            for (int ctr = 0; ctr < gameBoard.getNumPlayers(); ++ctr)
            {
                ResetPlayer(gameBoard.getPlayer(ctr));
            }
        }

        void ResetPlayer(BALL ball)
        {
            ball.room = ball.homeGate.room;    // Put us at our home castle
            ball.previousRoom = ball.room;
            ball.displayedRoom = ball.room;
            ball.x = Board.STARTING_X;
            ball.y = Board.STARTING_Y;
            ball.previousX = ball.x;
            ball.previousY = ball.y;
            ball.linkedObject = Board.OBJECT_NONE;  // Not carrying anything
            ball.setGlowing(false);

            // Make the bat want something right away
            // I guess the bat is reset just like the dragons are reset.
            if (bat.exists())
            {
                bat.lookForNewObject();
            }

            // Bring the dragons back to life
            for (int ctr = 0; ctr < numDragons; ++ctr)
            {
                Dragon dragon = dragons[ctr];
                if (dragon.state == Dragon.DEAD)
                {
                    dragon.respawn();
                }
                else if (dragon.eaten == ball)
                {
                    dragon.state = Dragon.STALKING;
                    dragon.eaten = null;
                }
            }
        }

        void SyncWithOthers()
        {
            sync.PullLatestMessages();

            // Check for any setup messages first.
            handleSetupMessages();

            // Move all the other players
            RemoteBallMovement();
            RemotePickupPutdown();

            // move the dragons
            SyncDragons();


            // Move the bat
            RemoteAction batAction = sync.GetNextBatAction();
            while ((batAction != null) && bat.exists())
            {
                bat.handleAction(batAction, thisBall);
                batAction = sync.GetNextBatAction();
            }

            // Handle any remote changes to the portal.
            PortcullisStateAction nextAction = sync.GetNextPortcullisAction();
            while (nextAction != null)
            {
                Portcullis port = (Portcullis)gameBoard.getObject(nextAction.portPkey);
                port.setState(nextAction.newState, nextAction.allowsEntry);
                nextAction = sync.GetNextPortcullisAction();
            }

            // Do reset after dragon and move actions.
            PlayerResetAction otherReset = sync.GetNextResetAction();
            while (otherReset != null)
            {
                ResetPlayer(gameBoard.getPlayer(otherReset.sender));
                if ((popupMgr != null) && popupMgr.needPopup[PopupMgr.RESPAWNED])
                {
                    popupMgr.ShowPopup(new Popup("dragon",
                        "All dead dragons have come back to life because another player respawned.",
                        popupMgr, PopupMgr.RESPAWNED));
                }
                otherReset = sync.GetNextResetAction();
            }

            // Handle won games last.
            PlayerWinAction lost = sync.GetGameWon();
            if (lost != null)
            {
                WinGame(lost.sender, lost.winInRoom);
                lost = null;
            }


        }

        public void Adventure_Run()
        {
            ++frameNumber;
            SyncWithOthers();
            checkPlayers();

            // read the console switches every frame
            bool reset = false;  // Whether the reset switch is pressed right now
            if (playerRecorder.Mode == PlayerRecorder.Modes.PLAYBACK)
            {
                playerRecorder.playSwitches(frameNumber, ref reset);
            } else
            {
                view.Platform_ReadConsoleSwitches(ref reset);
                playerRecorder.recordSwitches(frameNumber, reset);
            }

            // If joystick is disabled and we hit the reset switch we don't treat it as a reset but as
            // a enable the joystick.  The next time you hit the reset switch it will work as a reset.
            if (joystickDisabled && switchReset && !reset)
            {
                joystickDisabled = false;
                switchReset = false;
            }

            // Reset switch
            handleAiReset();

            // If the reset switch was being held down but is now released, trigger a reset.
            if (switchReset && !reset)
            {
                handleResetSwitch();
            }
            else
            {
                // Is the game active?
                if (gameState == GAMESTATE_GAMESELECT)
                {
                    --timeToStartGame;
                    if (timeToStartGame <= 0)
                    {
                        gameState = GAMESTATE_ACTIVE_1;
                        ResetPlayers();
                        view.Platform_GameChange(GAME_CHANGES.GAME_STARTED);
                        if (popupMgr != null)
                        {
                            popupMgr.StartedGameShowPopups();
                        }
                    }
                    else
                    {
                        int displayNum = timeToStartGame / 60;
                        gameBoard[Board.OBJECT_NUMBER].state = displayNum;

                        // Display the pre-game room
                        PrintDisplay(0);
                    }
                }
                else if ((gameState == GAMESTATE_ACTIVE_1) || (gameState == GAMESTATE_ACTIVE_2) || (gameState == GAMESTATE_ACTIVE_3))
                {
                    // Has someone won the game.
                    int winner = checkWonGame();
                    if (winner >= 0)
                    {
                        WinGame(winner, gameBoard.getPlayer(winner).room);
                    }
                    else if (EasterEgg.isGauntletTimeUp(frameNumber))
                    {
                        EasterEgg.endGauntlet();
                        gameState = GAMESTATE_WIN;
                        view.Platform_GameChange(GAME_CHANGES.GAME_ENDED);
                        winningRoom = thisBall.displayedRoom;
                    }
                    else
                    {
                        // Read joystick
                        if (playerRecorder.Mode == PlayerRecorder.Modes.PLAYBACK)
                        {
                            playerRecorder.playJoystick(frameNumber, ref joyLeft, ref joyUp, ref joyRight, ref joyDown, ref joyFire);
                        } else
                        {
                            view.Platform_ReadJoystick(ref joyLeft, ref joyUp, ref joyRight, ref joyDown, ref joyFire);
                            playerRecorder.recordJoystick(frameNumber, joyLeft, joyUp, joyRight, joyDown, joyFire);
                        }


                        if (EasterEgg.shouldStartGauntlet(frameNumber))
                        {
#if DEBUG_EASTEREGG
                            EasterEgg.startGauntlet(true);
#else
                            EasterEgg.startGauntlet(false);
#endif
                            gameMode = Adv.GAME_MODE_GAUNTLET;
                        }

                        if (gameState == GAMESTATE_ACTIVE_1)
                        {
                            // Move balls
                            bool broadcastMovement = ThisBallMovement();
                            for (int i = 0; i < numPlayers; ++i)
                            {
                                BallMovement(gameBoard.getPlayer(i), (i == thisPlayer) && broadcastMovement);
                            }

                            // Move the carried object
                            MoveCarriedObjects();

                            // Collision check the balls in their new coordinates against walls and objects
                            for (int i = 0; i < numPlayers; ++i)
                            {
                                BALL nextBall = gameBoard.getPlayer(i);
                                CollisionCheckBallWithEverything(nextBall, nextBall.room, false);
                            }

                            // Setup the room and object
                            PrintDisplay(thisBall.displayedRoom);

                            ++gameState;
                        }
                        else if (gameState == GAMESTATE_ACTIVE_2)
                        {
                            // Deal with object pickup and putdown
                            PickupPutdown();

                            for (int i = 0; i < numPlayers; ++i)
                            {
                                ReactToCollisionX(gameBoard.getPlayer(i));
                            }

                            // Increment the last object drawn
                            ++displayListIndex;

                            // deal with invisible surround moving
                            Surround();

                            // Move and deal with bat
                            if (bat.exists())
                            {
                                bat.moveOneTurn(sync, thisBall);
                            }

                            // Move and deal with portcullises
                            Portals();

                            // Display the room and objects
                            PrintDisplay(thisBall.displayedRoom);

                            ++gameState;
                        }
                        else if (gameState == GAMESTATE_ACTIVE_3)
                        {
                            // Move and deal with the dragons
                            for (int dragonCtr = 0; dragonCtr < numDragons; ++dragonCtr)
                            {
                                Dragon dragon = dragons[dragonCtr];
                                RemoteAction dragonAction = dragon.move();
                                if (dragonAction != null)
                                {
                                    sync.BroadcastAction(dragonAction);
                                }
                                // In gauntlet mode, getting eaten immediately triggers a reset.
                                if ((gameMode == Adv.GAME_MODE_GAUNTLET) && (dragon.state == Dragon.EATEN) && (dragon.eaten != null))
                                {
                                    if (!gameBoard.isPlayerRemote(dragon.eaten.playerNum))
                                    {
                                        ResetPlayer(dragon.eaten);
                                        if (dragon.eaten.playerNum == thisPlayer)
                                        {
                                            // Broadcast to everyone else
                                            PlayerResetAction action = new PlayerResetAction();
                                            sync.BroadcastAction(action);
                                        }
                                    }
                                }
                            }

                            for (int i = 0; i < numPlayers; ++i)
                            {
                                ReactToCollisionY(gameBoard.getPlayer(i));
                            }


                            // Deal with the magnet
                            Magnet();

                            // Display the room and objects
                            PrintDisplay(thisBall.displayedRoom);

                            gameState = GAMESTATE_ACTIVE_1;
                        }
                    }
                }
                else if (gameState == GAMESTATE_WIN)
                {
                    // We keep the display pointed at your current room while we make the
                    // whole board flash, but once the flash is done, we point the display
                    // at the winning castle.
                    if (winFlashTimer > 0)
                    {
                        --winFlashTimer;
                    }
                    else
                    {
                        displayWinningRoom = true;
                    }

                    // Increment the last object drawn
                    if (frameNumber % 3 == 0)
                    {
                        ++displayListIndex;
                    }

                    // Display the room and objects
                    PrintDisplay(thisBall.displayedRoom);
                }
            }

            // Check for popups (only ten times a second)
            if ((popupMgr != null) && (frameNumber % 6 == 0))
            {
                // Once a second we check for timed popups
                if (frameNumber % 60 == 0)
                {
                    popupMgr.CheckTimedPopups(frameNumber);
                }
                if (popupMgr.HasPopups &&
                     (frameNumber > lastPopupTime + (PopupMgr.MIN_SECONDS_BETWEEN_POPUPS * 60)))
                {
                    Popup popup = popupMgr.GetNextPopup();
                    if (popup != null)
                    {
                        view.Platform_PopupHelp(popup.Message, popup.ImageName);
                        lastPopupTime = frameNumber;
                    }
                }
            }

            switchReset = reset;
            AdvanceFlashColor();
        }

        private void SetupRoomObjects()
        {
            // Init all objects
            Board.ObjIter iter = gameBoard.getObjects();
            while (iter.hasNext())
            {
                OBJECT objct = iter.next();
                objct.setMovementX(0);
                objct.setMovementY(0);
            }


            // Set to no carried objects
            for (int ctr = 0; ctr < numDragons; ++ctr)
            {
                dragons[ctr].eaten = null;
            }
            bat.linkedObject = Board.OBJECT_NONE;


            // Read the object initialization table for the current game level
            int[,] p = new int[0, 0];
            if ((gameMode == Adv.GAME_MODE_1) || (gameMode == Adv.GAME_MODE_C_1))
            {
                p = game1Objects;
            }
            else if (gameMode == Adv.GAME_MODE_GAUNTLET)
            {
                p = gameGauntletObjects;
            }
            else
            {
                p = game2Objects;
            }

            for (int ctr = 0; ctr < p.GetLength(0); ++ctr)
            {
                int objct = p[ctr, 0];
                int room = p[ctr, 1];
                int oxpos = p[ctr, 2];
                int oypos = p[ctr, 3];
                int state = p[ctr, 4];
                int movementOX = p[ctr, 5];
                int movementOY = p[ctr, 6];

                OBJECT toInit = gameBoard[objct];
                toInit.init(room, oxpos, oypos, state, movementOX, movementOY);
            }

            // Hide the jade if only 2 player and both new keys if cooperative
            if ((numPlayers <= 2) || (isCooperative))
            {
                gameBoard[Board.OBJECT_JADEKEY].setExists(false);
                gameBoard[Board.OBJECT_JADEKEY].room = Map.NUMBER_ROOM;
                gameBoard[Board.OBJECT_JADEKEY].randomPlacement = OBJECT.RandomizedLocations.FIXED_LOCATION;
            }
            if (isCooperative)
            {
                gameBoard[Board.OBJECT_COPPERKEY].setExists(false);
                gameBoard[Board.OBJECT_COPPERKEY].randomPlacement = OBJECT.RandomizedLocations.FIXED_LOCATION;
            }

            // Put objects in random rooms for level 3.
            bool gameRandomized = ((gameMode == Adv.GAME_MODE_3) ||
              (gameMode == Adv.GAME_MODE_C_3) ||
              (gameMode == Adv.GAME_MODE_ROLE_PLAY));
            // In a multi-player game, only first player does this and then broadcasts to other players.
            if ((ai == null) && (thisPlayer != 0)) {
                gameRandomized = false;
            }
            if (gameRandomized)
            {
                randomizeRoomObjects();
            }

            // Open the gates if running the gauntlet
            if (gameMode == Adv.GAME_MODE_GAUNTLET)
            {
                Portcullis blackPort = (Portcullis)gameBoard[Board.OBJECT_BLACK_PORT];
                blackPort.setState(Portcullis.OPEN_STATE, true);
                Portcullis goldPort = (Portcullis)gameBoard[Board.OBJECT_YELLOW_PORT];
                goldPort.setState(Portcullis.OPEN_STATE, true);
            }
        }

        /**
         * Puts all the objects in random locations.
         * This follows a different algorithm than the original game.
         * We don't use the original algorithm because
         * 1) it had a vulnerability that the gold key could be in the black 
         * castle while the black key was in the gold castle
         * 2) with three times the number of home castles the algorithm was three
         * times more likely to be deadlocked
         */
        void randomizeRoomObjects()
        {
            int numRooms = Map.getNumRooms();
            Portcullis blackCastle = (Portcullis)gameBoard[Board.OBJECT_BLACK_PORT];
            Portcullis whiteCastle = (Portcullis)gameBoard[Board.OBJECT_WHITE_PORT];

            // Run through all the objects in the game.  The ones that shouldn't be
            // randomized will have their random location flag turned off.
            int numObjects = Board.NUM_OBJECTS;
            for (int objCtr = 0; objCtr < numObjects; ++objCtr)
            {
                OBJECT nextObj = gameBoard.getObject(objCtr);
                if (nextObj.randomPlacement != OBJECT.RandomizedLocations.FIXED_LOCATION)
                {
                    bool ok = false;
                    while (!ok)
                    {
                        int randomKey = randomGen.Next(Map.NUM_ROOMS);
                        ROOM randomRoom = gameMap.getRoom(randomKey);

                        // Make sure the object isn't put in a hidden room
                        ok = randomRoom.visibility != ROOM.RandomVisibility.HIDDEN;

                        // if the object can only be in the open, make sure that it's put in the open.
                        ok = ok && ((nextObj.randomPlacement != OBJECT.RandomizedLocations.OUT_IN_OPEN) || (randomRoom.visibility == ROOM.RandomVisibility.OPEN));

                        // Make sure chalice is in a castle
                        if (ok && (objCtr == Board.OBJECT_CHALISE))
                        {
                            ok = (blackCastle.containsRoom(randomKey) || whiteCastle.containsRoom(randomKey));
                        }

                        // Make sure white key not in white castle.
                        if (ok && (objCtr == Board.OBJECT_WHITEKEY))
                        {
                            ok = ok && !whiteCastle.containsRoom(randomKey);
                        }

                        // Make sure white and black key not cyclical
                        // We happen to know that the white key is placed first, so set the black.
                        if (ok && (objCtr == Board.OBJECT_BLACKKEY))
                        {
                            if (blackCastle.containsRoom(gameBoard[Board.OBJECT_WHITEKEY].room))
                            {
                                ok = !whiteCastle.containsRoom(randomKey);
                            }
                            // Also make sure black key not in black castle
                            ok = ok && !blackCastle.containsRoom(randomKey);
                        }

                        // There are parts of the white castle not accessible without the bridge, but the bat
                        // can get stuff out of there.  So make sure, if the black key is in the white castle
                        // that the bat is not in the black castle.
                        if (ok && (objCtr == Board.OBJECT_BAT))
                        {
                            if (whiteCastle.containsRoom(gameBoard[Board.OBJECT_BLACKKEY].room))
                            {
                                ok = !blackCastle.containsRoom(randomKey);
                            }
                        }

                        if (ok)
                        {
                            nextObj.room = randomKey;
                            ObjectMoveAction action = new ObjectMoveAction(objCtr, randomKey, nextObj.x, nextObj.y);
                            sync.BroadcastAction(action);
                        }
                    }
                }
            }
        }

        /**
         * If this was a randomized game, look for another game to define where the objects are placed.
         */
        void handleSetupMessages()
        {
            ObjectMoveAction nextMsg = sync.GetNextSetupAction();
            while (nextMsg != null)
            {
                OBJECT toSetup = gameBoard[nextMsg.objct];
                toSetup.room = nextMsg.room;
                toSetup.x = nextMsg.x;
                toSetup.y = nextMsg.y;
                nextMsg = sync.GetNextSetupAction();
            }
        }

        void handleResetSwitch()
        {
            // When can't you respawn?  Before the game starts, after it ends and
            // before the Easter Egg race.
            if ((gameState != GAMESTATE_WIN) && (gameState != GAMESTATE_GAMESELECT) && (EasterEgg.eggState != EGG_STATE.DEBRIEF))
            {
                // In the role playing version, the cleric has to be on the screen
                // to reset
                if ((gameMode != Adv.GAME_MODE_ROLE_PLAY) || (gameBoard.getPlayer(2).room == thisBall.room))
                {
                    ResetPlayer(thisBall);
                    if ((popupMgr != null) && popupMgr.needPopup[PopupMgr.RESPAWNED])
                    {
                        popupMgr.ShowPopup(new Popup("",
                            "Now that you respawned, all dead dragons have come back to life",
                            popupMgr, PopupMgr.RESPAWNED));
                    }
                    // Broadcast to everyone else
                    PlayerResetAction action = new PlayerResetAction();
                    sync.BroadcastAction(action);
                }
            }
        }

        private void handleAiReset()
        {
            for(int ctr=0; ctr<numPlayers; ++ctr)
            {
                if (aiPlayers[ctr] != null)
                {
                    if (aiPlayers[ctr].shouldReset())
                    {
                        ResetPlayer(gameBoard.getPlayer(ctr));
                        aiPlayers[ctr].resetPlayer();
                    }
                }
            }
        }

        /**
         * Checks if a player has gotten the chalise to their home castle and won the game, or, if
         * this is the gauntlet, if the player has reached the gold castle.  Doesn't check remote players.
         * @return number of the player who won, or -1 if no one won
         */
        int checkWonGame()
        {
            int winningPlayer = -1;
            for (int ctr = 0; (ctr < numPlayers) && (winningPlayer < 0); ++ctr)
            {
                // We don't calculate winning for remote players.  We rely on them
                // sending a remote win message.
                if (!gameBoard.isPlayerRemote(ctr))
                {
                    bool won = false;
                    BALL nextBall = gameBoard.getPlayer(ctr);
                    if (gameMode == Adv.GAME_MODE_GAUNTLET)
                    {
                        won = (nextBall.isGlowing() && (nextBall.room == nextBall.homeGate.insideRoom));
                    }
                    else
                    {
                        // Player MUST be holding the chalise to win (or holding the bat holding the chalise).
                        // Another player can't win for you.
                        if ((nextBall.linkedObject == Board.OBJECT_CHALISE) ||
                            ((nextBall.linkedObject == Board.OBJECT_BAT) && (bat.linkedObject == Board.OBJECT_CHALISE)))
                        {
                            // Player either has to bring the chalise into the castle or touch the chalise to the gate
                            if (gameBoard[Board.OBJECT_CHALISE].room == nextBall.homeGate.insideRoom)
                            {
                                won = true;
                            }
                            else if (nextBall.room == nextBall.homeGate.room) {
                                won = (nextBall.homeGate.state == Portcullis.OPEN_STATE) &&
                                    gameBoard.CollisionCheckObjectObject(nextBall.homeGate, gameBoard[Board.OBJECT_CHALISE]);
                            }
                        }
                    }
                    winningPlayer = (won ? ctr : -1);
                }
            }
            return winningPlayer;
        }

        /**
         * Handle the mechanics when a game is won.
         * @param winningPlayer the player who won
         * @param winRoom the room where the game was won
         */
        void WinGame(int winningPlayer, int winRoom)
        {

            if (popupMgr != null)
            {
                if (winningPlayer == thisPlayer)
                {
                    popupMgr.ShowPopupNow(new Popup("chalice",
                        "You won!!!!  Congratulations.", popupMgr));
                } else
                {
                    popupMgr.ShowPopupNow(new Popup("chalice",
                        "Oh no! You lost.  Player " + (winningPlayer + 1) +
                        " has won the game.", popupMgr));
                }
            }

            // Go to won state
            gameState = GAMESTATE_WIN;
            playerRecorder.close();
            winFlashTimer = 0xff;
            winningRoom = winRoom;
            view.Platform_GameChange(GAME_CHANGES.GAME_ENDED);

            // Play the sound
            view.Platform_MakeSound(SOUND.WON, MAX.VOLUME);

            if (winningPlayer == thisPlayer)
            {
                PlayerWinAction won = new PlayerWinAction(winningRoom);
                sync.BroadcastAction(won);
                // Report back to the server on competitive games
                if (!isCooperative)
                {
                    view.Platform_ReportToServer(AdventureReports.WON_GAME);
                }

                if (EasterEgg.eggState == EGG_STATE.IN_GAUNTLET)
                {
                    EasterEgg.winEgg();
                }

            }

        }

        void ReactToCollisionX(BALL ball)
        {
            if (ball.hit)
            {
                if (ball.velx != 0)
                {
                    if ((ball.hitObject > Board.OBJECT_NONE) && (ball.hitObject == ball.linkedObject))
                    {
                        ball.linkedObjectX += ball.velx;
                        if (ball.playerNum == thisPlayer)
                        {
                            // If this is adjusting how the current player holds an object,
                            // we broadcast to other players as a pickup action
                            PlayerPickupAction action = new PlayerPickupAction(ball.hitObject,
                                ball.linkedObjectX, ball.linkedObjectY, Board.OBJECT_NONE, 0, 0, 0);
                            sync.BroadcastAction(action);
                        }
                    }

                    if ((ball.room != ball.previousRoom) && (Math.Abs(ball.x - ball.previousX) > Math.Abs(ball.velx)))
                    {
                        // We switched rooms, kick them back
                        ball.room = ball.previousRoom;
                    }
                    ball.x = ball.previousX;
                }
                // Try recompute hit allowing for the bridge.
                CollisionCheckBallWithEverything(ball, ball.room, true);
            }
        }

        void ReactToCollisionY(BALL ball)
        {
            if ((ball.hit) && (ball.vely != 0))
            {
                if ((ball.hitObject > Board.OBJECT_NONE) && (ball.hitObject == ball.linkedObject))
                {
                    ball.linkedObjectY += ball.vely;
                    if (ball.playerNum == thisPlayer)
                    {
                        // If this is adjusting how the current player holds an object,
                        // we broadcast to other players as a pickup action
                        PlayerPickupAction action = new PlayerPickupAction(ball.hitObject,
                            ball.linkedObjectX, ball.linkedObjectY, Board.OBJECT_NONE, 0, 0, 0);
                        sync.BroadcastAction(action);
                    }
                }

                // We put y back to the last y, but if we are moving diagonally, we
                // put x back to the new x value which we had reverted last phase and try again.
                // if new x and old y is still a collision we revert at the beginning of the next phase
                if ((ball.room != ball.previousRoom) && (Math.Abs(ball.y - ball.previousY) > Math.Abs(ball.vely)))
                {
                    // We switched rooms, kick them back
                    ball.room = ball.previousRoom;
                }
                ball.y = ball.previousY;
                ball.x += ball.velx;
                // Need to check if new X takes us to new room (again)
                if (ball.x > Board.RIGHT_EDGE_FOR_BALL)
                {
                    ball.x = Board.LEFT_EDGE_FOR_BALL;
                    ball.room = ball.displayedRoom; // The displayed room hasn't changed
                }
                else if (ball.x < Board.LEFT_EDGE_FOR_BALL)
                {
                    ball.x = Board.RIGHT_EDGE_FOR_BALL;
                    ball.room = ball.displayedRoom;
                }

                CollisionCheckBallWithEverything(ball, ball.displayedRoom, false);
            }
        }

        private bool ThisBallMovement()
        {
            // Read the joystick and translate into a velocity
            int prevVelX = thisBall.velx;
            int prevVelY = thisBall.vely;
            if (!joystickDisabled && aiPlayers[thisPlayer] == null)
            {
                int newVelY = 0;
                if (joyUp)
                {
                    if (!joyDown)
                    {
                        newVelY = BALL.MOVEMENT;
                    }
                }
                else if (joyDown)
                {
                    newVelY = -BALL.MOVEMENT;
                }
                thisBall.vely = newVelY;

                int newVelX = 0;
                if (joyRight)
                {
                    if (!joyLeft)
                    {
                        newVelX = BALL.MOVEMENT;
                    }
                }
                else if (joyLeft)
                {
                    newVelX = -BALL.MOVEMENT;
                }
                thisBall.velx = newVelX;
            }

            bool broadcastMovement = !joystickDisabled &&
                 ((thisBall.velx != prevVelX) || (thisBall.vely != prevVelY));
            return broadcastMovement;
        }

        void BallMovement(BALL ball, bool broadcastMovement)
        {
            bool newRoom = false;
            // If an AI player, compute direction
            if (aiPlayers[ball.playerNum] != null)
            {
                aiPlayers[ball.playerNum].chooseDirection(frameNumber);
            }
            bool eaten = false;
            for (int ctr = 0; ctr < numDragons && !eaten; ++ctr)
            {
                eaten = (dragons[ctr].eaten == ball);
            }

            // Save the last, non-colliding position
            if (ball.hit)
            {
                ball.x = ball.previousX;
                ball.y = ball.previousY;
                ball.room = ball.previousRoom;
            }
            else
            {
                ball.previousX = ball.x;
                ball.previousY = ball.y;
                ball.previousRoom = ball.room;
            }

            ball.hit = eaten;
            ball.hitObject = Board.OBJECT_NONE;

            // Move the ball
            ball.x += ball.velx;
            ball.y += ball.vely;


            // Wrap rooms in Y if necessary
            if (ball.y > Board.TOP_EDGE_FOR_BALL)
            {
                ball.y = Board.BOTTOM_EDGE_FOR_BALL;
                ball.room = roomDefs[ball.room].roomUp;
                newRoom = true;
            }
            else if (ball.y < Board.BOTTOM_EDGE_FOR_BALL)
            {
                // Handle the ball leaving a castle.
                bool canUnlockFromInside = ((gameOptions & GAMEOPTION_UNLOCK_GATES_FROM_INSIDE) != 0);
                bool leftCastle = false;
                for (int portalCtr = 0; !leftCastle && (portalCtr < ports.Length); ++portalCtr)
                {
                    Portcullis port = ports[portalCtr];
                    if (port.exists())
                    {
                        if ((ball.room == port.insideRoom) &&
                            ((port.state != Portcullis.CLOSED_STATE) || canUnlockFromInside))
                        {
                            ball.x = Portcullis.EXIT_X;
                            ball.y = Portcullis.EXIT_Y;

                            ball.previousX = ball.x;
                            ball.previousY = ball.y;

                            ball.room = port.room;
                            ball.previousRoom = ball.room;
                            newRoom = true;
                            // If we were locked in the castle, open the portcullis.
                            if (port.state == Portcullis.CLOSED_STATE && canUnlockFromInside)
                            {
                                port.openFromInside();
                                if (ai != null)
                                {
                                    ai.ConnectPortcullisPlots(port, true);
                                }
                                PortcullisStateAction gateAction = new PortcullisStateAction(port.getPKey(), port.state, port.allowsEntry);
                                sync.BroadcastAction(gateAction);

                            }
                            leftCastle = true;
                        }
                    }
                }

                if (!leftCastle)
                {
                    // Wrap the ball to the top of the next screen
                    ball.y = Board.TOP_EDGE_FOR_BALL;
                    ball.room = roomDefs[ball.room].roomDown;
                    newRoom = true;
                }
            }

            if (ball.x > Board.RIGHT_EDGE_FOR_BALL)
            {
                // Can't diagonally switch rooms.  If trying, only allow changing rooms vertically
                if (ball.room != ball.previousRoom)
                {
                    ball.x = ball.previousX;
                    ball.velx = 0;
                }
                else
                {
                    // Wrap the ball to the left side of the next screen
                    ball.x = Board.LEFT_EDGE_FOR_BALL;

                    int rightRoom = roomDefs[ball.room].roomRight;
                    if (ball.room == Map.MAIN_HALL_RIGHT)
                    {
                        // Figure out the room to the right (which might be the secret room)
                        if (gameBoard[Board.OBJECT_DOT].room == Map.MAIN_HALL_RIGHT ||
                        EasterEgg.eggState == EGG_STATE.IN_GAUNTLET)
                        {
                            rightRoom = Map.ROBINETT_ROOM;
                        }
                    }
                    ball.room = rightRoom;
                    newRoom = true;
                }
            }
            else if (ball.x < Board.LEFT_EDGE_FOR_BALL)
            {
                // Can't diagonally switch rooms.  If trying, only allow changing rooms vertically
                if (ball.room != ball.previousRoom)
                {
                    ball.x = ball.previousX;
                    ball.velx = 0;
                }
                else
                {
                    ball.x = Board.RIGHT_EDGE_FOR_BALL;
                    ball.room = roomDefs[ball.room].roomLeft;
                    newRoom = true;
                }
            }

            if (newRoom && (ball.playerNum == thisPlayer))
            {
                if (popupMgr != null)
                {
                    popupMgr.EnteredRoomShowPopups(ball.room);
                }
            }

            if (ball.playerNum == thisPlayer)
            {
                if (ball.room == Map.CRYSTAL_CASTLE)
                {
                    EasterEgg.foundCastle(ball);
                }
                else if (ball.room == Map.ROBINETT_ROOM)
                {
                    EasterEgg.enteredRobinettRoom();
                }
            }

            ball.displayedRoom = ball.room;

            if (broadcastMovement)
            {
                PlayerMoveAction moveAction = new PlayerMoveAction(ball.room, ball.x, ball.y, ball.velx, ball.vely);
                sync.BroadcastAction(moveAction);
            }


        }

        // Check if the ball would be hitting anything (wall, object, ...)
        // ball - the ball to check
        // room - the room in which to check
        // x - the x position to check
        // y - the y position to check
        // allowBridge - if moving vertically, the bridge can allow you to not collide into a wall
        // hitObject - if we hit an object, will set this reference to the object we hit.  If NULL, will not try to set it.
        //
        private void CollisionCheckBallWithEverything(BALL ball, int checkRoom, bool allowBridge)
        {
            int hitObject = CollisionCheckBallWithAllObjects(ball);
            bool hitWall = false;
            if (hitObject == Board.OBJECT_NONE)
            {
                bool crossingBridge = allowBridge && CrossingBridge(checkRoom, ball);
                hitWall = !crossingBridge && CollisionCheckBallWithWalls(checkRoom, ball.x, ball.y);
            }
            ball.hitObject = hitObject;
            ball.hit = hitWall || (hitObject > Board.OBJECT_NONE);
        }


        void RemoteBallMovement()
        {
            for (int i = 0; i < numPlayers; ++i)
            {
                // We ignore messages to move our own player
                if (i != thisPlayer)
                {
                    BALL nextPayer = gameBoard.getPlayer(i);
                    PlayerMoveAction movement = sync.GetLatestBallSync(i);
                    if (movement != null)
                    {
                        nextPayer.room = movement.room;
                        nextPayer.previousRoom = movement.room;
                        nextPayer.displayedRoom = movement.room;
                        nextPayer.x = movement.posx;
                        nextPayer.previousX = movement.posx - movement.velx;
                        nextPayer.y = movement.posy;
                        nextPayer.previousY = movement.posy - movement.vely;
                        nextPayer.velx = movement.velx;
                        nextPayer.vely = movement.vely;
                    }
                }
            }

        }

        void SyncDragons()
        {
            RemoteAction nextAction = sync.GetNextDragonAction();
            while (nextAction != null)
            {
                if (nextAction.typeCode == DragonStateAction.CODE)
                {
                    DragonStateAction nextState = (DragonStateAction)nextAction;
                    Dragon dragon = dragons[nextState.dragonNum];
                    dragon.syncAction(nextState);
                }
                else
                {
                    // If we are in the same room as the dragon and are closer to it than the reporting player,
                    // then we ignore reports and trust our internal state.
                    // If the dragon is not in stalking state we ignore it.
                    // Otherwise, you use the reported state.
                    DragonMoveAction nextMove = (DragonMoveAction)nextAction;
                    Dragon dragon = dragons[nextMove.dragonNum];
                    if ((dragon.state == Dragon.STALKING) &&
                        ((dragon.room != thisBall.room) ||
                        (thisBall.distanceToObject(dragon.x+4, dragon.y-Dragon.MIDHEIGHT) > nextMove.distance)))
                    {

                        dragon.syncAction(nextMove);
                    }
                }
                nextAction = sync.GetNextDragonAction();
            }
        }

        void MoveCarriedObjects()
        {
            // RCA: moveBallIntoCastle originally was called after we moved the carried objects, but
            // this created too many problems with the ball being in the castle and the key being
            // still outside the castle.  So I moved it to before.
            moveBallIntoCastle();

            for (int ctr = 0; ctr < numPlayers; ++ctr)
            {
                BALL nextBall = gameBoard.getPlayer(ctr);
                if (nextBall.linkedObject != Board.OBJECT_NONE)
                {
                    OBJECT objct = gameBoard[nextBall.linkedObject];
                    objct.x = (nextBall.x / 2) + nextBall.linkedObjectX;
                    objct.y = (nextBall.y / 2) + nextBall.linkedObjectY;
                    objct.room = nextBall.room;
                }
            }

            // Seems like a weird place to call this but this matches the original game
            MoveGroundObject();
        }

        void moveBallIntoCastle()
        {
            for (int ctr = 0; ctr < numPlayers; ++ctr)
            {
                BALL nextBall = gameBoard.getPlayer(ctr);
                // Handle balls going into the castles
                for (int portalCtr = 0; portalCtr < ports.Length; ++portalCtr)
                {
                    Portcullis nextPort = ports[portalCtr];
                    if (nextPort.exists() && nextBall.room == nextPort.room && nextPort.allowsEntry && CollisionCheckObject(nextPort, (nextBall.x - 4), (nextBall.y - 1), 8, 8))
                    {
                        nextBall.room = nextPort.insideRoom;
                        nextBall.previousRoom = nextBall.room;
                        nextBall.displayedRoom = nextBall.room;
                        nextBall.y = Board.BOTTOM_EDGE_FOR_BALL;
                        nextBall.previousY = nextBall.y;
                        nextBall.vely = 0;
                        nextBall.velx = 0;
                        // make sure it stays unlocked in case we are walking in with the key
                        nextPort.forceOpen();
                        if (ai != null)
                        {
                            ai.ConnectPortcullisPlots(nextPort, true);
                        }
                        // Report to all the other players only if its the current player entering
                        if (ctr == thisPlayer)
                        {
                            PortcullisStateAction gateAction =
                            new PortcullisStateAction(nextPort.getPKey(), nextPort.state, nextPort.allowsEntry);
                            sync.BroadcastAction(gateAction);

                            // Report the ball entering the castle
                            PlayerMoveAction moveAction = new PlayerMoveAction(nextBall.room, nextBall.x, nextBall.y, nextBall.velx, nextBall.vely);
                            sync.BroadcastAction(moveAction);
                        }
                        if ((ctr == thisPlayer) && (popupMgr != null))
                        {
                            popupMgr.EnteredRoomShowPopups(nextPort.insideRoom);
                        }
                        // If entering the black castle in the gauntlet, glow.
                        if ((gameMode == Adv.GAME_MODE_GAUNTLET) && (nextPort == gameBoard[Board.OBJECT_BLACK_PORT]) && !nextBall.isGlowing())
                        {
                            nextBall.setGlowing(true);
                            gameBoard.makeSound(SOUND.GLOW, nextBall.room);
                        }
                        // If entering the crystal castle, trigger the easter egg
                        if ((nextBall.room == Map.CRYSTAL_FOYER) && (EasterEgg.shouldStartChallenge()))
                        {
                            EasterEgg.showChallengeMessage();
                        }
                        break;
                    }
                }
            }
        }

        void MoveGroundObject()
        {
            // Move any objects that need moving, and wrap objects from room to room
            Board.ObjIter iter = gameBoard.getMovableObjects();
            while (iter.hasNext())
            {

                OBJECT objct = iter.next();

                // Apply movement
                if (objct.state <= 1) // Any state above 2 is non-moving
                {
                    objct.x += objct.getMovementX();
                    objct.y += objct.getMovementY();
                }

                // Check and Deal with Up
                if (objct.y > Board.TOP_EDGE_FOR_OBJECTS)
                {
                    objct.y = Board.BOTTOM_EDGE_FOR_OBJECTS;
                    objct.room = roomDefs[objct.room].roomUp;
                }

                // Check and Deal with Left
                if (objct.x < Board.LEFT_EDGE_FOR_OBJECTS)
                {
                    objct.x = Board.RIGHT_EDGE_FOR_OBJECTS-1;
                    objct.room = roomDefs[objct.room].roomLeft;
                }

                // Check and Deal with Down
                if (objct.y < Board.BOTTOM_EDGE_FOR_OBJECTS)
                {
                    // Handle object leaving the castles
                    bool leftCastle = false;
                    for (int ctr = 0; (ctr < ports.Length) && (!leftCastle); ++ctr)
                    {
                        if (ports[ctr].exists() && (objct.room == ports[ctr].insideRoom) && (ports[ctr].allowsEntry))
                        {
                            objct.x = Portcullis.EXIT_X / 2;
                            objct.y = Portcullis.EXIT_Y / 2;
                            objct.room = ports[ctr].room;
                            // TODO: Do we need to broadcast leaving the castle?  Seems there might be quite a jump.
                            leftCastle = true;
                        }
                    }
                    if (!leftCastle)
                    {
                        objct.y = Board.TOP_EDGE_FOR_OBJECTS-1;
                        objct.room = roomDefs[objct.room].roomDown;
                    }
                }

                // Check and Deal with Right
                if (objct.x > Board.RIGHT_EDGE_FOR_OBJECTS)
                {
                    objct.x = Board.LEFT_EDGE_FOR_OBJECTS;
                    objct.room = roomDefs[objct.room].roomRight;
                }

                // If the object has a linked object
                if ((objct == bat) && (bat.linkedObject != Board.OBJECT_NONE))
                {
                    OBJECT linkedObj = gameBoard[bat.linkedObject];
                    linkedObj.x = objct.x + bat.linkedObjectX;
                    linkedObj.y = objct.y + bat.linkedObjectY;
                    linkedObj.room = objct.room;
                }
            }
        }

        void RemotePickupPutdown()
        {
            PlayerPickupAction newaction = sync.GetNextPickupAction();
            while (newaction != null)
            {
                int actorNum = newaction.sender;
                BALL actor = gameBoard.getPlayer(actorNum);
                if ((newaction.dropObject != Board.OBJECT_NONE) && (actor.linkedObject == newaction.dropObject))
                {
                    actor.linkedObject = Board.OBJECT_NONE;
                    OBJECT dropped = gameBoard[newaction.dropObject];
                    dropped.room = newaction.dropRoom;
                    dropped.x = newaction.dropX;
                    dropped.y = newaction.dropY;
                    // Only play a sound if the drop isn't caused by picking up a different object.
                    if (newaction.pickupObject == Board.OBJECT_NONE)
                    {
                        gameBoard.makeSound(SOUND.PUTDOWN, actor.room);
                    }
                }
                if (newaction.pickupObject != Board.OBJECT_NONE)
                {
                    actor.linkedObject = newaction.pickupObject;
                    actor.linkedObjectX = newaction.pickupX;
                    actor.linkedObjectY = newaction.pickupY;
                    // If anybody else was carrying this object, take it away.
                    for (int ctr = 0; ctr < numPlayers; ++ctr)
                    {
                        if ((ctr != actorNum) && (gameBoard.getPlayer(ctr).linkedObject == newaction.pickupObject))
                        {
                            gameBoard.getPlayer(ctr).linkedObject = Board.OBJECT_NONE;
                        }
                    }

                    if ((EasterEgg.crystalColor < COLOR.DARK_CRYSTAL2) && (newaction.pickupObject >= Board.OBJECT_CRYSTALKEY1) &&
                        (newaction.pickupObject <= Board.OBJECT_CRYSTALKEY3))
                    {
                        EasterEgg.darkenCastle(COLOR.DARK_CRYSTAL2);
                    }

                    // If they are within hearing distance play the pickup sound
                    gameBoard.makeSound(SOUND.PICKUP, actor.room);
                }
                newaction = sync.GetNextPickupAction();
            }
        }

        /**
         * To make game play more fun, you can't shove your key inside the walls of your own castle.  If you try, it will
         * stick out the other side.
         */
        void unhideKey(OBJECT droppedObject)
        {

            int objectPkey = droppedObject.getPKey();
            if ((objectPkey == Board.OBJECT_YELLOWKEY) || (objectPkey == Board.OBJECT_COPPERKEY) || (objectPkey == Board.OBJECT_JADEKEY))
            {
                int roomNum = droppedObject.room;
                if ((roomNum == Map.GOLD_FOYER) || (roomNum == Map.COPPER_FOYER) || (roomNum == Map.JADE_FOYER))
                {
                    if (droppedObject.y < 15)
                    {
                        droppedObject.y = 15;
                    }
                    else if (droppedObject.y > 99)
                    {
                        droppedObject.y = 99;
                    }
                }
            }
        }

        void PickupPutdown()
        {
            AiPutdown();
            if (!joystickDisabled && joyFire)
            {
                Putdown(thisBall);
            }
            else
            {
                Pickup();
            }
        }

        void AiPutdown()
        {
            // Check if any AI players are dropping
            for (int ctr = 0; ctr < numPlayers; ++ctr)
            {
                if (aiPlayers[ctr] != null)
                {
                    BALL aiBall = gameBoard.getPlayer(ctr);
                    if ((aiBall.linkedObject > Board.OBJECT_NONE) && aiPlayers[ctr].shouldDropHeldObject())
                    {
                        Putdown(aiBall);
                    }
                }
            }
        }

        void Putdown(BALL ball)
        {
            if (ball.linkedObject > Board.OBJECT_NONE)
            {
                int dropped = ball.linkedObject;
                OBJECT droppedObject = gameBoard[dropped];

                // Put down the current object!
                ball.linkedObject = Board.OBJECT_NONE;

                if ((gameOptions & GAMEOPTION_NO_HIDE_KEY_IN_CASTLE) != 0)
                {
                    unhideKey(droppedObject);
                }

                if (ball.playerNum == thisPlayer)
                {
                    // Tell other clients about the drop
                    PlayerPickupAction action = new PlayerPickupAction(Board.OBJECT_NONE, 0, 0, dropped, droppedObject.room,
                                                                        droppedObject.x, droppedObject.y);
                    sync.BroadcastAction(action);

                    if (popupMgr != null)
                    {
                        popupMgr.needPopup[PopupMgr.DROP_OBJECT] = false;
                    }
                }

                // Play the sound
                gameBoard.makeSound(SOUND.PUTDOWN, ball.room);
            }
        }


        void Pickup()
        {
            for (int playerctr = 0; playerctr < numPlayers; ++playerctr)
            {
                // We don't calculate pickup for remote players.  We rely on them
                // sending a remote pickup message.
                if (!gameBoard.isPlayerRemote(playerctr))
                {
                    bool isThisPlayer = (playerctr == thisPlayer);
                    // See if we are touching any carryable objects
                    Board.ObjIter iter = gameBoard.getCarryableObjects();
                    BALL nextBall = gameBoard.getPlayer(playerctr);
                    int hitIndex = CollisionCheckBallWithObjects(nextBall, iter);
                    if (hitIndex > Board.OBJECT_NONE)
                    {
                        // Ignore the object we are already carrying
                        if (hitIndex == nextBall.linkedObject)
                        {
                            // Check the remainder of the objects
                            hitIndex = CollisionCheckBallWithObjects(nextBall, iter);
                        }

                        if (hitIndex > Board.OBJECT_NONE)
                        {
                            // Collect info about whether we are also dropping an object (for when we broadcast the action)
                            PlayerPickupAction action = new PlayerPickupAction(Board.OBJECT_NONE, 0, 0, Board.OBJECT_NONE, 0, 0, 0);
                            int dropIndex = nextBall.linkedObject;
                            if (dropIndex > Board.OBJECT_NONE)
                            {
                                OBJECT dropped = gameBoard[dropIndex];
                                action.setDrop(dropIndex, dropped.room, dropped.x, dropped.y);
                            }

                            // If the bat is holding the object we do some of the pickup things but not all.
                            // We drop our current object and play the pickup sound, but we don't actually
                            // pick up the object.
                            // NOTE: Discrepancy here between C++ port behavior and original Atari behavior so
                            // not totally sure what should be done.  As a guess, we just set linkedObject to none and
                            // play the sound.
                            if (bat.exists() && (bat.linkedObject == hitIndex))
                            {
                                if (dropIndex > Board.OBJECT_NONE)
                                {
                                    // Drop our current object and broadcast it
                                    nextBall.linkedObject = Board.OBJECT_NONE;
                                    if (isThisPlayer)
                                    {
                                        sync.BroadcastAction(action);
                                    }
                                }
                            }
                            else
                            {

                                // Pick up this object!
                                nextBall.linkedObject = hitIndex;

                                // calculate the XY offsets from the ball's position
                                nextBall.linkedObjectX = gameBoard[hitIndex].x - (nextBall.x / Adv.BALL_SCALE);
                                nextBall.linkedObjectY = gameBoard[hitIndex].y - (nextBall.y / Adv.BALL_SCALE);

                                // Take it away from anyone else if they were holding it.
                                for (int otherPlayerCtr = 0; otherPlayerCtr < numPlayers; ++otherPlayerCtr)
                                {
                                    if ((otherPlayerCtr != playerctr) && (gameBoard.getPlayer(otherPlayerCtr).linkedObject == hitIndex))
                                    {
                                        gameBoard.getPlayer(otherPlayerCtr).linkedObject = Board.OBJECT_NONE;
                                    }
                                }

                                if ((hitIndex >= Board.OBJECT_CRYSTALKEY1) && (hitIndex <= Board.OBJECT_CRYSTALKEY3))
                                {
                                    EasterEgg.foundKey();
                                }

                                // Broadcast that we picked up an object
                                if (isThisPlayer)
                                {
                                    action.setPickup(hitIndex, nextBall.linkedObjectX, nextBall.linkedObjectY);
                                    sync.BroadcastAction(action);
                                }

                                if (isThisPlayer)
                                {
                                    if (popupMgr != null)
                                    {
                                        popupMgr.PickedUpObjectShowPopups(hitIndex);
                                    }
                                }
                            }

                            // Play the sound
                            gameBoard.makeSound(SOUND.PICKUP, nextBall.room);
                        }
                    }
                }
            }
        }

        void Surround()
        {
            // Calculate which surrounds are visible in the currently displayed room
            int roomNum = thisBall.room;
            ROOM currentRoom = roomDefs[roomNum];
            if (currentRoom.color == COLOR.LTGRAY)
            {
                for (int ctr = 0; ctr < numPlayers; ++ctr)
                {
                    BALL nextBall = gameBoard.getPlayer(ctr);
                    if (nextBall.room == roomNum)
                    {
                        // Put it in the same room as the ball (player) and center it under the ball
                        surrounds[ctr].room = roomNum;
                        surrounds[ctr].x = (nextBall.x - Board.SURROUND_RADIUS_X) / 2;
                        surrounds[ctr].y = (nextBall.y + Board.SURROUND_RADIUS_Y) / 2;
                    }
                    else
                    {
                        surrounds[ctr].room = -1;
                    }
                }
            }
            else
            {
                for (int ctr = 0; ctr < numPlayers; ++ctr)
                {
                    surrounds[ctr].room = -1;
                }
            }
        }

        void Portals()
        {
            // Handle all the local actions of portals
            for (int portalCtr = 0; portalCtr < ports.Length; ++portalCtr)
            {
                Portcullis port = ports[portalCtr];
                if (port.exists())
                {

                    // Someone has to be in the room for the key to trigger the gate
                    // or for any object to go into a gate
                    bool seen = gameBoard.isWitnessed(port.room);
                    if (seen)
                    {
                        // Check if a key unlocks the gate
                        PortcullisStateAction gateAction = port.checkKeyInteraction();
                        if (gateAction != null)
                        {
                            if (ai != null)
                            {
                                ai.ConnectPortcullisPlots(port, port.allowsEntry);
                            }
                            // Broadcast a state change if we are holding the key or if no one is holding the key and we
                            // are a witness
                            int heldBy = gameBoard.getPlayerHoldingObject(port.key);
                            if ((heldBy == thisPlayer) || ((heldBy < 0) && (thisBall.room == port.room)))
                            {
                                sync.BroadcastAction(gateAction);
                            }
                        }

                        // Check if anything runs into the gate
                        Board.ObjIter iter = gameBoard.getMovableObjects();
                        while (iter.hasNext())
                        {
                            OBJECT next = iter.next();
                            ObjectMoveAction reaction = port.checkObjectEnters(next);
                            if (reaction != null)
                            {
                                sync.BroadcastAction(reaction);
                            }
                        }
                    }

                    // Raise/lower the port
                    port.moveOneTurn();
                    if (port.allowsEntry)
                    {
                        // Port is unlocked
                        roomDefs[port.insideRoom].roomDown = port.room;
                    }
                    else
                    {
                        // Port is locked
                        roomDefs[port.insideRoom].roomDown = port.insideRoom;
                    }
                }
            }

        }



        void Magnet()
        {
            Magnet magnet = (Magnet)gameBoard[Board.OBJECT_MAGNET];
            OBJECT attracted = magnet.getAtractedObject();
            if (attracted != null)
            {
                attracted.x += Math.Sign(magnet.x - attracted.x);
                attracted.y += Math.Sign((magnet.y - magnet.Height) - attracted.y);
            }
        }

        void ClearDrawnObjects()
        {
            Board.ObjIter iter = gameBoard.getObjects();
            while (iter.hasNext())
            {
                OBJECT toDisplay = iter.next();
                // Init it to not displayed
                toDisplay.displayed = -1;
            }

            for (int ctr = 0; ctr < numPlayers; ++ctr)
            {
                surrounds[ctr].displayed = -1;
            }
        }

        void MarkDrawnObjects(int room)
        {
            // RCA - Completely redid how we compute which objects are displayed
            // to handle AI players that need objects to flicker even when
            // not displayed on the screen.

            // This assumes ClearDrawnObjects() has been called before this.

            // Create a list of objects in this room to be displayed
            List<int> objectsToDisplay = new List<int>(MAX_OBJECTS);
            Board.ObjIter iter = gameBoard.getObjects();
            while (iter.hasNext())
            {
                OBJECT toDisplay = iter.next();
                if (toDisplay.room == room)
                {
                    // This object is in the current room - add it to the list
                    objectsToDisplay.Add(toDisplay.getPKey());
                }
            }

            // Add the surrounds to the list
            for (int ctr = 0; ctr < numPlayers; ++ctr)
            {
                if (surrounds[ctr].room == room)
                {
                    objectsToDisplay.Add(Board.OBJECT_SURROUND - ctr);
                }
            }


            // If more than MAX_DISPLAYABLE_OBJECTS are needed to be drawn, we multiplex/cycle through them
            // Note that this also (intentionally) effects collision checking, as per the original game!!
            // RCA - Unlike the original game which used a state variable to track where in the list,
            // we use the frame counter
            int numObjects = objectsToDisplay.Count;
            int numToDisplay = (numObjects > MAX_DISPLAYABLE_OBJECTS ? MAX_DISPLAYABLE_OBJECTS : numObjects);
            int start = (numObjects > 0 ? (frameNumber / 3) % numObjects : 0);
            for(int ctr=0; ctr<numToDisplay; ++ctr)
            {
                int nextObjectKey = objectsToDisplay[(start + ctr) % numObjects];
                // There are both objects and surrounds in the list.  Deal with each.
                if (nextObjectKey > Board.OBJECT_NONE)
                {
                    OBJECT nextObject = gameBoard[nextObjectKey];
                    nextObject.displayed = room;
                }
                else if (nextObjectKey <= Board.OBJECT_SURROUND)
                {
                    surrounds[Board.OBJECT_SURROUND - nextObjectKey].displayed = room;
                }
            }
        }

        void DrawObjectsAndThinWalls(int room)
        {
            // We need to keep track of these for weird reasons
            int firstColorDrawn = -1;
            int lastColorDrawn = COLOR.BLACK;
            Board.ObjIter iter = gameBoard.getObjects();
            while (iter.hasNext())
            {
                OBJECT toDisplay = iter.next();
                if (toDisplay.displayed == room)
                {
                    DrawObject(toDisplay);
                    if (firstColorDrawn < 0)
                    {
                        firstColorDrawn = toDisplay.color;
                    }
                    else
                    {
                        lastColorDrawn = toDisplay.color;
                    }
                }
            }
            firstColorDrawn = (firstColorDrawn < 0 ? COLOR.BLACK : firstColorDrawn);

            if ((roomDefs[room].flags & ROOM.FLAG_LEFTTHINWALL) > 0)
            {
                // Position missile 00 to 0D,00 - left thin wall
                // Left wall is the color of the first displayed object
                COLOR color = COLOR.table(firstColorDrawn);
                view.Platform_PaintPixel(color.r, color.g, color.b, Map.LEFT_THIN_WALL, 0x00, Map.THIN_WALL_WIDTH, ADVENTURE_TOTAL_SCREEN_HEIGHT);
            }
            if ((roomDefs[room].flags & ROOM.FLAG_RIGHTTHINWALL) > 0)
            {
                // Position missile 01 to 96,00 - right thin wall
                // Right thin wall is the color of the last displayed object
                COLOR color = COLOR.table(lastColorDrawn);
                view.Platform_PaintPixel(color.r, color.g, color.b, Map.RIGHT_THIN_WALL, 0x00, Map.THIN_WALL_WIDTH, ADVENTURE_TOTAL_SCREEN_HEIGHT);
            }

        }

        private void DrawBall(BALL ball, COLOR color)
        {
            int left = ball.x; //& ~0x00000001;
            int top = ball.y; // & ~0x00000001;
            int bottom = top - BALL.DIAMETER;

            // scan the data
            for (int row = top, ctr = 0; row > bottom; --row, ++ctr)
            {
                byte rowByte = ball.gfxData[ctr];
                for (int bit = 0; bit < 8; bit++)
                {
                    // If there is a bit in the graphics matric at this row and bit, paint a pixel
                    if ((rowByte & (1 << (7 - bit))) > 0)
                    {
                        int x = left + bit;
                        if (x < ADVENTURE_SCREEN_WIDTH)
                        {
                            view.Platform_PaintPixel(color.r, color.g, color.b, x, row, 1, 1);
                        }
                    }
                }
            }
        }

        private void DrawObject(OBJECT objct)
        {
            int size = (objct.size / 2) + 1;

            // Look up the index to the current state for this object
            int stateIndex = objct.states.Length > objct.state ? objct.states[objct.state] : 0;
            byte[] dataP = objct.gfxData[stateIndex];
            DrawGraphic(objct.x, objct.y, dataP, objct.color, size);
        }

        private void DrawGraphic(int gfxX, int gfxY, byte[] gfx, int colorCode, int widthMultiplier)
        {
            COLOR color = colorCode == COLOR.FLASH ? GetFlashColor() : COLOR.table(colorCode);
            int cx = gfxX * 2;
            int cy = gfxY * 2;
            int gfxHeight = gfx.Length;

            // scan the data
            for (int i = 0; i < gfxHeight; i++)
            {
                byte rowByte = gfx[i];
                // Parse the row - each bit is a 2 x 2 block
                for (int bit = 0; bit < 8; bit++)
                {
                    if ((rowByte & (1 << (7 - bit))) > 0)
                    {
                        int x = cx + (bit * 2 * widthMultiplier);
                        if (x >= ADVENTURE_SCREEN_WIDTH)
                            x -= ADVENTURE_SCREEN_WIDTH;
                        view.Platform_PaintPixel(color.r, color.g, color.b, x, cy, 2 * widthMultiplier, 2);
                    }
                }

                // next byte - next row
                ++rowByte;
                cy -= 2;
            }
        }

        bool CollisionCheckBallWithWalls(int room, int x, int y)
        {
            bool hitWall = false;

            // get the playfield data
            ROOM currentRoom = roomDefs[room];

            // get the playfield mirror flag
            bool mirror = (currentRoom.flags & ROOM.FLAG_MIRROR) > 0;

            if (((currentRoom.flags & ROOM.FLAG_LEFTTHINWALL) > 0) &&
                (x < Map.LEFT_THIN_WALL + Map.THIN_WALL_WIDTH ) &&
                (x + BALL.DIAMETER >= Map.LEFT_THIN_WALL))
            {
                hitWall = true;
            }
            if (((currentRoom.flags & ROOM.FLAG_RIGHTTHINWALL) > 0) &&
                (x < Map.RIGHT_THIN_WALL + Map.THIN_WALL_WIDTH) &&
                (x + BALL.DIAMETER >= Map.RIGHT_THIN_WALL))
            {
                // If the dot is in this room, allow passage through the wall into the Easter Egg room
                if (gameBoard[Board.OBJECT_DOT].room != room)
                    hitWall = true;
            }

            hitWall = hitWall || currentRoom.hitsWall(x, y, BALL.DIAMETER, BALL.DIAMETER);

            return hitWall;
        }

        private bool CrossingBridge(int room, BALL ball)
        {
            // Check going through the bridge
            Bridge bridge = (Bridge)gameBoard[Board.OBJECT_BRIDGE];
            if ((bridge.room == room)
                && (ball.linkedObject != Board.OBJECT_BRIDGE))
            {
                if ((ball.x > bridge.InsideBLeft) && (ball.x + BALL.DIAMETER -1 < bridge.InsideBRight))
                {
                    int bridgeBTop = bridge.by;
                    int bridgeBBottom = bridge.by - bridge.BHeight + 1;
                    if ((ball.y >= bridgeBBottom) && (ball.y-BALL.DIAMETER < bridgeBTop))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /*
         * Checks if ball is colliding with any other object.  Returns first object it finds or OBJECT_NONE
         * if no collisions.
         */
        private int CollisionCheckBallWithAllObjects(BALL ball)
        {
            Board.ObjIter iter = gameBoard.getObjects();
            return CollisionCheckBallWithObjects(ball, iter);
        }

        /*
         * Checks if ball is colliding with any of the objects in the iterable collection.
         * Returns first object it finds or OBJECT_NONE if no collisions.
         */
        private int CollisionCheckBallWithObjects(BALL ball, Board.ObjIter iter)
        {
            // Go through all the objects
            while (iter.hasNext())
            {
                // If this object is in the current room and can be picked up, check it against the ball
                OBJECT objct = iter.next();
                if (CollisionCheckBallWithObject(ball, objct))
                {
                    return objct.getPKey();
                }
            }

            return Board.OBJECT_NONE;
        }

        /**
         * Checks if ball is colliding with object.
         */
        private bool CollisionCheckBallWithObject(BALL ball, OBJECT objct)
        {
            bool collision = ((objct.displayed >= 0) &&
                              objct.isTangibleTo(thisPlayer) &&
                              (ball.room == objct.room) &&
                              (CollisionCheckObject(objct, ball.x, ball.y, BALL.DIAMETER, BALL.DIAMETER)) ? true : false);
            return collision;
        }

        // Checks an object for collision against the specified rectangle
        // On the 2600 this is done in hardware by the Player/Missile collision registers
        private bool CollisionCheckObject(OBJECT objct, int x, int y, int width, int height)
        {
            return gameBoard.CollisionCheckObject(objct, x, y, width, height);
        }


        /**
         * If the player has become disconnect them, remove them from the game.
         */
        void dropPlayer(int player)
        {
            // Mostly just move the player to the 0 room, but drop any object they
            // are carrying and free any dragon they have been eaten by.
            if (player != thisPlayer)
            {
                BALL toDrop = gameBoard.getPlayer(player);

                // Drop anything player is carrying
                toDrop.linkedObject = Board.OBJECT_NONE;

                // Free the dragon if it has eaten the player
                for (int ctr = 0; ctr < numDragons; ++ctr)
                {
                    Dragon dragon = dragons[ctr];
                    if (dragon.eaten == toDrop)
                    {
                        dragon.state = Dragon.STALKING;
                        dragon.eaten = null;
                    }
                }

                // Move the player to the 0 room.
                toDrop.room = 0;
            }
        }

        /**
         * Sends a message that a player has gone offlline or come back online.
         * playerDropped - the player that dropped off, 0 means no one dropped off.  -1 means two players dropped off.
         * playerRejoined - the player that rejoined, 0 means no one rejoined.  -1 means two players rejoined.
         */
        void warnOfDropoffRejoin(int playerDroppedOff, int playerRejoined)
        {
            if ((playerRejoined != 0) || (playerDroppedOff != 0))
            {
                string message = "";
                if (playerRejoined < 0)
                {
                    message = "All other players have rejoined the game.\n";
                }
                else if (playerRejoined > 0)
                {
                    message = "Player " + playerRejoined + " has rejoined the game.\n";
                }
                if (playerDroppedOff < 0)
                {
                    message += "All other players have disconnected.\n";
                }
                else if (playerDroppedOff > 0)
                {
                    message += "Player " + playerDroppedOff + " has disconnected.\n";
                }
                if (message.Length > 0)
                {
                    view.Platform_DisplayStatus(message, 5);
                }
            }
        }

        /**
         * Report whether  we've gotten messages from other players recently.
         * Also send a ping to other players to make sure they see activity from us.
         */
        private void checkPlayers()
        {
            if (sync.IsNetworked)
            {
                // We check for players every 15 seconds (900 turns actually). We check to see if 
                // we've received anything from the other players.  If they've missed 3 15 second marks
                // in a row we assume they have disconnected and remove them from the game.
                // We also send out a ping every 15 seconds to others so we know they've heard from us.
                const int TURNS_BETWEEN_CHECKS = 900; // About 15 seconds.
                const int MAX_MISSED_CHECKS = 3;

                ++turnsSinceTimeCheck;
                if (turnsSinceTimeCheck >= TURNS_BETWEEN_CHECKS)
                {
                    int offline = 0;
                    int online = 0;
                    for (int ctr = 0; ctr < numPlayers; ++ctr)
                    {
                        if (ctr != thisPlayer)
                        {
                            if (sync.getMessagesReceived(ctr) == 0)
                            {
                                ++missedChecks[ctr];
                                if (missedChecks[ctr] == MAX_MISSED_CHECKS)
                                {
                                    dropPlayer(ctr);
                                    offline = (offline == 0 ? ctr + 1 : -1);
                                }
                            }
                            else
                            {
                                if (missedChecks[ctr] >= MAX_MISSED_CHECKS)
                                {
                                    online = (online == 0 ? ctr + 1 : -1);
                                }
                                missedChecks[ctr] = 0;
                            }
                        }
                    }
                    warnOfDropoffRejoin(offline, online);
                    PingAction action = new PingAction();
                    sync.BroadcastAction(action);
                    sync.resetMessagesReceived();

                    turnsSinceTimeCheck = 0;
                }
            }
        }



        //
        // Object definitions
        //

        private static byte[][] objectGfxNum =
        {
            new byte[] {
                // Object #5 State #1 Graphic :'1'
                0x04,                  //  X                                                                        
                0x0C,                  // XX                                                                        
                0x04,                  //  X                                                                        
                0x04,                  //  X                                                                        
                0x04,                  //  X                                                                        
                0x04,                  //  X                                                                        
                0x0E},                   // XXX
            new byte[] {
                // Object #5 State #2 Grphic : '2'                                                                                   
                0x0E,                  //  XXX                                                                      
                0x11,                  // X   X                                                                     
                0x01,                  //     X                                                                     
                0x02,                  //    X                                                                      
                0x04,                  //   X                                                                       
                0x08,                  //  X                                                                        
                0x1F},                 // XXXXX
            new byte[] {
                // Object #5 State #3 Graphic :'3'                                                                                   
                0x0E,                  //  XXX                                                                      
                0x11,                  // X   X                                                                     
                0x01,                  //     X                                                                     
                0x06,                  //   XX                                                                      
                0x01,                  //     X                                                                     
                0x11,                  // X   X                                                                     
                0x0E}                  //  XXX                                                                      
        };

        // Number states
        private static byte[] numberStates =
        {
            0,1,2
        };

        // Object #0B : State FF : Graphic
        private static byte[][] objectGfxKey =
        { new byte[] {
                0x07,                  //      XXX
                0xFD,                  // XXXXXX X
                0xA7                   // X X  XXX
        } };

        // Object #0A : Bridge defined in Bridge.cs                                                                                 

        // Object #1 : Graphic
        private static byte[][] objectGfxSurround =
        { new byte[] {
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF,                  // XXXXXXXX                                                                  
            0xFF                   // XXXXXXXX                                                                  
        } };

        // 
        // Object #9 : State FF : Graphics                                                                                   
        private static byte[][] objectGfxSword =
        { new byte[] {
            0x20,                  //   X                                                                       
            0x40,                  //  X                                                                        
            0xFF,                  // XXXXXXXX     
            0x40,                  //  X                                                                        
            0x20                   //   X                                                                       
        } };

        // Object #0F : State FF : Graphic                                                                                   
        private static byte[][] objectGfxDot =
        { new byte[] {
            0x80                   // X                                                                         
        } };

        // Object #4 : State FF : Graphic                                                                                    
        private static byte[][] objectGfxAuthor =
        { new byte[] {
            0xF0,                  // XXXX                                                                      
            0x80,                  // X                                                                         
            0x80,                  // X                                                                         
            0x80,                  // X                                                                         
            0xF4,                  // XXXX X                                                                    
            0x04,                  //      X                                                                    
            0x87,                  // X    XXX                                                                  
            0xE5,                  // XXX  X X                                                                  
            0x87,                  // X    XXX                                                                  
            0x80,                  // X                                                                         
            0x05,                  //      X X                                                                  
            0xE5,                  // XXX  X X                                                                 
            0xA7,                  // X X  XXX                                                                  
            0xE1,                  // XXX    X                                                                  
            0x87,                  // X    XXX                                                                  
            0xE0,                  // XXX                                                                       
            0x01,                  //        X                                                                  
            0xE0,                  // XXX                                                                       
            0xA0,                  // X X                                                                       
            0xF0,                  // XXXX                                                                      
            0x01,                  //        X                                                                  
            0x40,                  //  X                                                                        
            0xE0,                  // XXX                                                                       
            0x40,                  //  X                                                                       
            0x40,                  //  X                                                                        
            0x40,                  //  X                                                                        
            0x01,                  //        X                                                                  
            0xE0,                  // XXX                                                                       
            0xA0,                  // X X                                                                       
            0xE0,                  // XXX                                                                       
            0x80,                  // X                                                                         
            0xE0,                  // XXX                                                                       
            0x01,                  //        X                                                                  
            0x20,                  //   X                                                                       
            0x20,                  //   X                                                                       
            0xE0,                  // XXX                                                                       
            0xA0,                  // X X                                                                       
            0xE0,                  // XXX                                                                       
            0x01,                  //        X                                                                  
            0x01,                  //        X                                                                  
            0x01,                  //        X                                                                  
            0x88,                  //    X   X                                                                  
            0xA8,                  // X X X                                                                     
            0xA8,                  // X X X                                                                     
            0xA8,                  // X X X                                                                     
            0xF8,                  // XXXXX                                                                     
            0x01,                  //        X                                                                  
            0xE0,                  // XXX                                                                       
            0xA0,                  // X X                                                                       
            0xF0,                  // XXXX                                                                      
            0x01,                  //        X                                                                  
            0x80,                  // X                                                                         
            0xE0,                  // XXX                                                                       
            0x8F,                  // X   XXXX                                                                 
            0x89,                  // X   X  X                                                                  
            0x0F,                  //     XXXX                                                                  
            0x8A,                  // X   X X                                                                   
            0xE9,                  // XXX X  X                                                                  
            0x80,                  // X                                                                         
            0x8E,                  // X   XXX                                                                   
            0x0A,                  //     X X                                                                   
            0xEE,                  // XXX XXX                                                                   
            0xA0,                  // X X                                                                      
            0xE8,                  // XXX X                                                                     
            0x88,                  // X   X                                                                     
            0xEE,                  // XXX XXX                                                                   
            0x0A,                  //     X X                                                                   
            0x8E,                  // X   XXX                                                                   
            0xE0,                  // XXX                                                                       
            0xA4,                  // X X  X                                                                    
            0xA4,                  // X X  X                                                                    
            0x04,                  //      X                                                                    
            0x80,                  // X                                                                         
            0x08,                  //     X                                                                     
            0x0E,                  //     XXX                                                                   
            0x0A,                  //     X X                                                                   
            0x0A,                  //     X X                                                                   
            0x80,                  // X                                                                         
            0x0E,                  //     XXX                                                                   
            0x0A,                  //     X X                                                                   
            0x0E,                  //     XXX                                                                   
            0x08,                  //     X                                                                     
            0x0E,                  //     XXX                                                                   
            0x80,                  // X                                                                         
            0x04,                  //      X                                                                    
            0x0E,                  //     XXX                                                                   
            0x04,                  //      X                                                                    
            0x04,                  //      X                                                                    
            0x04,                  //      X                                                                    
            0x80,                  // X                                                                         
            0x04,                  //      X                                                                    
            0x0E,                  //     XXX                                                                   
            0x04,                  //      X                                                                    
            0x04,                  //      X                                                                    
            0x04                   //      X                                                                    
        } };

        // Object #10 : State FF : Graphic                                                                                   
        private static byte[][] objectGfxChallise =
        { new byte[] {
            0x81,                  // X      X                                                                  
            0x81,                  // X      X                                                                  
            0xC3,                  // XX    XX                                                                  
            0x7E,                  //  XXXXXX                                                                   
            0x7E,                  //  XXXXXX                                                                  
            0x3C,                  //   XXXX                                                                    
            0x18,                  //    XX                                                                     
            0x18,                  //    XX                                                                     
            0x7E                   //  XXXXXX                                                                   
        } };

        // Object #X : State FF : Graphic
        private static byte[][] objectGfxEasterEgg =
        { new byte[] {
            0x18,                  //    XX
            0x3C,                  //   XXXX
            0x24,                  //   X  X
            0x66,                  //  XX  XX
            0x42,                  //  X    X
            0x42,                  //  X    X
            0x81,                  // X      X
            0xD5,                  // XX X X X
            0xD5,                  // XX X X X
            0xAB,                  // X X X XX
            0xAB,                  // X X X XX
            0x81,                  // X      X
            0x81,                  // X      X
            0xD5,                  // XX X X X
            0xD5,                  // XX X X X
            0xAB,                  // X X X XX
            0xAB,                  // X X X XX
            0x81,                  // X      X
            0x42,                  //  X    X
            0x42,                  //  X    X
            0x66,                  //  XX  XX
            0x3C,                  //   XXXX
            0x18,                  //    XX
        } };


        // Indexed array of all objects and their properties
        //
        // Object locations (room and coordinate) for game 01
        //        - object, room, ox, oy, state, movement(ox/oy)
        private readonly int[,] game1Objects =
        {
            {Board.OBJECT_YELLOW_PORT, Map.GOLD_CASTLE, Portcullis.PORT_X, Portcullis.PORT_Y, Portcullis.CLOSED_STATE, 0x00, 0x00}, // Port 1
            {Board.OBJECT_COPPER_PORT, Map.COPPER_CASTLE, Portcullis.PORT_X, Portcullis.PORT_Y, Portcullis.CLOSED_STATE, 0x00, 0x00}, // Port 4
            {Board.OBJECT_JADE_PORT, Map.JADE_CASTLE, Portcullis.PORT_X, Portcullis.PORT_Y, Portcullis.CLOSED_STATE, 0x00, 0x00}, // Port 5
            {Board.OBJECT_WHITE_PORT, Map.WHITE_CASTLE, Portcullis.PORT_X, Portcullis.PORT_Y, Portcullis.CLOSED_STATE, 0x00, 0x00}, // Port 2
            {Board.OBJECT_BLACK_PORT, Map.BLACK_CASTLE, Portcullis.PORT_X, Portcullis.PORT_Y, Portcullis.CLOSED_STATE, 0x00, 0x00}, // Port 3
            {Board.OBJECT_NAME, Map.ROBINETT_ROOM, 0x4F, 0x67, 0x00, 0x00, 0x00}, // Robinett message
            {Board.OBJECT_NUMBER, Map.NUMBER_ROOM, 0x4F, 0x3E, 0x00, 0x00, 0x00}, // Starting number
            {Board.OBJECT_YELLOWDRAGON, Map.MAIN_HALL_LEFT, 0x4F, 0x1E, 0x00, 0x00, 0x00}, // Yellow Dragon
            {Board.OBJECT_GREENDRAGON, Map.SOUTHEAST_ROOM, 0x4F, 0x1E, 0x00, 0x00, 0x00}, // Green Dragon
            {Board.OBJECT_SWORD, Map.GOLD_FOYER, 0x1F, 0x1E, 0x00, 0x00, 0x00}, // Sword
            {Board.OBJECT_BRIDGE, Map.BLUE_MAZE_5, 0x29, 0x35, 0x00, 0x00, 0x00}, // Bridge
            {Board.OBJECT_YELLOWKEY, Map.GOLD_CASTLE, 0x1F, 0x3E, 0x00, 0x00, 0x00}, // Yellow Key
            {Board.OBJECT_COPPERKEY, Map.COPPER_CASTLE, 0x1F, 0x3E, 0x00, 0x00, 0x00}, // Copper Key
            {Board.OBJECT_JADEKEY, Map.JADE_CASTLE, 0x1F, 0x3E, 0x00, 0x00, 0x00}, // Jade Key
            {Board.OBJECT_BLACKKEY, Map.SOUTHEAST_ROOM, 0x1F, 0x3E, 0x00, 0x00, 0x00}, // Black Key
            {Board.OBJECT_CHALISE, Map.BLACK_INNERMOST_ROOM, 0x2F, 0x1E, 0x00, 0x00, 0x00}, // Challise
            {Board.OBJECT_MAGNET, Map.BLACK_FOYER, 0x7F, 0x1E, 0x00, 0x00, 0x00} // Magnet
        };




        // Object locations (room and coordinate) for Games 02 and 03
        //        - object, room, ox, oy, state, movement(ox/oy)
        private readonly int[,] game2Objects =
        {
            {Board.OBJECT_YELLOW_PORT, Map.GOLD_CASTLE, Portcullis.PORT_X, Portcullis.PORT_Y, Portcullis.CLOSED_STATE, 0x00, 0x00}, // Port 1
            {Board.OBJECT_COPPER_PORT, Map.COPPER_CASTLE, Portcullis.PORT_X, Portcullis.PORT_Y, Portcullis.CLOSED_STATE, 0x00, 0x00}, // Port 4
            {Board.OBJECT_JADE_PORT, Map.JADE_CASTLE, Portcullis.PORT_X, Portcullis.PORT_Y, Portcullis.CLOSED_STATE, 0x00, 0x00}, // Port 5
            {Board.OBJECT_WHITE_PORT, Map.WHITE_CASTLE, Portcullis.PORT_X, Portcullis.PORT_Y, Portcullis.CLOSED_STATE, 0x00, 0x00}, // Port 2
            {Board.OBJECT_BLACK_PORT, Map.BLACK_CASTLE, Portcullis.PORT_X, Portcullis.PORT_Y, Portcullis.CLOSED_STATE, 0x00, 0x00}, // Port 3
            {Board.OBJECT_CRYSTAL_PORT, Map.CRYSTAL_CASTLE, Portcullis.PORT_X, Portcullis.PORT_Y, Portcullis.CLOSED_STATE, 0x00, 0x00}, // Port 3
            {Board.OBJECT_NAME, Map.ROBINETT_ROOM, 0x4F, 0x67, 0x00, 0x00, 0x00}, // Robinett message
            {Board. OBJECT_NUMBER, Map.NUMBER_ROOM, 0x4F, 0x3E, 0x00, 0x00, 0x00}, // Starting number
            {Board.OBJECT_REDDRAGON, Map.BLACK_MAZE_2, 0x4F, 0x1E, 0x00, 3, 3}, // Red Dragon
            {Board.OBJECT_YELLOWDRAGON, Map.RED_MAZE_4, 0x4F, 0x1E, 0x00, 3, 3}, // Yellow Dragon
            // Commented out sections are for easy testing of Easter Egg
            #if DEBUG_EASTEREGG
            {Board.OBJECT_GREENDRAGON, Map.NUMBER_ROOM, 0x4F, 0x1E, 0x00, 3, 3}, // Green Dragon
            #else
            {Board.OBJECT_GREENDRAGON, Map.BLUE_MAZE_3, 0x4F, 0x1E, 0x00, 3, 3}, // Green Dragon
            #endif
            {Board.OBJECT_SWORD, Map.GOLD_CASTLE, 0x1F, 0x1E, 0x00, 0x00, 0x00}, // Sword
            #if DEBUG_EASTEREGG
            {Board.OBJECT_BRIDGE, Map.MAIN_HALL_RIGHT, 0x3F, 0x3E, 0x00, 0x00, 0x00}, // Bridge
            {Board.OBJECT_YELLOWKEY, Map.MAIN_HALL_RIGHT, 0x1F, 0x3E, 0x00, 0x00, 0x00}, // Yellow Key
            {Board.OBJECT_COPPERKEY, Map.MAIN_HALL_RIGHT, 0x79, 0x3E, 0x00, 0x00, 0x00}, // Copper Key
            #else
            {Board.OBJECT_BRIDGE, Map.WHITE_MAZE_3, 0x3F, 0x3E, 0x00, 0x00, 0x00}, // Bridge
            {Board.OBJECT_YELLOWKEY, Map.WHITE_MAZE_2, 0x1F, 0x3E, 0x00, 0x00, 0x00}, // Yellow Key
            {Board.OBJECT_COPPERKEY, Map.WHITE_MAZE_2, 0x79, 0x3E, 0x00, 0x00, 0x00}, // Copper Key
            #endif
            {Board.OBJECT_JADEKEY, Map.BLUE_MAZE_4, 0x79, 0x3E, 0x00, 0x00, 0x00}, // Jade Key
            {Board.OBJECT_WHITEKEY, Map.BLUE_MAZE_3, 0x1F, 0x3E, 0x00, 0x00, 0x00}, // White Key
            {Board.OBJECT_BLACKKEY, Map.RED_MAZE_4, 0x1F, 0x3E, 0x00, 0x00, 0x00}, // Black Key
            {Board.OBJECT_CRYSTALKEY1, Map.CRYSTAL_CASTLE, 0x4C, 0x53, 0x00, 0x00, 0x00}, // Crystal Key for Player 1
            {Board.OBJECT_CRYSTALKEY2, Map.CRYSTAL_CASTLE, 0x4C, 0x53, 0x00, 0x00, 0x00}, // Crystal Key for Player 2
            {Board.OBJECT_CRYSTALKEY3, Map.CRYSTAL_CASTLE, 0x4C, 0x53, 0x00, 0x00, 0x00}, // Crystal Key for Player 3
            {Board.OBJECT_BAT, Map.MAIN_HALL_CENTER, 0x1F, 0x1E, 0x00, 0, -3}, // Bat
#if DEBUG_EASTEREGG
            {Board.OBJECT_DOT, Map.MAIN_HALL_RIGHT, 0x1F, 0x0E, 0x00, 0x00, 0x00}, // Dot
#else
            {Board.OBJECT_DOT, Map.BLACK_MAZE_3, 0x44, 0x10, 0x00, 0x00, 0x00}, // Dot
#endif
            {Board.OBJECT_CHALISE, Map.BLACK_MAZE_2, 0x2F, 0x1E, 0x00, 0x00, 0x00}, // Challise
            {Board.OBJECT_MAGNET, Map.SOUTHWEST_ROOM, 0x7F, 0x1E, 0x00, 0x00, 0x00}, // Magnet
        };

        // Object locations (room and coordinate) for gauntlet
        //        - object, room, ox, oy, state, movement(ox/oy)
        private readonly int[,] gameGauntletObjects =
        {
            {Board.OBJECT_YELLOW_PORT, Map.GOLD_CASTLE, Portcullis.PORT_X, Portcullis.PORT_Y, 0x0C, 0x00, 0x00}, // Port 1
            {Board.OBJECT_BLACK_PORT, Map.BLACK_CASTLE, Portcullis.PORT_X, Portcullis.PORT_Y, 0x0C, 0x00, 0x00}, // Port 3
            {Board.OBJECT_NAME, Map.ROBINETT_ROOM, 0x4F, 0x67, 0x00, 0x00, 0x00}, // Robinett message
            {Board.OBJECT_NUMBER, Map.NUMBER_ROOM, 0x4F, 0x3E, 0x00, 0x00, 0x00}, // Starting number
            {Board.OBJECT_REDDRAGON, Map.BLUE_MAZE_1, 0x4F, 0x1E, 0x00, 0x00, 0x00}, // Red Dragon
            {Board.OBJECT_YELLOWDRAGON, Map.MAIN_HALL_CENTER, 0x4F, 0x1E, 0x00, 0x00, 0x00}, // Yellow Dragon
            {Board.OBJECT_GREENDRAGON, Map.MAIN_HALL_LEFT, 0x4F, 0x1E, 0x00, 0x00, 0x00} // Green Dragon
        };

        // Green Dragon's Object Matrix                                                                                      
        private int[] greenDragonMatrix =
        {
            Board.OBJECT_SWORD, Board.OBJECT_GREENDRAGON,       // runs from sword
            Board.OBJECT_JADEKEY, Board.OBJECT_GREENDRAGON,     // runs from Jade key
            Board.OBJECT_GREENDRAGON, Board.OBJECT_BALL,        // goes after any Ball
            Board.OBJECT_GREENDRAGON, Board.OBJECT_CHALISE,     // guards Chalise
            Board.OBJECT_GREENDRAGON, Board.OBJECT_BRIDGE,      // guards Bridge
            Board.OBJECT_GREENDRAGON, Board.OBJECT_MAGNET,      // guards Magnet
            Board.OBJECT_GREENDRAGON, Board.OBJECT_BLACKKEY    // guards Black Key
        };

        // Yellow Dragon's Object Matrix                                                                                      
        private int[] yellowDragonMatrix =
        {
            Board.OBJECT_SWORD, Board.OBJECT_YELLOWDRAGON,      // runs from sword
            Board.OBJECT_YELLOWKEY, Board.OBJECT_YELLOWDRAGON,  // runs from Yellow Key
            Board.OBJECT_YELLOWDRAGON, Board.OBJECT_BALL,       // goes after any Ball
            Board.OBJECT_YELLOWDRAGON, Board.OBJECT_CHALISE    // guards Challise
        };

        // Red Dragon's Object Matrix                                                                                      
        private int[] redDragonMatrix =
        {
            Board.OBJECT_SWORD, Board.OBJECT_REDDRAGON,         // runs from sword
            Board.OBJECT_COPPERKEY, Board.OBJECT_REDDRAGON,     // runs from Copper key
            Board.OBJECT_REDDRAGON, Board.OBJECT_BALL,          // goes after any Ball
            Board.OBJECT_REDDRAGON, Board.OBJECT_CHALISE,       // guards Chalise
            Board.OBJECT_REDDRAGON, Board.OBJECT_WHITEKEY       // guards White Key
        };


    }
}

