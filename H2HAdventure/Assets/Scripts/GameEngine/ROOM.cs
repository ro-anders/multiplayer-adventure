using System;
namespace GameEngine
{
    public class ROOM
    {
        public const int FLAG_NONE = 0x00;
        public const int FLAG_MIRROR = 0x01; // bit 0 - 1 if graphics are mirrored, 0 for reversed
        public const int FLAG_LEFTTHINWALL = 0x02; // bit 1 - 1 for left thin wall
        public const int FLAG_RIGHTTHINWALL = 0x04; // bit 2 - 1 for right thin wall

        // An attribute indicating whether objects should be placed in a room when randomly placing objects.
        public enum RandomVisibility
        {
            OPEN, // A room freely accessible without use of a key (e.g. blue labyrinth)
            IN_CASTLE, // a room behind a castle portcullis (e.g. red labyrinth)
            HIDDEN // a room that shouldn't have objects randomly put in it (e.g. the easter egg room)
        };

        public int index;                  // index into the map
        public byte[] graphicsData;   // pointer to room graphics data
        public byte flags;                 // room flags - see below
        public int color;                  // foreground color
        public int roomUp;                 // index of room UP
        public int roomRight;              // index of room RIGHT
        public int roomDown;               // index of room DOWN
        public int roomLeft;               // index of room LEFT
        public String label;                // a short, unique name for the object
        public RandomVisibility visibility; // attribute indicating whether objects can be randomly placed in this room.


        public ROOM(byte[] inGraphicsData, byte inFlags, int inColor,
                    int inRoomUp, int inRoomRight, int inRoomDown, int inRoomLeft, String inLabel, RandomVisibility inVis = RandomVisibility.OPEN)
        {
            graphicsData = inGraphicsData;
            flags = inFlags;
            color = (int)inColor;
            roomUp = inRoomUp;
            roomRight = inRoomRight;
            roomDown = inRoomDown;
            roomLeft = inRoomLeft;
            label = inLabel;
            visibility = inVis;
        }

        public void setIndex(int inIndex)
        {
            index = inIndex;
        }

        bool isNextTo(ROOM otherRoom)
        {
            int index2 = otherRoom.index;
            return (((this.roomUp == index2) && (otherRoom.roomDown == index)) ||
            ((this.roomRight == index2) && (otherRoom.roomLeft == index)) ||
            ((this.roomDown == index2) && (otherRoom.roomUp == index)) ||
            ((this.roomLeft == index2) && (otherRoom.roomRight == index)));
        }
    }
}
