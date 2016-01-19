

#ifndef Map_hpp
#define Map_hpp

#include <stdio.h>
#include "adventure_sys.h"
#include "color.h"


typedef struct ROOM
{
    const byte* graphicsData;   // pointer to room graphics data
    byte flags;                 // room flags - see below
    int color;                  // foreground color
    int roomUp;                 // index of room UP
    int roomRight;              // index of room RIGHT
    int roomDown;               // index of room DOWN
    int roomLeft;               // index of room LEFT
}ROOM;
#define ROOMFLAG_NONE           0x00
#define ROOMFLAG_MIRROR         0x01 // bit 0 - 1 if graphics are mirrored, 0 for reversed
#define ROOMFLAG_LEFTTHINWALL   0x02 // bit 1 - 1 for left thin wall
#define ROOMFLAG_RIGHTTHINWALL  0x04 // bit 2 - 1 for right thin wall



enum
{
    NUMBER_ROOM=0x00,
    MAIN_HALL_LEFT=0x01,
    MAIN_HALL_CENTER=0x02,
    MAIN_HALL_RIGHT=0x03,
    BLUE_MAZE_BLACK_END=0x04,
    BLUE_MAZE_JADE_END=0x05,
    BLUE_MAZE_LARGE_ROOM=0x06,
    BLUE_MAZE_VERT_PATHS=0x07,
    BLUE_MAZE_HALL_END=0x08,
    
    WHITE_MAZE_HALL_END=0x0a,
    
    SOUTH_HALL_RIGHT=0x0c,
    SOUTH_HALL_LEFT=0x0d,
    
    WHITE_CASTLE=0x0f,
    BLACK_CASTLE=0x10,
    GOLD_CASTLE=0x11,
    
    BLACK_MAZE_1=0x13,
    BLACK_MAZE_2=0x14,
    BLACK_MAZE_3=0x15,
    BLACK_MAZE_ENTRY=0x16,
    
    BLACK_FOYER=0x1b,
    BLACK_INNERMOST_ROOM=0x1c,
    SOUTH_EAST_ROOM=0x1d, // Southeast corner of the world.  Level 1 = south of main hall, Level 2 = south of south hall
    
    JADE_CASTLE=0x1f,
    JADE_FOYER=0x20,
    COPPER_CASTLE=0x21,
    COPPER_FOYER=0x22
};


class Map {
public:
    static ROOM roomDefs[]; // TODO: Migrate to being private with accessors.
    
    Map(int numPlayers, int gameMapLayout);
    
    ~Map();
    
private:
    
    /**
     * Map is initially setup for game 1 with 2 players.  This adjusts the map for the actual game
     * about to be played.
     */
    void ConfigureMaze(int numPlayers, int gameMapLayout);

    
};
#endif /* Map_hpp */
