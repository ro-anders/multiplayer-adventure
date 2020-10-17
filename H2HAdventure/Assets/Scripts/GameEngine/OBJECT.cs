using System;
namespace GameEngine
{
    public class OBJECT
    {

        public enum RandomizedLocations
        {
            OUT_IN_OPEN,
            OPEN_OR_IN_CASTLE,
            FIXED_LOCATION
        };

        // Unless the "size" modifier is used, objects are 8 blocks (16 pixels) wide
        public const int OBJECT_WIDTH = 8; 


        public readonly byte[][] gfxData;        // graphics data for each state
        public readonly byte[] states;         // array of indicies for each state
        public int state;                  // current state
        public int color;                  // color
        public int room;                   // room
        public int x;                      // x position
        public int y;                      // y position
        public int size;                   // size (used for bridge and surround)
        public int displayed;             // flag indicating object was displayed (when more than maxDisplayableObjects for instance)
                                          // will be -1 if not displayed or the room number if displayed  
        public String label;                // a short, unique name for the object
        public RandomizedLocations randomPlacement; // How to randomly place this object in game 3

        protected bool objExists;             // Whether the object is active in this game.  Starts out false until init() called.
        protected Board board;               // The board on which this object has been placed.

        protected int movementX;              // horizontal movement
        protected int movementY;              // vertical movement

        /** For private magnets. */
        protected int privateToPlayer;

        private int pkey;                   // The "primary key" or index to this object on the board

        public OBJECT(String inLabel, byte[][] inGfxData, byte[] inStates, int inState, int inColor,
                      RandomizedLocations inRandomPlacement=RandomizedLocations.OPEN_OR_IN_CASTLE, int inSize = 0) {
            gfxData = inGfxData;
            states = inStates;
            state = inState;
            color = inColor;
            room = -1;
            movementX = 0;
            movementY = 0;
            x = 0;
            y = 0;
            randomPlacement = inRandomPlacement;
            size = inSize;
            objExists = false;
            privateToPlayer = -1;
            label = inLabel;
        }

        public int getMovementX() { return movementX; }
        public void setMovementX(int moveX) { movementX = moveX; }
        public int getMovementY() { return movementY; }
        public void setMovementY(int moveY) { movementY = moveY; }

        public bool exists() {return objExists;}
        public void setExists(bool inExists) { objExists = inExists; }

        public int Width
        {
            get { return OBJECT_WIDTH * (size / 2 + 1); }
        }

        public int Height
        {
            get
            {
                int graphic = (states.Length > 0 ? states[state] : 0);
                return gfxData[graphic].Length;
            }
        }

        public void setBoard(Board newBoard, int newPKey)
        {
            board = newBoard;
            pkey = newPKey;
        }

        public int getPKey() {
            return pkey;
        }

        public override string ToString()
        {
            return label;
        }

        /**
         * Sets up the object in the room it will start off in.
         */
        public void init(int inRoom, int inX, int inY, int inState=0, int inMoveX=0, int inMoveY=0)
        {
            room = inRoom;
            x = inX;
            y = inY;
            objExists = true;
            state = inState;
            movementX = inMoveX;
            movementY = inMoveY;
        }

        /** 
         * Only one player can pickup this object.  All other players pass through it.  Used for private magnets.
         * player - player it is private to.  A negative number means obect is not private.
         */
        public void setPrivateToPlayer(int player)
        {
            privateToPlayer = player;
        }

        /**
         * Returns true if this object is solid or grabbable by the player.  If object is private (e.g. private magnet)
         * will return false and player will pass right through it without picking it up.
         */
        public bool isTangibleTo(int player) {
            return (privateToPlayer< 0) || (privateToPlayer == player);
        }

        protected OBJECT lookupObject(int objKey)
        {
            return board.getObject(objKey);
        }

    }
}
