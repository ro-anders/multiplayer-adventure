

#include "Map.hpp"

//
// Room graphics
//

// Left of Name Room
static const byte roomGfxLeftOfName [] =
{
    0xF0,0xFF,0xFF,     // XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRRRRRR
    0x00,0x00,0x00,
    0x00,0x00,0x00,
    0x00,0x00,0x00,
    0x00,0x00,0x00,
    0x00,0x00,0x00,
    0xF0,0xFF,0x0F      // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRRRRRR
};

// Below Yellow Castle
static const byte roomGfxBelowYellowCastle [] =
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
static const byte roomGfxSideCorridor [] =
{
    0xF0,0xFF,0x0F,     // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR
    0x00,0x00,0x00,
    0x00,0x00,0x00,
    0x00,0x00,0x00,
    0x00,0x00,0x00,
    0x00,0x00,0x00,
    0xF0,0xFF,0x0F      // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR
};


// Number Room Definition
static const byte roomGfxNumberRoom [] =
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
static const byte roomGfxTwoExitRoom [] =
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
static const byte roomGfxBlueMazeTop[] =
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
static const byte roomGfxBlueMaze1 [] =
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
static const byte roomGfxBlueMaze1B [] =
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
static const byte roomGfxBlueMazeBottom [] =
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
static const byte roomGfxBlueMazeCenter [] =
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
static const byte roomGfxBlueMazeEntry [] =
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
static const byte roomGfxMazeMiddle [] =
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
static const byte roomGfxMazeSide [] =
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
static const byte roomGfxMazeEntry [] =
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
static const byte roomGfxCastle [] =
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
static const byte roomGfxCastle2 [] =
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
static const byte roomGfxCastle3 [] =
{
    0xF0,0xFE,0x15,      // XXXXXXXXXXX X X X      R R R RRRRRRRRRRR
    0x30,0x03,0x1F,      // XX        XXXXXXX      RRRRRRR        RR
    0x30,0x03,0xF5,      // XX        XXX X XXXXRRRR R RRR        RR
    0x30,0x00,0xFF,      // XX          XXXXXXXXRRRRRRRR          RR
    0x30,0x00,0x3F,      // XX          XXXXXX    RRRRRR          RR
    0x30,0x00,0x00,      // XX                                    RR
    0xF0,0xFF,0x0F       // XXXXXXXXXXXXXX            RRRRRRRRRRRRRR
};

// Red Maze #1
static const byte roomGfxRedMaze1 [] =
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
static const byte roomGfxRedMazeBottom [] =
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
static const byte roomGfxRedMazeTop [] =
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
static const byte roomGfxWhiteCastleEntry [] =
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
static const byte roomGfxTopEntryRoom [] =
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
static const byte roomGfxBlackMaze1 [] =
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
static const byte roomGfxBlackMaze3 [] =
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
static const byte roomGfxBlackMaze2 [] =
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
static const byte roomGfxBlackMazeEntry [] =
{
    0x30,0xCF,0xCC,          // XX  XX  XXXX  XX  XXMM  MM  MMMM  MM  MM
    0x00,0xC0,0xCC,          //         XX        XX  XXRR  RR        RR
    0xF0,0xFF,0x0F,          // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR
    0x00,0x00,0x00,          //
    0xF0,0xFF,0x0F,          // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR
    0x00,0x00,0x00,          //
    0xF0,0xFF,0x0F           // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR
};

Map::Map(int numPlayers, int gameMapLayout) {
    ConfigureMaze(numPlayers, gameMapLayout);
}

