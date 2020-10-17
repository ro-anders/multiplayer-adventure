using System;
namespace GameEngine
{
    public class BALL
    {
        public const int RADIUS = 4;
        public const int DIAMETER = 2 * RADIUS;
        public const int MOVEMENT = 6;

        public int playerNum;              // 0-2.  Which player this is.
        public int room;                   // room
        public int previousRoom;
        public int displayedRoom;          // occassionally ball will appear in room other than it is in (usually using bridge across rooms)
        public int x;                      // x position
        public int y;                      // y position
        public int previousX;              // previous x position
        public int previousY;              // previous y position
        public int velx;                   // Current horizontal speed (walls notwithstanding).  Positive = right.  Negative = left.
        public int vely;                   // Current vertical speed (walls notwithstanding).  Positive = right.  Negative = down.
        public int linkedObject;           // index of linked (carried) object
        public int linkedObjectX;          // X value representing the offset from the ball to the object being carried
        public int linkedObjectY;          // Y value representing the offset from the ball to the object being carried
        public bool hit;                  // the ball hit something
        public int hitObject;              // the object that the ball hit
        public readonly byte[] gfxData;        // graphics data for ball
        public Portcullis homeGate;       // The gate of the castle you start at
        public bool isAi;                   // Whether this ball is for an AI player

        /** During the gauntlet, once you reach the black castle you flash like the chalise until you reset or you reach the
            * gold castle where you win. */
        private bool glowing;

        public BALL(int inPlayerNum, Portcullis inHomeGate, bool inAltIcons, bool inIsAi)
        {
            playerNum = inPlayerNum;
            room = 0;
            previousRoom = 0;
            displayedRoom = 0;
            x = 0;
            y = 0;
            previousX = 0;
            previousY = 0;
            velx = 0;
            vely = 0;
            linkedObject = Board.OBJECT_NONE;
            linkedObjectX = 0;
            linkedObjectY = 0;
            hit = false;
            hitObject = Board.OBJECT_NONE;
            gfxData = (inAltIcons ? 
                (playerNum == 0 ? ballGfxShield : (playerNum == 1 ? ballGfxSmall : ballGfxCross)) :
                (playerNum == 0 ? ballGfxSolid : (playerNum == 1 ? ballGfxOne : ballGfxTwo)));
            homeGate = inHomeGate;
            glowing = false;
            isAi = inIsAi;
        }

        public string toString()
        {
            return "player " + playerNum + " at (" + x + "," + y + ")@" + room +
                (linkedObject >= 0 ? " with " + linkedObject : "");
        }

        /** x coordinate of the middle of the ball **/
        public int midX
        {
            get { return x + RADIUS; }
            set { x = value - RADIUS; }
        }
        /** y coordinate of the middle of the ball **/
        public int midY
        {
            get { return y - RADIUS; }
            set { y = value + RADIUS; }
        }

        public bool isGlowing()
        {
            return glowing;
        }

        public void setGlowing(bool nowIsGlowing)
        {
            glowing = nowIsGlowing;
        }

        /**
         * The distance to an object
         * @param objectX the x of the object (IN OBJECT COORDINATE SYSTEM)
         * @param otherY the y of the object (IN OBJECT COORDINATE SYSTEM)
         * @return the distance IN BALL COORDINATE SYSTEM to object
         */
        public int distanceToObject(int objectMidX, int objectMidY)
        {
            // Figure out the distance (which is really the max difference along one axis)
            int xdist = this.midX - 2 * objectMidX;
            if (xdist < 0)
            {
                xdist = -xdist;
            }
            int ydist = this.midY - 2 * objectMidY;
            if (ydist < 0)
            {
                ydist = -ydist;
            }
            int dist = (xdist > ydist ? xdist : ydist);
            return dist;
        }






        private readonly byte[] ballGfxSolid = new byte[]
        {
            0xFF,                  // XXXXXXXX
            0xFF,                  // XXXXXXXX
            0xFF,                  // XXXXXXXX
            0xFF,                  // XXXXXXXX
            0xFF,                  // XXXXXXXX
            0xFF,                  // XXXXXXXX
            0xFF,                  // XXXXXXXX
            0xFF                   // XXXXXXXX
        };

        private readonly byte[] ballGfxOne = new byte[]
        {
            0xFF,                  // XXXXXXXX
            0xC3,                  // XX    XX
            0xC3,                  // XX    XX
            0xC3,                  // XX    XX
            0xC3,                  // XX    XX
            0xC3,                  // XX    XX
            0xC3,                  // XX    XX
            0xFF                   // XXXXXXXX
        };

        private readonly byte[] ballGfxTwo = new byte[]
        {
            0xFF,                  // XXXXXXXX
            0xFF,                  // XXXXXXXX
            0x18,                  //    XX
            0x18,                  //    XX
            0x18,                  //    XX
            0x18,                  //    XX
            0xFF,                  // XXXXXXXX
            0xFF                   // XXXXXXXX
        };

        private readonly byte[] ballGfxShield = new byte[]
        {
            0xFF,                  // XXXXXXXX
            0xFF,                  // XXXXXXXX
            0xFF,                  // XXXXXXXX
            0xFF,                  // XXXXXXXX
            0xFF,                  //  XXXXXX
            0x7E,                  //  XXXXXX
            0x3C,                  //   XXXX
            0x18                   //    XX
        };

        private readonly byte[] ballGfxSmall = new byte[]
        {
            0x00,                  //         
            0x00,                  //         
            0x7E,                  //  XXXXXX
            0x7E,                  //  XXXXXX
            0x7E,                  //  XXXXXX
            0x7E,                  //  XXXXXX
            0x00,                  //         
            0x00                   //         
        };


        private readonly byte[] ballGfxCross = new byte[]
        {
            0x18,                  //    XX
            0x18,                  //    XX
            0x18,                  //    XX
            0xFF,                  // XXXXXXXX
            0xFF,                  // XXXXXXXX
            0x18,                  //    XX
            0x18,                  //    XX
            0x18                   //    XX
        };


    }
}
