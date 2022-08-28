using System;
using System.Collections.Generic;

namespace GameEngine
{

    /**
     * AI Logic for navigating the game board - knowing how to traverse mazes, 
     * and when castles can be entered.  This is not player specific and is 
     * shared between the AIs.
     */
    public class AiNav {

        private Map map;

        private AiMapNode[] aiPlots;
        private AiMapNode[][] aiPlotsByRoom;

        // When finding a path to something, keep a table of plots you've already visited
        private bool[] alreadyVisited;

        public AiNav(Map inMap)
        {
            map = inMap;
            ComputeAllPlots();
            ConnectAllPlots();
            alreadyVisited = new bool[aiPlots.Length];
        }

        /**
         * Compute a path from one point to another.
         * @return a path from the from a plot containing the from point to
         * a plot containing the to point.  Or null if no such path exists.
         */
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
                return new AiPathNode(aiPlots[toPlot]);
            }
            //UnityEngine.Debug.Log("Computing path from " + aiPlots[fromPlot].thisPlot + 
            //    " to " + aiPlots[toPlot].thisPlot);


            // Reset the already visited array
            for (int ctr = 0; ctr < alreadyVisited.Length; ++ctr)
            {
                alreadyVisited[ctr] = false;
            }

            // We do a breadth first search, which requires a queue.
            // Although we have a data structure for 
            List<AiPathNode> q = new List<AiPathNode>();
            // We start from the desired point and work our way back
            q.Add(new AiPathNode(aiPlots[toPlot]));
            alreadyVisited[toPlot] = true;
            AiPathNode found = null;
            while ((found == null) && (q.Count > 0))
            {
                found = ComputeNextStep(fromPlot, q, alreadyVisited);
            }

