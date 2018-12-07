using System;
namespace GameEngine
{

    public enum EGG_STATE
    {
        NOT_STARTED,
        ENTERED_ROBINETT_ROOM,
        GLIMPSED_CASTLE,
        FOUND_CASTLE,
        FOUND_KEY,
        DEBRIEF,
        IN_GAUNTLET,
        OUT_OF_TIME
    }

    public class EasterEgg
    {
        public const long EGG_GAUNTLET_TIME_LIMIT = 600000; // 10 minutes

        public static EGG_STATE eggState = EGG_STATE.NOT_STARTED;
        
        public static int crystalColor = COLOR.CRYSTAL;

        private static AdventureView view;

        private static Board board;

        private static DateTime startOfTimer;

        public static void setup(AdventureView inView, Board inBoard)
        {
            eggState = EGG_STATE.NOT_STARTED;
            view = inView;
            board = inBoard;
        }

        public static void enteredRobinettRoom()
        {
            if (eggState < EGG_STATE.ENTERED_ROBINETT_ROOM)
            {
                eggState = EGG_STATE.ENTERED_ROBINETT_ROOM;
                view.Platform_ReportToServer("Robinett Room entered.");
            }
        }

        public static void foundCastle(BALL ball)
        {
            if ((eggState < EGG_STATE.FOUND_CASTLE) && (ball.linkedObject != Board.OBJECT_NONE) && (ball.y > 0x68))
            {
                eggState = EGG_STATE.FOUND_CASTLE;
                darkenCastle(COLOR.DARK_CRYSTAL1);
                view.Platform_ReportToServer("Crystal castle found.");
            }
            else if (eggState < EGG_STATE.GLIMPSED_CASTLE)
            {
                eggState = EGG_STATE.GLIMPSED_CASTLE;
                view.Platform_ReportToServer("Crystal castle glimpsed.");
            }
        }

        public static void foundKey()
        {
            if (eggState < EGG_STATE.FOUND_KEY)
            {
                eggState = EGG_STATE.FOUND_KEY;
                darkenCastle(COLOR.DARK_CRYSTAL2);
                view.Platform_ReportToServer("Crystal key found.");
            }
        }

        public static void openedCastle() {
            view.Platform_ReportToServer("Crystal gate has been opened.");
        }

        /**
        * Darken the colors of the crystal castle, gate, and key so that they can be
        * more easily seen once the puzzle has been solved.
        */
        public static void darkenCastle(int color)
        {
            crystalColor = color;
            Map gameMap = board.map;
            gameMap.getRoom(Map.CRYSTAL_CASTLE).color = color;
            board.getObject(Board.OBJECT_CRYSTAL_PORT).color = color;
            // We decided the keys never get darkened
            //board.getObject(Board.OBJECT_CRYSTALKEY1).color = color;
            //board.getObject(Board.OBJECT_CRYSTALKEY2).color = color;
            //board.getObject(Board.OBJECT_CRYSTALKEY3).color = color;
        }

        /**
         * Called when someone enters the Crystal Castle.
         * Check to see whether to kick off the gauntlet.
         */
        public static bool shouldStartChallenge()
        {
            bool test = (eggState < EGG_STATE.DEBRIEF);
            if (test)
            {
                // See if all the players have entered the crystal castle.
                int numPlayers = board.getNumPlayers();
                for (int ctr = 0; ctr < numPlayers; ++ctr)
                {
                    test = test && (board.getPlayer(ctr).room == Map.CRYSTAL_FOYER);
                }
            }
            return test;
        }

        public static void showChallengeMessage()
        {
            // Move anyone out of the entrance so they aren't stuck in the wall
            int numPlayers = board.getNumPlayers();
            for (int ctr = 0; ctr < numPlayers; ++ctr)
            {
                BALL nextPlayer = board.getPlayer(ctr);
                const int Y_BOTTOM_WALL = 0x2A;
                if (nextPlayer.y < Y_BOTTOM_WALL)
                {
                    nextPlayer.y = Y_BOTTOM_WALL;
                }
            }

            // Seal the entrance
            board.map.easterEggLayout1();

            // Display the message
            view.Platform_DisplayStatus("First one to the black castle and back wins the egg.", 5);

            // Start counting down to start
            eggState = EGG_STATE.DEBRIEF;
            startOfTimer = DateTime.UtcNow;
        }

        public static bool shouldStartGauntlet(int frameNum)
        {
            bool test = false;
            if (eggState == EGG_STATE.DEBRIEF)
            {
                // We only check the time 4 times a second.
                if (frameNum % 15 == 0)
                {
                    DateTime currentTime = DateTime.Now;
                    int elapsed = (int)(DateTime.UtcNow - startOfTimer).TotalSeconds;
                    if (elapsed >= 10000)
                    {
                        test = true;
                    }
                    else if (elapsed > 7000)
                    {
                        OBJECT number = board.getObject(Board.OBJECT_NUMBER);
                        number.setExists(true);
                        number.room = Map.CRYSTAL_FOYER;
                        number.state = (10000-elapsed) / 1000;
                    }
                }
            }
            return test;
        }

