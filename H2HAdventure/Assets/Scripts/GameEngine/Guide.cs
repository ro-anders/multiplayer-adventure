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

    }
}
