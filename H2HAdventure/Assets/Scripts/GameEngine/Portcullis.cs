using System;
using System.Collections.Generic;
namespace GameEngine
{
    public class Portcullis : OBJECT
    {

        /** The x-coord you come out at when you leave a castle. */
        public const int EXIT_X = 0xA0;

        /** The y-coord you come out at when you leave a castle. */
        public const int EXIT_Y = 0x58;

        /** The x-coordinate of where the portcullis is placed. */
        public const int PORT_X = 0x4d;

        /** The y-coordinate of where the portcullis is placed. */
        public const int PORT_Y = 0x31;

        public const int OPEN_STATE = 0;
        public const int CLOSED_STATE = 12;

        /** True if touching the gate will take you inside the castle.  False if gate is locked. */
        public bool allowsEntry;

        /** Room that the gate takes you to */
        public int insideRoom;

        /** The key that unlocks this gate */
        public OBJECT key;

        /** Array of rooms inside this castle.  If NULL then it means only the insideRoom is inside this castle.  If not NULL,
         * the inside room will be included in the list.
         */
        private List<int> allInsideRooms = new List<int>();

        /**
         * label - unique name only used for logging and debugging
         * outsideRoom - index of the room the portal will be placed in
         * insideRoom - index of the room the portal leads to
         * key - the key that opens this gate.
         */
        public Portcullis(String inLabel, int inOutsideRoom, ROOM inInsideRoom, OBJECT inKey) :
            base(inLabel, objectGfxPort, portStates, 0x0C, COLOR.BLACK, OBJECT.RandomizedLocations.FIXED_LOCATION)
        {
            allowsEntry = false;
            insideRoom = inInsideRoom.index;
            key = inKey;

            // Portcullis's unlike other objects, we know the location of before the game level is selected.
            room = inOutsideRoom;
            x = PORT_X;
            y = PORT_Y;

            allInsideRooms.Add(inInsideRoom.index);
            if (inInsideRoom.visibility == ROOM.RandomVisibility.OPEN)
            {
                inInsideRoom.visibility = ROOM.RandomVisibility.IN_CASTLE;
            }

        }

        public virtual void setState(int newState, bool newAllowsEntry)
        {
            state = newState;
            allowsEntry = newAllowsEntry;
        }

        /**
         * Update its internal state for this turn.  This involves lifting the gate if it is currently opening, etc...
         */
        public virtual void moveOneTurn()
        {
            if (state == OPEN_STATE)
            {
                allowsEntry = true;
            }
            else if (state == CLOSED_STATE)
            {
                allowsEntry = false;
            }
            else
            {
                // Raise/lower the gate
                ++state;
                if (state > 22)
                {
                    // Port is unlocked
                    state = OPEN_STATE;
                }
            }
        }

        /**
         * Check if an unheld object touches an open gate (a dragon or the bat could walk into it, a magnet could pull an object
         * into it, or a closing gate may touch a still object).  If it does, move the object into the castle.
        */
        public ObjectMoveAction checkObjectEnters(OBJECT objct)
        {
            // Gate must be open and someone else must be in the room to witness (objects don't go through gates if no one
            // is in the room).  Object must be in room, touching gate, and not held by anyone.

            // For efficiency, we've computed whether someone is in the room to witness before we ever make this call, so we don't
            // need to check it again.

            ObjectMoveAction newAction = null;
            if ((objct.room == this.room) && this.allowsEntry)
            {
                bool held = false;
                int numPlayers = this.board.getNumPlayers();
                for (int ctr = 0; !held && (ctr < numPlayers); ++ctr)
                {
                    held = (this.board.getPlayer(ctr).linkedObject == objct.getPKey());
                }

                if (!held && board.CollisionCheckObjectObject(this, objct))
                {
                    objct.room = this.insideRoom;
                    objct.y = Board.ENTER_AT_BOTTOM;
                    // We only generate an event if we are in the room.
                    newAction = new ObjectMoveAction(objct.getPKey(), objct.room, objct.x, objct.y);
                }
            }

            return newAction;
        }

