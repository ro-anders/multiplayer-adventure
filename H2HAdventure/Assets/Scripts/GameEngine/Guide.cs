using System.Collections;
using System.Collections.Generic;

namespace GameEngine {

    /**
     * Represents a helpful line through a maze to guide newbies
     */
    public class Guide
    {

        /**********************************
         * An (x,y) point
         */
        public class Point
        {
            public readonly int x;
            public readonly int y;
            public Point(int inX, int inY)
            {
                x = inX;
                y = inY;
            }
        }

        /**********************************
         * An segmented line as a list of vertices.
         * A line of n segments will have n+1 points.        
         */
        public class Line: List<Point> {
            private COLOR color;
            public COLOR Color
            {
                get { return color; }
            }
            public Line(COLOR inColor)
            {
                color = inColor;
            }
        }

        /**********************************
         * A small image to indicate the purpose of a guide
         */
        public class Marker
        {
            private int x;
            private int y;
            private byte[] gfx;
            private int color;
            public int X
            {
                get { return x;}
            }
            public int Y
            {
                get { return y; }
            }
            public byte[] Gfx
            {
                get { return gfx; }
            }
            public int Color
            {
                get { return color; }
            }

            public Marker(int inX, int inY, byte[] inGfx, int inColor)
            {
                x = inX;
                y = inY;
                gfx = inGfx;
                color = inColor;
            }
        }

        /**********************************
         * The guides that appear in this room.
         */
        public class RoomGuide
        {
            int room;
            List<Line> lines = new List<Line>();
            List<Marker> markers = new List<Marker>();

            public RoomGuide(int inRoom)
            {
                room = inRoom;
            }

            public void AddLine(Line inLine)
            {
                lines.Add(inLine);
            }
            public void AddMarker(Marker inMarker)
            {
                markers.Add(inMarker);
            }

            public IEnumerator<Line> GetLines()
            {
                return lines.GetEnumerator();
            }
            public IEnumerator<Marker> GetMarkers()
            {
                return markers.GetEnumerator();
            }
        }

        private RoomGuide[] roomGuides;
        private static readonly List<Line> EMPTY_ROOM_LINES = new List<Line>();
        private static readonly List<Marker> EMPTY_ROOM_MARKERS = new List<Marker>();

        public Guide()
        {
            roomGuides = new RoomGuide[Map.getNumRooms()];
        }

        public void AddLine(int room, Line line)
        {
            if (roomGuides[room] == null)
            {
                roomGuides[room] = new RoomGuide(room);
            }
            roomGuides[room].AddLine(line);
        }
        public void AddMarker(int room, Marker marker)
        {
            if (roomGuides[room] == null)
            {
                roomGuides[room] = new RoomGuide(room);
            }
            roomGuides[room].AddMarker(marker);
        }

        public IEnumerator<Line> GetLines(int room)
        {
            if (roomGuides[room] == null)
            {
                return EMPTY_ROOM_LINES.GetEnumerator();
            }
            else
            {
                return roomGuides[room].GetLines();
            }
        }

        public IEnumerator<Marker> GetMarkers(int room)
        {
            if (roomGuides[room] == null)
            {
                return EMPTY_ROOM_MARKERS.GetEnumerator();
            }
            else
            {
                return roomGuides[room].GetMarkers();
            }
        }

