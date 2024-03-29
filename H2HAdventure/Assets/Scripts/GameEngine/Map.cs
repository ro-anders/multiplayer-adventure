﻿using System.Collections.Generic;

namespace GameEngine
{
    public class Map
    {

        // When drawing on the display, each map block is an 8x32 block
        public const int WALL_WIDTH = 8; // In ball coordinates
        public const int WALL_HEIGHT = 32; // In ball coordinates
        public const int MAX_WALL_X = Adv.ADVENTURE_SCREEN_BWIDTH / WALL_WIDTH;
        public const int MAX_WALL_Y = Adv.ADVENTURE_TOTAL_SCREEN_HEIGHT / WALL_HEIGHT;


        public const int LEFT_THIN_WALL = 0x0D * 2;
        public const int RIGHT_THIN_WALL = 0x96 * 2;
        public const int THIN_WALL_WIDTH = 4;

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
        public const int RED_MAZE_3 = 0x17; // Room above hidden room in white castle maze
        public const int RED_MAZE_2 = 0x18; // Room above entry room in the white castle maze
        public const int RED_MAZE_4 = 0x19; // Hidden room in white castle maze
        public const int RED_MAZE_1 = 0x1A; // First room as you enter the white castle maze
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

        public const int NUM_ROOMS = COPPER_FOYER + 1;

        public ROOM[] roomDefs;
        public int layout; // MAP_LAYOUT_SMALL or MAP_LAYOUT_BIG


        public const int LONG_WAY = 5;

        private int[,] distances = new int[0, 0];

        private Guide guide = new Guide();
        public Guide Guide
        {
            get { return guide; }
        }

        public Map(int numPlayers, int inGameMapLayout, bool isCooperative, bool usesGuides)
        {
            roomDefs = new ROOM[NUM_ROOMS];
            layout = inGameMapLayout;
            defaultRooms();
            ConfigureMaze(numPlayers, isCooperative);
            if (usesGuides)
            {
                guide.ConfigureGuide(numPlayers==3);
            }
        }

        static public int getNumRooms()
        {
            return NUM_ROOMS;
        }

