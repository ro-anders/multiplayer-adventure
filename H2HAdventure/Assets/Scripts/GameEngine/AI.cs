using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{

    public class AI {

        private Map map;

        private AiMapNode[] aiPlots;
        private AiMapNode[][] aiPlotsByRoom;

        // When finding a path to something, keep a table of plots you've already visited
        private bool[] alreadyVisited;

        public AI(Map inMap)
        {
            map = inMap;
            ComputeAllPlots();
            ConnectAllPlots();
            alreadyVisited = new bool[aiPlots.Length];
        }

        public AiPathNode ComputePath(int fromRoom, int fromX, int fromY, int toRoom, int toX, int toY)
        {
            int fromPlot = FindPlot(fromRoom, fromX, fromY);
            if (fromPlot < 0)
            {
                UnityEngine.Debug.LogError("Couldn't find starting plot");
            }
            int toPlot = FindPlot(toRoom, toX, toY);
            if ((fromPlot < 0) || (toPlot < 0))
            {
                return null;
            }
            else if (fromPlot == toPlot)
            {
                return new AiPathNode(aiPlots[toPlot], Plot.NO_DIRECTION, null);
            }

            // Reset the already visited array
            for(int ctr=0; ctr<alreadyVisited.Length; ++ctr)
            {
                alreadyVisited[ctr] = false;
            }

            // We do a breadth first search, which requires a queue.
            // Although we have a data structure for 
            List<AiPathNode> q = new List<AiPathNode>();
            // We start from the desired point and work our way back
            q.Add(new AiPathNode(aiPlots[toPlot], Plot.NO_DIRECTION, null));
            alreadyVisited[toPlot] = true;
            AiPathNode found = null;
            while ((found == null) && (q.Count > 0))
            {
                found = ComputeNextStep(fromPlot, q, alreadyVisited);
            }
            return found;
        }

        private void ComputeAllPlots()
        {
            // Right now this is hardcoded and just for a couple of rooms

            aiPlotsByRoom = new AiMapNode[Map.NUM_ROOMS][];
            for (int ctr = 0; ctr < Map.NUM_ROOMS; ++ctr)
            {
                aiPlotsByRoom[ctr] = new AiMapNode[0];
            }
            int totalPlots = 0;

            ComputePlotsInRoom(Map.MAIN_HALL_LEFT, plotsLeftHallWithTop, ref totalPlots);
            ComputePlotsInRoom(Map.MAIN_HALL_CENTER, plotsHallWithTop, ref totalPlots);
            ComputePlotsInRoom(Map.MAIN_HALL_RIGHT, plotsRightHallWithBoth, ref totalPlots);
            ComputePlotsInRoom(Map.GOLD_CASTLE, plotsCastle, ref totalPlots);
            ComputePlotsInRoom(Map.SOUTHEAST_ROOM, plotsRoomWithTop, ref totalPlots);
            ComputePlotsInRoom(Map.COPPER_CASTLE, plotsCastle, ref totalPlots);

            aiPlots = new AiMapNode[totalPlots];
            int plotCtr = 0;
            for (int roomCtr = 0; roomCtr < Map.NUM_ROOMS; ++roomCtr)
            {
                for (int plotInRoomCtr = 0; plotInRoomCtr < aiPlotsByRoom[roomCtr].Length; ++plotInRoomCtr)
                {
                    aiPlots[plotCtr] = aiPlotsByRoom[roomCtr][plotInRoomCtr];
                    ++plotCtr;
                }
            }
        }

        private void ComputePlotsInRoom(int room, byte[][] plotData, ref int totalPlots)
        {
            AiMapNode[] roomPlots = new AiMapNode[plotData.Length];

            for (int plotCtr = 0; plotCtr < plotData.Length; ++plotCtr)
            {
                byte[] plotValues = plotData[plotCtr];
                Plot newPlot = new Plot(totalPlots, room,
                    plotValues[1], plotValues[0], plotValues[3], plotValues[2]);
                roomPlots[plotCtr] = new AiMapNode(newPlot);
                ++totalPlots;
            }
            aiPlotsByRoom[room] = roomPlots;
        }

        private void ConnectAllPlots()
        {
            for (int roomCtr = 0; roomCtr < Map.NUM_ROOMS; ++roomCtr)
            {
                ROOM room = map.roomDefs[roomCtr];
                AiMapNode[] plotsInRoom = aiPlotsByRoom[roomCtr];
                for (int pctr1 = 0; pctr1 < plotsInRoom.Length; ++pctr1)
                {
                    Plot plot1 = plotsInRoom[pctr1].thisPlot;
                    for (int direction = 0; direction < 4; ++direction)
                    {
                        AiMapNode[] plotsInOtherRoom = (plot1.OnEdge(direction) ?
                            aiPlotsByRoom[room.roomNext(direction)] : plotsInRoom);
                        // Go through all the plots in the room to find one 
                        // adjacent to this one
                        int found = -1;
                        for (int pctr2 = 0; (found < 0) && (pctr2 < plotsInOtherRoom.Length); ++pctr2)
                        {
                            Plot plot2 = plotsInOtherRoom[pctr2].thisPlot;
                            if (plot1.AdjacentTo(plot2, direction))
                            {
                                found = pctr2;
                            }
                        }
                        if (found >= 0)
                        {
                            plotsInRoom[pctr1].SetNeighbor(direction, plotsInOtherRoom[found]);
                        }
                    }
                }
            }
        }

        private int FindPlot(int room, int x, int y)
        {
            int found = -1;
            AiMapNode[] plots = aiPlotsByRoom[room];
            for (int ctr=0; (found < 0) && (ctr < plots.Length); ++ctr)
            {
                if (plots[ctr].thisPlot.Contains(x, y))
                {
                    found = plots[ctr].thisPlot.Key;
                }
            }
            return found;
        }

        private AiPathNode ComputeNextStep(int goalPlot, List<AiPathNode> q, bool[] alreadyFound)
        {
            AiPathNode nextStep = q[0];
            q.RemoveAt(0);
            for(int ctr=0; ctr< 4; ++ctr)
            {
                AiMapNode neighbor = nextStep.thisNode.neighbors[ctr];
                if ((neighbor != null) && !alreadyVisited[neighbor.thisPlot.Key])
                {
                    AiPathNode nextNextStep = new AiPathNode(neighbor, ctr + 2 % 4, nextStep);
                    if (neighbor.thisPlot.Key == goalPlot)
                    {
                        return nextNextStep;
                    }
                    else
                    {
                        q.Add(nextNextStep);
                        alreadyVisited[nextNextStep.ThisPlot.Key] = true;
                    }
                }
            }
            return null;
        }


        private static readonly byte[][] plotsCastle =
        {
            new byte[] {0,16, 0,23},
            new byte[] {1,3,3,11},
            new byte[] {1,12,1,27},
            new byte[] {1,28,1,37},
            new byte[] {2,18,2,21},
            new byte[] {3,19,3,20},
            new byte[] {4,2,5,9},
            new byte[] {4,30,5,37},
            new byte[] {5,17,6,22},
        };

        private static readonly byte[][] plotsLeftHallWithTop =
        {
            new byte[] {1,3,5,39},
            new byte[] {6,16,6,23}
        };

        private static readonly byte[][] plotsLeftHallWithBoth =
        {
            new byte[] {0,16,0,23},
            new byte[] {1,3,5,39},
            new byte[] {6,16,6,23}
        };

        private static readonly byte[][] plotsHallWithTop =
        {
            new byte[] {1,3,5,39},
            new byte[] {6,16, 6,23}
        };

        private static readonly byte[][] plotsRightHallWithBottom =
        {
            new byte[] {0,16,0,23},
            new byte[] {1,0,5,36}
        };

        private static readonly byte[][] plotsRightHallWithBoth =
        {
            new byte[] {0,16,0,23},
            new byte[] {1,0,5,36},
            new byte[] {6,16,6,23}
        };

        private static readonly byte[][] plotsRoomWithTop =
        {
            new byte[] {1,2,5,37},
            new byte[] {6,16, 6,23}
        };

    }

    public class Plot
    {
        public const int NO_DIRECTION = -1;
        public const int UP = 0;
        public const int RIGHT = 1;
        public const int DOWN = 2;
        public const int LEFT = 3;
        public const int FIRST_DIRECTION = 0;
        public const int LAST_DIRECTION = 3;

        // When computing overlap, how far beyond the edge of the plot to shoot for
        private const int OVERLAP_EXTENT = 6; // Distance ball can move in one turn

        public const int inputXScale = 8;
        public const int inputYScale = 32;
        public static readonly int[] roomEdges = { 7 * inputYScale - 1, 40 * inputXScale - 1, 0, 0 };

        private readonly int key;
        public int Key
        {
            get { return key; }
        }
        private readonly int room;
        public int Room
        {
            get { return room; }
        }
        private readonly int[] edges;

        public Plot(int inKey, int inRoom, int inLeft, int inBottom, int inRight, int inTop)
        {
            key = inKey;
            room = inRoom;
            edges = new int[] { (inTop+1) * inputYScale - 1 , (inRight+1) * inputXScale - 1,
                                 inBottom * inputYScale, inLeft * inputXScale };
        }

        public int Top
        {
            get { return edges[UP]; }
        }
        public int Bottom
        {
            get { return edges[DOWN]; }
        }
        public int Left
        {
            get { return edges[LEFT]; }
        }
        public int Right
        {
            get { return edges[RIGHT]; }
        }

        public int Edge(int direction)
        {
            return edges[direction % 4];
        }

        public bool OnEdge(int direction)
        {
            direction = direction % 4;
            return edges[direction] == roomEdges[direction];
        }

        public bool Contains(int x, int y)
        {
            return ((x >= edges[LEFT]) && (x <= edges[RIGHT]) &&
                    (y >= edges[DOWN]) && (y <= edges[UP]));
        }
        public bool Contains(int inRoom, int x, int y)
        {
            return ((inRoom == room) && 
                    (x >= edges[LEFT]) && (x <= edges[RIGHT]) &&
                    (y >= edges[DOWN]) && (y <= edges[UP]));
        }

        public bool AdjacentTo(Plot otherPlot, int direction)
        {
            bool isAdjacent = false;
            // Do the sides overlap
            int side1a = this.Edge(direction + 3);
            int side1b = this.Edge(direction + 1);
            int side2a = otherPlot.Edge(direction + 3);
            int side2b = otherPlot.Edge(direction + 1);
            bool disjoint = ((side1a < side2a) && (side1a < side2b) & (side1b < side2a) && (side1b < side2b)) ||
                ((side1a > side2a) && (side1a > side2b) & (side1b > side2a) && (side1b > side2b));
            if (disjoint)
            {
                isAdjacent = false;
            }
            else if (this.OnEdge(direction))
            {
                isAdjacent = otherPlot.OnEdge(direction + 2);
            }
            else
            {
                int edge1 = this.Edge(direction);
                int edge2 = otherPlot.Edge(direction + 2);
                isAdjacent = Math.Abs(edge1 - edge2) == 1;
            }
            return isAdjacent;
        }

        public void GetOverlap(Plot otherPlot, int direction, ref int outX, ref int outY)
        {
            int side1a = this.Edge(direction + 3);
            int side1b = this.Edge(direction + 1);
            int side2a = otherPlot.Edge(direction + 3);
            int side2b = otherPlot.Edge(direction + 1);

            int sidea = (Math.Abs(side1a - side1b) < Math.Abs(side2b - side1b) ? side1a : side2b);
            int sideb = (Math.Abs(side1b - side1a) < Math.Abs(side2a - side1a) ? side1b : side2a);
            int midpoint = (sidea + sideb) / 2;

            switch(direction) {
                case UP:
                    outX = midpoint;
                    outY = this.Top + OVERLAP_EXTENT;
                    return;
                case DOWN:
                    outX = midpoint;
                    outY = this.Bottom - OVERLAP_EXTENT;
                    return;
                case LEFT:
                    outX = this.Left - OVERLAP_EXTENT;
                    outY = midpoint;
                    return;
                case RIGHT:
                default:
                    outX = this.Right + OVERLAP_EXTENT;
                    outY = midpoint;
                    return;
            }

        }

        public override string ToString()
        {
            return "[" + key + "=" + room + "-" + Left + "," + Bottom + "-" + Right + "," + Top + "]";
        }

        public static string DirToString(int dir)
        {
            switch (dir) {
                case UP: return "N";
                case RIGHT: return "E";
                case DOWN: return "S";
                case NO_DIRECTION: return "X";
                case LEFT: default: return "W";
            }
        }
    }

    public class AiMapNode
    {
        public Plot thisPlot;
        public AiMapNode[] neighbors = { null, null, null, null };

        public AiMapNode(Plot inPlot)
        {
            thisPlot = inPlot;
        }

        public void SetNeighbor(int direction, AiMapNode neighbor)
        {
            neighbors[direction] = neighbor;
        }
    }

    public class AiPathNode
    {
        public AiMapNode thisNode;
        public int nextDirection;
        public AiPathNode nextNode;
        public AiPathNode(AiMapNode inPlot, int inDirection, AiPathNode inPath)
        {
            thisNode = inPlot;
            nextDirection = inDirection;
            nextNode = inPath;
        }
        public Plot ThisPlot
        {
            get { return thisNode.thisPlot; }
        }
        public override string ToString()
        {
            string str = "" + ThisPlot.Key;
            string dirStr = Plot.DirToString(nextDirection);
            for(AiPathNode n = nextNode; n != null; n = n.nextNode)
            {
                str += "-" + dirStr + "-" + n.ThisPlot.Key;
                dirStr = Plot.DirToString(nextDirection);
            }
            return str;
        }
    }

}
