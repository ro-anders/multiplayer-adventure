

#include "Map.hpp"

#include <stdlib.h>
#include <string.h>
#include "Adventure.h"
#include "Portcullis.hpp"
#include "Room.hpp"

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

// Straight Hall
static const byte roomGfxStraightHall [] =
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

int Map::numRooms = COPPER_FOYER + 1;

int Map::LONG_WAY = 5;

Map::Map(int numPlayers, int gameMapLayout) {
    roomDefs = new ROOM*[numRooms];
    memset(roomDefs, 0, numRooms*sizeof(ROOM*));
    
    defaultRooms();
    ConfigureMaze(numPlayers, gameMapLayout);
    ComputeDistances(0 , NULL);
}

Map::~Map() {
    // Delete all the rooms and the whole distance table
    for(int ctr=0; ctr<numRooms; ++ctr) {
        if (roomDefs[ctr] != NULL) {
            delete roomDefs[ctr];
        }
        delete[] distances[ctr];
    }
    delete[] roomDefs;
    delete[] distances;
}

int Map::getNumRooms() {
    return numRooms;
}

void Map::defaultRooms() {
    
    addRoom(NUMBER_ROOM, new ROOM(roomGfxNumberRoom, ROOMFLAG_NONE, COLOR_PURPLE,                       // 0x00
                                  NUMBER_ROOM, NUMBER_ROOM, NUMBER_ROOM, NUMBER_ROOM, "Number Room", ROOM::HIDDEN));
    addRoom(MAIN_HALL_LEFT, new ROOM(roomGfxBelowYellowCastle, ROOMFLAG_LEFTTHINWALL, COLOR_OLIVEGREEN, // 0x01
                                     BLUE_MAZE_1, MAIN_HALL_CENTER,BLACK_CASTLE, MAIN_HALL_RIGHT, "Main Hall Left"));
    addRoom(MAIN_HALL_CENTER, new ROOM(roomGfxBelowYellowCastle, ROOMFLAG_NONE, COLOR_LIMEGREEN,        // 0x02
                                       GOLD_CASTLE, MAIN_HALL_RIGHT, BLUE_MAZE_2, MAIN_HALL_LEFT, "Main Hall Center"));
    addRoom(MAIN_HALL_RIGHT , new ROOM(roomGfxSideCorridor, ROOMFLAG_RIGHTTHINWALL, COLOR_TAN,          // 0x03
                                    COPPER_CASTLE, MAIN_HALL_LEFT,SOUTHEAST_ROOM, MAIN_HALL_CENTER, "Main Hall Right"));
    addRoom(BLUE_MAZE_5, new ROOM(roomGfxBlueMazeTop, ROOMFLAG_NONE, COLOR_BLUE,                        // 0x04
                                  0x10,0x05,0x07,0x06, "Blue Maze 5"));
    addRoom(BLUE_MAZE_2, new ROOM(roomGfxBlueMaze1, ROOMFLAG_NONE, COLOR_BLUE,                          // 0x05
                                  0x1D,0x06,0x08,0x04, "Blue Maze 2"));
    addRoom(BLUE_MAZE_3, new ROOM(roomGfxBlueMazeBottom, ROOMFLAG_NONE, COLOR_BLUE,                     // 0x06
                                  0x07,0x04,0x03,0x05, "Blue Maze 3"));
    addRoom(BLUE_MAZE_4, new ROOM(roomGfxBlueMazeCenter, ROOMFLAG_NONE, COLOR_BLUE,                     // 0x07
                                  0x04,0x08,0x06,0x08, "Blue Maze 4"));
    addRoom(BLUE_MAZE_1, new ROOM(roomGfxBlueMazeEntry, ROOMFLAG_NONE, COLOR_BLUE,                      // 0x08
                                  0x05,0x07,0x01,0x07, "Blue Maze 1"));
    addRoom(WHITE_MAZE_2, new ROOM(roomGfxMazeMiddle, ROOMFLAG_NONE, COLOR_LTGRAY,                      // 0x09
                                   0x0A,0x0A,0x0B,0x0A, "White Maze 1"));
    addRoom(WHITE_MAZE_1, new ROOM(roomGfxMazeEntry, ROOMFLAG_NONE, COLOR_LTGRAY,                       // 0x0A
                                   0x03,0x09,0x09,0x09, "White Maze 2"));
    addRoom(WHITE_MAZE_3, new ROOM(roomGfxMazeSide, ROOMFLAG_NONE, COLOR_LTGRAY,                        // 0x0B
                                   0x09,0x0C,0x1C,0x0D, "White Maze 3"));
    addRoom(SOUTH_HALL_RIGHT, new ROOM(roomGfxSideCorridor, ROOMFLAG_RIGHTTHINWALL, COLOR_LTCYAN,       // 0x0C
                                       COPPER_CASTLE, MAIN_HALL_LEFT,SOUTHEAST_ROOM, WHITE_MAZE_3, "South Hall RIGHT"));
    addRoom(SOUTH_HALL_LEFT, new ROOM(roomGfxSideCorridor, ROOMFLAG_LEFTTHINWALL, COLOR_DKGREEN,        // 0x0D
                                      0x0F,0x0B,0x0E,0x0C, "South Hall Left"));                         // 0x0E
    addRoom(SOUTHWEST_ROOM, new ROOM(roomGfxTopEntryRoom, ROOMFLAG_NONE, COLOR_CYAN,
                                     0x0D,0x10,0x0F,0x10, "Southwest Room"));
    addRoom(WHITE_CASTLE, new ROOM(roomGfxCastle, ROOMFLAG_NONE, COLOR_WHITE,                           // 0x0F
                                   0x0E,0x0F,0x0D,0x0F, "White Castle"));
    addRoom(BLACK_CASTLE, new ROOM(roomGfxCastle, ROOMFLAG_NONE, COLOR_BLACK,                           // 0x10
                                   0x01,0x1C,0x04,0x1C, "Black Castle"));
    addRoom(GOLD_CASTLE, new ROOM(roomGfxCastle, ROOMFLAG_NONE, COLOR_YELLOW,                           // 0x11
                                  0x06,0x03,0x02,0x01, "Gold Castle"));
    addRoom(GOLD_FOYER, new ROOM(roomGfxNumberRoom, ROOMFLAG_NONE, COLOR_YELLOW,                        // 0x12
                                 GOLD_FOYER,GOLD_FOYER,GOLD_FOYER,GOLD_FOYER, "Gold Foyer"));
    addRoom(BLACK_MAZE_1, new ROOM(roomGfxBlackMaze1, ROOMFLAG_NONE, COLOR_LTGRAY,                      // 0x13
                                   0x15,0x14,0x15,0x16, "Black Maze 1"));
    addRoom(BLACK_MAZE_2, new ROOM(roomGfxBlackMaze2, ROOMFLAG_MIRROR, COLOR_LTGRAY,                    // 0x14
                                   0x16,0x15,0x16,0x13, "Black Maze 2"));
    addRoom(BLACK_MAZE_3, new ROOM(roomGfxBlackMaze3, ROOMFLAG_MIRROR, COLOR_LTGRAY,                    // 0x15
                                   0x13,0x16,0x13,0x14, "Black Maze 3"));
    addRoom(BLACK_MAZE_ENTRY, new ROOM(roomGfxBlackMazeEntry, ROOMFLAG_NONE, COLOR_LTGRAY,              // 0x16
                                       0x14,0x13,0x1B,0x15, "Black Maze Entry"));
    addRoom(RED_MAZE_3, new ROOM(roomGfxRedMaze1, ROOMFLAG_NONE, COLOR_RED,                             // 0x17
                                 0x19,0x18,0x19,0x18, "Red Maze 3"));
    addRoom(RED_MAZE_2, new ROOM(roomGfxRedMazeTop, ROOMFLAG_NONE, COLOR_RED,                           // 0x18
                                 0x1A,0x17,0x1A,0x17, "Red Maze 2"));
    addRoom(RED_MAZE_4, new ROOM(roomGfxRedMazeBottom, ROOMFLAG_NONE, COLOR_RED,                        // 0x19
                                 0x17,0x1A,0x17,0x1A, "Red Maze4 "));
    addRoom(RED_MAZE_1, new ROOM(roomGfxWhiteCastleEntry, ROOMFLAG_NONE, COLOR_RED,                     // 0x1A
                                 0x18,0x19,0x18,0x19, "Red Maze 1"));
    addRoom(BLACK_FOYER, new ROOM(roomGfxTwoExitRoom, ROOMFLAG_NONE, COLOR_RED,                         // 0x1B
                        BLACK_INNERMOST_ROOM,  BLACK_INNERMOST_ROOM, BLACK_INNERMOST_ROOM, BLACK_INNERMOST_ROOM, "Black Foyer"));
    addRoom(BLACK_INNERMOST_ROOM, new ROOM(roomGfxNumberRoom, ROOMFLAG_NONE, COLOR_PURPLE,              // 0x1C
                            SOUTHEAST_ROOM, BLUE_MAZE_4, BLACK_FOYER, BLUE_MAZE_1, "Black Innermost Room"));
    addRoom(SOUTHEAST_ROOM, new ROOM(roomGfxTopEntryRoom, ROOMFLAG_NONE, COLOR_RED,                     // 0x1D
                                     MAIN_HALL_RIGHT, MAIN_HALL_LEFT, BLACK_CASTLE, MAIN_HALL_RIGHT, "Southeast Room"));
    addRoom(ROBINETT_ROOM, new ROOM(roomGfxBelowYellowCastle, ROOMFLAG_NONE, COLOR_PURPLE,              // 0x1E
                                    0x06,0x01,0x06,0x03, "Robinett Room", ROOM::HIDDEN));
    addRoom(JADE_CASTLE, new ROOM(roomGfxCastle3, ROOMFLAG_NONE, COLOR_JADE,                            // 0x1F
                                  SOUTHEAST_ROOM, BLUE_MAZE_3, BLUE_MAZE_2, BLUE_MAZE_5, "Jade Castle", ROOM::HIDDEN));
    addRoom(JADE_FOYER, new ROOM(roomGfxNumberRoom, ROOMFLAG_NONE, COLOR_JADE,                          // 0x20
                                 JADE_FOYER, JADE_FOYER, JADE_FOYER, JADE_FOYER, "Jade Foyer", ROOM::HIDDEN));
    addRoom(COPPER_CASTLE, new ROOM(roomGfxCastle2, ROOMFLAG_NONE, COLOR_COPPER,                        // 0x21
                                    BLUE_MAZE_3, MAIN_HALL_LEFT, MAIN_HALL_RIGHT, GOLD_CASTLE, "Copper Castle"));
    addRoom(COPPER_FOYER, new ROOM(roomGfxNumberRoom, ROOMFLAG_NONE, COLOR_COPPER,                      // 0x22
                                   COPPER_FOYER, COPPER_FOYER, COPPER_FOYER, COPPER_FOYER, "Copper Foyer"));
}

