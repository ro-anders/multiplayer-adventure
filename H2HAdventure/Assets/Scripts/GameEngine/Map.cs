using System;
namespace GameEngine
{
    public class Map
    {
        public const int MAP_LAYOUT_SMALL = 0;
        public const int MAP_LAYOUT_BIG = 1;
        public const int MAP_LAYOUT_GAUNTLET = 2;

        public const int NUMBER_ROOM = 0x00;
        public const int MAIN_HALL_LEFT = 0x01;
        public const int MAIN_HALL_CENTER = 0x02;
        public const int MAIN_HALL_RIGHT = 0x03;
        public const int BLUE_MAZE_5 = 0x04; // Down from the black castle
        public const int BLUE_MAZE_2 = 0x05; // Down from the jade castle
        public const int BLUE_MAZE_3 = 0x06; // The big center room (where Art3mis waited)
        public const int BLUE_MAZE_4 = 0x07; // 2 down from the black castle
        public const int BLUE_MAZE_1 = 0x08; // Up from the main hall
        public const int WHITE_MAZE_2 = 0x09; // Catacombs to white castle, second one on way to castle
        public const int WHITE_MAZE_1 = 0x0A; // Catacombs to white castle, down from main hall
        public const int WHITE_MAZE_3 = 0x0B; // Catacombs to white castle, right of south hall left
        public const int SOUTH_HALL_RIGHT = 0x0C;
        public const int SOUTH_HALL_LEFT = 0x0D;
        public const int SOUTHWEST_ROOM = 0x0E; // Two down from white castle
        public const int WHITE_CASTLE = 0x0F;
        public const int BLACK_CASTLE = 0x10;
        public const int GOLD_CASTLE = 0x11;
        public const int GOLD_FOYER = 0x12;
        // The following Black Maze values are a group and shouldn't be broken up
        public const int BLACK_MAZE_1 = 0x13;
        public const int BLACK_MAZE_2 = 0x14;
        public const int BLACK_MAZE_3 = 0x15;
        public const int BLACK_MAZE_ENTRY = 0x16;
        // The following Red Maze values are a group and shouldn't be broken up
        public const int RED_MAZE_3 = 0x17;
        public const int RED_MAZE_2 = 0x18;
        public const int RED_MAZE_4 = 0x19;
        public const int RED_MAZE_1 = 0x1A;
        public const int BLACK_FOYER = 0x1B;
        public const int BLACK_INNERMOST_ROOM = 0x1C;
        public const int SOUTHEAST_ROOM = 0x1D; // Two down from copper castle (moves with castle in different levels)
        public const int ROBINETT_ROOM = 0x1E;
        public const int JADE_CASTLE = 0x1F;
        public const int JADE_FOYER = 0x20;
        public const int CRYSTAL_CASTLE = 0x21;
        public const int CRYSTAL_FOYER = 0x22;
        public const int COPPER_CASTLE = 0x23;
        public const int COPPER_FOYER = 0x24;

        public ROOM[] roomDefs; // TODO: Migrate to being private with accessors.

        public const int LONG_WAY = 5;

        private const int numRooms = COPPER_FOYER + 1;

        private int[,] distances = new int[0, 0];

        public Map(int numPlayers, int gameMapLayout)
        {
            roomDefs = new ROOM[numRooms];
            defaultRooms();
            ConfigureMaze(numPlayers, gameMapLayout);
        }

        public int getNumRooms()
        {
            return numRooms;
        }