        public void ConfigureGuide(bool withJadeCastle)
        {
            int[][][] pointsLists = new int[][][]
            {
                new int[][]{ },
                new int[][]{ },
                new int[][]{ },
                new int[][]{ },
                new int[][]{    // Blue maze top
                    new int[]{34, 0, 34, 1, 39, 1},
                    new int[]{19, 0, 19, 6}
                },
                new int[][]{    // Blue maze 1
                    new int[]{-1, 1, 4, 1, 4, 3, 10, 3, 10, 5, 39, 5},
                    new int[]{ 19, 6, 19, 5},
                },
                new int[][]{    // Blue maze bottom
                    new int[]{-1, 5, 4, 5, 4, 3, 19, 3, 19, 6}
                },
                new int[][]{    // Blue maze center
                    new int[]{39, 1, 34, 1, 34, 3, 39, 3},
                    new int[]{39, 5, 34, 5, 34, 6},
                    new int[]{19, 0, 19, 6}
                },
                new int[][]{    // Blue maze entry
                    new int[]{ 19, 0, 19, 1, -1, 1 },
                    new int[]{ -1, 3, 4, 3, 4, 5, -1, 5}
                },
                new int[][]{    // White maze 2
                    new int[]{ 39, 1, 34, 1, 34, 0 },
                    new int[]{ -1, 1, 4, 1, 4, 0},
                    new int[]{ 16, 0, 16, 1, 22, 1, 22, 0},
                    new int[]{COLOR.COPPER, 12, 6, 12, 5, 9, 5, 9, 0},
                    new int[]{COLOR.COPPER, 26, 6, 26, 5, 29, 5, 29, 0},
                    new int[]{COLOR.COPPER, 12, 0, 12, 3, 26, 3, 26, 0}
                },
                new int[][]{    // White maze 1
                    new int[]{ COLOR.COPPER, 9, 3, 10, 3, 12, 3, 12, 0},
                    new int[]{ COLOR.COPPER, 28, 3, 27, 3, 26, 3, 26, 0},
                    new int[]{ 19, 6, 19, 5, 9, 5, 9, 3, 8, 3, 8, 1, -1, 1 },
                    new int[]{ 19, 5, 28, 5, 28, 3, 30, 3, 30, 1, 39, 1 },
                },
                new int[][]{    // White maze 3
                    new int[]{ -1, 5, 4, 5, 4, 6 },
                    new int[]{ 39, 5, 34, 5, 34, 6 },
                    new int[]{ -1, 3, 16, 3, 16, 6},
                    new int[]{ 39, 3, 22, 3, 22, 6},
                    new int[]{ COLOR.COPPER, 8, 6, 8, 5, 12, 5, 12, 6},
                    new int[]{ COLOR.COPPER, 26, 6, 26, 5, 30, 5, 30, 6}
                },
                new int[][]{ },
                new int[][]{ },
                new int[][]{ },
                new int[][]{ },
                new int[][]{ },
                new int[][]{ },
                new int[][]{ },
                new int[][]{    // Black maze 1
                    new int[]{ COLOR.COPPER, 39, 3, 35, 3, 35, 1, 39, 1},
                    new int[]{ COLOR.COPPER, 9, 6, 9, 5, -1, 5},
                },
                new int[][]{    // Black maze 2
                    new int[]{ COLOR.COPPER, 16, 0, 16, 1, 12, 1, 12, 0},
                    new int[]{ COLOR.COPPER, 6, 0, 6, 3, -1, 3},
                    new int[]{ COLOR.COPPER, -1, 1, 2, 1, 2, 0},
                    new int[]{ COLOR.COPPER, 22, 0, 22, 1, 26, 1, 26, 0},
                    new int[]{ COLOR.COPPER, 32, 0, 32, 1, 36, 1, 36, 0},
                },
                new int[][]{    // Black maze 3
                    new int[]{ COLOR.COPPER, 39, 5, 23, 5, 23, 3, 9, 3, 9, 0},
                },
                new int[][]{    // Black maze entry
                    new int[]{ COLOR.COPPER, 16, 0, 16, 6},
                    new int[]{ COLOR.COPPER, 6, 6, 6, 5, 12, 5, 12, 6},
                    new int[]{ COLOR.COPPER, 2, 6, 2, 5, -1, 5},
                    new int[]{ COLOR.COPPER, 22, 0, 22, 6},
                    new int[]{ COLOR.COPPER, 32, 6, 32, 5, 26, 5, 26, 6},
                    new int[]{ COLOR.COPPER, 36, 6, 36, 5, 39, 5},
                },
                new int[][]{    // Red maze 1
                    new int[]{ COLOR.COPPER, 39, 3, 30, 3, 30, 0},
                },
                new int[][]{    // Red maze top
                    new int[]{ COLOR.COPPER, 19, 0, 19, 1, 12, 1, 12, 0},
                    new int[]{ COLOR.COPPER, 4, 0, 4, 3, -1, 3},
                },
                new int[][]{    // Red maze bottom
                    new int[]{ COLOR.COPPER, 30, 6, 30, 5},
                },
                new int[][]{    // Red maze entry
                    new int[]{ COLOR.COPPER, 19, 0, 19, 6},
                    new int[]{ COLOR.COPPER, 12, 6, 12, 5, 4, 5, 4, 6},
                },
            };
            Marker[][] markerLists = new Marker[][]
           {
                new Marker[]{ },
                new Marker[]{ },
                new Marker[]{ },
                new Marker[]{ },
                new Marker[]{    // Blue maze top
                    new Marker(98, 104, gfxUpArrow, COLOR.BLACK),
                    new Marker(107, 104, gfxCastle, COLOR.BLACK),
                    new Marker(57, 95, gfxDownArrow, COLOR.BLACK),
                    new Marker(56, 104, gfxCastle, COLOR.YELLOW),
                    new Marker(46, 104, gfxCastle, COLOR.JADE),
                },
                new Marker[]{    // Blue maze 1
                    new Marker(154, 104, gfxRightArrow, COLOR.BLACK),
                    new Marker(144, 104, gfxCastle, COLOR.BLACK),
                    new Marker(2, 16, gfxLeftArrow, COLOR.BLACK),
                    new Marker(12, 16, gfxCastle, COLOR.YELLOW),
                    new Marker(56, 104, gfxUpArrow, COLOR.BLACK),
                    new Marker(46, 104, gfxCastle, COLOR.JADE),
                },
                new Marker[]{    // Blue maze bottom
                },
                new Marker[]{    // Blue maze center
                },
                new Marker[]{    // Blue maze entry
                    new Marker(98, 16, gfxDownArrow, COLOR.BLACK),
                    new Marker(106, 16, gfxCastle, COLOR.YELLOW),
                    new Marker(2, 16, gfxLeftArrow, COLOR.BLACK),
                    new Marker(12, 16, gfxCastle, COLOR.BLACK),
                    new Marker(22, 16, gfxCastle, COLOR.JADE),
                },
                new Marker[]{    // White maze 2
                },
                new Marker[]{    // White maze 1
                    new Marker(46, 104, gfxCastle, COLOR.YELLOW),
                    new Marker(56, 104, gfxUpArrow, COLOR.BLACK),
                    new Marker(140, 16, gfxCastle, COLOR.WHITE),
                    new Marker(150, 16, gfxRightArrow, COLOR.BLACK),
                    new Marker(12, 16, gfxCastle, COLOR.COPPER),
                    new Marker(2, 16, gfxLeftArrow, COLOR.BLACK),
                },
                new Marker[]{    // White maze 3
                    new Marker(2, 104, gfxCastle, COLOR.YELLOW),
                    new Marker(11, 104, gfxUpArrow, COLOR.BLACK),
                    new Marker(152, 104, gfxCastle, COLOR.YELLOW),
                    new Marker(144, 104, gfxUpArrow, COLOR.BLACK),
                    new Marker(140, 52, gfxCastle, COLOR.COPPER),
                    new Marker(150, 52, gfxRightArrow, COLOR.BLACK),
                    new Marker(12, 52, gfxCastle, COLOR.WHITE),
                    new Marker(2, 52, gfxLeftArrow, COLOR.BLACK),
                },
            };
            if (!withJadeCastle)
            {
                // Null out all Jade castle paths and markers
                // that includes all markers in the middle of the maze
                // which are only for coming out of the jade castle
                pointsLists[Map.BLUE_MAZE_2][1] = null;
                markerLists[Map.BLUE_MAZE_1][4] = null;
                markerLists[Map.BLUE_MAZE_2][0] = null;
                markerLists[Map.BLUE_MAZE_2][1] = null;
                markerLists[Map.BLUE_MAZE_2][2] = null;
                markerLists[Map.BLUE_MAZE_2][3] = null;
                markerLists[Map.BLUE_MAZE_2][4] = null;
                markerLists[Map.BLUE_MAZE_2][5] = null;
                markerLists[Map.BLUE_MAZE_5][4] = null;
            }
            for (int roomCtr = 0; roomCtr < pointsLists.Length; ++roomCtr)
            {
                int[][] roomPointsLists = pointsLists[roomCtr];
                for (int ctr = 0; ctr < roomPointsLists.Length; ++ctr)
                {
                    if (roomPointsLists[ctr] != null)
                    {
                        Guide.Line nextline = MakeLine(roomPointsLists[ctr]);
                        this.AddLine(roomCtr, nextline);
                    }
                }
                if (markerLists.Length > roomCtr)
                {
                    Marker[] roomMarkerList = markerLists[roomCtr];
                    for(int ctr=0; ctr < roomMarkerList.Length; ++ctr)
                    {
                        if (roomMarkerList[ctr] != null)
                        {
                            this.AddMarker(roomCtr, roomMarkerList[ctr]);
                        }
                    }
                }
            }
        }