ROOM Map::roomDefs[] =
{
    { roomGfxNumberRoom, ROOMFLAG_NONE, COLOR_PURPLE,                                       // 0 - Number Room
        NUMBER_ROOM, NUMBER_ROOM, NUMBER_ROOM, NUMBER_ROOM},
    { roomGfxBelowYellowCastle, ROOMFLAG_LEFTTHINWALL, COLOR_OLIVEGREEN,                    // 1 - Main Hall Left
        BLUE_MAZE_HALL_END, MAIN_HALL_CENTER,BLACK_CASTLE, MAIN_HALL_RIGHT},
    { roomGfxBelowYellowCastle, ROOMFLAG_NONE, COLOR_LIMEGREEN,                             // 2 - Main Hall Center
        GOLD_CASTLE, MAIN_HALL_RIGHT, BLUE_MAZE_JADE_END, MAIN_HALL_LEFT },
    { roomGfxSideCorridor, ROOMFLAG_RIGHTTHINWALL, COLOR_TAN,                                 // 3 - Main Hall Right
        COPPER_CASTLE, MAIN_HALL_LEFT,SOUTH_EAST_ROOM, MAIN_HALL_CENTER },
    { roomGfxBlueMazeTop, ROOMFLAG_NONE, COLOR_BLUE, 0x10,0x05,0x07,0x06 },       // 4 - Blue Maze next to Black Castle
    { roomGfxBlueMaze1, ROOMFLAG_NONE, COLOR_BLUE, 0x1D,0x06,0x08,0x04 },       // 5 - Blue Maze next to Jade Castle
    { roomGfxBlueMazeBottom, ROOMFLAG_NONE, COLOR_BLUE, 0x07,0x04,0x03,0x05 },       // 6 - Large Room at Bottom of Blue Maze
    { roomGfxBlueMazeCenter, ROOMFLAG_NONE, COLOR_BLUE, 0x04,0x08,0x06,0x08 },       // 7 - Blue Maze with all vertical paths
    { roomGfxBlueMazeEntry, ROOMFLAG_NONE, COLOR_BLUE, 0x05,0x07,0x01,0x07 },       // 8 - Blue Maze next to Main Hall
    { roomGfxMazeMiddle, ROOMFLAG_NONE, COLOR_LTGRAY, 0x0A,0x0A,0x0B,0x0A },       // 9 - Maze Middle
    { roomGfxMazeEntry, ROOMFLAG_NONE, COLOR_LTGRAY, 0x03,0x09,0x09,0x09 },       // A - Maze Entry
    { roomGfxMazeSide, ROOMFLAG_NONE, COLOR_LTGRAY, 0x09,0x0C,0x1C,0x0D },       // B - Maze Side
    { roomGfxSideCorridor, ROOMFLAG_RIGHTTHINWALL, COLOR_LTCYAN,                            // C - South Hall Right
        COPPER_CASTLE, MAIN_HALL_LEFT,SOUTH_EAST_ROOM, 0x0B },
    { roomGfxSideCorridor, ROOMFLAG_LEFTTHINWALL, COLOR_DKGREEN, 0x0F,0x0B,0x0E,0x0C },       // D - Side Corridor
    { roomGfxTopEntryRoom, ROOMFLAG_NONE, COLOR_CYAN, 0x0D,0x10,0x0F,0x10 },       // E - Top Entry Room
    { roomGfxCastle, ROOMFLAG_NONE, COLOR_WHITE, 0x0E,0x0F,0x0D,0x0F },       // F - White Castle
    { roomGfxCastle, ROOMFLAG_NONE, COLOR_BLACK, 0x01,0x1C,0x04,0x1C },       // 10 - Black Castle
    { roomGfxCastle, ROOMFLAG_NONE, COLOR_YELLOW, 0x06,0x03,0x02,0x01 },            // 11 - Yellow Castle
    { roomGfxNumberRoom, ROOMFLAG_NONE, COLOR_YELLOW, 0x12,0x12,0x12,0x12 },       // 12 - Yellow Castle Entry
    { roomGfxBlackMaze1, ROOMFLAG_NONE, COLOR_LTGRAY, 0x15,0x14,0x15,0x16 },       // 13 - Black Maze #1
    { roomGfxBlackMaze2, ROOMFLAG_MIRROR, COLOR_LTGRAY, 0x16,0x15,0x16,0x13 },       // 14 - Black Maze #2
    { roomGfxBlackMaze3, ROOMFLAG_MIRROR, COLOR_LTGRAY, 0x13,0x16,0x13,0x14 },       // 15 - Black Maze #3
    { roomGfxBlackMazeEntry, ROOMFLAG_NONE, COLOR_LTGRAY, 0x14,0x13,0x1B,0x15 },       // 16 - Black Maze Entry
    { roomGfxRedMaze1, ROOMFLAG_NONE, COLOR_RED, 0x19,0x18,0x19,0x18 },       // 17 - Red Maze #1
    { roomGfxRedMazeTop, ROOMFLAG_NONE, COLOR_RED, 0x1A,0x17,0x1A,0x17 },       // 18 - Top of Red Maze
    { roomGfxRedMazeBottom, ROOMFLAG_NONE, COLOR_RED, 0x17,0x1A,0x17,0x1A },       // 19 - Bottom of Red Maze
    { roomGfxWhiteCastleEntry, ROOMFLAG_NONE, COLOR_RED, 0x18,0x19,0x18,0x19 },       // 1A - White Castle Entry
    { roomGfxTwoExitRoom, ROOMFLAG_NONE, COLOR_RED,                                       // 1B - Black Castle First Room
        BLACK_INNERMOST_ROOM,  BLACK_INNERMOST_ROOM, BLACK_INNERMOST_ROOM, BLACK_INNERMOST_ROOM },
    { roomGfxNumberRoom, ROOMFLAG_NONE, COLOR_PURPLE,                               // 1C - Second Room in Black Castle
        SOUTH_EAST_ROOM, BLUE_MAZE_VERT_PATHS, BLACK_FOYER, BLUE_MAZE_HALL_END},    // TODO: Used to be north of se room.
    { roomGfxTopEntryRoom, ROOMFLAG_NONE, COLOR_RED,                                        // 1D - South East Room
        MAIN_HALL_RIGHT, MAIN_HALL_LEFT, BLACK_CASTLE, MAIN_HALL_RIGHT },
    { roomGfxBelowYellowCastle, ROOMFLAG_NONE, COLOR_PURPLE, 0x06,0x01,0x06,0x03 },        // 1E - Name Room
    { roomGfxCastle3, ROOMFLAG_NONE, COLOR_JADE, 0x1D, 0x06, 0x05, 0x04 },            // 1F - Jade Castle
    { roomGfxNumberRoom, ROOMFLAG_NONE, COLOR_JADE, 0x20, 0x20, 0x20, 0x20 },       // 20 - Copper Castle Entry
    { roomGfxCastle2, ROOMFLAG_NONE, COLOR_COPPER,
        BLUE_MAZE_LARGE_ROOM, MAIN_HALL_LEFT, MAIN_HALL_RIGHT, GOLD_CASTLE},
    { roomGfxNumberRoom, ROOMFLAG_NONE, COLOR_COPPER,                                     // 21 - Copper Foyer
        COPPER_FOYER, COPPER_FOYER, COPPER_FOYER, COPPER_FOYER}
};