        void defaultRooms()
        {

            addRoom(NUMBER_ROOM, new ROOM(roomGfxNumberRoom, ROOM.FLAG_NONE, COLOR.PURPLE,                       // 0x00
                                                NUMBER_ROOM, NUMBER_ROOM, NUMBER_ROOM, NUMBER_ROOM, "Number Room", ROOM.RandomVisibility.HIDDEN));
            addRoom(MAIN_HALL_LEFT, new ROOM(roomGfxBelowYellowCastle, ROOM.FLAG_LEFTTHINWALL, COLOR.OLIVEGREEN, // 0x01
                                             BLUE_MAZE_1, MAIN_HALL_CENTER, BLACK_CASTLE, MAIN_HALL_RIGHT, "Main Hall Left"));
            addRoom(MAIN_HALL_CENTER, new ROOM(roomGfxBelowYellowCastle, ROOM.FLAG_NONE, COLOR.LIMEGREEN,        // 0x02
                                               GOLD_CASTLE, MAIN_HALL_RIGHT, BLUE_MAZE_2, MAIN_HALL_LEFT, "Main Hall Center"));
            addRoom(MAIN_HALL_RIGHT, new ROOM(roomGfxSideCorridor, ROOM.FLAG_RIGHTTHINWALL, COLOR.TAN,          // 0x03
                                            COPPER_CASTLE, MAIN_HALL_LEFT, SOUTHEAST_ROOM, MAIN_HALL_CENTER, "Main Hall Right"));
            addRoom(BLUE_MAZE_5, new ROOM(roomGfxBlueMazeTop, ROOM.FLAG_NONE, COLOR.BLUE,                        // 0x04
                                          0x10, 0x05, 0x07, 0x06, "Blue Maze 5"));
            addRoom(BLUE_MAZE_2, new ROOM(roomGfxBlueMaze1, ROOM.FLAG_NONE, COLOR.BLUE,                          // 0x05
                                          0x1D, 0x06, 0x08, 0x04, "Blue Maze 2"));
            addRoom(BLUE_MAZE_3, new ROOM(roomGfxBlueMazeBottom, ROOM.FLAG_NONE, COLOR.BLUE,                     // 0x06
                                          0x07, 0x04, 0x03, 0x05, "Blue Maze 3"));
            addRoom(BLUE_MAZE_4, new ROOM(roomGfxBlueMazeCenter, ROOM.FLAG_NONE, COLOR.BLUE,                     // 0x07
                                          0x04, 0x08, 0x06, 0x08, "Blue Maze 4"));
            addRoom(BLUE_MAZE_1, new ROOM(roomGfxBlueMazeEntry, ROOM.FLAG_NONE, COLOR.BLUE,                      // 0x08
                                          0x05, 0x07, 0x01, 0x07, "Blue Maze 1"));
            addRoom(WHITE_MAZE_2, new ROOM(roomGfxMazeMiddle, ROOM.FLAG_NONE, COLOR.LTGRAY,                      // 0x09
                                           WHITE_MAZE_1, WHITE_MAZE_1, WHITE_MAZE_3, WHITE_MAZE_1, "White Maze 2"));
            addRoom(WHITE_MAZE_1, new ROOM(roomGfxMazeEntry, ROOM.FLAG_NONE, COLOR.LTGRAY,                       // 0x0A
                                           MAIN_HALL_RIGHT, WHITE_MAZE_2, WHITE_MAZE_2, WHITE_MAZE_2, "White Maze 1"));
            addRoom(WHITE_MAZE_3, new ROOM(roomGfxMazeSide, ROOM.FLAG_NONE, COLOR.LTGRAY,                        // 0x0B
                                           WHITE_MAZE_2, SOUTH_HALL_RIGHT, COPPER_CASTLE, SOUTH_HALL_LEFT, "White Maze 3"));
            addRoom(SOUTH_HALL_RIGHT, new ROOM(roomGfxSideCorridor, ROOM.FLAG_RIGHTTHINWALL, COLOR.LTCYAN,       // 0x0C
                                               COPPER_CASTLE, SOUTH_HALL_LEFT, SOUTHEAST_ROOM, WHITE_MAZE_3, "South Hall Right"));
            addRoom(SOUTH_HALL_LEFT, new ROOM(roomGfxSideCorridor, ROOM.FLAG_LEFTTHINWALL, COLOR.DKGREEN,        // 0x0D
                                              0x0F, 0x0B, 0x0E, 0x0C, "South Hall Left"));                         // 0x0E
            addRoom(SOUTHWEST_ROOM, new ROOM(roomGfxTopEntryRoom, ROOM.FLAG_NONE, COLOR.CYAN,
                                             0x0D, 0x10, 0x0F, 0x10, "Southwest Room"));
            addRoom(WHITE_CASTLE, new ROOM(roomGfxCastle, ROOM.FLAG_NONE, COLOR.WHITE,                           // 0x0F
                                           0x0E, 0x0F, 0x0D, 0x0F, "White Castle"));
            addRoom(BLACK_CASTLE, new ROOM(roomGfxCastle, ROOM.FLAG_NONE, COLOR.BLACK,                           // 0x10
                                           0x01, 0x1C, 0x04, 0x1C, "Black Castle"));
            addRoom(GOLD_CASTLE, new ROOM(roomGfxCastle, ROOM.FLAG_NONE, COLOR.YELLOW,                           // 0x11
                                          0x06, 0x03, 0x02, 0x01, "Gold Castle"));
            addRoom(GOLD_FOYER, new ROOM(roomGfxNumberRoom, ROOM.FLAG_NONE, COLOR.YELLOW,                        // 0x12
                                         GOLD_FOYER, GOLD_FOYER, GOLD_FOYER, GOLD_FOYER, "Gold Foyer"));
            addRoom(BLACK_MAZE_1, new ROOM(roomGfxBlackMaze1, ROOM.FLAG_NONE, COLOR.LTGRAY,                      // 0x13
                                           0x15, 0x14, 0x15, 0x16, "Black Maze 1"));
            addRoom(BLACK_MAZE_2, new ROOM(roomGfxBlackMaze2, ROOM.FLAG_MIRROR, COLOR.LTGRAY,                    // 0x14
                                           0x16, 0x15, 0x16, 0x13, "Black Maze 2"));
            addRoom(BLACK_MAZE_3, new ROOM(roomGfxBlackMaze3, ROOM.FLAG_MIRROR, COLOR.LTGRAY,                    // 0x15
                                           0x13, 0x16, 0x13, 0x14, "Black Maze 3"));
            addRoom(BLACK_MAZE_ENTRY, new ROOM(roomGfxBlackMazeEntry, ROOM.FLAG_NONE, COLOR.LTGRAY,              // 0x16
                                               0x14, 0x13, 0x1B, 0x15, "Black Maze Entry"));
            addRoom(RED_MAZE_3, new ROOM(roomGfxRedMaze1, ROOM.FLAG_NONE, COLOR.RED,                             // 0x17
                                         0x19, 0x18, 0x19, 0x18, "Red Maze 3"));
            addRoom(RED_MAZE_2, new ROOM(roomGfxRedMazeTop, ROOM.FLAG_NONE, COLOR.RED,                           // 0x18
                                         0x1A, 0x17, 0x1A, 0x17, "Red Maze 2"));
            addRoom(RED_MAZE_4, new ROOM(roomGfxRedMazeBottom, ROOM.FLAG_NONE, COLOR.RED,                        // 0x19
                                         0x17, 0x1A, 0x17, 0x1A, "Red Maze4 "));
            addRoom(RED_MAZE_1, new ROOM(roomGfxWhiteCastleEntry, ROOM.FLAG_NONE, COLOR.RED,                     // 0x1A
                                         0x18, 0x19, 0x18, 0x19, "Red Maze 1"));
            addRoom(BLACK_FOYER, new ROOM(roomGfxTwoExitRoom, ROOM.FLAG_NONE, COLOR.RED,                         // 0x1B
                                BLACK_INNERMOST_ROOM, BLACK_INNERMOST_ROOM, BLACK_INNERMOST_ROOM, BLACK_INNERMOST_ROOM, "Black Foyer"));
            addRoom(BLACK_INNERMOST_ROOM, new ROOM(roomGfxNumberRoom, ROOM.FLAG_NONE, COLOR.PURPLE,              // 0x1C
                                    SOUTHEAST_ROOM, BLUE_MAZE_4, BLACK_FOYER, BLUE_MAZE_1, "Black Innermost Room"));
            addRoom(SOUTHEAST_ROOM, new ROOM(roomGfxTopEntryRoom, ROOM.FLAG_NONE, COLOR.RED,                     // 0x1D
                                             MAIN_HALL_RIGHT, MAIN_HALL_LEFT, BLACK_CASTLE, MAIN_HALL_RIGHT, "Southeast Room"));
            addRoom(ROBINETT_ROOM, new ROOM(roomGfxBelowYellowCastle, ROOM.FLAG_NONE, COLOR.PURPLE,              // 0x1E
                                            CRYSTAL_CASTLE, MAIN_HALL_LEFT, CRYSTAL_CASTLE, MAIN_HALL_RIGHT, "Robinett Room", ROOM.RandomVisibility.HIDDEN));
            addRoom(JADE_CASTLE, new ROOM(roomGfxCastle3, ROOM.FLAG_NONE, COLOR.JADE,                            // 0x1F
                                          SOUTHEAST_ROOM, BLUE_MAZE_3, BLUE_MAZE_2, BLUE_MAZE_5, "Jade Castle", ROOM.RandomVisibility.HIDDEN));
            addRoom(JADE_FOYER, new ROOM(roomGfxNumberRoom, ROOM.FLAG_NONE, COLOR.JADE,                          // 0x20
                                         JADE_FOYER, JADE_FOYER, JADE_FOYER, JADE_FOYER, "Jade Foyer", ROOM.RandomVisibility.HIDDEN));
            addRoom(COPPER_CASTLE, new ROOM(roomGfxCastle2, ROOM.FLAG_NONE, COLOR.COPPER,                        // 0x21
                                            BLUE_MAZE_3, MAIN_HALL_LEFT, MAIN_HALL_RIGHT, GOLD_CASTLE, "Copper Castle"));
            addRoom(COPPER_FOYER, new ROOM(roomGfxNumberRoom, ROOM.FLAG_NONE, COLOR.COPPER,                      // 0x22
                                           COPPER_FOYER, COPPER_FOYER, COPPER_FOYER, COPPER_FOYER, "Copper Foyer"));
            addRoom(CRYSTAL_CASTLE, new ROOM(roomGfxCastle4, ROOM.FLAG_NONE, COLOR.CRYSTAL,                        // 0x23
                                             ROBINETT_ROOM, CRYSTAL_CASTLE, ROBINETT_ROOM, CRYSTAL_CASTLE, "Crystal Castle", ROOM.RandomVisibility.HIDDEN));
            addRoom(CRYSTAL_FOYER, new ROOM(roomGfxNumberRoom, ROOM.FLAG_NONE, COLOR.DARK_CRYSTAL4,                      // 0x24
                                            CRYSTAL_FOYER, CRYSTAL_FOYER, CRYSTAL_FOYER, CRYSTAL_FOYER, "Crystal Foyer", ROOM.RandomVisibility.HIDDEN));
        }

