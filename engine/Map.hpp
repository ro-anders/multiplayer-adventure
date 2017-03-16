

#ifndef Map_hpp
#define Map_hpp

#include <stdio.h>
#include "adventure_sys.h"
#include "color.h"


class Portcullis;
class ROOM;

enum
{
    NUMBER_ROOM=0x00,
    MAIN_HALL_LEFT=0x01,
    MAIN_HALL_CENTER=0x02,
    MAIN_HALL_RIGHT=0x03,
    BLUE_MAZE_5=0x04, // Down from the black castle
    BLUE_MAZE_2=0x05, // Down from the jade castle
    BLUE_MAZE_3=0x06, // The big center room (where Art3mis waited)
    BLUE_MAZE_4=0x07, // 2 down from the black castle
    BLUE_MAZE_1=0x08, // Up from the main hall
    WHITE_MAZE_2=0x09, // Catacombs to white castle, second one on way to castle
    WHITE_MAZE_1=0x0A, // Catacombs to white castle, down from main hall
    WHITE_MAZE_3=0x0B, // Catacombs to white castle, right of south hall left
    SOUTH_HALL_RIGHT=0x0C,
    SOUTH_HALL_LEFT=0x0D,
    SOUTHWEST_ROOM=0x0E, // Two down from white castle
    WHITE_CASTLE=0x0F,
    BLACK_CASTLE=0x10,
    GOLD_CASTLE=0x11,
    GOLD_FOYER=0x12,
    // The following Black Maze values are a group and shouldn't be broken up
    BLACK_MAZE_1=0x13,
    BLACK_MAZE_2=0x14,
    BLACK_MAZE_3=0x15,
    BLACK_MAZE_ENTRY=0x16,
    // The following Red Maze values are a group and shouldn't be broken up
    RED_MAZE_3=0x17,
    RED_MAZE_2=0x18,
    RED_MAZE_4=0x19,
    RED_MAZE_1=0x1A,
    BLACK_FOYER=0x1B,
    BLACK_INNERMOST_ROOM=0x1C,
    SOUTHEAST_ROOM=0x1D, // Two down from copper castle (moves with castle in different levels)
    ROBINETT_ROOM=0x1E,
    JADE_CASTLE=0x1F,
    JADE_FOYER=0x20,
    COPPER_CASTLE=0x21,
    COPPER_FOYER=0x22
    
};

class Map {
public:
    ROOM** roomDefs; // TODO: Migrate to being private with accessors.
    
    static int LONG_WAY;
    
    Map(int numPlayers, int gameMapLayout);
    
    ~Map();
    
    /**
     * Adds a room to the map.
     */
    void addRoom(int key, ROOM* room);
    
    /**
     * Return the number of rooms in the map (includes rooms that may not be reachable on current game).
     */
    static int getNumRooms();
    
    /**
     * Lookup a room in the map
     */
    ROOM* getRoom(int key);
    
    /**
     * Gives the distance from one room to the other, meaning how many rooms
     * you have to pass through to get from one room to the other.
     * Rooms right next to each other will have a distance of 1.  Rooms separated by a
     * single, third room will have a distance of 2.  Rooms a long distance away will
     * simply be reported as having LONG_WAY as a distance.
     */
    int distance(int fromRoom, int toRoom); // TODO: This should be a method on ROOM
    
    /**
     * Map needs to know about portcullises in order to compute distances between inside a castle and outside.
     */
    void addCastles(int numPorts, Portcullis** ports);
    
private:
    
    static int numRooms;
    
    int** distances;
    
    void defaultRooms();
    
    /**
     * Map is initially setup for game 1 with 2 players.  This adjusts the map for the actual game
     * about to be played.
     */
    void ConfigureMaze(int numPlayers, int gameMapLayout);
    
    void ComputeDistances(int numPorts, Portcullis** ports);
    
    bool isNextTo(int room1, int room2);

    
};
#endif /* Map_hpp */