void Map::ConfigureMaze(int numPlayers, int gameMapLayout) {
    
    // Add the Jade Castle if 3 players
    if (numPlayers > 2) {
        roomDefs[BLUE_MAZE_2]->roomUp = JADE_CASTLE;
        roomDefs[BLUE_MAZE_2]->graphicsData = roomGfxBlueMaze1B;
        roomDefs[JADE_CASTLE]->visibility = ROOM::OPEN;
        roomDefs[JADE_FOYER]->visibility = ROOM::IN_CASTLE;
    }
    
    if (gameMapLayout == GAME_MODE_1) {
        // This is the default setup, so don't need to do anything.
    } else if (gameMapLayout == GAME_MODE_GAUNTLET) {
        // Make the right side of the main hall a dead end.
        roomDefs[MAIN_HALL_RIGHT]->roomUp = BLUE_MAZE_3;
        roomDefs[MAIN_HALL_RIGHT]->roomDown = BLACK_CASTLE;
        roomDefs[MAIN_HALL_RIGHT]->graphicsData = roomGfxStraightHall;
    } else {
        // Games 2 or 3.
        // Connect the lower half of the world.
        roomDefs[MAIN_HALL_LEFT]->roomDown = WHITE_CASTLE;
        roomDefs[MAIN_HALL_CENTER]->roomDown = GOLD_CASTLE;
        roomDefs[MAIN_HALL_RIGHT]->roomDown = WHITE_MAZE_1;
        roomDefs[SOUTHEAST_ROOM]->roomUp = SOUTH_HALL_RIGHT;
        
        // Move the Copper Castle to the White Maze
        roomDefs[MAIN_HALL_RIGHT]->graphicsData = roomGfxLeftOfName;
        roomDefs[MAIN_HALL_RIGHT]->roomUp = BLUE_MAZE_3;
        roomDefs[COPPER_CASTLE]->roomDown = SOUTH_HALL_RIGHT;
        // TODO: Change up, left, and right of COPPER_CASTLE
        
        // Put the Black Maze in the Black Castle
        roomDefs[BLACK_FOYER]->roomUp = BLACK_MAZE_ENTRY;
        roomDefs[BLACK_FOYER]->roomRight = BLACK_MAZE_ENTRY;
        roomDefs[BLACK_FOYER]->roomDown = BLACK_MAZE_ENTRY;
        roomDefs[BLACK_FOYER]->roomLeft = BLACK_MAZE_ENTRY;
        roomDefs[BLACK_INNERMOST_ROOM]->visibility = ROOM::HIDDEN;
        
    }
}

