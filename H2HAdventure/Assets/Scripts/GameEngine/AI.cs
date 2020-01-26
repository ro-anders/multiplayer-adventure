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
            if (toPlot < 0)
            {
                UnityEngine.Debug.LogError("Couldn't find ending plot");
            }
            if ((fromPlot < 0) || (toPlot < 0))
            {
                return null;
            }
            else if (fromPlot == toPlot)
            {
                return new AiPathNode(aiPlots[toPlot], Plot.NO_DIRECTION, null);
            }
            UnityEngine.Debug.Log("Computing path from " + aiPlots[fromPlot].thisPlot + 
                " to " + aiPlots[toPlot].thisPlot);


            // Reset the already visited array
            for (int ctr=0; ctr<alreadyVisited.Length; ++ctr)
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

            if (found==null)
            {
                UnityEngine.Debug.Log("Could not find path from " + aiPlots[fromPlot] + " to " +
                    aiPlots[toPlot]);
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
            List<AiMapNode> allPlotsList = new List<AiMapNode>();
            ComputePlotsInRoom(Map.MAIN_HALL_LEFT, plotsLeftHallWithTop, allPlotsList);
            ComputePlotsInRoom(Map.MAIN_HALL_CENTER, plotsHallWithTop, allPlotsList);
            ComputePlotsInRoom(Map.MAIN_HALL_RIGHT, plotsRightHallWithBoth, allPlotsList);
            ComputePlotsInRoom(Map.GOLD_CASTLE, plotsCastle, allPlotsList);
            ComputePlotsInRoom(Map.SOUTHEAST_ROOM, plotsRoomWithTop, allPlotsList);
            ComputePlotsInRoom(Map.COPPER_CASTLE, plotsCastle, allPlotsList);
            ComputePlotsInRoom(Map.BLUE_MAZE_5, plotsBlueMazeTop, allPlotsList);
            ComputePlotsInRoom(Map.BLUE_MAZE_2, plotsBlueMaze2, allPlotsList);
            ComputePlotsInRoom(Map.BLUE_MAZE_3, plotsBlueMazeBottom, allPlotsList);
            ComputePlotsInRoom(Map.BLUE_MAZE_4, plotsBlueMazeCenter, allPlotsList);
            ComputePlotsInRoom(Map.BLUE_MAZE_1, plotsBlueMazeEntry, allPlotsList);
            ComputePlotsInRoom(Map.BLACK_CASTLE, plotsCastle, allPlotsList);

            aiPlots = allPlotsList.ToArray();
        }

        private void ComputePlotsInRoom(int room, byte[][] plotData, List<AiMapNode> allPlotsList)
        {
            AiMapNode[] roomPlots = new AiMapNode[plotData.Length];

            for (int plotCtr = 0; plotCtr < plotData.Length; ++plotCtr)
            {
                byte[] plotValues = plotData[plotCtr];
                Plot newPlot = new Plot(allPlotsList.Count, room,
                    plotValues[1], plotValues[0], plotValues[3], plotValues[2]);
                roomPlots[plotCtr] = new AiMapNode(newPlot);
                allPlotsList.Add(roomPlots[plotCtr]);
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
                    AiPathNode nextNextStep = new AiPathNode(neighbor, (ctr + 2) % 4, nextStep);
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
            new byte[] {1,0,5,39},
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

        private static readonly byte[][] plotsBlueMazeTop =
        {
            new byte[] {0,4,0,5},
            new byte[] {0,8,0,9},
            new byte[] {0,18,4,21},
            new byte[] {0,30,0,31},
            new byte[] {0,34,0,35},
            new byte[] {1,0,1,5},
            new byte[] {1,8,1,15},
            new byte[] {1,24,1,31},
            new byte[] {1,34,1,39},
            new byte[] {3,4,4,7},
            new byte[] {3,10,3,17},
            new byte[] {3,22,3,29},
            new byte[] {3,32,4,35},
            new byte[] {4,10,5,13},
            new byte[] {4,26,5,29},
            new byte[] {5,0,5,7},
            new byte[] {5,16,6,23},
            new byte[] {5,32,5,39},
        };

        private static readonly byte[][] plotsBlueMaze2 =
        {
            new byte[] {0,8,0,9},
            new byte[] {0,12,1,13},
            new byte[] {0,16,3,17},
            new byte[] {0,22,3,23},
            new byte[] {0,26,1,27},
            new byte[] {0,30,0,31},
            new byte[] {1,0,1,5},
            new byte[] {1,8,1,11},
            new byte[] {1,28,1,31},
            new byte[] {1,34,1,39},
            new byte[] {2,4,2,5},
            new byte[] {2,34,2,35},
            new byte[] {3,4,3,15},
            new byte[] {3,24,3,35},
            new byte[] {4,10,4,11},
            new byte[] {4,28,4,29},
            new byte[] {5,0,5,15},
            new byte[] {5,16,5,23},
            new byte[] {5,24,5,39},
            new byte[] {6,16,6,23} // This one not there if no green castle
       };

        private static readonly byte[][] plotsBlueMazeBottom =
        {
            new byte[] {1,0,1,5},
            new byte[] {1,8,3,31},
            new byte[] {1,34,1,39},
            new byte[] {3,4,5,5},
            new byte[] {3,6,3,7},
            new byte[] {3,32,3,33},
            new byte[] {3,34,5,35},
            new byte[] {4,16,6,23},
            new byte[] {5,0,5,3},
            new byte[] {5,8,6,9},
            new byte[] {5,10,5,11},
            new byte[] {5,12,6,13},
            new byte[] {5,26,6,27},
            new byte[] {5,28,5,29},
            new byte[] {5,30,6,31},
            new byte[] {5,36,5,39},
     };

        private static readonly byte[][] plotsBlueMazeCenter =
        {
            new byte[] {0,8,3,9},
            new byte[] {0,12,5,13},
            new byte[] {0,16,1,23},
            new byte[] {0,26,5,27},
            new byte[] {0,30,3,31},
            new byte[] {1,0,1,5},
            new byte[] {1,34,1,39},
            new byte[] {2,4,2,5},
            new byte[] {2,18,6,21},
            new byte[] {2,34,2,35},
            new byte[] {3,0,3,7},
            new byte[] {3,32,3,39},
            new byte[] {5,0,5,5},
            new byte[] {5,8,6,9},
            new byte[] {5,10,5,11},
            new byte[] {5,28,5,29},
            new byte[] {5,30,6,31},
            new byte[] {5,34,5,39},
            new byte[] {6,4,6,5},
            new byte[] {6,34,6,35},
        };

        private static readonly byte[][] plotsBlueMazeEntry =
        {
            new byte[] {0,16,0,23},
            new byte[] {1,0,1,23},
            new byte[] {1,24,1,39},
            new byte[] {2,8,6,9},
            new byte[] {2,30,6,31},
            new byte[] {3,0,3,5},
            new byte[] {3,12,6,13},
            new byte[] {3,14,3,25},
            new byte[] {3,26,6,27},
            new byte[] {3,34,3,39},
            new byte[] {4,4,4,5},
            new byte[] {4,34,4,35},
            new byte[] {5,0,5,5},
            new byte[] {5,16,5,23},
            new byte[] {5,34,5,39},
            new byte[] {6,16,6,17},
            new byte[] {6,22,6,23},
        };

    }

    /*
     * A plot is simply a rectangle on the map with a unique id.
     * It knows its room and xy boundaries and can compare itself
     * with other plots.
     */
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

        /**
         * Returns true if the plot contains the coordinates or they are very close
         */
        public bool RoughlyContains(int inRoom, int x, int y)
        {
            return ((inRoom == room) &&
                    (x >= edges[LEFT]-OVERLAP_EXTENT) && (x <= edges[RIGHT] + OVERLAP_EXTENT) &&
                    (y >= edges[DOWN] - OVERLAP_EXTENT) && (y <= edges[UP] + OVERLAP_EXTENT));
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

        // If two plots are adjacent, this will return the point just inside the
        // other plot and adjacent to the this plot.  The point will be the
        // midpoint of their intersection.
        // If the plots are not adjacent or are adjacent in the direction other
        // than the one specified, the return value is undefined.
        // If the plots are adjacent across a room switch it will still work.
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
                    outY = this.Top + 1;
                    return;
                case DOWN:
                    outX = midpoint;
                    outY = this.Bottom - 1;
                    return;
                case LEFT:
                    outX = this.Left - 1;
                    outY = midpoint;
                    return;
                case RIGHT:
                default:
                    outX = this.Right + 1;
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
                case LEFT: return "W";
                case NO_DIRECTION: return "X";
                default: return "?"+dir;
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

        public override string ToString()
        {
            string str = thisPlot + "(";
            for (int ctr = 0; ctr < 4; ++ctr) {
                str += (neighbors[ctr] == null ? "-" : "" + neighbors[ctr].thisPlot.Key);
                if (ctr < 3)
                {
                    str += ",";
                }
            }
            str += ")";
            return str;
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
                dirStr = Plot.DirToString(n.nextDirection);
            }
            return str;
        }
    }

}