        void ConfigureMaze(int numPlayers, int gameMapLayout)
        {

            // Add the Jade Castle if 3 players
            if (numPlayers > 2)
            {
                roomDefs[BLUE_MAZE_2].roomUp = JADE_CASTLE;
                roomDefs[BLUE_MAZE_2].graphicsData = roomGfxBlueMaze1B;
                roomDefs[JADE_CASTLE].visibility = ROOM.RandomVisibility.OPEN;
                roomDefs[JADE_FOYER].visibility = ROOM.RandomVisibility.IN_CASTLE;
            }

            if (gameMapLayout == MAP_LAYOUT_SMALL)
            {
                // This is the default setup, so don't need to do anything.
            }
            else if (gameMapLayout == MAP_LAYOUT_GAUNTLET)
            {
                // Make the right side of the main hall a dead end.
                roomDefs[MAIN_HALL_RIGHT].roomUp = BLUE_MAZE_3;
                roomDefs[MAIN_HALL_RIGHT].roomDown = BLACK_CASTLE;
                roomDefs[MAIN_HALL_RIGHT].graphicsData = roomGfxStraightHall;
            }
            else
            {
                // Games with the big map.
                // Connect the lower half of the world.
                roomDefs[MAIN_HALL_LEFT].roomDown = WHITE_CASTLE;
                roomDefs[MAIN_HALL_CENTER].roomDown = GOLD_CASTLE;
                roomDefs[MAIN_HALL_RIGHT].roomDown = WHITE_MAZE_1;
                roomDefs[SOUTHEAST_ROOM].roomUp = SOUTH_HALL_RIGHT;

                // Move the Copper Castle to the White Maze
                roomDefs[MAIN_HALL_RIGHT].graphicsData = roomGfxLeftOfName;
                roomDefs[MAIN_HALL_RIGHT].roomUp = BLUE_MAZE_3;
                roomDefs[COPPER_CASTLE].roomDown = SOUTH_HALL_RIGHT;
                roomDefs[COPPER_CASTLE].roomUp = SOUTHEAST_ROOM;
                roomDefs[COPPER_CASTLE].roomRight = BLUE_MAZE_4;
                roomDefs[COPPER_CASTLE].roomLeft = BLUE_MAZE_1;
                roomDefs[BLACK_CASTLE].roomLeft = COPPER_CASTLE;
                roomDefs[BLACK_CASTLE].roomRight = COPPER_CASTLE;

                // Put the Black Maze in the Black Castle
                roomDefs[BLACK_FOYER].roomUp = BLACK_MAZE_ENTRY;
                roomDefs[BLACK_FOYER].roomRight = BLACK_MAZE_ENTRY;
                roomDefs[BLACK_FOYER].roomDown = BLACK_MAZE_ENTRY;
                roomDefs[BLACK_FOYER].roomLeft = BLACK_MAZE_ENTRY;
                roomDefs[BLACK_INNERMOST_ROOM].visibility = ROOM.RandomVisibility.HIDDEN;

            }
        }

