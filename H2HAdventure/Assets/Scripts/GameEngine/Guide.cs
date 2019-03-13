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
        public class Line: List<Point> { }

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
            int[] points = { 0, 3, 39, 3 };
            Guide.Line line = MakeLine(points);
            this.AddLine(2, line);
            int[][][] pointsLists = new int[][][]
            {
                new int[][]{ },
                new int[][]{ },
                new int[][]{ },
                new int[][]{ },
                new int[][]{ }, // Blue maze top
                new int[][]{ }, // Blue maze 1
                new int[][]{ }, // Blue maze bottom
                new int[][]{    // Blue maze center
                    new int[]{39, 1, 34, 1, 34, 3, 39, 3},
                    new int[]{39, 5, 34, 5, 34, 6}
                }, 
                new int[][]{    // Blue maze entry
                    new int[]{ 20, 0, 20, 1, 0, 1 },
                    new int[]{ 0, 3, 5, 3, 5, 5, 0, 5}
                },
                new int[][]{ } // Blue maze middle
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
            Guide.Line line = new Guide.Line();
            for (int ctr = 0; ctr < gridNums.Length; ctr += 2)
            {
                line.Add(new Guide.Point(gridNums[ctr] * 8 + 4, gridNums[ctr + 1] * 32 + 16));
            }
            return line;
        }



    }
}