        /**
         * Check and handle locking or unlocked of the portcullis this turn.
         * Returns a remote action describing any state change in the portcullis or null
         * if no action is taken.
         * Caller is responsible for releasing memory of returned remote action.
         */
        public virtual PortcullisStateAction checkKeyInteraction()
        {
            PortcullisStateAction gateAction = null;

            // We only change the state of the castle gate if we are in the room.  Otherwise we wait for
            // another player to notify us of the state change.
            BALL thisPlayer = board.getCurrentPlayer();
            if ((thisPlayer.room == this.room) && checkKeyTouch(key))
            {

                int heldBy = board.getPlayerHoldingObject(key);
                bool stateChange = false;

                // If the gate is closed, we open the gate
                if (state == CLOSED_STATE)
                {
                    state++;
                    allowsEntry = true;
                    stateChange = true;
                }
                // If the gate is in the process of closing, we do nothing unless the key
                // isn't held by anyone then we open the gate to prevent the key from being locked inside
                else if ((state > OPEN_STATE) && (state < CLOSED_STATE) && (heldBy < 0))
                {
                    state += CLOSED_STATE;
                    allowsEntry = true;
                    stateChange = true;
                }
                // If the gate is open, we only close it if the key is held by someone
                else if ((state == OPEN_STATE) && (heldBy >= 0))
                {
                    // Toggle the port state
                    state++;
                    allowsEntry = true; // The gate is now closing but still active
                    stateChange = true;
                }

                if (stateChange)
                {
                    // Broadcast a state change if we are holding the key or if no one is holding the key and we
                    // are a witness
                    if ((heldBy == thisPlayer.playerNum) || ((heldBy < 0) && (thisPlayer.room == room)))
                    {
                        gateAction = new PortcullisStateAction(getPKey(), state, allowsEntry);
                    }
                }
            }

            return gateAction;
        }

        /**
         * Returns whether the key is touching the portcullis.  Does not care about other things like
         * whether the gate is in a state where it cares or whether anyone is in the room
         */
        protected bool checkKeyTouch(OBJECT keyToCheck)
        {
            bool touched = ((keyToCheck.room == this.room) && (board.CollisionCheckObjectObject(this, keyToCheck)));
            return touched;
        }

        public void openFromInside()
        {
            ++state;
        }

        /**
         * Called when a player enters a gate that is not completely open.
         */
        public void forceOpen()
        {
            state = OPEN_STATE;
            allowsEntry = true;
        }

        /**
         * If there are multiple rooms inside this castle, this will add this room to the portcullis's list of inside rooms.
         * This list is used for things like randomizing objects for game 3 and making sure all objects can still be reached.
         * The 'insideRoom' passed into the constructor is already added to this list.
         */
        public void addRoom(ROOM room)
        {
            allInsideRooms.Add(room.index);
            if (room.visibility == ROOM.RandomVisibility.OPEN)
            {
                room.visibility = ROOM.RandomVisibility.IN_CASTLE;
            }
        }



        /**
         * Returns whether the room passed in is somewhere behind this gate.
         */
        public bool containsRoom(int room)
        {
            return allInsideRooms.Contains(room);
        }

        // Object #1 States 940FF (Graphic)
        private static byte[][] objectGfxPort = new byte[][]
        { new byte[] {
            // state 1
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA},                 // X X X X
            new byte[] {
                // state 2
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA},                 // X X X X
            new byte[] {
                // state 3
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA},                 // X X X X
            new byte[] {
                // state 4
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA},                 // X X X X
            new byte[] {
                // state 5
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA},                 // X X X X
            new byte[] {
                // state 6
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA},                 // X X X X
            new byte[] {
                // state 7
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA,                  // X X X X
                0xFE,                  // XXXXXXX
                0xAA}                  // X X X X
        };

        private static byte[] portStates = new byte[]
{
            0,0,1,1,2,2,3,3,4,4,5,5,6,6,5,5,4,4,3,3,2,2,1,1
};


    }
}