void Map::ComputeDistances(int numPorts, Portcullis** ports) {
    distances = new int*[numRooms];
    for(int ctr1=0; ctr1<numRooms; ++ctr1) {
        distances[ctr1] = new int[numRooms];
        for(int ctr2=0; ctr2<numRooms; ++ctr2) {
            if (ctr1 == ctr2) {
                distances[ctr1][ctr2] = 0;
            } else if (isNextTo(ctr1, ctr2)) {
                distances[ctr1][ctr2] = 1;
            } else  {
                distances[ctr1][ctr2] = LONG_WAY;
            }
        }
    }
    
    // Adjust for castles
    if (numPorts > 0) {
        for(int ctr=0; ctr<numPorts; ++ctr) {
            Portcullis* nextPort = ports[ctr];
            distances[nextPort->room][nextPort->insideRoom] = 1;
            distances[nextPort->insideRoom][nextPort->room] = 1;
        }
    }
    
    // Adjust for Robinett room
    distances[ROBINETT_ROOM][MAIN_HALL_LEFT] = 1;
    distances[MAIN_HALL_LEFT][ROBINETT_ROOM] = 1;
    distances[ROBINETT_ROOM][MAIN_HALL_RIGHT] = 1;
    distances[MAIN_HALL_RIGHT][ROBINETT_ROOM] = 1;
    
    // Remove paths that aren't really paths because full length walls block them.
    distances[MAIN_HALL_RIGHT][BLUE_MAZE_3] = LONG_WAY;
    distances[BLUE_MAZE_3][MAIN_HALL_RIGHT] = LONG_WAY;
    distances[MAIN_HALL_LEFT][MAIN_HALL_RIGHT] = LONG_WAY;
    distances[MAIN_HALL_RIGHT][MAIN_HALL_LEFT] = LONG_WAY;
    distances[MAIN_HALL_LEFT][BLACK_CASTLE] = LONG_WAY;
    distances[BLACK_CASTLE][MAIN_HALL_LEFT] = LONG_WAY;
    distances[WHITE_CASTLE][SOUTHWEST_ROOM] = LONG_WAY;
    distances[SOUTHWEST_ROOM][WHITE_CASTLE] = LONG_WAY;
    distances[SOUTH_HALL_LEFT][SOUTH_HALL_RIGHT] = LONG_WAY;
    distances[SOUTH_HALL_RIGHT][SOUTH_HALL_LEFT] = LONG_WAY;

    int tracker = LONG_WAY;
    // Now compute the distances using isNextTo()
    for(int step = 2; step < LONG_WAY; ++step) {
        for(int ctr1=0; ctr1<numRooms; ++ctr1) {
            for(int ctr2=0; ctr2<numRooms; ++ctr2) {
                if (distances[ctr1][ctr2] == LONG_WAY) {
                    for(int ctr3=0; ctr3<numRooms; ++ctr3) {
                        if ((distances[ctr3][ctr2] < step) && (distances[ctr1][ctr3] == 1)) {
                            distances[ctr1][ctr2] = distances[ctr3][ctr2] + 1;
                            break;
                        }
                    }
                }
                if (distances[MAIN_HALL_RIGHT][MAIN_HALL_CENTER] != tracker) {
                    tracker = distances[MAIN_HALL_RIGHT][MAIN_HALL_CENTER];
                }
            }
        }
    }
    
}

void Map::addRoom(int key, ROOM* newRoom) {
    roomDefs[key] = newRoom;
    newRoom->setIndex(key);
}

ROOM* Map::getRoom(int key) {
    if ((key < 0) || (key > numRooms)) {
        return NULL;
    } else {
        return roomDefs[key];
    }
}


int Map::distance(int fromRoom, int toRoom) {
    return distances[fromRoom][toRoom];
}

bool Map::isNextTo(int room1, int room2) {
    ROOM* robj1 = roomDefs[room1];
    ROOM* robj2 = roomDefs[room2];
    return (((robj1->roomUp == room2) && (robj2->roomDown == room1)) ||
            ((robj1->roomRight == room2) && (robj2->roomLeft == room1)) ||
            ((robj1->roomDown == room2) && (robj2->roomUp == room1)) ||
            ((robj1->roomLeft == room2) && (robj2->roomRight == room1)));
    
}

void Map::addCastles(int numPorts, Portcullis** ports) {
    ComputeDistances(numPorts, ports);
}