        void ComputeDistances(Portcullis[] ports)
        {
            // This may get called more than once.
            distances = new int[numRooms, numRooms];
            for (int ctr1 = 0; ctr1 < numRooms; ++ctr1)
            {
                for (int ctr2 = 0; ctr2 < numRooms; ++ctr2)
                {
                    if (ctr1 == ctr2)
                    {
                        distances[ctr1, ctr2] = 0;
                    }
                    else if (isNextTo(ctr1, ctr2))
                    {
                        distances[ctr1, ctr2] = 1;
                    }
                    else
                    {
                        distances[ctr1, ctr2] = LONG_WAY;
                    }
                }
            }

            // Adjust for castles
            if (ports.Length > 0)
            {
                for (int ctr = 0; ctr < ports.Length; ++ctr)
                {
                    Portcullis nextPort = ports[ctr];
                    distances[nextPort.room, nextPort.insideRoom] = 1;
                    distances[nextPort.insideRoom, nextPort.room] = 1;
                }
            }

            // Adjust for Robinett room
            distances[ROBINETT_ROOM, MAIN_HALL_LEFT] = 1;
            distances[MAIN_HALL_LEFT, ROBINETT_ROOM] = 1;
            distances[ROBINETT_ROOM, MAIN_HALL_RIGHT] = 1;
            distances[MAIN_HALL_RIGHT, ROBINETT_ROOM] = 1;

            // Remove paths that aren't really paths because full length walls block them.
            distances[MAIN_HALL_RIGHT, BLUE_MAZE_3] = LONG_WAY;
            distances[BLUE_MAZE_3, MAIN_HALL_RIGHT] = LONG_WAY;
            distances[MAIN_HALL_LEFT, MAIN_HALL_RIGHT] = LONG_WAY;
            distances[MAIN_HALL_RIGHT, MAIN_HALL_LEFT] = LONG_WAY;
            distances[MAIN_HALL_LEFT, BLACK_CASTLE] = LONG_WAY;
            distances[BLACK_CASTLE, MAIN_HALL_LEFT] = LONG_WAY;
            distances[WHITE_CASTLE, SOUTHWEST_ROOM] = LONG_WAY;
            distances[SOUTHWEST_ROOM, WHITE_CASTLE] = LONG_WAY;
            distances[SOUTH_HALL_LEFT, SOUTH_HALL_RIGHT] = LONG_WAY;
            distances[SOUTH_HALL_RIGHT, SOUTH_HALL_LEFT] = LONG_WAY;

            int tracker = LONG_WAY;
            // Now compute the distances using isNextTo()
            for (int step = 2; step < LONG_WAY; ++step)
            {
                for (int ctr1 = 0; ctr1 < numRooms; ++ctr1)
                {
                    for (int ctr2 = 0; ctr2 < numRooms; ++ctr2)
                    {
                        if (distances[ctr1, ctr2] == LONG_WAY)
                        {
                            for (int ctr3 = 0; ctr3 < numRooms; ++ctr3)
                            {
                                if ((distances[ctr3, ctr2] < step) && (distances[ctr1, ctr3] == 1))
                                {
                                    distances[ctr1, ctr2] = distances[ctr3, ctr2] + 1;
                                    break;
                                }
                            }
                        }
                        if (distances[MAIN_HALL_RIGHT, MAIN_HALL_CENTER] != tracker)
                        {
                            tracker = distances[MAIN_HALL_RIGHT, MAIN_HALL_CENTER];
                        }
                    }
                }
            }
        }

        private void addRoom(int key, ROOM newRoom)
        {
            roomDefs[key] = newRoom;
            newRoom.setIndex(key);
        }

        public ROOM getRoom(int key)
        {
            if ((key < 0) || (key > numRooms))
            {
                return null;
            }
            else
            {
                return roomDefs[key];
            }
        }


        public int distance(int fromRoom, int toRoom)
        {
            return distances[fromRoom, toRoom];
        }