        public static void startGauntlet()
        {

            // Drop all objects
            for (int playerCtr = 0; playerCtr < board.getNumPlayers(); ++playerCtr)
            {
                BALL nextPayer = board.getPlayer(playerCtr);
                nextPayer.linkedObject = Board.OBJECT_NONE;
            }
            Bat bat = (Bat)board.getObject(Board.OBJECT_BAT);
            bat.linkedObject = Board.OBJECT_NONE;

            // Remove all carryable objects from the game (and the number)
            Board.ObjIter iter = board.getCarryableObjects();
            while (iter.hasNext())
            {
                OBJECT objct = iter.next();
                objct.setExists(false);
                objct.room = Map.NUMBER_ROOM;
            }
            OBJECT number = board.getObject(Board.OBJECT_NUMBER);
            number.setExists(false);
            number.room = Map.NUMBER_ROOM;

            // Close gold castle.  Open black castle and crystal castle
            Portcullis goldGate = (Portcullis)board.getObject(Board.OBJECT_YELLOW_PORT);
            goldGate.setState(Portcullis.CLOSED_STATE, false);
            Portcullis blackGate = (Portcullis)board.getObject(Board.OBJECT_BLACK_PORT);
            blackGate.setState(Portcullis.OPEN_STATE, true);
            Portcullis crystalGate = (Portcullis)board.getObject(Board.OBJECT_CRYSTAL_PORT);
            crystalGate.setState(Portcullis.OPEN_STATE, true);

            // Block off parts of the board
            board.map.easterEggLayout2();

            // Change everyone's home castle to crystal
            for (int playerCtr = 0; playerCtr < board.getNumPlayers(); ++playerCtr)
            {
                BALL nextPlayer = board.getPlayer(playerCtr);
                nextPlayer.homeGate = crystalGate;
            }

            // Plant the dragons
            int[] dragonList = { Board.OBJECT_YELLOWDRAGON, Board.OBJECT_GREENDRAGON, Board.OBJECT_REDDRAGON };
            for (int ctr = 0; ctr < 3; ++ctr)
            {
                Dragon dragon = (Dragon)board.getObject(dragonList[ctr]);
                dragon.eaten = null;
                dragon.state = Dragon.STALKING;
                Dragon.setDifficulty(Dragon.Difficulty.HARD);
                dragon.setMovementX(0);
                dragon.setMovementY(0);
                switch (dragon.getPKey())
                {
                    case Board.OBJECT_YELLOWDRAGON:
                        dragon.x = 20;
                        dragon.y = 20;
                        dragon.room = Map.MAIN_HALL_RIGHT;
                        break;
                    case Board.OBJECT_GREENDRAGON:
                        dragon.x = 20;
                        dragon.y = 100;
                        dragon.room = Map.MAIN_HALL_CENTER;
                        break;
                    case Board.OBJECT_REDDRAGON:
                        dragon.x = 80;
                        dragon.y = 20;
                        dragon.room = Map.BLUE_MAZE_1;
                        break;
                }
            }

            darkenCastle(COLOR.DARK_CRYSTAL4);

            eggState = EGG_STATE.IN_GAUNTLET;

            // Start the timer
            startOfTimer = DateTime.UtcNow;
        }

        public static bool isGauntletTimeUp(int frameNum)
        {
            bool test = false;
            if (eggState == EGG_STATE.IN_GAUNTLET)
            {
                // We only check the time 4 times a second.
                if (frameNum % 15 == 0)
                {
                    int elapsed = (int)(DateTime.UtcNow - startOfTimer).TotalSeconds;
                    long timeLeft = EGG_GAUNTLET_TIME_LIMIT - elapsed;
                    if (timeLeft < 0)
                    {
                        test = true;
                    }
                    else if ((timeLeft <= 120000) && (timeLeft > 119000))
                    {
                        view.Platform_DisplayStatus("Two minute warning.", 3);
                    }
                    else if ((timeLeft <= 60000) && (timeLeft > 59000))
                    {
                        view.Platform_DisplayStatus("One minute warning.", 3);
                    }
                }
            }
            return test;
        }

        public static void endGauntlet()
        {
            eggState = EGG_STATE.OUT_OF_TIME;
            view.Platform_DisplayStatus("Time has expired.  Game over.", -1);
        }


        public static void winEgg()
        {
            // Display the Easter Egg
            OBJECT egg = board.getObject(Board.OBJECT_EASTEREGG);
            egg.setExists(true);
            egg.room = Map.CRYSTAL_FOYER;
            egg.x = 0x4A;
            egg.y = 0x56;
            view.Platform_ReportToServer("Easter egg has been claimed.");
        }

    }
}