        void defaultRooms()
        {

            addRoom(NUMBER_ROOM, new ROOM(roomWallsNumberRoom, ROOM.FLAG_MIRROR, COLOR.PURPLE,                       // 0x00
                                                NUMBER_ROOM, NUMBER_ROOM, NUMBER_ROOM, NUMBER_ROOM, "Number Room", ROOM.RandomVisibility.HIDDEN));
            addRoom(MAIN_HALL_LEFT, new ROOM(roomWallsCorridorOpenTop, ROOM.FLAG_MIRROR | ROOM.FLAG_LEFTTHINWALL, COLOR.OLIVEGREEN, // 0x01
                                             BLUE_MAZE_1, MAIN_HALL_CENTER, BLACK_CASTLE, MAIN_HALL_RIGHT, "Main Hall Left"));
            addRoom(MAIN_HALL_CENTER, new ROOM(roomWallsCorridorOpenTop, ROOM.FLAG_MIRROR, COLOR.LIMEGREEN,        // 0x02
                                               GOLD_CASTLE, MAIN_HALL_RIGHT, BLUE_MAZE_2, MAIN_HALL_LEFT, "Main Hall Center"));
            addRoom(MAIN_HALL_RIGHT, new ROOM(roomWallsSideCorridor, ROOM.FLAG_MIRROR | ROOM.FLAG_RIGHTTHINWALL, COLOR.TAN,          // 0x03
                                            COPPER_CASTLE, MAIN_HALL_LEFT, SOUTHEAST_ROOM, MAIN_HALL_CENTER, "Main Hall Right"));
            addRoom(BLUE_MAZE_5, new ROOM(roomWallsBlueMazeTop, ROOM.FLAG_MIRROR, COLOR.BLUE,                        // 0x04
                                          0x10, 0x05, 0x07, 0x06, "Blue Maze 5"));
            addRoom(BLUE_MAZE_2, new ROOM(roomWallsBlueMaze2, ROOM.FLAG_MIRROR, COLOR.BLUE,                          // 0x05
                                          0x1D, 0x06, 0x08, 0x04, "Blue Maze 2"));
            addRoom(BLUE_MAZE_3, new ROOM(roomWallsBlueMazeBottom, ROOM.FLAG_MIRROR, COLOR.BLUE,                     // 0x06
                                          0x07, 0x04, 0x03, 0x05, "Blue Maze 3"));
            addRoom(BLUE_MAZE_4, new ROOM(roomWallsBlueMazeCenter, ROOM.FLAG_MIRROR, COLOR.BLUE,                     // 0x07
                                          0x04, 0x08, 0x06, 0x08, "Blue Maze 4"));
            addRoom(BLUE_MAZE_1, new ROOM(roomWallsBlueMazeEntry, ROOM.FLAG_MIRROR, COLOR.BLUE,                      // 0x08
                                          0x05, 0x07, 0x01, 0x07, "Blue Maze 1"));
            addRoom(WHITE_MAZE_2, new ROOM(roomWallsMazeMiddle, ROOM.FLAG_MIRROR, COLOR.LTGRAY,                      // 0x09
                                           WHITE_MAZE_1, WHITE_MAZE_1, WHITE_MAZE_3, WHITE_MAZE_1, "White Maze 2"));
            addRoom(WHITE_MAZE_1, new ROOM(roomWallsMazeEntry, ROOM.FLAG_MIRROR, COLOR.LTGRAY,                       // 0x0A
                                           MAIN_HALL_RIGHT, WHITE_MAZE_2, WHITE_MAZE_2, WHITE_MAZE_2, "White Maze 1"));
            addRoom(WHITE_MAZE_3, new ROOM(roomWallsMazeSide, ROOM.FLAG_MIRROR, COLOR.LTGRAY,                        // 0x0B
                                           WHITE_MAZE_2, SOUTH_HALL_RIGHT, BLACK_INNERMOST_ROOM, SOUTH_HALL_LEFT, "White Maze 3"));
            addRoom(SOUTH_HALL_RIGHT, new ROOM(roomWallsSideCorridor, ROOM.FLAG_MIRROR | ROOM.FLAG_RIGHTTHINWALL, COLOR.LTCYAN,       // 0x0C
                                               BLACK_INNERMOST_ROOM, SOUTH_HALL_LEFT, SOUTHEAST_ROOM, WHITE_MAZE_3, "South Hall Right"));
            addRoom(SOUTH_HALL_LEFT, new ROOM(roomWallsSideCorridor, ROOM.FLAG_MIRROR | ROOM.FLAG_LEFTTHINWALL, COLOR.DKGREEN,        // 0x0D
                                              0x0F, 0x0B, 0x0E, 0x0C, "South Hall Left"));                         // 0x0E
            addRoom(SOUTHWEST_ROOM, new ROOM(roomWallsTopEntryRoom, ROOM.FLAG_MIRROR, COLOR.CYAN,
                                             0x0D, 0x10, 0x0F, 0x10, "Southwest Room"));
            addRoom(WHITE_CASTLE, new ROOM(roomWallsCastle, ROOM.FLAG_MIRROR, COLOR.WHITE,                           // 0x0F
                                           0x0E, 0x0F, 0x0D, 0x0F, "White Castle"));
            addRoom(BLACK_CASTLE, new ROOM(roomWallsCastle, ROOM.FLAG_MIRROR, COLOR.BLACK,                           // 0x10
                                           0x01, 0x1C, 0x04, 0x1C, "Black Castle"));
            addRoom(GOLD_CASTLE, new ROOM(roomWallsCastle, ROOM.FLAG_MIRROR, COLOR.YELLOW,                           // 0x11
                                          0x06, 0x03, 0x02, 0x01, "Gold Castle"));
            addRoom(GOLD_FOYER, new ROOM(roomWallsNumberRoom, ROOM.FLAG_MIRROR, COLOR.YELLOW,                        // 0x12
                                         GOLD_FOYER, GOLD_FOYER, GOLD_FOYER, GOLD_FOYER, "Gold Foyer"));
            addRoom(BLACK_MAZE_1, new ROOM(roomWallsBlackMaze1, ROOM.FLAG_MIRROR, COLOR.LTGRAY,                      // 0x13
                                           0x15, 0x14, 0x15, 0x16, "Black Maze 1"));
            addRoom(BLACK_MAZE_2, new ROOM(roomWallsBlackMaze2, ROOM.FLAG_NONE, COLOR.LTGRAY,                    // 0x14
                                           0x16, 0x15, 0x16, 0x13, "Black Maze 2"));
            addRoom(BLACK_MAZE_3, new ROOM(roomWallsBlackMaze3, ROOM.FLAG_NONE, COLOR.LTGRAY,                    // 0x15
                                           0x13, 0x16, 0x13, 0x14, "Black Maze 3"));
            addRoom(BLACK_MAZE_ENTRY, new ROOM(roomWallsBlackMazeEntry, ROOM.FLAG_MIRROR, COLOR.LTGRAY,              // 0x16
                                               0x14, 0x13, 0x1B, 0x15, "Black Maze Entry"));
            addRoom(RED_MAZE_3, new ROOM(roomWallsRedMaze3, ROOM.FLAG_MIRROR, COLOR.RED,                             // 0x17
                                         0x19, 0x18, 0x19, 0x18, "Red Maze 3"));
            addRoom(RED_MAZE_2, new ROOM(roomWallsRedMaze2, ROOM.FLAG_MIRROR, COLOR.RED,                           // 0x18
                                         0x1A, 0x17, 0x1A, 0x17, "Red Maze 2"));
            addRoom(RED_MAZE_4, new ROOM(roomWallsRedMaze4, ROOM.FLAG_MIRROR, COLOR.RED,                        // 0x19
                                         0x17, 0x1A, 0x17, 0x1A, "Red Maze4 "));
            addRoom(RED_MAZE_1, new ROOM(roomRedMaze1, ROOM.FLAG_MIRROR, COLOR.RED,                     // 0x1A
                                         0x18, 0x19, 0x18, 0x19, "Red Maze 1"));
            addRoom(BLACK_FOYER, new ROOM(roomWallsTwoExitRoom, ROOM.FLAG_MIRROR, COLOR.RED,                         // 0x1B
                                BLACK_INNERMOST_ROOM, BLACK_INNERMOST_ROOM, BLACK_INNERMOST_ROOM, BLACK_INNERMOST_ROOM, "Black Foyer"));
            addRoom(BLACK_INNERMOST_ROOM, new ROOM(roomWallsNumberRoom, ROOM.FLAG_MIRROR, COLOR.PURPLE,              // 0x1C
                                    SOUTHEAST_ROOM, BLUE_MAZE_4, BLACK_FOYER, BLUE_MAZE_1, "Black Innermost Room"));
            addRoom(SOUTHEAST_ROOM, new ROOM(roomWallsTopEntryRoom, ROOM.FLAG_MIRROR, COLOR.RED,                     // 0x1D
                                             MAIN_HALL_RIGHT, MAIN_HALL_LEFT, BLACK_CASTLE, MAIN_HALL_RIGHT, "Southeast Room"));
            addRoom(ROBINETT_ROOM, new ROOM(roomWallsCorridorOpenTop, ROOM.FLAG_MIRROR, COLOR.PURPLE,              // 0x1E
                                            CRYSTAL_CASTLE, MAIN_HALL_LEFT, CRYSTAL_CASTLE, MAIN_HALL_RIGHT, "Robinett Room", ROOM.RandomVisibility.HIDDEN));
            addRoom(JADE_CASTLE, new ROOM(roomWallsCastle3, ROOM.FLAG_MIRROR, COLOR.JADE,                            // 0x1F
                                          SOUTHEAST_ROOM, BLUE_MAZE_3, BLUE_MAZE_2, BLUE_MAZE_5, "Jade Castle", ROOM.RandomVisibility.HIDDEN));
            addRoom(JADE_FOYER, new ROOM(roomWallsNumberRoom, ROOM.FLAG_MIRROR, COLOR.JADE,                          // 0x20
                                         JADE_FOYER, JADE_FOYER, JADE_FOYER, JADE_FOYER, "Jade Foyer", ROOM.RandomVisibility.HIDDEN));
            addRoom(COPPER_CASTLE, new ROOM(roomWallsCastle2, ROOM.FLAG_MIRROR, COLOR.COPPER,                        // 0x21
                                            BLUE_MAZE_3, MAIN_HALL_LEFT, MAIN_HALL_RIGHT, GOLD_CASTLE, "Copper Castle"));
            addRoom(COPPER_FOYER, new ROOM(roomWallsNumberRoom, ROOM.FLAG_MIRROR, COLOR.COPPER,                      // 0x22
                                           COPPER_FOYER, COPPER_FOYER, COPPER_FOYER, COPPER_FOYER, "Copper Foyer"));
            addRoom(CRYSTAL_CASTLE, new ROOM(roomWallsCastle4, ROOM.FLAG_MIRROR, COLOR.CRYSTAL,                        // 0x23
                                             ROBINETT_ROOM, CRYSTAL_CASTLE, ROBINETT_ROOM, CRYSTAL_CASTLE, "Crystal Castle", ROOM.RandomVisibility.HIDDEN));
            addRoom(CRYSTAL_FOYER, new ROOM(roomWallsNumberRoom, ROOM.FLAG_MIRROR, COLOR.DARK_CRYSTAL4,                      // 0x24
                                            CRYSTAL_FOYER, CRYSTAL_FOYER, CRYSTAL_FOYER, CRYSTAL_FOYER, "Crystal Foyer", ROOM.RandomVisibility.HIDDEN));
        }