            if (found == null)
            {
                UnityEngine.Debug.Log("Could not find path from " + aiPlots[fromPlot] + " to " +
                    aiPlots[toPlot]);
            }
            return found;
        }

        /**
         * Look at all plots in a certain area, compute which one is closest and return a path
         * to that plot.
         * @param startRoom the room of the starting point (not the room with the desired region)
         * @param startX the starting x position
         * @param startY the starting y position
         * @param desiredArea the area of a room that the plot needs to touch
         */
        public AiPathNode ComputePathToArea(int startRoom, int startX, int startY, RRect desiredArea)
        {
            AiPathNode shortestPath = null;
            AiMapNode[] plots = aiPlotsByRoom[desiredArea.room];
            foreach (AiMapNode nextPlot in plots)
            {
                Plot plot = nextPlot.thisPlot;
                bool touches = (plot.Touches(desiredArea));
                if (touches)
                {
                    AiPathNode path = ComputePath(startRoom, startX, startY, plot.Room, plot.MidX, plot.MidY);
                    if ((path != null) &&
                        ((shortestPath == null) || (path.distance < shortestPath.distance)))
                    {
                        shortestPath = path;
                    }
                }
            }
            return shortestPath;
        }

        /**
         * Look at all exit plots in a room and determine which one
         * is closest to a starting point
         * @param startRoom the room of the starting point (not the room 
         * where we are looking for exits)
         * @param startX the starting x position
         * @param startY the starting y position
         * @param roomWithExits the room you are looking for an exit in
         * @returns the path to that closest exit plot or null if not reachable
         */
        public AiPathNode ComputePathToClosestExit(int startRoom, int startX, int fromy, int roomWithExits)
        {
            AiPathNode shortestPath = null;
            AiMapNode[] plots = aiPlotsByRoom[roomWithExits];
            foreach (AiMapNode nextPlot in plots)
            {
                Plot plot = nextPlot.thisPlot;
                bool onEdge = (plot.OnEdge(Plot.UP) || plot.OnEdge(Plot.RIGHT) ||
                    (plot.OnEdge(Plot.DOWN) || plot.OnEdge(Plot.LEFT)));
                if (onEdge)
                {
                    AiPathNode path = ComputePath(startRoom, startX, fromy, roomWithExits, plot.MidX, plot.MidY);
                    if ((path != null) &&
                        ((shortestPath == null) || (path.distance < shortestPath.distance)))
                    {
                        shortestPath = path;
                    }
                }
            }
            return shortestPath;
        }


        /**
         * Returns true if an object is in or overlapping a reachable area.
         * Returns false if the object is totally embedded in the wall.
         */
        public bool IsReachable(int room, int x, int y, int width, int height)
        {
            int[] plots = FindPlots(room, x, y, width, height, true);
            return plots.Length > 0;
        }

        /**
         * Find all plots that overlap this region
         */
        public Plot[] GetPlots(int room, int x, int y, int width, int height)
        {
            int[] plotIndexes = FindPlots(room, x, y, width, height, false);
            Plot[] plots = new Plot[plotIndexes.Length];
            for (int ctr = 0; ctr < plotIndexes.Length; ++ctr)
            {
                plots[ctr] = aiPlots[plotIndexes[ctr]].thisPlot;
            }
            return plots;
        }

        /**
         * Find all plots that overlap this rectangle
         */
        public Plot[] GetPlots(RRect rect)
        {
            return GetPlots(rect.room, rect.x, rect.y, rect.width, rect.height);
        }

        /**
         * See where the ai player is on their current path.
         * If they have advanced upon the path, will return the advanced path.
         * If they have fallen off the path, will return null.
         */
        public AiPathNode checkPathProgress(AiPathNode desiredPath, int currentRoom, int currentX, int currentY)
        {
            AiPathNode checkedPath = null;
            // Make sure we're still on the path
            if (desiredPath.ThisPlot.Contains(currentRoom, currentX, currentY))
            {
                checkedPath = desiredPath;
            }
            else
            {
                // Most probable cause is we've gotten to the next step in the path
                if ((desiredPath.nextNode != null) &&
                    (desiredPath.nextNode.ThisPlot.Contains(currentRoom, currentX, currentY)))
                {
                    checkedPath = desiredPath.nextNode;
                }
                // Next most probable cause is we've missed the path by just a little.
                else if (desiredPath.ThisPlot.RoughlyContains(currentRoom, currentX, currentY))
                {
                    // We're ok.  Don't need to do anything.
                    checkedPath = desiredPath;
                }
                else
                {
                    // We're off the path.  See if, by any chance, we are now somewhere further on
                    // the path
                    AiPathNode found = null;
                    for (AiPathNode newNode = desiredPath.nextNode;
                        (newNode != null) && (found == null);
                        newNode = newNode.nextNode)
                    {
                        if (newNode.ThisPlot.Contains(currentRoom, currentX, currentY))
                        {
                            found = newNode;
                        }
                    }
                    if (found != null)
                    {
                        checkedPath = found;
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("Player has fallen off the AI path!\n" +
                            currentRoom + "(" + currentX + "," +
                            currentY + ") not in " +
                            desiredPath.ThisPlot +
                            (desiredPath.nextNode == null ? "" : " or " + desiredPath.nextNode.ThisPlot));
                        checkedPath = null;
                    }
                }
            }
            return checkedPath;
        }

        /**
         * This computes all the plots that are on the game board (it doesn't
         * actually connect them) and sticks them in the aiPlots and aiPlotsByRoom
         * data structures.
         */
        private void ComputeAllPlots()
        {
            // Right now this is hardcoded and just for a couple of rooms

            aiPlotsByRoom = new AiMapNode[Map.NUM_ROOMS][];
            for (int ctr = 0; ctr < Map.NUM_ROOMS; ++ctr)
            {
                aiPlotsByRoom[ctr] = new AiMapNode[0];
            }
            List<AiMapNode> allPlotsList = new List<AiMapNode>();
            ComputePlotsInRoom(Map.NUMBER_ROOM, plotsRoomWithBottom, allPlotsList);
            ComputePlotsInRoom(Map.MAIN_HALL_LEFT, plotsLeftHallWithTop, allPlotsList);
            ComputePlotsInRoom(Map.MAIN_HALL_CENTER, plotsHallWithTop, allPlotsList);
            ComputePlotsInRoom(Map.MAIN_HALL_RIGHT, plotsRightHallWithBoth, allPlotsList);
            ComputePlotsInRoom(Map.BLUE_MAZE_5, plotsBlueMazeTop, allPlotsList);
            ComputePlotsInRoom(Map.BLUE_MAZE_2, plotsBlueMaze2, allPlotsList);
            ComputePlotsInRoom(Map.BLUE_MAZE_3, plotsBlueMazeBottom, allPlotsList);
            ComputePlotsInRoom(Map.BLUE_MAZE_4, plotsBlueMazeCenter, allPlotsList);
            ComputePlotsInRoom(Map.BLUE_MAZE_1, plotsBlueMazeEntry, allPlotsList);
            ComputePlotsInRoom(Map.WHITE_MAZE_2, plotsWhiteMazeMiddle, allPlotsList);
            ComputePlotsInRoom(Map.WHITE_MAZE_1, plotsWhiteMazeEntry, allPlotsList);
            ComputePlotsInRoom(Map.WHITE_MAZE_3, plotsWhiteMazeSide, allPlotsList);
            ComputePlotsInRoom(Map.SOUTH_HALL_RIGHT, plotsRightHallWithBoth, allPlotsList);
            ComputePlotsInRoom(Map.SOUTH_HALL_LEFT, plotsLeftHallWithBoth, allPlotsList);
            ComputePlotsInRoom(Map.SOUTHWEST_ROOM, plotsRoomWithTop, allPlotsList);
            ComputePlotsInRoom(Map.WHITE_CASTLE, plotsCastle, allPlotsList);
            ComputePlotsInRoom(Map.BLACK_CASTLE, plotsCastle, allPlotsList);
            ComputePlotsInRoom(Map.GOLD_CASTLE, plotsCastle, allPlotsList);
            ComputePlotsInRoom(Map.GOLD_FOYER, plotsRoomWithBottom, allPlotsList);
            ComputePlotsInRoom(Map.BLACK_MAZE_1, plotsBlackMaze1, allPlotsList);
            ComputePlotsInRoom(Map.BLACK_MAZE_2, plotsBlackMaze2, allPlotsList);
            ComputePlotsInRoom(Map.BLACK_MAZE_3, plotsBlackMaze3, allPlotsList);
            ComputePlotsInRoom(Map.BLACK_MAZE_ENTRY, plotsBlackMazeEntry, allPlotsList);
            ComputePlotsInRoom(Map.RED_MAZE_3, plotsRedMaze1, allPlotsList);
            ComputePlotsInRoom(Map.RED_MAZE_2, plotsRedMazeTop, allPlotsList);
            ComputePlotsInRoom(Map.RED_MAZE_4, plotsRedMazeBottom, allPlotsList);
            ComputePlotsInRoom(Map.RED_MAZE_1, plotsRedMazeEntry, allPlotsList);
            ComputePlotsInRoom(Map.BLACK_FOYER, plotsRoomWithBoth, allPlotsList);
            ComputePlotsInRoom(Map.BLACK_INNERMOST_ROOM, plotsRoomWithBottom, allPlotsList);
            ComputePlotsInRoom(Map.SOUTHEAST_ROOM, plotsRoomWithTop, allPlotsList);
            ComputePlotsInRoom(Map.ROBINETT_ROOM, plotsHallWithTop, allPlotsList);
            ComputePlotsInRoom(Map.JADE_CASTLE, plotsCastle, allPlotsList);
            ComputePlotsInRoom(Map.JADE_FOYER, plotsRoomWithBottom, allPlotsList);
            ComputePlotsInRoom(Map.CRYSTAL_CASTLE, plotsCastle, allPlotsList);
            ComputePlotsInRoom(Map.CRYSTAL_FOYER, plotsRoomWithBottom, allPlotsList);
            ComputePlotsInRoom(Map.COPPER_CASTLE, plotsCastle, allPlotsList);
            ComputePlotsInRoom(Map.COPPER_FOYER, plotsRoomWithBottom, allPlotsList);

            aiPlots = allPlotsList.ToArray();
        }

        /**
         * This creates a multi-line string showing a room and all it's plots
         * @param roomNum the number of the room
         * @returns a cool ascii art of the room and its plots
         */
        private string getDebugRoomPlotsString(int roomNum)
        {
            // DEBUG
            ROOM room = map.getRoom(roomNum);
            string debugStr = "Room #" + room.index + ": " + room.label + "\n";
            for (int y = 6; y >= 0; --y)
            {
                for (int x = 0; x < 40; ++x)
                {
                    int plot = FindPlot(roomNum, x * Plot.WALL_X_SCALE, y * Plot.WALL_Y_SCALE);
                    if (plot < 0)
                    {
                        debugStr = debugStr + (room.walls[x, y] ? "█" : "?");
                    }
                    else if (room.walls[x, y])
                    {
                        debugStr = debugStr + "?";
                    }
                    else
                    {
                        char c = (char)((int)'a' + aiPlots[plot].thisPlot.PlaceInRoom);
                        debugStr = debugStr + c;
                    }
                }
                debugStr = debugStr + "\n";
            }

            return debugStr;
        }

        private void ComputePlotsInRoom(int room, byte[][] plotData, List<AiMapNode> allPlotsList)
        {
            AiMapNode[] roomPlots = new AiMapNode[plotData.Length];

            for (int plotCtr = 0; plotCtr < plotData.Length; ++plotCtr)
            {
                byte[] plotValues = plotData[plotCtr];
                Plot newPlot = new Plot(allPlotsList.Count, room,
                    plotCtr, plotValues[1], plotValues[0], plotValues[3], plotValues[2]);
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

        /**
         * A castle has just been unlocked.  Tell the AI that you can 
         * now get from the outside of the castle to the inside.
         */
        public void ConnectPortcullisPlots(int outsideRoom, int insideRoom, bool open)
        {
            // Outside plot defined with {3,19,3,20},
            // Inside plot define with {0,16, 0,23}
            int outsidePlotindex = FindPlot(outsideRoom, 160, 112);
            // Just happen to know exactly which plots need to be connected
            AiMapNode outsidePlot = aiPlotsByRoom[outsideRoom][5];
            AiMapNode insidePlot = aiPlotsByRoom[insideRoom][0];
            outsidePlot.SetNeighbor(Plot.UP, (open ? insidePlot : null));
            insidePlot.SetNeighbor(Plot.DOWN, (open ? outsidePlot : null));
        }

        /**
         * Find the plot in the room that contains that (x,y)
         * @return the index into the aiPlotsByRoom[room] array of the desired node
         */
        private int FindPlot(int room, int x, int y)
        {
            int found = -1;
            AiMapNode[] plots = aiPlotsByRoom[room];
            for (int ctr = 0; (found < 0) && (ctr < plots.Length); ++ctr)
            {
                if (plots[ctr].thisPlot.Contains(x, y))
                {
                    found = plots[ctr].thisPlot.Key;
                }
            }
            return found;
        }

        /**
         * Find all plots that overlap this region
         */
        private int[] FindPlots(int room, int x, int y, int width, int height, bool abortAfterOne = false)
        {
            List<int> found = new List<int>(12); // Even the bridge can only touch 8 plots at once
            AiMapNode[] plots = aiPlotsByRoom[room];
            for (int ctr = 0; ctr < plots.Length; ++ctr)
            {
                if (plots[ctr].thisPlot.Touches(x, y, width, height))
                {
                    found.Add(plots[ctr].thisPlot.Key);
                    if (abortAfterOne)
                    {
                        break;
                    }
                }
            }
            return found.ToArray();
        }

        private AiPathNode ComputeNextStep(int goalPlot, List<AiPathNode> q, bool[] alreadyFound)
        {
            AiPathNode nextStep = q[0];
            q.RemoveAt(0);
            for (int ctr = 0; ctr < 4; ++ctr)
            {
                AiMapNode neighbor = nextStep.thisNode.neighbors[ctr];
                if ((neighbor != null) && !alreadyVisited[neighbor.thisPlot.Key])
                {
                    AiPathNode nextNextStep = nextStep.Prepend(neighbor, (ctr + 2) % 4);
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
            new byte[] {1,2,3,11},
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

        private static readonly byte[][] plotsRoomWithBottom =
        {
            new byte[] {0,16, 0,23},
            new byte[] {1,2,5,37}
        };

        private static readonly byte[][] plotsRoomWithBoth =
        {
            new byte[] {0,16, 0,23},
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

        private static readonly byte[][] plotsWhiteMazeEntry =
        {
            new byte[] {0,12,0,13},
            new byte[] {0,16,3,17},
            new byte[] {0,22,3,23},
            new byte[] {0,26,0,27},
            new byte[] {1,0,1,9},
            new byte[] {1,12,3,15},
            new byte[] {1,24,3,27},
            new byte[] {1,30,1,39},
            new byte[] {2,8,2,9},
            new byte[] {2,30,2,31},
            new byte[] {3,0,3,5},
            new byte[] {3,8,4,11},
            new byte[] {3,28,4,31},
            new byte[] {3,34,3,39},
            new byte[] {4,4,4,5},
            new byte[] {4,34,4,35},
            new byte[] {5,0,5,5},
            new byte[] {5,8,5,15},
            new byte[] {5,16,6,23},
            new byte[] {5,24,5,31},
            new byte[] {5,34,5,39}
        };

        private static readonly byte[][] plotsWhiteMazeMiddle =
        {
            new byte[] {0,4,0,5},
            new byte[] {0,8,2,9},
            new byte[] {0,12,2,13},
            new byte[] {0,16,1,17},
            new byte[] {0,22,0,23},
            new byte[] {0,26,2,27},
            new byte[] {0,30,2,31},
            new byte[] {0,34,0,35},
            new byte[] {1,0,1,5},
            new byte[] {1,18,1,23},
            new byte[] {1,34,1,39},
            new byte[] {3,0,3,9},
            new byte[] {3,12,3,17},
            new byte[] {3,18,3,27},
            new byte[] {3,30,3,39},
            new byte[] {4,4,4,9},
            new byte[] {4,16,6,17},
            new byte[] {4,22,6,23},
            new byte[] {4,30,4,35},
            new byte[] {5,0,5,13},
            new byte[] {5,26,5,39},
            new byte[] {6,12,6,13},
            new byte[] {6,26,6,27}
        };

        private static readonly byte[][] plotsWhiteMazeSide =
        {
            new byte[] {1,0,5,5},
            new byte[] {1,8,1,17},
            new byte[] {1,22,1,31},
            new byte[] {1,34,5,39},
            new byte[] {2,14,2,17},
            new byte[] {2,22,2,25},
            new byte[] {3,6,3,17},
            new byte[] {3,22,3,33},
            new byte[] {4,16,6,17},
            new byte[] {4,22,6,23},
            new byte[] {5,8,6,9},
            new byte[] {5,10,5,11},
            new byte[] {5,12,6,13},
            new byte[] {5,26,6,27},
            new byte[] {5,28,5,29},
            new byte[] {5,30,6,31},
            new byte[] {6,4,6,5},
            new byte[] {6,34,6,35}
        };

        private static readonly byte[][] plotsRedMaze1 =
        {
            new byte[] {0,4,0,5},
            new byte[] {0,8,0,9},
            new byte[] {0,16,1,17},
            new byte[] {0,22,1,23},
            new byte[] {0,30,0,31},
            new byte[] {0,34,0,35},
            new byte[] {1,4,1,9},
            new byte[] {1,12,2,13},
            new byte[] {1,26,2,27},
            new byte[] {1,30,1,35},
            new byte[] {2,16,4,19},
            new byte[] {2,20,4,23},
            new byte[] {3,0,3,13},
            new byte[] {3,26,3,39},
            new byte[] {5,0,5,19},
            new byte[] {5,20,5,39}
        };

        private static readonly byte[][] plotsRedMazeBottom =
        {
            new byte[] {0,16,0,23},
            new byte[] {1,0,1,39},
            new byte[] {2,12,3,27},
            new byte[] {3,0,3,5},
            new byte[] {3,8,6,9},
            new byte[] {3,30,6,31},
            new byte[] {3,34,3,39},
            new byte[] {4,4,6,5},
            new byte[] {4,34,6,35},
            new byte[] {5,10,5,19},
            new byte[] {5,20,5,29},
            new byte[] {6,16,6,17},
            new byte[] {6,22,6,23}
        };

        private static readonly byte[][] plotsRedMazeTop =
        {
            new byte[] {0,4,2,5},
            new byte[] {0,12,1,13},
            new byte[] {0,16,0,23},
            new byte[] {0,26,1,27},
            new byte[] {0,34,2,35},
            new byte[] {1,8,2,9},
            new byte[] {1,14,1,25},
            new byte[] {1,30,2,31},
            new byte[] {3,0,3,13},
            new byte[] {3,16,4,17},
            new byte[] {3,22,4,23},
            new byte[] {3,26,3,39},
            new byte[] {5,0,5,17},
            new byte[] {5,22,5,39}
        };

        private static readonly byte[][] plotsRedMazeEntry =
        {
            new byte[] {0,16,0,23},
            new byte[] {1,0,1,5},
            new byte[] {1,8,3,31},
            new byte[] {1,34,1,39},
            new byte[] {2,4,2,5},
            new byte[] {2,34,2,35},
            new byte[] {3,0,3,5},
            new byte[] {3,34,3,39},
            new byte[] {4,16,6,23},
            new byte[] {5,4,5,13},
            new byte[] {5,26,5,35},
            new byte[] {6,4,6,5},
            new byte[] {6,12,6,13},
            new byte[] {6,26,6,27},
            new byte[] {6,34,6,35}
        };

        private static readonly byte[][] plotsBlackMaze1 =
        {
            new byte[] {0,8,1,11},
            new byte[] {0,28,1,31},
            new byte[] {1,0,1,5},
            new byte[] {1,12,1,27},
            new byte[] {1,34,1,39},
            new byte[] {2,2,2,5},
            new byte[] {2,34,2,37},
            new byte[] {3,0,3,13},
            new byte[] {3,14,5,25},
            new byte[] {3,26,3,39},
            new byte[] {5,0,5,11},
            new byte[] {5,28,5,39},
            new byte[] {6,8,6,11},
            new byte[] {6,28,6,31}
        };

        private static readonly byte[][] plotsBlackMaze3 =
        {
            new byte[] {0,8,3,11},
            new byte[] {0,28,3,31},
            new byte[] {1,2,1,7},
            new byte[] {1,14,1,19},
            new byte[] {1,22,1,27},
            new byte[] {1,34,1,39},
            new byte[] {3,0,3,1},
            new byte[] {3,2,5,5},
            new byte[] {3,12,3,21},
            new byte[] {3,22,5,25},
            new byte[] {3,32,3,39},
            new byte[] {5,6,5,19},
            new byte[] {5,26,5,39},
            new byte[] {6,8,6,11},
            new byte[] {6,28,6,31}
        };

       private static readonly byte[][] plotsBlackMaze2 =
       {
            new byte[] {0,2,0,3},
            new byte[] {0,6,0,7},
            new byte[] {0,12,0,13},
            new byte[] {0,16,1,17},
            new byte[] {0,22,0,23},
            new byte[] {0,26,0,27},
            new byte[] {0,32,0,33},
            new byte[] {0,36,1,37},
            new byte[] {1,0,1,3},
            new byte[] {1,4,2,7},
            new byte[] {1,12,1,15},
            new byte[] {1,20,1,23},
            new byte[] {1,24,2,27},
            new byte[] {1,32,1,35},
            new byte[] {3,0,3,13},
            new byte[] {3,16,3,33},
            new byte[] {3,36,3,39},
            new byte[] {4,16,4,17},
            new byte[] {4,36,4,37},
            new byte[] {5,0,5,17},
            new byte[] {5,20,5,37}
        };

        private static readonly byte[][] plotsBlackMazeEntry =
        {
            new byte[] {0,16,3,23},
            new byte[] {1,0,1,15},
            new byte[] {1,24,1,39},
            new byte[] {3,0,3,15},
            new byte[] {3,24,3,39},
            new byte[] {4,18,4,23}, // Put this before {4,16,6,17}.  Makes a discontinuance work.
            new byte[] {4,16,6,17},
            new byte[] {5,0,5,3},
            new byte[] {5,6,5,13},
            new byte[] {5,22,6,23},
            new byte[] {5,26,5,33},
            new byte[] {5,36,5,39},
            new byte[] {6,2,6,3},
            new byte[] {6,6,6,7},
            new byte[] {6,12,6,13},
            new byte[] {6,26,6,27},
            new byte[] {6,32,6,33},
            new byte[] {6,36,6,37}
        };

    }

    /*
     * A plot is an open rectangle on the map with a unique id.
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
        private const int OVERLAP_EXTENT = BALL.MOVEMENT;

        public const int WALL_X_SCALE = 8;
        public const int WALL_Y_SCALE = 32;
        public static readonly int[] roomEdges = { 7 * WALL_Y_SCALE - 1, 40 * WALL_X_SCALE - 1, 0, 0 };

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
        private readonly int placeInRoom;
        public int PlaceInRoom
        {
            get { return placeInRoom; }
        }
        private readonly int[] edges;

        /**
         * Create a plot
         * inKey - the unique key of the plot
         * inRoom - the room that the plot is in
         * inLeft - the left edge of the plot USING WALL SCALE (so 0-39).  Will be converted to standard coordinates.
         * inBottom - the bottom edge of the plot USING WALL SCALED (so 0-6).  Will be converted to standard coordinates.
         * inRight - the right edge of the plot USING WALL SCALE (so 0-39).  Will be converted to standard coordinates.
         * inTop - the top edge of the plot USING WALL SCALE (so 0-6).  Will be converted to standard coordinates.
         */
        public Plot(int inKey, int inRoom, int inPlaceInRoom, int inLeft, int inBottom, int inRight, int inTop)
        {
            key = inKey;
            room = inRoom;
            placeInRoom = inPlaceInRoom;
            edges = new int[] { (inTop+1) * WALL_Y_SCALE - 1 , (inRight+1) * WALL_X_SCALE - 1,
                                 inBottom * WALL_Y_SCALE, inLeft * WALL_X_SCALE };
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
        public int MidX
        {
            get { return (edges[LEFT] + edges[RIGHT]) / 2; }
        }
        public int MidY
        {
            get { return (edges[UP] + edges[DOWN]) / 2; }
        }
        public int Height
        {
            get { return edges[UP] - edges[DOWN]; }
        }
        public int Width
        {
            get { return edges[RIGHT] - edges[LEFT]; }
        }
        public int TopWallScale
        {
            get { return (edges[UP]+1) / WALL_Y_SCALE - 1; }
        }
        public int BottomWallScale
        {
            get { return edges[DOWN] / WALL_Y_SCALE; }
        }
        public int LeftWallScale
        {
            get { return edges[LEFT] / WALL_X_SCALE; }
        }
        public int RightWallScale
        {
            get { return (edges[RIGHT]+1) / WALL_X_SCALE - 1; }
        }
        public RRect Rect
        {
            get { return new RRect(room, edges[LEFT], edges[UP], Width, Height); }
        }

        public int Edge(int direction)
        {
            return edges[direction % 4];
        }

        public bool equals(Plot other)
        {
            return this.key == other.key;
        }

        /**
         * Whether this plot touches the edge of the room
         * (consequently leading to another room).
         */
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

        public bool Touches(int x, int y, int width, int height)
        {
            return !((x > edges[RIGHT]) || (x + width -1 < edges[LEFT]) ||
                (y < edges[DOWN]) || (y - height + 1 > edges[UP])); 
        }
        public bool Touches(RRect rect)
        {
            return !((rect.left > edges[RIGHT]) || (rect.right < edges[LEFT]) ||
                (rect.top < edges[DOWN]) || (rect.bottom > edges[UP]));
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

        // If two plots are adjacent, this will return the endpoints of the line
        // segment marking where they are adjacent - it will be just inside the
        // other plot and adjacent to the this plot.
        // If the plots are not adjacent or are adjacent in the direction other
        // than the one specified, the return value is undefined.
        // If the plots are adjacent across a room switch it will still work.
        public void GetOverlapSegment(Plot otherPlot, int direction,
            ref int point1x, ref int point1y, ref int point2x, ref int point2y)
        {
            // We know the two edges overlap so to find the segment of intersection we put the four
            // endpoints in sorted order and make a segment between the middle two.
            int[] endpoints = new int[4]{ this.Edge(direction + 3), this.Edge(direction + 1),
                otherPlot.Edge(direction + 3), otherPlot.Edge(direction + 1)};
            Array.Sort(endpoints); // endpoints are point in slot 1 & 2

            switch (direction)
            {
                case UP:
                    point1x = endpoints[1];
                    point2x = endpoints[2];
                    point1y = point2y = this.Top + 1;
                    return;
                case DOWN:
                    point1x = endpoints[1];
                    point2x = endpoints[2];
                    point1y = point2y = this.Bottom - 1;
                    return;
                case LEFT:
                    point1y = endpoints[1];
                    point2y = endpoints[2];
                    point1x = point2x = this.Left - 1;
                    return;
                case RIGHT:
                default:
                    point1y = endpoints[1];
                    point2y = endpoints[2];
                    point1x = point2x = this.Right + 1;
                    return;
            }

        }

        // If two plots are adjacent, this will return the point just inside the
        // other plot and adjacent to the this plot.  The point will be the
        // midpoint of their intersection.
        // If the plots are not adjacent or are adjacent in the direction other
        // than the one specified, the return value is undefined.
        // If the plots are adjacent across a room switch it will still work.
        public void GetOverlapMidpoint(Plot otherPlot, int direction, ref int outX, ref int outY)
        {
            int point1x = 0, point1y = 0, point2x = 0, point2y = 0;
            GetOverlapSegment(otherPlot, direction, ref point1x, ref point1y, ref point2x, ref point2y);
            outX = (point1x + point2x) / 2;
            outY = (point1y + point2y) / 2;
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

    /**
     * A node in the network of plots that make up the whole adventure map.
     * A node represents a single plot and knows what other plots
     * neighbor it.
     */
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

    /**
     * A path represents the plots you have to get through to get from
     * one place to another on the board.  A path is represented as a linked
     * list of path nodes, so a path node can be just an individial node or
     * a whole path.
     * A path node refers to a plot (actuall the map node for that plot )
     * the direction to go to get to get to the next plot and a pointer
     * to the next path node in the path which will represent the next plot.
     */
    public class AiPathNode
    {
        public readonly AiMapNode thisNode;
        public readonly int nextDirection;
        public readonly AiPathNode nextNode;
        public readonly int distance; // Rough guess at the distance of this path

        /**
         * Construct a one hop path.
         * @param inPlot the plot that is the start and end of the path
         */
        public AiPathNode(AiMapNode inPlot)
        {
            thisNode = inPlot;
            nextDirection = Plot.NO_DIRECTION;
            nextNode = null;
            int height = thisNode.thisPlot.Height;
            int width = thisNode.thisPlot.Width;
            distance = (height > width ? height : width);
        }

        /**
         * Construct a new path by adding on to the front of an existing path.
         * @param inPlot the plot to start the path
         * @param inDirection the direction to go from the starting plot or 
         *   Plot.NO_DIRECTION if this path is a one hop path
         * @param inPath the rest of the path or null if this path is a one
         *   hop path
         */
        private AiPathNode(AiMapNode inPlot, int inDirection, AiPathNode inPath)
        {
            thisNode = inPlot;
            nextDirection = inDirection;
            nextNode = inPath;
            int height = thisNode.thisPlot.Height;
            int width = thisNode.thisPlot.Width;
            distance = (nextNode != null ? nextNode.distance : 0) +
                (height > width ? height : width);
        }

        /**
         * Construct a new path by adding on to the front of an existing path.
         * @param newStart the plot to start the path
         * @param firstDirection the direction to go from the starting plot
         */
        public AiPathNode Prepend(AiMapNode newStart, int firstDirection)
        {
            return new AiPathNode(newStart, firstDirection, this);
        }

        /**
         * Construct a new path by adding on to the end of an existing path.
         * @param newEnd the plot to end the path
         * @param lastDirection the direction to get from the second-to-last
         *   to the last step in the path.
         */
        public AiPathNode Append(int lastDirection, AiMapNode newEnd)
        {
            if (nextNode == null)
            {
                AiPathNode end = new AiPathNode(newEnd);
                return new AiPathNode(thisNode, lastDirection, end);
            } else
            {
                AiPathNode rest = nextNode.Append(lastDirection, newEnd);
                return new AiPathNode(thisNode, nextDirection, rest);
            }
        }

        /**
         * The map node for this point in the path
         */
        public Plot ThisPlot
        {
            get { return thisNode.thisPlot; }
        }

        /**
         * The last node in the path
         */
        public AiPathNode End
        {
            get { return nextNode == null ? this : nextNode.End; }
        }

        /**
         * Whether this path leads to a coordinate.
         * Looks only at whether the plot at the end of the path
         * contains the coordinate.  Does not look at any
         * intermedite plots in the path.
         */
        public bool leadsTo(int room, int x, int y)
        {
            return End.thisNode.thisPlot.RoughlyContains(room, x, y);
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
