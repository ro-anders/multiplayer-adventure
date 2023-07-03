using System;
namespace GameEngine
{

    /**
     * The bridge object.  This may at some point carry state about what plots
     * it connects, but at the moment simply stores a bunch of bridge specific
     * constants.
     */
    class Bridge : OBJECT
    {
        // Bridge is 4 times wider than everything else.
        // The way Robinett did this was to pass in a 7 to the width ( 7/2 + 1 = 4 )
        public const int BRIDGE_SIZE = 0x07;

        /** The width of the foot of the bridge */
        public const int FOOT_BWIDTH = 16;
        /** The height of the foot of the bridge */
        public const int FOOT_BHEIGHT = 8;
        /** The width of the part of the foot that sticks out past the main part */
        public const int FOOT_EXTENSION_BWIDTH = 8;


        public Bridge() :
        base("bridge", objectGfxBridge, new byte[0], 0, COLOR.PURPLE,
                OBJECT.RandomizedLocations.OPEN_OR_IN_CASTLE, BRIDGE_SIZE)
        {}

        /** The left most x-coordinate of the inside area of the bridge in ball scale*/
        public int InsideBLeft
        {
            get { return base.x * Adv.BALL_SCALE + FOOT_BWIDTH; }
        }

        /** The right most x-coordinate of the inside area of the bridge in ball scale*/
        public int InsideBRight
        {
            get { return (base.x + width) * Adv.BALL_SCALE - FOOT_BWIDTH - 1; }
        }

        /**
         * The rectangle representing the inside part of the bridge (the part that you cross)
         */
        public RRect InsideBRect
        {
            get {
                RRect whole_brect = base.BRect;
                return new RRect(
                        whole_brect.room,
                        whole_brect.x + FOOT_BWIDTH,
                        whole_brect.y,
                        whole_brect.width - 2 * FOOT_BWIDTH,
                        whole_brect.height);
            }
        }

        /**
         * The area just above the bridge
         * */
        public RRect TopExitBRect
        {
            get
            {
                return RRect.fromTRBL(room, by + 1, InsideBRight, by + 1, InsideBLeft);
            }
        }

        /**
         * The area just below the bridge
         * */
        public RRect BottomExitBRect
        {
            get
            {
                return RRect.fromTRBL(room, by - BHeight, InsideBRight, by-BHeight, InsideBLeft);
            }
        }

        // Object #0A : State FF : Graphic                                                                                   
        private static byte[][] objectGfxBridge =
        { new byte[] {
            0xC3,                  // XX    XX                                                                  
            0xC3,                  // XX    XX                                                                  
            0xC3,                  // XX    XX                                                                  
            0xC3,                  // XX    XX                                                                  
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0x42,                  //  X    X                                                                   
            0xC3,                  // XX    XX                                                                  
            0xC3,                  // XX    XX                                                                  
            0xC3,                  // XX    XX                                                                  
            0xC3                   // XX    XX                                                                  
        } };


    }
}