        bool isNextTo(int room1, int room2)
        {
            ROOM robj1 = roomDefs[room1];
            ROOM robj2 = roomDefs[room2];
            return (((robj1.roomUp == room2) && (robj2.roomDown == room1)) ||
                    ((robj1.roomRight == room2) && (robj2.roomLeft == room1)) ||
                    ((robj1.roomDown == room2) && (robj2.roomUp == room1)) ||
                    ((robj1.roomLeft == room2) && (robj2.roomRight == room1)));

        }

        public void addCastles(Portcullis[] ports)
        {
            ComputeDistances(ports);
        }

        // Close exit to crystal castle to keep everyone there until the race starts
        public void easterEggLayout1()
        {
            roomDefs[CRYSTAL_FOYER].graphicsData = roomGfxClosedRoom;
        }

        // Setup map for Easter Egg gauntlet.
        public void easterEggLayout2()
        {
            // Block of Jade Castle
            roomDefs[BLUE_MAZE_2].roomUp = MAIN_HALL_RIGHT;
            roomDefs[BLUE_MAZE_2].graphicsData = roomGfxBlueMaze1;

            // Block off white catacombs
            roomDefs[MAIN_HALL_RIGHT].roomUp = BLUE_MAZE_3;
            roomDefs[MAIN_HALL_RIGHT].roomDown = BLACK_CASTLE;
            roomDefs[MAIN_HALL_RIGHT].graphicsData = roomGfxStraightHall;
            roomDefs[MAIN_HALL_LEFT].roomDown = BLACK_CASTLE;
            roomDefs[BLACK_CASTLE].roomLeft = BLUE_MAZE_4;
            roomDefs[BLACK_CASTLE].roomRight = BLUE_MAZE_1;

            // Block off black catacombs
            roomDefs[BLACK_FOYER].graphicsData = roomGfxNumberRoom;
            roomDefs[BLACK_FOYER].roomUp = MAIN_HALL_LEFT;
            roomDefs[BLACK_FOYER].roomRight = BLUE_MAZE_4;
            roomDefs[BLACK_FOYER].roomDown = BLACK_FOYER;
            roomDefs[BLACK_FOYER].roomLeft = BLUE_MAZE_1;

            // Open up exit of crystal castle
            roomDefs[CRYSTAL_FOYER].graphicsData = roomGfxNumberRoom;
            roomDefs[CRYSTAL_CASTLE].graphicsData = roomGfxCastle;

            // Remove the MAIN_HALL_RIGHT wall
            roomDefs[MAIN_HALL_RIGHT].flags = ROOM.FLAG_NONE;

        }

        //-- ROOM SHAPES ------------------------------------------------------
        // Left of Name Room
        private static readonly byte[] roomGfxLeftOfName = {
            0xF0,0xFF,0xFF,     // XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRRRRRR
            0x00,0x00,0x00,
            0x00,0x00,0x00,
            0x00,0x00,0x00,
            0x00,0x00,0x00,
            0x00,0x00,0x00,
            0xF0,0xFF,0x0F      // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRRRRRR
        };

        // Straight Hall
        private static readonly byte[] roomGfxStraightHall =
        {
            0xF0,0xFF,0xFF,     // XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRRRRRR
            0x00,0x00,0x00,
            0x00,0x00,0x00,
            0x00,0x00,0x00,
            0x00,0x00,0x00,
            0x00,0x00,0x00,
            0xF0,0xFF,0xFF      // XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRRRRRR
        };

        // Below Yellow Castle
        private static readonly byte[] roomGfxBelowYellowCastle =
        {
            0xF0,0xFF,0x0F,     // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRRRRRR
            0x00,0x00,0x00,
            0x00,0x00,0x00,
            0x00,0x00,0x00,
            0x00,0x00,0x00,
            0x00,0x00,0x00,
            0xF0,0xFF,0xFF      // XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRRRRRR
        };


        // Side Corridor
        private static readonly byte[] roomGfxSideCorridor =
        {
            0xF0,0xFF,0x0F,     // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR
            0x00,0x00,0x00,
            0x00,0x00,0x00,
            0x00,0x00,0x00,
            0x00,0x00,0x00,
            0x00,0x00,0x00,
            0xF0,0xFF,0x0F      // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR
        };

        // Closed Room Definition
        private static readonly byte[] roomGfxClosedRoom =
        {
            0xF0,0xFF,0xFF,     // XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR
            0x30,0x00,0x00,     // XX                                    RR
            0x30,0x00,0x00,     // XX                                    RR
            0x30,0x00,0x00,     // XX                                    RR
            0x30,0x00,0x00,     // XX                                    RR
            0x30,0x00,0x00,     // XX                                    RR
            0xF0,0xFF,0xFF      // XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR
        };



        // Number Room Definition
        private static readonly byte[] roomGfxNumberRoom =
        {
            0xF0,0xFF,0xFF,     // XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR
            0x30,0x00,0x00,     // XX                                    RR
            0x30,0x00,0x00,     // XX                                    RR
            0x30,0x00,0x00,     // XX                                    RR
            0x30,0x00,0x00,     // XX                                    RR
            0x30,0x00,0x00,     // XX                                    RR
            0xF0,0xFF,0x0F      // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR
        };

        // `
        private static readonly byte[] roomGfxTwoExitRoom =
        {
            0xF0,0xFF,0x0F,     // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR
            0x30,0x00,0x00,     // XX                                    RR
            0x30,0x00,0x00,     // XX                                    RR
            0x30,0x00,0x00,     // XX                                    RR
            0x30,0x00,0x00,     // XX                                    RR
            0x30,0x00,0x00,     // XX                                    RR
            0xF0,0xFF,0x0F      // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR
        };

