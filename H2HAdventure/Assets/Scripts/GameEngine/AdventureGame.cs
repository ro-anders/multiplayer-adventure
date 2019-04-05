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
        private const int ADVENTURE_SCREEN_WIDTH = Adv.ADVENTURE_SCREEN_WIDTH;
        private const int ADVENTURE_SCREEN_HEIGHT = Adv.ADVENTURE_SCREEN_HEIGHT;
        private const int ADVENTURE_OVERSCAN = Adv.ADVENTURE_OVERSCAN;
        private const int ADVENTURE_TOTAL_SCREEN_HEIGHT = Adv.ADVENTURE_TOTAL_SCREEN_HEIGHT;
        private const double ADVENTURE_FRAME_PERIOD = Adv.ADVENTURE_FRAME_PERIOD;
        private const int ADVENTURE_MAX_NAME_LENGTH = Adv.ADVENTURE_MAX_NAME_LENGTH;
        private const int MAX_OBJECTS = 32;                      // Should be plenty
        private const int MAX_DISPLAYABLE_OBJECTS = 2;             // The 2600 only has 2 Player (sprite) objects. Accuracy will be compromised if this is changed!
        private static readonly bool SHOW_OBJECT_FLICKER = true;

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
        private bool joystickDisabled; // No longer ever set, but left in in case we come up with a need
        private bool switchReset;
        private bool useMazeGuides;

        private int turnsSinceTimeCheck;
        private readonly int[] missedChecks = { 0, 0, 0 };

        private Sync sync;
        private readonly Transport transport;
        private readonly int thisPlayer;
        private BALL objectBall;

        private readonly OBJECT[] surrounds;

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
            bool inUseHelpPopups, bool inUseMazeGuides)
        {
            view = inView;

            numPlayers = inNumPlayers;
            thisPlayer = inThisPlayer;
            gameMode = inGameNum;
            isCooperative = (gameMode > Adv.GAME_MODE_3);
            useMazeGuides = inUseMazeGuides;
            timeToStartGame = 60 * 3;
            frameNumber = 0;

            // The map for game 3 is the same as 2.
            gameMapLayout = (gameMode == Adv.GAME_MODE_GAUNTLET ? Map.MAP_LAYOUT_SMALL :
                (gameMode == Adv.GAME_MODE_1 || gameMode == Adv.GAME_MODE_C_1 ? Map.MAP_LAYOUT_SMALL :
                Map.MAP_LAYOUT_BIG));
            gameMap = new Map(numPlayers, gameMapLayout, isCooperative, useMazeGuides);
            roomDefs = gameMap.roomDefs;
            gameBoard = new Board(gameMap, view);
            EasterEgg.setup(view, gameBoard);

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
            dragons[0] = new Dragon("grindle", 0, COLOR.LIMEGREEN, 2, greenDragonMatrix);
            dragons[1] = new Dragon("yorgle", 1, COLOR.YELLOW, 2, yellowDragonMatrix);
            dragons[2] = new Dragon("rhindle", 2, COLOR.RED, 3, redDragonMatrix);
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
            ports[0] = new Portcullis("gold gate", Map.GOLD_CASTLE, gameMap.getRoom(Map.GOLD_FOYER), goldKey); // Gold
            ports[1] = new Portcullis("white gate", Map.WHITE_CASTLE, gameMap.getRoom(Map.RED_MAZE_1), whiteKey); // White
            addAllRoomsToPort(ports[1], Map.RED_MAZE_3, Map.RED_MAZE_1);
            ports[2] = new Portcullis("black gate", Map.BLACK_CASTLE, gameMap.getRoom(Map.BLACK_FOYER), blackKey); // Black
            addAllRoomsToPort(ports[2], Map.BLACK_MAZE_1, Map.BLACK_MAZE_ENTRY);
            ports[2].addRoom(gameMap.getRoom(Map.BLACK_FOYER));
            ports[2].addRoom(gameMap.getRoom(Map.BLACK_INNERMOST_ROOM));
            ports[3] = new CrystalPortcullis(gameMap.getRoom(Map.CRYSTAL_FOYER), crystalKeys);
            ports[4] = new Portcullis("copper gate", Map.COPPER_CASTLE, gameMap.getRoom(Map.COPPER_FOYER), copperKey);
            ports[5] = new Portcullis("jade gate", Map.JADE_CASTLE, gameMap.getRoom(Map.JADE_FOYER), jadeKey);
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
            gameBoard.addObject(Board.OBJECT_BRIDGE, new OBJECT("bridge", objectGfxBridge, new byte[0], 0, COLOR.PURPLE,
                                                                OBJECT.RandomizedLocations.OPEN_OR_IN_CASTLE, 0x07));
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
            gameBoard.addObject(Board.OBJECT_MAGNET, new OBJECT("magnet", objectGfxMagnet, new byte[0], 0, COLOR.BLACK));

            // Setup the players
            bool useAltIcons = (gameMode == Adv.GAME_MODE_ROLE_PLAY);
            UnityEngine.Debug.Log((useAltIcons ? "" : "not ") + " using alt icons when game mode = " + gameMode);
            gameBoard.addPlayer(new BALL(0, ports[0], useAltIcons), thisPlayer == 0);
            Portcullis p2Home = (isCooperative ? ports[0] : ports[4]);
            gameBoard.addPlayer(new BALL(1, p2Home, useAltIcons), thisPlayer == 1);
            if (numPlayers > 2)
            {
                Portcullis p3Home = (isCooperative ? ports[0] : ports[5]);
                gameBoard.addPlayer(new BALL(2, p3Home, useAltIcons), thisPlayer == 2);
            }
            objectBall = gameBoard.getPlayer(thisPlayer);

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

            if (inUseHelpPopups)
            {
                popupMgr = new PopupMgr(gameBoard);
                popupMgr.EnteredRoomShowPopups(objectBall.room);

            }
        }

        public void PrintDisplay()
        {
            // get the playfield data
            int displayedRoom = (displayWinningRoom ? winningRoom : objectBall.displayedRoom);

            ROOM currentRoom = roomDefs[displayedRoom];
            byte[] roomData = currentRoom.graphicsData;

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
            // get the playfield mirror flag
            bool mirror = (currentRoom.flags & ROOM.FLAG_MIRROR) > 0;

            //
            // Extract the playfield register bits and paint the playfield
            // The playfied register is 20 bits wide encoded across 3 bytes
            // as follows:
            //    PF0   |  PF1   |  PF2
            //  xxxx4567|76543210|01234567
            // Each set bit indicates playfield color - else background color -
            // the size of each block is 8 x 32, and the drawing is shifted
            // upwards by 16 pixels
            //

            // mask values for playfield bits
            byte[] shiftreg = {
                0x10,0x20,0x40,0x80,
                0x80,0x40,0x20,0x10,0x8,0x4,0x2,0x1,
                0x1,0x2,0x4,0x8,0x10,0x20,0x40,0x80
            };

            // each cell is 8 x 32
            const int cell_width = 8;
            const int cell_height = 32;


            // draw the playfield
            for (int cy = 0; cy <= 6; cy++)
            {
                byte pf0 = roomData[(cy * 3) + 0];
                byte pf1 = roomData[(cy * 3) + 1];
                byte pf2 = roomData[(cy * 3) + 2];

                int ypos = 6 - cy;

                for (int cx = 0; cx < 20; cx++)
                {
                    int bit = 0;

                    if (cx < 4)
                        bit = pf0 & shiftreg[cx];
                    else if (cx < 12)
                        bit = pf1 & shiftreg[cx];
                    else
                        bit = pf2 & shiftreg[cx];

                    if (bit != 0)
                    {
                        view.Platform_PaintPixel(color.r, color.g, color.b, cx * cell_width, ypos * cell_height, cell_width, cell_height);
                        if (mirror)
                            view.Platform_PaintPixel(color.r, color.g, color.b, (cx + 20) * cell_width, ypos * cell_height, cell_width, cell_height);
                        else
                            view.Platform_PaintPixel(color.r, color.g, color.b, ((40 - (cx + 1)) * cell_width), ypos * cell_height, cell_width, cell_height);
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
            DrawObjects(displayedRoom);

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
            ball.room = ball.homeGate.room;               // Put us at our home castle
            ball.previousRoom = ball.room;
            ball.displayedRoom = ball.room;
            ball.x = 0x50 * 2;                  //
            ball.y = 0x20 * 2;                  //
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
            OtherBallMovement();
            OthersPickupPutdown();

            // move the dragons
            SyncDragons();

            // Move the bat
            RemoteAction batAction = sync.GetNextBatAction();
            while ((batAction != null) && bat.exists())
            {
                bat.handleAction(batAction, objectBall);
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
                otherReset = sync.GetNextResetAction();
            }

            // Handle won games last.
            PlayerWinAction lost = sync.GetGameWon();
            if (lost != null)
            {
                WinGame(lost.winInRoom);
                lost = null;
            }


        }

        public void Adventure_Run()
        {
            ++frameNumber;
            SyncWithOthers();
            checkPlayers();

            // read the console switches every frame
            bool reset = false;
            view.Platform_ReadConsoleSwitches(ref reset);

            // If joystick is disabled and we hit the reset switch we don't treat it as a reset but as
            // a enable the joystick.  The next time you hit the reset switch it will work as a reset.
            if (joystickDisabled && switchReset && !reset)
            {
                joystickDisabled = false;
                switchReset = false;
            }

            // Reset switch
            if ((gameState != GAMESTATE_WIN) && switchReset && !reset && (EasterEgg.eggState != EGG_STATE.DEBRIEF))
            {
                if (gameState != GAMESTATE_GAMESELECT)
                {
                    // In the role playing version, the cleric has to be on the screen
                    // to reset
                    if ((gameMode != Adv.GAME_MODE_ROLE_PLAY) || (gameBoard.getPlayer(2).room == objectBall.room))
                    {
                        ResetPlayer(objectBall);
                        // Broadcast to everyone else
                        PlayerResetAction action = new PlayerResetAction();
                        sync.BroadcastAction(action);
                    }
                }
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
                    }
                    else
                    {
                        int displayNum = timeToStartGame / 60;
                        gameBoard[Board.OBJECT_NUMBER].state = displayNum;

                        // Display the room and objects
                        objectBall.room = 0;
                        objectBall.previousRoom = 0;
                        objectBall.displayedRoom = 0;
                        objectBall.x = 0;
                        objectBall.y = 0;
                        objectBall.previousX = 0;
                        objectBall.previousY = 0;
                        PrintDisplay();
                    }
                }
                else if ((gameState == GAMESTATE_ACTIVE_1) || (gameState == GAMESTATE_ACTIVE_2) || (gameState == GAMESTATE_ACTIVE_3))
                {
                    // Has someone won the game.
                    if (checkWonGame())
                    {
                        WinGame(objectBall.room);
                        PlayerWinAction won = new PlayerWinAction(objectBall.room);
                        sync.BroadcastAction(won);
                        // Report back to the server.
                        view.Platform_ReportToServer("Has won a game");
                    }
                    else if (EasterEgg.isGauntletTimeUp(frameNumber))
                    {
                        EasterEgg.endGauntlet();
                        gameState = GAMESTATE_WIN;
                        view.Platform_GameChange(GAME_CHANGES.GAME_ENDED);
                        winningRoom = objectBall.displayedRoom;
                    }
                    else
                    {
                        // Read joystick
                        view.Platform_ReadJoystick(ref joyLeft, ref joyUp, ref joyRight, ref joyDown, ref joyFire);

                        if (EasterEgg.shouldStartGauntlet(frameNumber))
                        {
                            EasterEgg.startGauntlet();
                            gameMode = Adv.GAME_MODE_GAUNTLET;
                        }

                        if (gameState == GAMESTATE_ACTIVE_1)
                        {
                            // Move balls
                            ThisBallMovement();
                            for (int i = 0; i < numPlayers; ++i)
                            {
                                if (i != thisPlayer)
                                {
                                    BallMovement(gameBoard.getPlayer(i));
                                }
                            }

                            // Move the carried object
                            MoveCarriedObjects();

                            // Collision check the balls in their new coordinates against walls and objects
                            for (int i = 0; i < numPlayers; ++i)
                            {
                                BALL nextBall = gameBoard.getPlayer(i);
                                CollisionCheckBallWithEverything(nextBall, nextBall.room, nextBall.x, nextBall.y, false);
                            }

                            // Setup the room and object
                            PrintDisplay();

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
                                bat.moveOneTurn(sync, objectBall);
                            }

                            // Move and deal with portcullises
                            Portals();

                            // Display the room and objects
                            PrintDisplay();

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
                                if ((gameMode == Adv.GAME_MODE_GAUNTLET) && (dragon.state == Dragon.EATEN) && (dragon.eaten == objectBall))
                                {
                                    ResetPlayer(objectBall);
                                    // Broadcast to everyone else
                                    PlayerResetAction action = new PlayerResetAction();
                                    sync.BroadcastAction(action);

                                }
                            }

                            for (int i = 0; i < numPlayers; ++i)
                            {
                                ReactToCollisionY(gameBoard.getPlayer(i));
                            }


                            // Deal with the magnet
                            Magnet();

                            if (popupMgr.HasPopups &&
                                 (frameNumber > lastPopupTime + (PopupMgr.MIN_SECONDS_BETWEEN_POPUPS*60) ))
                            {
                                Popup popup = popupMgr.GetNextPopup();
                                if (popup != null)
                                {
                                    view.Platform_PopupHelp(popup.Message, popup.ImageName);
                                    lastPopupTime = frameNumber;
                                }
                            }

                            // Display the room and objects
                            PrintDisplay();

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
                    PrintDisplay();
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
            if((gameMode == Adv.GAME_MODE_1) || (gameMode == Adv.GAME_MODE_C_1))
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
                int xpos = p[ctr, 2];
                int ypos = p[ctr, 3];
                int state = p[ctr, 4];
                int movementX = p[ctr, 5];
                int movementY = p[ctr, 6];

                OBJECT toInit = gameBoard[objct];
                toInit.init(room, xpos, ypos, state, movementX, movementY);
            }

            // Hide the jade if only 2 player and both new keys if cooperative
            if ((numPlayers <= 2) || (isCooperative))
            {
                gameBoard[Board.OBJECT_JADEKEY].setExists(false);
                gameBoard[Board.OBJECT_JADEKEY].randomPlacement = OBJECT.RandomizedLocations.FIXED_LOCATION;
            }
            if (isCooperative)
            {
                gameBoard[Board.OBJECT_COPPERKEY].setExists(false);
                gameBoard[Board.OBJECT_COPPERKEY].randomPlacement = OBJECT.RandomizedLocations.FIXED_LOCATION;
            }

            // Put objects in random rooms for level 3.
            // Only first player does this and then broadcasts to other players.
            bool gameRandomized = ((gameMode == Adv.GAME_MODE_3) ||
              (gameMode == Adv.GAME_MODE_C_3) ||
              (gameMode == Adv.GAME_MODE_ROLE_PLAY));
            if (gameRandomized && (thisPlayer == 0))
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
            int numObjects = gameBoard.getNumObjects();
            for (int objCtr = 0; objCtr < numObjects; ++objCtr)
            {
                OBJECT nextObj = gameBoard.getObject(objCtr);
                if (nextObj.randomPlacement != OBJECT.RandomizedLocations.FIXED_LOCATION)
                {
                    bool ok = false;
                    while (!ok)
                    {
                        int randomKey = randomGen.Next(numRooms);
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

        float volumeAtDistance(int room)
        {
            float NEAR_VOLUME = MAX.VOLUME / 3;
            float FAR_VOLUME = MAX.VOLUME / 9;

            int distance = gameMap.distance(room, objectBall.room);

            float volume = 0.0f;
            switch (distance)
            {
                case 0:
                    volume = MAX.VOLUME;
                    break;
                case 1:
                    volume = NEAR_VOLUME;
                    break;
                case 2:
                    volume = FAR_VOLUME;
                    break;
                default:
                    volume = 0;
                    break;
            }
            return volume;
        }

        /**
         * Returns true if this player has gotten the chalise to their home castle and won the game, or, if
         * this is the gauntlet, if the player has reached the gold castle.
         */
        bool checkWonGame()
        {
            bool won = false;
            if (gameMode == Adv.GAME_MODE_GAUNTLET)
            {
                won = (objectBall.isGlowing() && (objectBall.room == objectBall.homeGate.insideRoom));
                if (won && (EasterEgg.eggState == EGG_STATE.IN_GAUNTLET))
                {
                    EasterEgg.winEgg();
                }
            }
            else
            {
                // Player MUST be holding the chalise to win (or holding the bat holding the chalise).
                // Another player can't win for you.
                if ((objectBall.linkedObject == Board.OBJECT_CHALISE) ||
                    ((objectBall.linkedObject == Board.OBJECT_BAT) && (bat.linkedObject == Board.OBJECT_CHALISE)))
                {
                    // Player either has to bring the chalise into the castle or touch the chalise to the gate
                    if (gameBoard[Board.OBJECT_CHALISE].room == objectBall.homeGate.insideRoom)
                    {
                        won = true;
                    }
                    else
                    {
                        if ((objectBall.room == objectBall.homeGate.room) &&
                            (objectBall.homeGate.state == Portcullis.OPEN_STATE) &&
                            gameBoard.CollisionCheckObjectObject(objectBall.homeGate, gameBoard[Board.OBJECT_CHALISE]))
                        {

                            won = true;
                        }
                    }
                }
            }
            return won;
        }

        void WinGame(int winRoom)
        {
            // Go to won state
            gameState = GAMESTATE_WIN;
            winFlashTimer = 0xff;
            winningRoom = winRoom;
            view.Platform_GameChange(GAME_CHANGES.GAME_ENDED);

            // Play the sound
            view.Platform_MakeSound(SOUND.WON, MAX.VOLUME);
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
                        if (ball == objectBall)
                        {
                            // If this is adjusting how the current player holds an object,
                            // we broadcast to other players as a pickup action
                            PlayerPickupAction action = new PlayerPickupAction(ball.hitObject,
                                objectBall.linkedObjectX, objectBall.linkedObjectY, Board.OBJECT_NONE, 0, 0, 0);
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
                CollisionCheckBallWithEverything(ball, ball.room, ball.x, ball.y, true);
            }
        }

        void ReactToCollisionY(BALL ball)
        {
            if ((ball.hit) && (ball.vely != 0))
            {
                if ((ball.hitObject > Board.OBJECT_NONE) && (ball.hitObject == ball.linkedObject))
                {
                    ball.linkedObjectY += ball.vely;
                    if (ball == objectBall)
                    {
                        // If this is adjusting how the current player holds an object,
                        // we broadcast to other players as a pickup action
                        PlayerPickupAction action = new PlayerPickupAction(ball.hitObject,
                            objectBall.linkedObjectX, objectBall.linkedObjectY, Board.OBJECT_NONE, 0, 0, 0);
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
                if (ball.x >= Board.RIGHT_EDGE)
                {
                    ball.x = Board.ENTER_AT_LEFT;
                    ball.room = ball.displayedRoom; // The displayed room hasn't changed
                }
                else if (ball.x < Board.LEFT_EDGE)
                {
                    ball.x = Board.ENTER_AT_RIGHT;
                    ball.room = ball.displayedRoom;
                }

                CollisionCheckBallWithEverything(ball, ball.displayedRoom, ball.x, ball.y, false);
            }
        }

        void ThisBallMovement()
        {
            // Read the joystick and translate into a velocity
            int prevVelX = objectBall.velx;
            int prevVelY = objectBall.vely;
            if (!joystickDisabled)
            {
                int newVelY = 0;
                if (joyUp)
                {
                    if (!joyDown)
                    {
                        newVelY = 6;
                    }
                }
                else if (joyDown)
                {
                    newVelY = -6;
                }
                objectBall.vely = newVelY;

                int newVelX = 0;
                if (joyRight)
                {
                    if (!joyLeft)
                    {
                        newVelX = 6;
                    }
                }
                else if (joyLeft)
                {
                    newVelX = -6;
                }
                objectBall.velx = newVelX;
            }

            BallMovement(objectBall);

            if (!joystickDisabled && ((objectBall.velx != prevVelX) || (objectBall.vely != prevVelY)))
            {
                PlayerMoveAction moveAction = new PlayerMoveAction(objectBall.room, objectBall.x, objectBall.y, objectBall.velx, objectBall.vely);
                sync.BroadcastAction(moveAction);
            }
        }

        void BallMovement(BALL ball)
        {
            bool newRoom = false;
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
            if (ball.y > Board.TOP_EDGE)
            {
                ball.y = Board.ENTER_AT_BOTTOM;
                ball.room = roomDefs[ball.room].roomUp;
                newRoom = true;
            }
            else if (ball.y < Board.BOTTOM_EDGE)
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
                    ball.y = Board.ENTER_AT_TOP;
                    ball.room = roomDefs[ball.room].roomDown;
                    newRoom = true;
                }
            }

            if (ball.x >= Board.RIGHT_EDGE)
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
                    ball.x = Board.ENTER_AT_LEFT;

                    // Figure out the room to the right (which might be the secret room)
                    ball.room = (ball.room == Map.MAIN_HALL_RIGHT && gameBoard[Board.OBJECT_DOT].room == Map.MAIN_HALL_RIGHT ?
                                  Map.ROBINETT_ROOM : roomDefs[ball.room].roomRight);
                    newRoom = true;

                }
            }
            else if (ball.x < Board.LEFT_EDGE)
            {
                // Can't diagonally switch rooms.  If trying, only allow changing rooms vertically
                if (ball.room != ball.previousRoom)
                {
                    ball.x = ball.previousX;
                    ball.velx = 0;
                }
                else
                {
                    ball.x = Board.ENTER_AT_RIGHT;
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

            if (ball == objectBall)
            {
                if (ball.room == Map.CRYSTAL_CASTLE)
                {
                    EasterEgg.foundCastle(objectBall);
                }
                else if (ball.room == Map.ROBINETT_ROOM)
                {
                    EasterEgg.enteredRobinettRoom();
                }
            }

            ball.displayedRoom = ball.room;

        }

        // Check if the ball would be hitting anything (wall, object, ...)
        // ball - the ball to check
        // room - the room in which to check
        // x - the x position to check
        // y - the y position to check
        // allowBridge - if moving vertically, the bridge can allow you to not collide into a wall
        // hitObject - if we hit an object, will set this reference to the object we hit.  If NULL, will not try to set it.
        //
        private void CollisionCheckBallWithEverything(BALL ball, int checkRoom, int checkX, int checkY, bool allowBridge)
        {
            int hitObject = CollisionCheckBallWithAllObjects(ball);
            bool hitWall = false;
            if (hitObject == Board.OBJECT_NONE)
            {
                bool crossingBridge = allowBridge && CrossingBridge(checkRoom, checkX, checkY, ball);
                hitWall = !crossingBridge && CollisionCheckBallWithWalls(checkRoom, checkX, checkY);
            }
            ball.hitObject = hitObject;
            ball.hit = hitWall || (hitObject > Board.OBJECT_NONE);
        }


        void OtherBallMovement()
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
                    // If something causes a sound, we need to know how far away it is.
                    float volume = volumeAtDistance(dragon.room);
                    dragon.syncAction(nextState, volume);
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
                        ((dragon.room != objectBall.room) ||
                        (objectBall.distanceTo(dragon.x, dragon.y) > nextMove.distance)))
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
                        nextBall.y = Board.ENTER_AT_BOTTOM;
                        nextBall.previousY = nextBall.y;
                        nextBall.vely = 0;
                        nextBall.velx = 0;
                        // make sure it stays unlocked in case we are walking in with the key
                        nextPort.forceOpen();
                        // Report to all the other players only if its the current player entering
                        if (ctr == thisPlayer)
                        {
                            PortcullisStateAction gateAction =
                            new PortcullisStateAction(nextPort.getPKey(), nextPort.state, nextPort.allowsEntry);
                            sync.BroadcastAction(gateAction);

                            // Report the ball entering the castle
                            PlayerMoveAction moveAction = new PlayerMoveAction(objectBall.room, objectBall.x, objectBall.y, objectBall.velx, objectBall.vely);
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
                            view.Platform_MakeSound(SOUND.GLOW, volumeAtDistance(nextBall.room));
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
                if (objct.y > 0x6A)
                {
                    objct.y = 0x0D;
                    objct.room = roomDefs[objct.room].roomUp;
                }

                // Check and Deal with Left
                if (objct.x < 0x03)
                {
                    objct.x = 0x9A;
                    objct.room = roomDefs[objct.room].roomLeft;
                }

                // Check and Deal with Down
                if (objct.y < 0x0D)
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
                        objct.y = 0x69;
                        objct.room = roomDefs[objct.room].roomDown;
                    }
                }

                // Check and Deal with Right
                if (objct.x > 0x9B)
                {
                    objct.x = 0x03;
                    objct.room = roomDefs[objct.room].roomRight;
                }

                // If the objct has a linked object
                if ((objct == bat) && (bat.linkedObject != Board.OBJECT_NONE))
                {
                    OBJECT linkedObj = gameBoard[bat.linkedObject];
                    linkedObj.x = objct.x + bat.linkedObjectX;
                    linkedObj.y = objct.y + bat.linkedObjectY;
                    linkedObj.room = objct.room;
                }
            }
        }

        void OthersPickupPutdown()
        {
            PlayerPickupAction newaction = sync.GetNextPickupAction();
            while (newaction != null)
            {
                int actorNum = newaction.sender;
                BALL actor = gameBoard.getPlayer(actorNum);
                if (newaction.dropObject != Board.OBJECT_NONE)
                {
                }
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
                        gameBoard.makeSound(SOUND.PUTDOWN, volumeAtDistance(actor.room));
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
                    gameBoard.makeSound(SOUND.PICKUP, volumeAtDistance(actor.room));
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
            if (!joystickDisabled && joyFire && (objectBall.linkedObject >= 0))
            {
                int dropped = objectBall.linkedObject;
                OBJECT droppedObject = gameBoard[dropped];

                // Put down the current object!
                objectBall.linkedObject = Board.OBJECT_NONE;

                if ((gameOptions & GAMEOPTION_NO_HIDE_KEY_IN_CASTLE) != 0)
                {
                    unhideKey(droppedObject);
                }

                // Tell other clients about the drop
                PlayerPickupAction action = new PlayerPickupAction(Board.OBJECT_NONE, 0, 0, dropped, droppedObject.room,
                                                                   droppedObject.x, droppedObject.y);
                sync.BroadcastAction(action);

                // Play the sound
                view.Platform_MakeSound(SOUND.PUTDOWN, MAX.VOLUME);
            }
            else
            {
                // See if we are touching any carryable objects
                Board.ObjIter iter = gameBoard.getCarryableObjects();
                int hitIndex = CollisionCheckBallWithObjects(objectBall, iter);
                if (hitIndex > Board.OBJECT_NONE)
                {
                    // Ignore the object we are already carrying
                    if (hitIndex == objectBall.linkedObject)
                    {
                        // Check the remainder of the objects
                        hitIndex = CollisionCheckBallWithObjects(objectBall, iter);
                    }

                    if (hitIndex > Board.OBJECT_NONE)
                    {
                        // Collect info about whether we are also dropping an object (for when we broadcast the action)
                        PlayerPickupAction action = new PlayerPickupAction(Board.OBJECT_NONE, 0, 0, Board.OBJECT_NONE, 0, 0, 0);
                        int dropIndex = objectBall.linkedObject;
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
                                objectBall.linkedObject = Board.OBJECT_NONE;
                                sync.BroadcastAction(action);
                            }
                        }
                        else
                        {

                            // Pick up this object!
                            objectBall.linkedObject = hitIndex;

                            // calculate the XY offsets from the ball's position
                            objectBall.linkedObjectX = gameBoard[hitIndex].x - (objectBall.x / 2);
                            objectBall.linkedObjectY = gameBoard[hitIndex].y - (objectBall.y / 2);

                            // Take it away from anyone else if they were holding it.
                            for (int ctr = 0; ctr < numPlayers; ++ctr)
                            {
                                if ((ctr != thisPlayer) && (gameBoard.getPlayer(ctr).linkedObject == hitIndex))
                                {
                                    gameBoard.getPlayer(ctr).linkedObject = Board.OBJECT_NONE;
                                }
                            }

                            if ((hitIndex >= Board.OBJECT_CRYSTALKEY1) && (hitIndex <= Board.OBJECT_CRYSTALKEY3))
                            {
                                EasterEgg.foundKey();
                            }

                            // Broadcast that we picked up an object
                            action.setPickup(hitIndex, objectBall.linkedObjectX, objectBall.linkedObjectY);
                            sync.BroadcastAction(action);

                        }

                        // Play the sound
                        view.Platform_MakeSound(SOUND.PICKUP, MAX.VOLUME);
                    }
                }
            }
        }

        void Surround()
        {
            // get the playfield data
            int roomNum = objectBall.room;
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
                        surrounds[ctr].x = (nextBall.x - 0x1E) / 2;
                        surrounds[ctr].y = (nextBall.y + 0x18) / 2;
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
                    bool seen = false;
                    for (int ctr = 0; !seen && ctr < numPlayers; ++ctr)
                    {
                        seen = (gameBoard.getPlayer(ctr).room == port.room);
                    }
                    if (seen)
                    {
                        // Check if a key unlocks the gate
                        PortcullisStateAction gateAction = port.checkKeyInteraction();
                        if (gateAction != null)
                        {
                            sync.BroadcastAction(gateAction);
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
            OBJECT magnet = gameBoard[Board.OBJECT_MAGNET];

            for (int i = 0; i < magnetMatrix.Length; ++i)
            {
                // Look for items in the magnet matrix that are in the same room as the magnet
                OBJECT objct = gameBoard[magnetMatrix[i]];
                if ((objct.room == magnet.room) && (objct.exists()))
                {
                    bool held = false;
                    for(int playerCtr=0; playerCtr<numPlayers && !held; ++playerCtr)
                    {
                        held = gameBoard.getPlayer(playerCtr).linkedObject == magnetMatrix[i];
                    }
                    if (!held)
                    {
                        // horizontal axis
                        if (objct.x < magnet.x)
                            objct.x++;
                        else if (objct.x > magnet.x)
                            objct.x--;

                        // vertical axis - offset by the height of the magnet so items stick to the "bottom"
                        if (objct.y < (magnet.y - magnet.gfxData[0].Length))
                            objct.y++;
                        else if (objct.y > (magnet.y - magnet.gfxData[0].Length))
                            objct.y--;
                    }

                    // Only attract the first item found in the matrix
                    break;
                }
            }
        }

        void DrawObjects(int room)
        {
            // Clear out the display list
            int[] displayList = new int[MAX_OBJECTS];
            for (int ctr = 0; ctr < MAX_OBJECTS; ctr++)
            {
                displayList[ctr] = Board.OBJECT_NONE;
            }

            // Create a list of all the objects that want to be drawn
            int numAdded = 0;

            for (int ctr = 0; ctr < numPlayers; ++ctr)
            {
                if (surrounds[ctr].room == room)
                {
                    displayList[numAdded++] = Board.OBJECT_SURROUND - ctr;
                }
            }

            int colorFirst = -1;
            int colorLast = -1;

            Board.ObjIter iter = gameBoard.getObjects();
            while (iter.hasNext())
            {
                OBJECT toDisplay = iter.next();
                // Init it to not displayed
                toDisplay.displayed = false;
                if (toDisplay.room == room)
                {
                    // This object is in the current room - add it to the list
                    displayList[numAdded++] = toDisplay.getPKey();

                    if (colorFirst < 0) colorFirst = toDisplay.color;
                    colorLast = toDisplay.color;
                }
            }

            // Now display the objects in the list, up to the max number of objects at a time

            if (numAdded <= MAX_DISPLAYABLE_OBJECTS)
                displayListIndex = 0;
            else
            {
                if (displayListIndex > numAdded)
                    displayListIndex = 0;
                if (displayListIndex > MAX_OBJECTS)
                    displayListIndex = 0;
                if (displayList[displayListIndex] == Board.OBJECT_NONE)
                    displayListIndex = 0;
            }

            for (int ctr = 0; ctr < numPlayers; ++ctr)
            {
                surrounds[ctr].displayed = false;
            }

            int numDisplayed = 0;
            int i = displayListIndex;
            //
            // If more than MAX_DISPLAYABLE_OBJECTS are needed to be drawn, we multiplex/cycle through them
            // Note that this also (intentionally) effects collision checking, as per the original game!!
            //
            while ((numDisplayed++) < numAdded && (numDisplayed <= MAX_DISPLAYABLE_OBJECTS))
            {
                if (displayList[i] > Board.OBJECT_NONE)
                {
                    OBJECT toDraw = gameBoard[displayList[i]];
                    if (SHOW_OBJECT_FLICKER)
                    {
                        DrawObject(toDraw);
                    }
                    toDraw.displayed = true;
                    colorLast = toDraw.color;
                }
                else if (displayList[i] <= Board.OBJECT_SURROUND)
                {
                    surrounds[Board.OBJECT_SURROUND - displayList[i]].displayed = true;
                }

                // wrap to the beginning of the list if we've reached the end
                ++i;
                if (i > MAX_OBJECTS)
                    i = 0;
                else if (displayList[i] == Board.OBJECT_NONE)
                    i = 0;
            }

            if (!SHOW_OBJECT_FLICKER)
            {
                // Just paint everything in this room so we bypass the flicker if desired
                Board.ObjIter iter2 = gameBoard.getObjects();
                while (iter2.hasNext())
                {
                    OBJECT next = iter2.next();
                    if (next.room == room)
                        DrawObject(next);
                }
            }

            if ((roomDefs[room].flags & ROOM.FLAG_LEFTTHINWALL) > 0)
            {
                // Position missile 00 to 0D,00 - left thin wall
                COLOR color = COLOR.table((colorFirst > 0) ? colorFirst : COLOR.BLACK);
                view.Platform_PaintPixel(color.r, color.g, color.b, 0x0D * 2, 0x00 * 2, 4, ADVENTURE_TOTAL_SCREEN_HEIGHT);
            }
            if ((roomDefs[room].flags & ROOM.FLAG_RIGHTTHINWALL) > 0)
            {
                // Position missile 01 to 96,00 - right thin wall
                COLOR color = COLOR.table((colorFirst > 0) ? colorLast : COLOR.BLACK);
                view.Platform_PaintPixel(color.r, color.g, color.b, 0x96 * 2, 0x00 * 2, 4, ADVENTURE_TOTAL_SCREEN_HEIGHT);
            }
        }

        private void DrawBall(BALL ball, COLOR color)
        {
            int left = (ball.x - 4) & ~0x00000001;
            int bottom = (ball.y - 10) & ~0x00000001; // Don't know why ball is drawn 2 pixels below y value

            // scan the data
            for (int row = bottom + 7, ctr = 0; row >= bottom; --row, ++ctr)
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
            // Get object color, size, and position
            COLOR color = objct.color == COLOR.FLASH ? GetFlashColor() : COLOR.table(objct.color);
            int cx = objct.x * 2;
            int cy = objct.y * 2;
            int size = (objct.size / 2) + 1;

            // Look up the index to the current state for this object
            int stateIndex = objct.states.Length > objct.state ? objct.states[objct.state] : 0;

            // Get the height, then the data
            byte[] dataP = objct.gfxData[stateIndex];
            int objHeight = dataP.Length;

            // Adjust for proper position
            cx -= Board.CLOCKS_HSYNC;
            cy -= Board.CLOCKS_VSYNC;

            // scan the data
            for (int i = 0; i < objHeight; i++)
            {
                byte rowByte = dataP[i];
                // Parse the row - each bit is a 2 x 2 block
                for (int bit = 0; bit < 8; bit++)
                {
                    if ((rowByte & (1 << (7 - bit))) > 0)
                    {
                        int x = cx + (bit * 2 * size);
                        if (x >= ADVENTURE_SCREEN_WIDTH)
                            x -= ADVENTURE_SCREEN_WIDTH;
                        view.Platform_PaintPixel(color.r, color.g, color.b, x, cy, 2 * size, 2);
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

            // The playfield is drawn partially in the overscan area, so shift that out here
            y -= 30;

            // get the playfield data
            ROOM currentRoom = roomDefs[room];
            byte[] roomData = currentRoom.graphicsData;

            // get the playfield mirror flag
            bool mirror = (currentRoom.flags & ROOM.FLAG_MIRROR) > 0;

            // mask values for playfield bits
            byte[] shiftreg =
            {
        0x10,0x20,0x40,0x80,
        0x80,0x40,0x20,0x10,0x8,0x4,0x2,0x1,
        0x1,0x2,0x4,0x8,0x10,0x20,0x40,0x80
    };

            // each cell is 8 x 32
            int cell_width = 8;
            int cell_height = 32;

            if (((currentRoom.flags & ROOM.FLAG_LEFTTHINWALL) > 0) && ((x - (4 + 4)) < 0x0D * 2) && ((x + 4) > 0x0D * 2))
            {
                hitWall = true;
            }
            if (((currentRoom.flags & ROOM.FLAG_RIGHTTHINWALL) > 0) && ((x + 4) > 0x96 * 2) && ((x - (4 + 4) < 0x96 * 2)))
            {
                // If the dot is in this room, allow passage through the wall into the Easter Egg room
                if (gameBoard[Board.OBJECT_DOT].room != room)
                    hitWall = true;
            }

            // Check each bit of the playfield data to see if they intersect the ball
            for (int cy = 0; (cy <= 6) & !hitWall; cy++)
            {
                byte pf0 = roomData[(cy * 3) + 0];
                byte pf1 = roomData[(cy * 3) + 1];
                byte pf2 = roomData[(cy * 3) + 2];

                int ypos = 6 - cy;

                for (int cx = 0; cx < 20; cx++)
                {
                    byte bit = 0;

                    if (cx < 4)
                        bit = (byte)(pf0 & shiftreg[cx]);
                    else if (cx < 12)
                        bit = (byte)(pf1 & shiftreg[cx]);
                    else
                        bit = (byte)(pf2 & shiftreg[cx]);

                    if (bit != 0)
                    {
                        if (Board.HitTestRects(x - 4, (y - 4), 8, 8, cx * cell_width, (ypos * cell_height), cell_width, cell_height))
                        {
                            hitWall = true;
                            break;
                        }

                        if (mirror)
                        {
                            if (Board.HitTestRects(x - 4, (y - 4), 8, 8, (cx + 20) * cell_width, (ypos * cell_height), cell_width, cell_height))
                            {
                                hitWall = true;
                                break;
                            }
                        }
                        else
                        {
                            if (Board.HitTestRects(x - 4, (y - 4), 8, 8, ((40 - (cx + 1)) * cell_width), (ypos * cell_height), cell_width, cell_height))
                            {
                                hitWall = true;
                                break;
                            }
                        }

                    }

                }
            }

            return hitWall;
        }

        private bool CrossingBridge(int room, int x, int y, BALL ball)
        {
            // Check going through the bridge
            OBJECT bridge = gameBoard[Board.OBJECT_BRIDGE];
            if ((bridge.room == room)
                && (ball.linkedObject != Board.OBJECT_BRIDGE))
            {
                int xDiff = (x / 2) - bridge.x;
                if ((xDiff >= 0x0A) && (xDiff <= 0x17))
                {
                    int yDiff = bridge.y - (y / 2);

                    if ((yDiff >= -5) && (yDiff <= 0x15))
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
            bool collision = (objct.displayed &&
                              objct.isTangibleTo(thisPlayer) &&
                              (ball.room == objct.room) &&
                              (CollisionCheckObject(objct, ball.x - 4, (ball.y - 1), 8, 8)) ? true : false);
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
                if (message.Length > 0) {
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

        // Object #0A : State FF : Graphic                                                                                   
        private static byte[][] objectGfxBridge =
        { new byte[] {
            0xC3,                  // XX    XX                                                                  
            0xC3,                  // XX    XX                                                                  
            0xC3,                  // XX    XX                                                                  
            0xC3,                  // XX    XX                                                                  
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0xC3,                  // XX    XX                                                                  
            0xC3,                  // XX    XX                                                                  
            0xC3,                  // XX    XX                                                                  
            0xC3                   // XX    XX                                                                  
        } };

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


        // Object #11 : State FF : Graphic                                                                                   
        private static byte[][] objectGfxMagnet =
        { new byte[] {
            0x3C,                  //   XXXX                                                                    
            0x7E,                  //  XXXXXX                                                                   
            0xE7,                  // XXX  XXX                                                                  
            0xC3,                  // XX    XX                                                                  
            0xC3,                  // XX    XX                                                                  
            0xC3,                  // XX    XX                                                                  
            0xC3,                  // XX    XX                                                                  
            0xC3                   // XX    XX                                                                  
        } };

        // Indexed array of all objects and their properties
        //
        // Object locations (room and coordinate) for game 01
        //        - object, room, x, y, state, movement(x/y)
        private readonly int[,] game1Objects =
        {
            {Board.OBJECT_YELLOW_PORT, Map.GOLD_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00}, // Port 1
            {Board.OBJECT_COPPER_PORT, Map.COPPER_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00}, // Port 4
            {Board.OBJECT_JADE_PORT, Map.JADE_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00}, // Port 5
            {Board.OBJECT_WHITE_PORT, Map.WHITE_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00}, // Port 2
            {Board.OBJECT_BLACK_PORT, Map.BLACK_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00}, // Port 3
            {Board.OBJECT_NAME, Map.ROBINETT_ROOM, 0x50, 0x69, 0x00, 0x00, 0x00}, // Robinett message
            {Board.OBJECT_NUMBER, Map.NUMBER_ROOM, 0x50, 0x40, 0x00, 0x00, 0x00}, // Starting number
            {Board.OBJECT_YELLOWDRAGON, Map.MAIN_HALL_LEFT, 0x50, 0x20, 0x00, 0x00, 0x00}, // Yellow Dragon
            {Board.OBJECT_GREENDRAGON, Map.SOUTHEAST_ROOM, 0x50, 0x20, 0x00, 0x00, 0x00}, // Green Dragon
            {Board.OBJECT_SWORD, Map.GOLD_FOYER, 0x20, 0x20, 0x00, 0x00, 0x00}, // Sword
            {Board.OBJECT_BRIDGE, Map.BLUE_MAZE_5, 0x2A, 0x37, 0x00, 0x00, 0x00}, // Bridge
            {Board.OBJECT_YELLOWKEY, Map.GOLD_CASTLE, 0x20, 0x41, 0x00, 0x00, 0x00}, // Yellow Key
            {Board.OBJECT_COPPERKEY, Map.COPPER_CASTLE, 0x20, 0x41, 0x00, 0x00, 0x00}, // Copper Key
            {Board.OBJECT_JADEKEY, Map.JADE_CASTLE, 0x20, 0x41, 0x00, 0x00, 0x00}, // Jade Key
            {Board.OBJECT_BLACKKEY, Map.SOUTHEAST_ROOM, 0x20, 0x40, 0x00, 0x00, 0x00}, // Black Key
            {Board.OBJECT_CHALISE, Map.BLACK_INNERMOST_ROOM, 0x30, 0x20, 0x00, 0x00, 0x00}, // Challise
            {Board.OBJECT_MAGNET, Map.BLACK_FOYER, 0x80, 0x20, 0x00, 0x00, 0x00} // Magnet
        };




        // Object locations (room and coordinate) for Games 02 and 03
        //        - object, room, x, y, state, movement(x/y)
        private readonly int[,] game2Objects =
        {
            {Board.OBJECT_YELLOW_PORT, Map.GOLD_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00}, // Port 1
            {Board.OBJECT_COPPER_PORT, Map.COPPER_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00}, // Port 4
            {Board.OBJECT_JADE_PORT, Map.JADE_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00}, // Port 5
            {Board.OBJECT_WHITE_PORT, Map.WHITE_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00}, // Port 2
            {Board.OBJECT_BLACK_PORT, Map.BLACK_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00}, // Port 3
            {Board.OBJECT_CRYSTAL_PORT, Map.CRYSTAL_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00}, // Port 3
            {Board.OBJECT_NAME, Map.ROBINETT_ROOM, 0x50, 0x69, 0x00, 0x00, 0x00}, // Robinett message
            {Board. OBJECT_NUMBER, Map.NUMBER_ROOM, 0x50, 0x40, 0x00, 0x00, 0x00}, // Starting number
            {Board.OBJECT_REDDRAGON, Map.BLACK_MAZE_2, 0x50, 0x20, 0x00, 3, 3}, // Red Dragon
            {Board.OBJECT_YELLOWDRAGON, Map.RED_MAZE_4, 0x50, 0x20, 0x00, 3, 3}, // Yellow Dragon
            // Commented out sections are for easy testing of Easter Egg
            #if DEBUG_EASTEREGG
            {Board.OBJECT_GREENDRAGON, Map.NUMBER_ROOM, 0x50, 0x20, 0x00, 3, 3}, // Green Dragon
            #else
            {Board.OBJECT_GREENDRAGON, Map.BLUE_MAZE_3, 0x50, 0x20, 0x00, 3, 3}, // Green Dragon
            #endif
            {Board.OBJECT_SWORD, Map.GOLD_CASTLE, 0x20, 0x20, 0x00, 0x00, 0x00}, // Sword
            #if DEBUG_EASTEREGG
            {Board.OBJECT_BRIDGE, Map.MAIN_HALL_RIGHT, 0x40, 0x40, 0x00, 0x00, 0x00}, // Bridge
            {Board.OBJECT_YELLOWKEY, Map.MAIN_HALL_RIGHT, 0x20, 0x40, 0x00, 0x00, 0x00}, // Yellow Key
            {Board.OBJECT_COPPERKEY, Map.MAIN_HALL_RIGHT, 0x7a, 0x40, 0x00, 0x00, 0x00}, // Copper Key
            #else
            {Board.OBJECT_BRIDGE, Map.WHITE_MAZE_3, 0x40, 0x40, 0x00, 0x00, 0x00}, // Bridge
            {Board.OBJECT_YELLOWKEY, Map.WHITE_MAZE_2, 0x20, 0x40, 0x00, 0x00, 0x00}, // Yellow Key
            {Board.OBJECT_COPPERKEY, Map.WHITE_MAZE_2, 0x7a, 0x40, 0x00, 0x00, 0x00}, // Copper Key
            #endif
            {Board.OBJECT_JADEKEY, Map.BLUE_MAZE_4, 0x7a, 0x40, 0x00, 0x00, 0x00}, // Jade Key
            {Board.OBJECT_WHITEKEY, Map.BLUE_MAZE_3, 0x20, 0x40, 0x00, 0x00, 0x00}, // White Key
            {Board.OBJECT_BLACKKEY, Map.RED_MAZE_4, 0x20, 0x40, 0x00, 0x00, 0x00}, // Black Key
            {Board.OBJECT_CRYSTALKEY1, Map.CRYSTAL_CASTLE, 0x4D, 0x55, 0x00, 0x00, 0x00}, // Crystal Key for Player 1
            {Board.OBJECT_CRYSTALKEY2, Map.CRYSTAL_CASTLE, 0x4D, 0x55, 0x00, 0x00, 0x00}, // Crystal Key for Player 2
            {Board.OBJECT_CRYSTALKEY3, Map.CRYSTAL_CASTLE, 0x4D, 0x55, 0x00, 0x00, 0x00}, // Crystal Key for Player 3
            {Board.OBJECT_BAT, Map.MAIN_HALL_CENTER, 0x20, 0x20, 0x00, 0, -3}, // Bat
            #if DEBUG_EASTEREGG
            {Board.OBJECT_DOT, Map.MAIN_HALL_RIGHT, 0x20, 0x10, 0x00, 0x00, 0x00}, // Dot
            #else
            {Board.OBJECT_DOT, Map.BLACK_MAZE_3, 0x45, 0x12, 0x00, 0x00, 0x00}, // Dot
            #endif
            {Board.OBJECT_CHALISE, Map.BLACK_MAZE_2, 0x30, 0x20, 0x00, 0x00, 0x00}, // Challise
            {Board.OBJECT_MAGNET, Map.SOUTHWEST_ROOM, 0x80, 0x20, 0x00, 0x00, 0x00}, // Magnet
        };

        // Object locations (room and coordinate) for game 01
        //        - object, room, x, y, state, movement(x/y)
        private readonly int[,] gameGauntletObjects =
        {
            {Board.OBJECT_YELLOW_PORT, Map.GOLD_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00}, // Port 1
            {Board.OBJECT_BLACK_PORT, Map.BLACK_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00}, // Port 3
            {Board.OBJECT_NAME, Map.ROBINETT_ROOM, 0x50, 0x69, 0x00, 0x00, 0x00}, // Robinett message
            {Board.OBJECT_NUMBER, Map.NUMBER_ROOM, 0x50, 0x40, 0x00, 0x00, 0x00}, // Starting number
            {Board.OBJECT_REDDRAGON, Map.BLUE_MAZE_1, 0x50, 0x20, 0x00, 0x00, 0x00}, // Red Dragon
            {Board.OBJECT_YELLOWDRAGON, Map.MAIN_HALL_CENTER, 0x50, 0x20, 0x00, 0x00, 0x00}, // Yellow Dragon
            {Board.OBJECT_GREENDRAGON, Map.MAIN_HALL_LEFT, 0x50, 0x20, 0x00, 0x00, 0x00} // Green Dragon
        };


        // Magnet Object Matrix
        private int[] magnetMatrix =
        {
               Board.OBJECT_YELLOWKEY,
               Board.OBJECT_JADEKEY,
               Board.OBJECT_COPPERKEY,
               Board.OBJECT_WHITEKEY,
               Board.OBJECT_BLACKKEY,
               Board.OBJECT_SWORD,
               Board.OBJECT_BRIDGE,
               Board.OBJECT_CHALISE
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