        private Guide.Line MakeLine(int[] gridNums)
        {
            COLOR color = COLOR.table(gridNums.Length % 2 == 1 ? gridNums[0] : COLOR.BLACK);
            Guide.Line line = new Guide.Line(color);
            for (int ctr = gridNums.Length % 2; ctr < gridNums.Length; ctr += 2)
            {
                line.Add(new Guide.Point(gridNums[ctr] * 8 + 8, gridNums[ctr + 1] * 32 + 16));
            }
            return line;
        }

        // Object #0B : State FF : Graphic
        private static byte[] gfxCastle = new byte[] {
            0xA5,                  // X X  X X
            0xE7,                  // XXX  XXX
            0xFF,                  // XXXXXXXX
            0x7E,                  //  XXXXXX 
            0x66,                  //  XX  XX 
            0x66                   //  XX  XX 
        };

        private static byte[] gfxUpArrow = new byte[] {
            0x10,                  //    X     
            0x38,                  //   XXX   
            0x54,                  //  X X X  
            0x92,                  // X  X  X
            0x10,                  //    X     
            0x10,                  //    X     
        };

        private static byte[] gfxDownArrow = new byte[] {
            0x10,                  //    X     
            0x10,                  //    X     
            0x92,                  // X  X  X
            0x54,                  //  X X X  
            0x38,                  //   XXX   
            0x10,                  //    X     
        };

        private static byte[] gfxLeftArrow = new byte[] {
            0x08,                  //     X     
            0x10,                  //    X  
            0x20,                  //   X  
            0x7F,                  //  XXXXXXX
            0x20,                  //   X  
            0x10,                  //    X  
            0x08,                  //     X     
        };

        private static byte[] gfxRightArrow = new byte[] {
            0x10,                  //    X     
            0x08,                  //     X     
            0x04,                  //      X
            0xFE,                  // XXXXXXX
            0x04,                  //      X
            0x08,                  //     X     
            0x10,                  //    X     
        };


    }
}