        // Top of Blue Maze
        private static readonly byte[] roomGfxBlueMazeTop =
        {
            0xF0,0xFF,0x0F,     // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR
            0x00,0x0C,0x0C,     //         XX    XX        RR    RR
            0xF0,0x0C,0x3C,     // XXXX    XX    XXXX    RRRR    RR    RRRR
            0xF0,0x0C,0x00,     // XXXX    XX                    RR    RRRR
            0xF0,0xFF,0x3F,     // XXXXXXXXXXXXXXXXXX    RRRRRRRRRRRRRRRRRR
            0x00,0x30,0x30,     //       XX        XX    RR        RR
            0xF0,0x33,0x3F      // XXXX  XX  XXXXXXXX    RRRRRRRR  RR  RRRR
        };

        // Blue Maze #1
        private static readonly byte[] roomGfxBlueMaze1 =
        {
            0xF0,0xFF,0xFF,          // XXXXXXXXXXXXXXXX--------RRRRRRRRRRRRRRRR
            0x00,0x00,0x00,          //
            0xF0,0xFC,0xFF,          // XXXXXXXXXX  XXXXXXXXRRRRRRRR  RRRRRRRRRR
            0xF0,0x00,0xC0,          // XXXX              XXRR              RRRR
            0xF0,0x3F,0xCF,          // XXXX  XXXXXXXXXX  XXRR  RRRRRRRRRR  RRRR
            0x00,0x30,0xCC,          //       XX      XX  XXRR  RR      RR
            0xF0,0xF3,0xCC           // XXXXXXXX  XX  XX  XXRR  RR  RR  RRRRRRRR
        };

        // Blue Maze #1 with entrance to Jade Castle (only used with 3 players)
        private static readonly byte[] roomGfxBlueMaze1B =
        {
            0xF0,0xFF,0x0F,          // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR
            0x00,0x00,0x00,          //
            0xF0,0xFC,0xFF,          // XXXXXXXXXX  XXXXXXXXRRRRRRRR  RRRRRRRRRR
            0xF0,0x00,0xC0,          // XXXX              XXRR              RRRR
            0xF0,0x3F,0xCF,          // XXXX  XXXXXXXXXX  XXRR  RRRRRRRRRR  RRRR
            0x00,0x30,0xCC,          //       XX      XX  XXRR  RR      RR
            0xF0,0xF3,0xCC           // XXXXXXXX  XX  XX  XXRR  RR  RR  RRRRRRRR
        };

        // Bottom of Blue Maze
        private static readonly byte[] roomGfxBlueMazeBottom =
        {
            0xF0,0xF3,0x0C,          // XXXXXXXX  XX  XX        RR  RR  RRRRRRRR
            0x00,0x30,0x0C,          //       XX      XX        RR      RR
            0xF0,0x3F,0x0F,          // XXXX  XXXXXXXXXX        RRRRRRRRRR  RRRR
            0xF0,0x00,0x00,          // XXXX                                RRRR
            0xF0,0xF0,0x00,          // XXXXXXXX                        RRRRRRRR
            0x00,0x30,0x00,          //       XX                        RR
            0xF0,0xFF,0xFF           // XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR
        };

        // Center of Blue Maze
        private static readonly byte[] roomGfxBlueMazeCenter =
        {
            0xF0,0x33,0x3F,          // XXXX  XX  XXXXXXXX    RRRRRRRR  RR  RRRR
            0x00,0x30,0x3C,          //       XX      XXXX    RRRR      RR
            0xF0,0xFF,0x3C,          // XXXXXXXXXXXX  XXXX    RRRR  RRRRRRRRRRRR
            0x00,0x03,0x3C,          //           XX  XXXX    RRRR  RR
            0xF0,0x33,0x3C,          // XXXX  XX  XX  XXXX    RRRR  RR  RR  RRRR
            0x00,0x33,0x0C,          //       XX  XX  XX        RR  RR  RR
            0xF0,0xF3,0x0C           // XXXXXXXX  XX  XX        RR  RR  RRRRRRRR
        };

        // Blue Maze Entry
        private static readonly byte[] roomGfxBlueMazeEntry =
        {
            0xF0,0xF3,0xCC,          // XXXXXXXX  XX  XX  XXRR  RR  RR  RRRRRRRR
            0x00,0x33,0x0C,          //       XX  XX  XX        RR  RR  RR
            0xF0,0x33,0xFC,          // XXXX  XX  XX  XXXXXXRRRRRR  RR  RR  RRRR
            0x00,0x33,0x00,          //       XX  XX                RR  RR
            0xF0,0xF3,0xFF,          // XXXXXXXX  XXXXXXXXXXRRRRRRRRRR  RRRRRRRR
            0x00,0x00,0x00,          //
            0xF0,0xFF,0x0F           // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR
        };

        // Maze Middle
        private static readonly byte[] roomGfxMazeMiddle =
        {
            0xF0,0xFF,0xCC,          // XXXXXXXXXXXX  XX  XXRR  RR  RRRRRRRRRRRR
            0x00,0x00,0xCC,          //               XX  XXRR  RR
            0xF0,0x03,0xCF,          // XXXX      XXXXXX  XXRR  RRRRRR      RRRR
            0x00,0x03,0x00,          //           XX                RR
            0xF0,0xF3,0xFC,          // XXXXXXXX  XX  XXXXXXRRRRRR  RR  RRRRRRRR
            0x00,0x33,0x0C,          //       XX  XX  XX        RR  RR  RR
            0xF0,0x33,0xCC           // XXXX  XX  XX  XX  XXRR  RR  RR  RR  RRRR
        };

