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
         * The guides that appear in this room.
         */
        public class RoomGuide
        {
            int room;
            List<Line> lines = new List<Line>();

            public RoomGuide(int inRoom)
            {
                room = inRoom;
            }

            public void AddLine(Line inLine)
            {
                lines.Add(inLine);
            }

            public IEnumerator<Line> GetLines()
            {
                return lines.GetEnumerator();
            }
        }

        private RoomGuide[] roomGuides;
        private static readonly List<Line> EMPTY_ROOM = new List<Line>();

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

        public IEnumerator<Line> GetLines(int room)
        {
            if (roomGuides[room] == null)
            {
                return EMPTY_ROOM.GetEnumerator();
            }
            else
            {
                return roomGuides[room].GetLines();
            }
        }

        public void ConfigureGuide()
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
                    new int[]{-1, 1, 4, 1, 4, 3, 10, 3, 10, 5, 39, 5}
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
                    new int[]{COLOR.COPPER, 12, 6, 12, 5, 9, 5, 9, 0},
                    new int[]{COLOR.COPPER, 26, 6, 26, 5, 29, 5, 29, 0},
                    new int[]{COLOR.COPPER, 12, 0, 12, 3, 26, 3, 26, 0}
                },
                new int[][]{    // White maze 1
                    new int[]{ COLOR.COPPER, 9, 3, 10, 3, 12, 3, 12, 0},
                    new int[]{ COLOR.COPPER, 28, 3, 27, 3, 26, 3, 26, 0},
                    new int[]{ 18, 6, 18, 5, 9, 5, 9, 3, 8, 3, 8, 1, -1, 1 },
                    new int[]{ 20, 6, 20, 5, 28, 5, 28, 3, 30, 3, 30, 1, 39, 1 },
                },
                new int[][]{    // White maze 3
                    new int[]{ -1, 5, 4, 5, 4, 6 },
                    new int[]{ 39, 5, 34, 5, 34, 6 },
                    new int[]{ -1, 3, 16, 3, 16, 6},
                    new int[]{ 39, 3, 22, 3, 22, 6},
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
            for (int roomCtr = 0; roomCtr < pointsLists.Length; ++roomCtr)
            {
                int[][] roomPointsLists = pointsLists[roomCtr];
                for (int ctr = 0; ctr < roomPointsLists.Length; ++ctr)
                {
                    Guide.Line nextline = MakeLine(roomPointsLists[ctr]);
                    this.AddLine(roomCtr, nextline);
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



    }
}