        private void ConfigureMaze(int numPlayers, bool isCooperative)
        {
            // Remove the Copper Castle in cooperative games
            if (isCooperative)
            {
                // Remove the copper castle
                roomDefs[MAIN_HALL_RIGHT].closeTop();
                roomDefs[MAIN_HALL_RIGHT].roomUp = BLUE_MAZE_3;
                roomDefs[COPPER_CASTLE].visibility = ROOM.RandomVisibility.HIDDEN;
                roomDefs[COPPER_FOYER].visibility = ROOM.RandomVisibility.HIDDEN;
            }

            // Add the Jade Castle if 3 players
            if ((numPlayers > 2) && !isCooperative)
            {
                roomDefs[BLUE_MAZE_2].roomUp = JADE_CASTLE;
                roomDefs[BLUE_MAZE_2].openTop();
                roomDefs[JADE_CASTLE].visibility = ROOM.RandomVisibility.OPEN;
                roomDefs[JADE_FOYER].visibility = ROOM.RandomVisibility.IN_CASTLE;
            }

            if (layout == MAP_LAYOUT_SMALL)
            {
                // This is the default.  Nothing to do.
            }
            else if (layout == MAP_LAYOUT_GAUNTLET)
            {
                // Make the right side of the main hall a dead end.
                roomDefs[MAIN_HALL_RIGHT].roomUp = BLUE_MAZE_3;
                roomDefs[MAIN_HALL_RIGHT].roomDown = BLACK_CASTLE;
                roomDefs[MAIN_HALL_RIGHT].closeTop();
                roomDefs[MAIN_HALL_RIGHT].closeBottom();
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
                // (or the Black Castle's innermost room if its cooperative)
                if (!isCooperative)
                {
                    roomDefs[MAIN_HALL_RIGHT].closeTop();
                    roomDefs[MAIN_HALL_RIGHT].roomUp = BLUE_MAZE_3;
                    roomDefs[COPPER_CASTLE].roomDown = SOUTH_HALL_RIGHT;
                    roomDefs[COPPER_CASTLE].roomUp = SOUTHEAST_ROOM;
                    roomDefs[COPPER_CASTLE].roomRight = BLUE_MAZE_4;
                    roomDefs[COPPER_CASTLE].roomLeft = BLUE_MAZE_1;
                    roomDefs[WHITE_MAZE_3].roomDown = COPPER_CASTLE;
                    roomDefs[SOUTH_HALL_RIGHT].roomUp = COPPER_CASTLE;
                    roomDefs[BLACK_CASTLE].roomLeft = COPPER_CASTLE;
                    roomDefs[BLACK_CASTLE].roomRight = COPPER_CASTLE;
                }
                else
                {
                    roomDefs[BLACK_INNERMOST_ROOM].roomDown = SOUTH_HALL_RIGHT;
                    roomDefs[BLACK_INNERMOST_ROOM].roomUp = SOUTHEAST_ROOM;
                    roomDefs[BLACK_INNERMOST_ROOM].roomRight = BLUE_MAZE_4;
                    roomDefs[BLACK_INNERMOST_ROOM].roomLeft = BLUE_MAZE_1;
                }

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
            distances = new int[NUM_ROOMS, NUM_ROOMS];
            for (int ctr1 = 0; ctr1 < NUM_ROOMS; ++ctr1)
            {
                for (int ctr2 = 0; ctr2 < NUM_ROOMS; ++ctr2)
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
                for (int ctr1 = 0; ctr1 < NUM_ROOMS; ++ctr1)
                {
                    for (int ctr2 = 0; ctr2 < NUM_ROOMS; ++ctr2)
                    {
                        if (distances[ctr1, ctr2] == LONG_WAY)
                        {
                            for (int ctr3 = 0; ctr3 < NUM_ROOMS; ++ctr3)
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
            if ((key < 0) || (key > NUM_ROOMS))
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
            roomDefs[CRYSTAL_FOYER].closeBottom();
        }

        // Setup map for Easter Egg gauntlet.
        public void easterEggLayout2()
        {
            // Block off Jade Castle
            roomDefs[BLUE_MAZE_2].roomUp = MAIN_HALL_RIGHT;
            roomDefs[BLUE_MAZE_2].closeTop();

            // Block off white catacombs
            roomDefs[MAIN_HALL_RIGHT].roomUp = BLUE_MAZE_3;
            roomDefs[MAIN_HALL_RIGHT].roomDown = BLACK_CASTLE;
            roomDefs[MAIN_HALL_RIGHT].closeTop();
            roomDefs[MAIN_HALL_RIGHT].closeBottom();
            roomDefs[MAIN_HALL_LEFT].roomDown = BLACK_CASTLE;
            roomDefs[BLACK_CASTLE].roomLeft = BLUE_MAZE_4;
            roomDefs[BLACK_CASTLE].roomRight = BLUE_MAZE_1;

            // Block off black catacombs
            roomDefs[BLACK_FOYER].closeTop();
            roomDefs[BLACK_FOYER].roomUp = MAIN_HALL_LEFT;
            roomDefs[BLACK_FOYER].roomRight = BLUE_MAZE_4;
            roomDefs[BLACK_FOYER].roomDown = BLACK_FOYER;
            roomDefs[BLACK_FOYER].roomLeft = BLUE_MAZE_1;

            // Open up exit of crystal castle
            roomDefs[CRYSTAL_FOYER].openBottom();
            roomDefs[CRYSTAL_CASTLE].openBottom();

            // Remove the MAIN_HALL_RIGHT wall
            roomDefs[MAIN_HALL_RIGHT].flags = ROOM.FLAG_MIRROR;

        }


        //-- ROOM SHAPES ------------------------------------------------------

        // Below Yellow Castle
        private static readonly string[] roomWallsCorridorOpenTop =
        {
            "XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR",
            "                                        ",
            "                                        ",
            "                                        ",
            "                                        ",
            "                                        ",
            "XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR"
        };

        // Side Corridor
        private static readonly string[] roomWallsSideCorridor =
        {
            "XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR",
            "                                        ",
            "                                        ",
            "                                        ",
            "                                        ",
            "                                        ",
            "XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR"
        };

        // Number Room Definition
        private static readonly string[] roomWallsNumberRoom =
        {
            "XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR",
            "XX                                    RR",
            "XX                                    RR",
            "XX                                    RR",
            "XX                                    RR",
            "XX                                    RR",
            "XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR"
        };

        // `
        private static readonly string[] roomWallsTwoExitRoom =
        {
            "XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR",
            "XX                                    RR",
            "XX                                    RR",
            "XX                                    RR",
            "XX                                    RR",
            "XX                                    RR",
            "XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR"
        };

        // Blue Maze 5 - Top of Blue Maze
        private static readonly string[] roomWallsBlueMazeTop =
        {
            "XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR",
            "        XX    XX        RR    RR        ",
            "XXXX    XX    XXXX    RRRR    RR    RRRR",
            "XXXX    XX                    RR    RRRR",
            "XXXXXXXXXXXXXXXXXX    RRRRRRRRRRRRRRRRRR",
            "      XX        XX    RR        RR      ",
            "XXXX  XX  XXXXXXXX    RRRRRRRR  RR  RRRR"
        };


        // Blue Maze #2
        private static readonly string[] roomWallsBlueMaze2 =
        {
            "XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR",
            "                                        ",
            "XXXXXXXXXX  XXXXXXXXRRRRRRRR  RRRRRRRRRR",
            "XXXX              XXRR              RRRR",
            "XXXX  XXXXXXXXXX  XXRR  RRRRRRRRRR  RRRR",
            "      XX      XX  XXRR  RR      RR      ",
            "XXXXXXXX  XX  XX  XXRR  RR  RR  RRRRRRRR"
        };


        // Blue Maze 3 - Bottom of Blue Maze
        private static readonly string[] roomWallsBlueMazeBottom =
        {
            "XXXXXXXX  XX  XX        RR  RR  RRRRRRRR",
            "      XX      XX        RR      RR      ",
            "XXXX  XXXXXXXXXX        RRRRRRRRRR  RRRR",
            "XXXX                                RRRR",
            "XXXXXXXX                        RRRRRRRR",
            "      XX                        RR      ",
            "XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR"
        };


        // Blue Maze 4 - Center of Blue Maze
        private static readonly string[] roomWallsBlueMazeCenter =
        {
            "XXXX  XX  XXXXXXXX    RRRRRRRR  RR  RRRR",
            "      XX      XXXX    RRRR      RR      ",
            "XXXXXXXXXXXX  XXXX    RRRR  RRRRRRRRRRRR",
            "          XX  XXXX    RRRR  RR          ",
            "XXXX  XX  XX  XXXX    RRRR  RR  RR  RRRR",
            "      XX  XX  XX        RR  RR  RR      ",
            "XXXXXXXX  XX  XX        RR  RR  RRRRRRRR"
        };

        // Blue Maze 1 - Blue Maze Entry
        private static readonly string[] roomWallsBlueMazeEntry =
        {
            "XXXXXXXX  XX  XX  XXRR  RR  RR  RRRRRRRR",
            "      XX  XX  XX        RR  RR  RR      ",
            "XXXX  XX  XX  XXXXXXRRRRRR  RR  RR  RRRR",
            "      XX  XX                RR  RR      ",
            "XXXXXXXX  XXXXXXXXXXRRRRRRRRRR  RRRRRRRR",
            "                                        ",
            "XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR"
        };

        // Maze Middle
        private static readonly string[] roomWallsMazeMiddle =
        {
            "XXXXXXXXXXXX  XX  XXRR  RR  RRRRRRRRRRRR",
            "              XX  XXRR  RR              ",
            "XXXX      XXXXXX  XXRR  RRRRRR      RRRR",
            "          XX                RR          ",
            "XXXXXXXX  XX  XXXXXXRRRRRR  RR  RRRRRRRR",
            "      XX  XX  XX        RR  RR  RR      ",
            "XXXX  XX  XX  XX  XXRR  RR  RR  RR  RRRR"
        };

        // Maze Side
        private static readonly string[] roomWallsMazeSide =
        {
            "XXXX  XX  XX  XX  XXRR  RR  RR  RR  RRRR",
            "      XX      XX  XXRR  RR      RR      ",
            "      XXXXXXXXXX  XXRR  RRRRRRRRRR      ",
            "                  XXRR                  ",
            "      XXXXXXXX    XXRR    RRRRRRRR      ",
            "      XX          XXRR          RR      ",
            "XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR"
        };

        // Maze Entry
        private static readonly string[] roomWallsMazeEntry =
        { // 0123456789012345678901234567890123456789
            "XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR",
            "      XX                        RR      ",
            "XXXX  XX    XXXXXXXXRRRRRRRR    RR  RRRR",
            "      XX          XXRR          RR      ",
            "XXXXXXXX  XX      XXRR      RR  RRRRRRRR",
            "          XX      XXRR      RR          ",
            "XXXXXXXXXXXX  XX  XXRR  RR  RRRRRRRRRRRR"
        };

        // Castle
        private static readonly string[] roomWallsCastle = 
        { // 0123456789012345678901234567890123456789
            "XXXXXXXXXXX X X X      R R R RRRRRRRRRRR",
            "XX        XXXXXXX      RRRRRRR        RR",
            "XX        XXXXXXXXXXRRRRRRRRRR        RR",
            "XX          XXXXXXXXRRRRRRRR          RR",
            "XX          XXXXXX    RRRRRR          RR",
            "XX                                    RR",
            "XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR"
        };// 0123456789012345678901234567890123456789

        // Castle
        private static readonly string[] roomWallsCastle2 =
        {
            "XXXXXXXXXXX X X X      R R R RRRRRRRRRRR",
            "XX        XXXXXXX      RRRRRRR        RR",
            "XX        XXXX  XXXXRRRR  RRRR        RR",
            "XX          XXXXXXXXRRRRRRRR          RR",
            "XX          XXXXXX    RRRRRR          RR",
            "XX                                    RR",
            "XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR"
        };

        // Castle
        private static readonly string[] roomWallsCastle3 =
        {
            "XXXXXXXXXXX X X X      R R R RRRRRRRRRRR",
            "XX        XXXXXXX      RRRRRRR        RR",
            "XX        XXX X XXXXRRRR R RRR        RR",
            "XX          XXXXXXXXRRRRRRRR          RR",
            "XX          XXXXXX    RRRRRR          RR",
            "XX                                    RR",
            "XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR"
        };

        // Walled off Castle
        private static readonly string[] roomWallsCastle4 =
        {
            "XXXXXXXXXXX X X X      R R R RRRRRRRRRRR",
            "XX        XXXXXXX      RRRRRRR        RR",
            "XX        XXXXXXXXXXRRRRRRRRRR        RR",
            "XX          XXXXXXXXRRRRRRRR          RR",
            "XX          XXXXXX    RRRRRR          RR",
            "XX                                    RR",
            "XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR"
        };

        // Red Maze #1
        private static readonly string[] roomWallsRedMaze3 =
        {
            "XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR",
            "                                        ",
            "XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR",
            "              XX        RR              ",
            "XXXXXXXXXXXX  XX        RR  RRRRRRRRRRRR",
            "XXXX      XX  XX  XXRR  RR  RR      RRRR",
            "XXXX  XX  XXXXXX  XXRR  RRRRRR  RR  RRRR"
        };

        // Bottom of Red Maze
        private static readonly string[] roomWallsRedMaze4 =
        {
            "XXXX  XX  XXXXXX  XXRR  RRRRRR  RR  RRRR",
            "XXXX  XX                        RR  RRRR",
            "XXXX  XX  XXXXXXXXXXRRRRRRRRRR  RR  RRRR",
            "      XX  XX                RR  RR      ",
            "XXXXXXXXXXXX                RRRRRRRRRRRR",
            "                                        ",
            "XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR"
        };

        // Top of Red Maze
        private static readonly string[] roomWallsRedMaze2 =
        {
            "XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR",
            "                  XXRR                  ",
            "XXXXXXXXXXXXXXXX  XXRR  RRRRRRRRRRRRRRRR",
            "              XX  XXRR  RR              ",
            "XXXX  XX  XXXXXXXXXXRRRRRRRRRR  RR  RRRR",
            "XXXX  XX  XX                RR  RR  RRRR",
            "XXXX  XXXXXX  XX        RR  RRRRRR  RRRR"
        };


        // White Castle Entry
        private static readonly string[] roomRedMaze1 =
        {
            "XXXX  XXXXXX  XX        RR  RRRRRR  RRRR",
            "XXXX          XX        RR          RRRR",
            "XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR",
            "      XX                        RR      ",
            "XXXX  XX                        RR  RRRR",
            "      XX                        RR      ",
            "XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR"
        };

        // Top Entry Room
        private static readonly string[] roomWallsTopEntryRoom =
        {
            "XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR",
            "XX                                    RR",
            "XX                                    RR",
            "XX                                    RR",
            "XX                                    RR",
            "XX                                    RR",
            "XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR"
        };

        // Black Maze #1
        private static readonly string[] roomWallsBlackMaze1 =
        {
            "XXXXXXXX    XXXXXXXXRRRRRRRR    RRRRRRRR",
            "            XX            RR            ",
            "XXXXXXXXXXXXXX            RRRRRRRRRRRRRR",
            "                                        ",
            "XX    XXXXXXXXXXXXXXRRRRRRRRRRRRRR    RR",
            "      XX                        RR      ",
            "XXXXXXXX    XXXXXXXXRRRRRRRR    RRRRRRRR"
        };

        // Black Maze #3
        private static readonly string[] roomWallsBlackMaze3 =
        {
            "XXXXXXXX    XXXXXXXXMMMMMMMM    MMMMMMMM",
            "XX                  MM                  ",
            "XX    XXXXXXXXXXXXXXMM    MMMMMMMMMMMMMM",
            "      XX                  MM            ",
            "XXXXXXXX    XXXXXXXXMMMMMMMM    MMMMMMMM",
            "XX          XX      MM          MM      ",
            "XXXXXXXX    XXXXXXXXMMMMMMMM    MMMMMMMM"
        };

        // Black Maze #2
        private static readonly string[] roomWallsBlackMaze2 =
        {
            "XXXXXXXXXXXXXXXXXXXXMMMMMMMMMMMMMMMMMMMM",
            "                  XX                  MM",
            "XXXXXXXXXXXXXXXX  XXMMMMMMMMMMMMMMMM  MM",
            "              XX                  MM    ",
            "XXXX    XXXXXXXXXXXXMMMM    MMMMMMMMMMMM",
            "        XXXX      XX        MMMM      MM",
            "XX  XX  XXXX  XX  XXMM  MM  MMMM  MM  MM"
        };

        // Black Maze Entry
        private static readonly string[] roomWallsBlackMazeEntry =
        {
            "XX  XX  XXXX  XX  XXRR  RR  RRRR  RR  RR",
            "    XX        XX  XXRR  RR        RR    ",
            "XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR",
            "                                        ",
            "XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR",
            "                                        ",
            "XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR"
        };

    }

}