        // Maze Side
        private static readonly byte[] roomGfxMazeSide =
        {
            0xF0,0x33,0xCC,          // XXXX  XX  XX  XX  XXRR  RR  RR  RR  RRRR
            0x00,0x30,0xCC,          //       XX      XX  XXRR  RR      RR
            0x00,0x3F,0xCF,          //       XXXXXX  XX  XXRR  RR  RRRRRR
            0x00,0x00,0xC0,          //                   XXRR
            0x00,0x3F,0xC3,          //       XXXXXXXX    XXRR    RRRRRRRR
            0x00,0x30,0xC0,          //       XX          XXRR          RR
            0xF0,0xFF,0xFF           // XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR
        };

        // Maze Entry
        private static readonly byte[] roomGfxMazeEntry =
        {
            0xF0,0xFF,0x0F,          // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR
            0x00,0x30,0x00,          //       XX                        RR
            0xF0,0x30,0xFF,          // XXXX  XX    XXXXXXXXRRRRRRRRR   RR  RRRR
            0x00,0x30,0xC0,          //       XX          XXRR          RR
            0xF0,0xF3,0xC0,          // XXXXXXXX  XX      XXRR      RR  RRRRRRRR
            0x00,0x03,0xC0,          //           XX      XXRR      RR
            0xF0,0xFF,0xCC           // XXXXXXXXXXXX  XX  XXRR  RR  RRRRRRRRRRRR
        };

        // Castle
        public static readonly byte[] roomGfxCastle = // This one is public!
        {
            0xF0,0xFE,0x15,      // XXXXXXXXXXX X X X      R R R RRRRRRRRRRR
            0x30,0x03,0x1F,      // XX        XXXXXXX      RRRRRRR        RR
            0x30,0x03,0xFF,      // XX        XXXXXXXXXXRRRRRRRRRR        RR
            0x30,0x00,0xFF,      // XX          XXXXXXXXRRRRRRRR          RR
            0x30,0x00,0x3F,      // XX          XXXXXX    RRRRRR          RR
            0x30,0x00,0x00,      // XX                                    RR
            0xF0,0xFF,0x0F       // XXXXXXXXXXXXXX            RRRRRRRRRRRRRR
        };

        // Castle
        private static readonly byte[] roomGfxCastle2 =
        {
            0xF0,0xFE,0x15,      // XXXXXXXXXXX X X X      R R R RRRRRRRRRRR
            0x30,0x03,0x1F,      // XX        XXXXXXX      RRRRRRR        RR
            0x30,0x03,0xF3,      // XX        XXXX  XXXXRRRR  RRRR        RR
            0x30,0x00,0xFF,      // XX          XXXXXXXXRRRRRRRR          RR
            0x30,0x00,0x3F,      // XX          XXXXXX    RRRRRR          RR
            0x30,0x00,0x00,      // XX                                    RR
            0xF0,0xFF,0x0F       // XXXXXXXXXXXXXX            RRRRRRRRRRRRRR
        };

        // Castle
        private static readonly byte[] roomGfxCastle3 =
        {
            0xF0,0xFE,0x15,      // XXXXXXXXXXX X X X      R R R RRRRRRRRRRR
            0x30,0x03,0x1F,      // XX        XXXXXXX      RRRRRRR        RR
            0x30,0x03,0xF5,      // XX        XXX X XXXXRRRR R RRR        RR
            0x30,0x00,0xFF,      // XX          XXXXXXXXRRRRRRRR          RR
            0x30,0x00,0x3F,      // XX          XXXXXX    RRRRRR          RR
            0x30,0x00,0x00,      // XX                                    RR
            0xF0,0xFF,0x0F       // XXXXXXXXXXXXXX            RRRRRRRRRRRRRR
        };

        // Walled off Castle
        private static readonly byte[] roomGfxCastle4 =
        {
            0xF0,0xFE,0x15,      // XXXXXXXXXXX X X X      R R R RRRRRRRRRRR
            0x30,0x03,0x1F,      // XX        XXXXXXX      RRRRRRR        RR
            0x30,0x03,0xFF,      // XX        XXXXXXXXXXRRRRRRRRRR        RR
            0x30,0x00,0xFF,      // XX          XXXXXXXXRRRRRRRR          RR
            0x30,0x00,0x3F,      // XX          XXXXXX    RRRRRR          RR
            0x30,0x00,0x00,      // XX                                    RR
            0xF0,0xFF,0xFF       // XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR
        };

        // Red Maze #1
        private static readonly byte[] roomGfxRedMaze1 =
        {
            0xF0,0xFF,0xFF,          // XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR
            0x00,0x00,0x00,          //
            0xF0,0xFF,0x0F,          // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR
            0x00,0x00,0x0C,          //                   XX        RR
            0xF0,0xFF,0x0C,          // XXXXXXXXXXXX  XX        RR  RRRRRRRRRRRR
            0xF0,0x03,0xCC,          // XXXX      XX  XX  XXRR  RR  RR      RRRR
            0xF0,0x33,0xCF           // XXXX  XX  XXXXXX  XXRR  RRRRRR  RR  RRRR
        };