void Map::ConfigureMaze(int numPlayers, int gameMapLayout) {
    
    // Add the Jade Castle if 3 players
    if (numPlayers > 2) {
        roomDefs[BLUE_MAZE_JADE_END].roomUp = JADE_CASTLE;
        roomDefs[BLUE_MAZE_JADE_END].graphicsData = roomGfxBlueMaze1B;
    }
    
    if (gameMapLayout == 0) {
        // This is the default setup, so don't need to do anything.
    } else {
        // Games 2 or 3.
        // Connect the lower half of the world.
        roomDefs[MAIN_HALL_LEFT].roomDown = WHITE_CASTLE;
        roomDefs[MAIN_HALL_CENTER].roomDown = GOLD_CASTLE;
        roomDefs[MAIN_HALL_RIGHT].roomDown = WHITE_MAZE_HALL_END;
        roomDefs[SOUTH_EAST_ROOM].roomUp = SOUTH_HALL_RIGHT;
        
        // Move the Copper Castle to the White Maze
        roomDefs[MAIN_HALL_RIGHT].graphicsData = roomGfxLeftOfName;
        roomDefs[MAIN_HALL_RIGHT].roomUp = BLUE_MAZE_LARGE_ROOM;
        roomDefs[COPPER_CASTLE].roomDown = SOUTH_HALL_RIGHT;
        // TODO: Change up, left, and right of COPPER_CASTLE
        
        // Put the Black Maze in the Black Castle
        roomDefs[BLACK_FOYER].roomUp = BLACK_MAZE_ENTRY;
        roomDefs[BLACK_FOYER].roomRight = BLACK_MAZE_ENTRY;
        roomDefs[BLACK_FOYER].roomDown = BLACK_MAZE_ENTRY;
        roomDefs[BLACK_FOYER].roomLeft = BLACK_MAZE_ENTRY;
        
    }
}