        // Bottom of Red Maze
        private static readonly byte[] roomGfxRedMazeBottom =
        {
            0xF0,0x33,0xCF,          // XXXX  XX  XXXXXX  XXRR  RRRRRR  RR  RRRR
            0xF0,0x30,0x00,          // XXXX  XX                        RR  RRRR
            0xF0,0x33,0xFF,          // XXXX  XX  XXXXXXXXXXRRRRRRRRRR  RR  RRRR
            0x00,0x33,0x00,          //       XX  XX                RR  RR  RRRR
            0xF0,0xFF,0x00,          // XXXXXXXXXXXX                RRRRRRRRRRRR
            0x00,0x00,0x00,          //
            0xF0,0xFF,0x0F           // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR
        };

        // Top of Red Maze
        private static readonly byte[] roomGfxRedMazeTop =
        {
            0xF0,0xFF,0xFF,          // XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR
            0x00,0x00,0xC0,          //                   XXRR
            0xF0,0xFF,0xCF,          // XXXXXXXXXXXXXXXX  XXRR  RRRRRRRRRRRRRRRR
            0x00,0x00,0xCC,          //               XX  XXRR  RR
            0xF0,0x33,0xFF,          // XXXX  XX  XXXXXXXXXXRRRRRRRRRR  RR  RRRR
            0xF0,0x33,0x00,          // XXXX  XX  XX                RR  RR  RRRR
            0xF0,0x3F,0x0C           // XXXX  XXXXXX  XX        RR  RRRRRR  RRRR
        };


        // White Castle Entry
        private static readonly byte[] roomGfxWhiteCastleEntry =
        {
            0xF0,0x3F,0x0C,          // XXXX  XXXXXX  XX        RR  RRRRRR  RRRR
            0xF0,0x00,0x0C,          // XXXX          XX        RR          RRRR
            0xF0,0xFF,0x0F,          // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR
            0x00,0x30,0x00,          //       XX                        RR
            0xF0,0x30,0x00,          // XXXX  XX                        RR  RRRR
            0x00,0x30,0x00,          //       XX                        RR
            0xF0,0xFF,0x0F           // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR
        };

        // Top Entry Room
        private static readonly byte[] roomGfxTopEntryRoom =
        {
            0xF0,0xFF,0x0F,          // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR
            0x30,0x00,0x00,          // XX                                    RR
            0x30,0x00,0x00,          // XX                                    RR
            0x30,0x00,0x00,          // XX                                    RR
            0x30,0x00,0x00,          // XX                                    RR
            0x30,0x00,0x00,          // XX                                    RR
            0xF0,0xFF,0xFF           // XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR
        };

        // Black Maze #1
        private static readonly byte[] roomGfxBlackMaze1 =
        {
            0xF0,0xF0,0xFF,          // XXXXXXXX    XXXXXXXXRRRRRRRR    RRRRRRRR
            0x00,0x00,0x03,          //             XX            RR
            0xF0,0xFF,0x03,          // XXXXXXXXXXXXXX            RRRRRRRRRRRRRR
            0x00,0x00,0x00,          //
            0x30,0x3F,0xFF,          // XX    XXXXXXXXXXXXXXRRRRRRRRRRRRRR    RR
            0x00,0x30,0x00,          //       XX                        RR
            0xF0,0xF0,0xFF           // XXXXXXXX    XXXXXXXXRRRRRRRR    RRRRRRRR
        };

        // Black Maze #3
        private static readonly byte[] roomGfxBlackMaze3 =
        {
            0xF0,0xF0,0xFF,          // XXXXXXXX    XXXXXXXXRRRRRRRR    RRRRRRRR
            0x30,0x00,0x00,          // XX                  MM
            0x30,0x3F,0xFF,          // XX    XXXXXXXXXXXXXXMM    MMMMMMMMMMMMMM
            0x00,0x30,0x00,          //       XX                  MM
            0xF0,0xF0,0xFF,          // XXXXXXXX    XXXXXXXXMMMMMMMM    MMMMMMMM
            0x30,0x00,0x03,          // XX          XX      MM          MM
            0xF0,0xF0,0xFF           // XXXXXXXX    XXXXXXXXMMMMMMMM    MMMMMMMM
        };

        // Black Maze #2
        private static readonly byte[] roomGfxBlackMaze2 =
        {
            0xF0,0xFF,0xFF,          // XXXXXXXXXXXXXXXXXXXXMMMMMMMMMMMMMMMMMMMM
            0x00,0x00,0xC0,          //                   XX                  MM
            0xF0,0xFF,0xCF,          // XXXXXXXXXXXXXXXX  XXMMMMMMMMMMMMMMMM  MM
            0x00,0x00,0x0C,          //                   XX                  MM
            0xF0,0x0F,0xFF,          // XXXX    XXXXXXXXXXXXMMMM    MMMMMMMMMMMM
            0x00,0x0F,0xC0,          //         XXXX      XX        MMMM      MM
            0x30,0xCF,0xCC           // XX  XX  XXXX  XX  XXMM  MM  MMMM  MM  MM
        };

        // Black Maze Entry
        private static readonly byte[] roomGfxBlackMazeEntry =
        {
            0x30,0xCF,0xCC,          // XX  XX  XXXX  XX  XXMM  MM  MMMM  MM  MM
            0x00,0xC0,0xCC,          //         XX        XX  XXRR  RR        RR
            0xF0,0xFF,0x0F,          // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR
            0x00,0x00,0x00,          //
            0xF0,0xFF,0x0F,          // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR
            0x00,0x00,0x00,          //
            0xF0,0xFF,0x0F           // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR
        };


    }
}
