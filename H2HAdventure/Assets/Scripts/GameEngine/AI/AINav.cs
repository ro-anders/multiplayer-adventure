using System;
using System.Collections.Generic;
using System.Linq;
using GameEngine;

namespace GameEngine.Ai
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
            ComputeZones();
            alreadyVisited = new bool[aiPlots.Length];
        }

        /**
         * Compute a path from one point to another.
         * @return a path from the from a plot containing the from point to
         * a plot containing the to point.  Or null if no such path exists.
         */
        public AiPathNode ComputePath(int fromRoom, int fromBX, int fromBY, int toRoom, int toBX, int toBY)
        {
            // Determine the starting plot
            int fromPlot = FindPlot(fromRoom, fromBX, fromBY);
            if (fromPlot < 0)
            {
                GameEngine.Logger.Error("Couldn't find starting plot");
                return null;
            }

            // Determine the ending plot
            int toPlot = FindPlot(toRoom, toBX, toBY);
            if (toPlot < 0)
            {
                GameEngine.Logger.Error("Couldn't find ending plot");
                return null;
            }

            if (aiPlots[fromPlot].thisPlot.Zone != aiPlots[toPlot].thisPlot.Zone)
            {
                return null;
            }

            ToPlotGoal goal = new ToPlotGoal(toPlot);
            AiPathNode path = this.BreadthFirstPathSearch(fromPlot, goal);
            return path;
        }

        /**
         * Compute path to get anywhere within a room.
         * @param startRoom the room of the starting point (not the room we want to get to)
         * @param startX the starting x position
         * @param startY the starting y position
         * @param toRoom the room that we want to get into
         */
        public AiPathNode ComputePathToRoom(int startRoom, int startX, int startY, int toRoom)
        {
            int fromPlot = FindPlot(startRoom, startX, startY);
            PathSearchGoal goal = new InRoomGoal(toRoom);
            AiPathNode path = BreadthFirstPathSearch(fromPlot, goal);
            return path;
        }

        /**
         * Compute path to get out of the current room.  Don't care where we end up
         * as long as it's not in that room.
         * @param startRoom the room of the starting point
         * @param startX the starting x position
         * @param startY the starting y position
         */
        public AiPathNode ComputePathOutOfRoom(int startRoom, int startX, int startY)
        {
            int fromPlot = FindPlot(startRoom, startX, startY);
            PathSearchGoal goal = new NotInRoomGoal(startRoom);
            AiPathNode path = BreadthFirstPathSearch(fromPlot, goal);
            return path;
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
            int fromPlot = FindPlot(startRoom, startX, startY);
            int[] toPlots = FindPlots(desiredArea.room, desiredArea.x, desiredArea.y, desiredArea.width, desiredArea.height);
            PathSearchGoal goal = new ToPlotsGoal(toPlots);
            AiPathNode path = BreadthFirstPathSearch(fromPlot, goal);
            return path;
        }

        /**
         * Look at all plots in a certain area, compute which one is closest and return a path
         * to that plot.
         * @param startRoom the room of the starting point (not the room with the desired region)
         * @param startX the starting x position
         * @param startY the starting y position
         * @param desiredArea the area of a room that the plot needs to touch
         */
        public AiPathNode ComputePathToAreas(int startRoom, int startX, int startY, RRect[] desiredAreas)
        {
            int fromPlot = FindPlot(startRoom, startX, startY);
            List<int> toPlotList = new List<int>();
            foreach (RRect nextArea in desiredAreas)
            {
                int[] toPlots = FindPlots(nextArea.room, nextArea.x, nextArea.y, nextArea.width, nextArea.height);
                toPlotList.AddRange(toPlots);
            }
            PathSearchGoal goal = new ToPlotsGoal(toPlotList.ToArray());
            AiPathNode path = BreadthFirstPathSearch(fromPlot, goal);
            return path;
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
                    AiPathNode path = ComputePath(startRoom, startX, fromy, roomWithExits, plot.MidBX, plot.MidBY);
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
         * Find all plots that overlap this region
         */
        public Plot[] GetPlots(int room, int bx, int by, int bwidth, int bheight)
        {
            int[] plotIndexes = FindPlots(room, bx, by, bwidth, bheight, false);
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
        public Plot[] GetPlots(in RRect brect)
        {
            return GetPlots(brect.room, brect.x, brect.y, brect.width, brect.height);
        }

        /**
         * Return what zone a rectangle is in or NO_ZONE if rectangle
         * touches no navigable plots.
         * @param brect an area to check (in standard/ball coordinates)
         * @param preferredZone if area spans multiple zones and 
         *   preferredZone is one of those zones, return preferredZone.
         *   Otherwise returns the zone that comes first in the enumeration.
         */
        public NavZone WhichZone(
            in RRect brect,
            NavZone preferredZone = NavZone.NO_ZONE)
        {
            // If the rectangle is in a room that is all one zone, this is easy
            ROOM room = map.getRoom(brect.room);
            if (room.zone != NavZone.NO_ZONE)
            {
                return room.zone;
            }

            // Need to figure out what plots it touches
            NavZone found = NavZone.NO_ZONE;
            int[] plotIndexes = FindPlots(brect.room,
                brect.x, brect.y, brect.width, brect.height, false);
            foreach (int plotIndex in plotIndexes)
            {
                NavZone plotZone = aiPlots[plotIndex].thisPlot.Zone;
                if ((preferredZone != NavZone.NO_ZONE) &&
                    (plotZone == preferredZone))
                {
                    return preferredZone;
                }
                if ((found == NavZone.NO_ZONE) || (plotZone < found))
                {
                    found = plotZone;
                }
            }
            return found;
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
                        GameEngine.Logger.Error("Player has fallen off the AI path!\n" +
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
            // Easiest to just hardcode

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
            ComputePlotsInRoom(Map.CRYSTAL_CASTLE, plotsCrystalCastle, allPlotsList);
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
            for (int wy = 6; wy >= 0; --wy)
            {
                for (int wx = 0; wx < 40; ++wx)
                {
                    int plot = FindPlot(roomNum, wx * Plot.WALL_BX_SCALE, wy * Plot.WALL_BY_SCALE);
                    if (plot < 0)
                    {
                        debugStr = debugStr + (room.walls[wx, wy] ? "█" : "?");
                    }
                    else if (room.walls[wx, wy])
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
         * Determine the zones of the map.
         */
        private void ComputeZones()
        {
            // Easiest to just hardcode.
            setZoneToAllConnectedPlots(aiPlotsByRoom[Map.GOLD_FOYER][0], NavZone.GOLD_CASTLE);
            setZoneToAllConnectedPlots(aiPlotsByRoom[Map.BLACK_FOYER][0], NavZone.BLACK_CASTLE);
            setZoneToAllConnectedPlots(aiPlotsByRoom[Map.RED_MAZE_1][0], NavZone.WHITE_CASTLE_1);
            setZoneToAllConnectedPlots(aiPlotsByRoom[Map.CRYSTAL_FOYER][0], NavZone.CRYSTAL_CASTLE);
            setZoneToAllConnectedPlots(aiPlotsByRoom[Map.COPPER_FOYER][0], NavZone.COPPER_CASTLE);
            setZoneToAllConnectedPlots(aiPlotsByRoom[Map.JADE_FOYER][0], NavZone.JADE_CASTLE);
            setZoneToAllConnectedPlots(aiPlotsByRoom[Map.RED_MAZE_4][0], NavZone.WHITE_CASTLE_2);
            setZoneToAllConnectedPlots(aiPlotsByRoom[Map.BLACK_MAZE_3][0], NavZone.DOT_LOCATION);
            setZoneToAllConnectedPlots(aiPlotsByRoom[Map.ROBINETT_ROOM][0], NavZone.ROBINETT_ROOM);
            setZoneToAllConnectedPlots(aiPlotsByRoom[Map.CRYSTAL_CASTLE][0], NavZone.CRYSTAL_GROUNDS);
            setZoneToAllConnectedPlots(aiPlotsByRoom[Map.GOLD_CASTLE][0], NavZone.MAIN);

            setZonesOnRooms();
        }

        /**
         * Figure out all plots that are reachable from this plot and put them
         * in the passed in zone.
         *  @param plotInZone the AiMapNode for a plot in a specific zone
         *  @param zone the zone the plot should be in
         */
        private void setZoneToAllConnectedPlots(AiMapNode plotInZone, NavZone zone)
        {
            // Create a set of plots in the zone and a second set
            // of plots on the edge (plots whose neighbors haven't
            // been added to the zone yet.
            // Then keep processing plots on the edge until there
            // are no more edge plots to process.
            HashSet<AiMapNode> allConnectedPlots = new HashSet<AiMapNode>();
            HashSet<AiMapNode> toProcess = new HashSet<AiMapNode>();
            toProcess.Add(plotInZone);
            while (toProcess.Count > 0)
            {
                // Process the next edge plot.  Set it's zone, add it to the
                // zone set and add all its neighbors that haven't been seen
                // yet to the edge set.
                HashSet<AiMapNode>.Enumerator enumerator = toProcess.GetEnumerator();
                enumerator.MoveNext();
                AiMapNode nextNode = enumerator.Current;
                toProcess.Remove(nextNode);
                nextNode.thisPlot.Zone = zone;
                bool isNew = allConnectedPlots.Add(nextNode);
                if (isNew)
                {
                    for(int ctr=Plot.FIRST_DIRECTION; ctr <= Plot.LAST_DIRECTION; ++ctr)
                    {
                        AiMapNode neighbor = nextNode.neighbors[ctr];
                        // Note, two plots are in the same zone if there is a TWO WAY
                        // connection between them.  This means the Robinett Room and the
                        // Main Hall are not connected.
                        if ((neighbor != null) && (neighbor.neighbors[Plot.OppositeDirection(ctr)] == nextNode))
                        {
                            toProcess.Add(nextNode.neighbors[ctr]);
                        }
                    }   
                }
            }
        }

        /**
         * Computes which rooms are in which zones.
         */
        private void setZonesOnRooms()
        {
            for (int roomCtr=0; roomCtr < Map.getNumRooms(); ++roomCtr)
            {
                AiMapNode[] aiPlots = this.aiPlotsByRoom[roomCtr];
                if (aiPlots.Length > 0)
                {
                    NavZone firstZone = aiPlots[0].thisPlot.Zone;
                    bool allSameZone = true;
                    for ( int plotCtr=0; allSameZone && plotCtr<aiPlots.Length; ++plotCtr)
                    {
                        allSameZone = aiPlots[plotCtr].thisPlot.Zone == firstZone;
                    }

                    ROOM nextRoom = map.getRoom(roomCtr);
                    nextRoom.zone = (allSameZone ? firstZone : NavZone.NO_ZONE);
                }
            }
        }

        /**
         * A castle has just been unlocked.  Tell the AI that you can 
         * now get from the outside of the castle to the inside.
         */
        public void ConnectPortcullisPlots(Portcullis gate, bool open)
        {
            // Outside plot defined with {3,19,3,20},
            // Inside plot define with {0,16,0,23}
            int outsidePlotindex = FindPlot(gate.room, 160, 112);
            // Just happen to know exactly which plots need to be connected
            AiMapNode outsidePlot = aiPlots[outsidePlotindex];
            AiMapNode insidePlot = aiPlotsByRoom[gate.insideRoom][0];
            outsidePlot.SetNeighbor(Plot.UP, (open ? insidePlot : null));
            insidePlot.SetNeighbor(Plot.DOWN, (open ? outsidePlot : null));

            // We also change the zone of all the plots inside the castle
            // (except for those that are their own zones)
            int[] insideRooms = gate.AllInsideRooms;
            NavZone from = (open ? gate.InsideZone : outsidePlot.thisPlot.Zone);
            NavZone to = (open ? outsidePlot.thisPlot.Zone : gate.InsideZone);
            foreach(int nextRoomCtr in insideRooms)
            {
                ROOM nextRoom = map.getRoom(nextRoomCtr);
                if (nextRoom.zone == from)
                {
                    nextRoom.zone = to;
                }
                foreach(AiMapNode nextNode in aiPlotsByRoom[nextRoomCtr])
                {
                    if (nextNode.thisPlot.Zone == from)
                    {
                        nextNode.thisPlot.Zone = to;
                    }
                }
            }


        }

        /**
         * Find the plot in the room that contains that (x,y)
         * @return the index into the aiPlotsByRoom[room] array of the desired node
         */
        private int FindPlot(int room, int bx, int by)
        {
            int found = -1;
            AiMapNode[] plots = aiPlotsByRoom[room];
            for (int ctr = 0; (found < 0) && (ctr < plots.Length); ++ctr)
            {
                if (plots[ctr].thisPlot.Contains(bx, by))
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

        /**
         * Do a breadth first search of all reachable plots until we find one that 
         * satisifies the goal.
         * @param startPlot the plot to start from
         * @param goal the goal that indicates when we have reached a plot that works
         * @return a path from the start plot to the plot that satisfies the goal
         *   or null if no path was found
         */
        private AiPathNode BreadthFirstPathSearch(int startPlot, PathSearchGoal goal)
        {
            // Make sure from plot is valid
            if (startPlot < 0)
            {
                return null;
            }

            AiMapNode startNode = aiPlots[startPlot];

            // Check to see if we're already there
            if (goal.Found(startNode.thisPlot))
            {
                return new AiPathNode(startNode);
            }

            // We use a queue of plots to process and a hash array of plots we have visited
            // (hash array allocated once but reset everytime).
            Array.Clear(alreadyVisited, 0, alreadyVisited.Length);
            alreadyVisited[startPlot] = true;
            List<AiPathNode> q = new List<AiPathNode>();
            q.Add(new AiPathNode(aiPlots[startPlot]));

            // Iterate through every reachable plot looking for the target plot
            // As we look, new reachable plots are added to the queue.
            // Continue until we find the target plot or run out of plots in the queue.
            AiPathNode found = null;
            while ((found == null) && (q.Count > 0))
            {
                found = ComputeNextStep(goal, q, alreadyVisited);
            }

            // It was more efficient to compute the reverse of the desired path,
            // so now that we've found it, reverse it.
            AiPathNode reversed = (found != null ? found.Reverse() : null);

            return reversed;
        }

        /**
         * This is one step in a breadth-first search function finding the path to
         * a plot.  If it finds a path during this step, returns a path.  If not,
         * returns null but updates the queue with more steps to take.
         * @param goal - the test to tell when we've found the plot we're looking for
         * @param q a queue of partial paths.  Take the next partial path off the queue, 
         *   compute all the paths that could extend it and add them back to the q.
         *   NOTE: these paths are all from current search point TO THE START PLOT (the reverse of what
         *   we will want)
         * @param alreadyFound - hasharray of already visited plots to prevent infinite loops
         * @return path from goal to start node (NOTE: WE NEED TO REVERSE THIS BEFORE IT'S USEFUL) or null
         * if no path found yet
         */
        private AiPathNode ComputeNextStep(PathSearchGoal goal, List<AiPathNode> q, bool[] alreadyFound)
        {
            AiPathNode nextStep = q[0];
            q.RemoveAt(0);
            for (int ctr = 0; ctr < 4; ++ctr)
            {
                AiMapNode neighbor = nextStep.thisNode.neighbors[ctr];
                if ((neighbor != null) && !alreadyVisited[neighbor.thisPlot.Key])
                {
                    AiPathNode nextNextStep = nextStep.Prepend(neighbor, Plot.OppositeDirection(ctr));
                    if (goal.Found(neighbor.thisPlot))
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

        private static readonly byte[][] plotsCrystalCastle =
        {
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

        //    new byte[] {2,14,2,17},
        //    new byte[] {2,22,2,25},
        //    new byte[] {3,6,3,17},
        //    new byte[] {3,22,3,33},
            new byte[] {2,14,3,17},
            new byte[] {2,22,3,25},
            new byte[] {3,6,3,13},
            new byte[] {3,26,3,33},

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
            new byte[] {0,4,1,5},
            new byte[] {0,8,0,9},
            new byte[] {0,16,1,17},
            new byte[] {0,22,1,23},
            new byte[] {0,30,0,31},
            new byte[] {0,34,1,35},
            new byte[] {1,6,1,9},
            new byte[] {1,12,2,13},
            new byte[] {1,26,2,27},
            new byte[] {1,30,1,33},
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
            new byte[] {0,16,0,23}, // First entry should be plot that portcullis leads to. 
            new byte[] {1,0,1,5},
            new byte[] {1,8,3,31},
            new byte[] {1,34,1,39},
            new byte[] {2,4,2,5},
            new byte[] {2,34,2,35},
            new byte[] {3,0,3,5},
            new byte[] {3,34,3,39},
            new byte[] {4,16,6,23},
            new byte[] {5,4,6,5},
            new byte[] {5,6,5,11},
            new byte[] {5,12,6,13},
            new byte[] {5,26,6,27},
            new byte[] {5,28,5,33},
            new byte[] {5,34,6,35}
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
            new byte[] {1,14,1,19}, // This is the plot with the dot.  Put it first.  We reference it later.
            new byte[] {0,8,3,11},
            new byte[] {0,28,3,31},
            new byte[] {1,2,1,7},
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

    public enum NavZone
    {
        NO_ZONE = -1,
        MAIN,
        GOLD_CASTLE,
        COPPER_CASTLE,
        JADE_CASTLE,
        BLACK_CASTLE,
        WHITE_CASTLE_1,
        WHITE_CASTLE_2,
        DOT_LOCATION,
        ROBINETT_ROOM,
        CRYSTAL_GROUNDS,
        CRYSTAL_CASTLE,
        NOT_PART_OF_GAME
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

        public static int OppositeDirection(int direction)
        {
            return (direction + 2) % 4; 
        }

        // When computing overlap, how far beyond the edge of the plot to shoot for
        private const int OVERLAP_EXTENT = BALL.MOVEMENT;

        public const int WALL_BX_SCALE = 8;
        public const int WALL_BY_SCALE = 32;
        public static readonly int[] roomEdges = { 7 * WALL_BY_SCALE - 1, 40 * WALL_BX_SCALE - 1, 0, 0 };

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
        private readonly int[] edgesb;

        private NavZone zone;
        public NavZone Zone
        {
            get { return zone; }
            set { zone = value; }
        }

        /**
         * Create a plot
         * inKey - the unique key of the plot
         * inRoom - the room that the plot is in
         * inPlaceInRoom - the index of this plot in the room's list of plots
         * inLeft - the left edge of the plot USING WALL SCALE (so 0-39).  Will be converted to ball coordinates.
         * inBottom - the bottom edge of the plot USING WALL SCALED (so 0-6).  Will be converted to ball coordinates.
         * inRight - the right edge of the plot USING WALL SCALE (so 0-39).  Will be converted to ball coordinates.
         * inTop - the top edge of the plot USING WALL SCALE (so 0-6).  Will be converted to ball coordinates.
         */
        public Plot(int inKey, int inRoom, int inPlaceInRoom,
            int inWLeft, int inWBottom, int inWRight, int inWTop,
            NavZone inZone = NavZone.NOT_PART_OF_GAME)
        {
            key = inKey;
            room = inRoom;
            zone = inZone;
            placeInRoom = inPlaceInRoom;
            edgesb = new int[] { (inWTop+1) * WALL_BY_SCALE - 1 , (inWRight+1) * WALL_BX_SCALE - 1,
                                 inWBottom * WALL_BY_SCALE, inWLeft * WALL_BX_SCALE };
        }

        public int BTop
        {
            get { return edgesb[UP]; }
        }
        public int BBottom
        {
            get { return edgesb[DOWN]; }
        }
        public int BLeft
        {
            get { return edgesb[LEFT]; }
        }
        public int BRight
        {
            get { return edgesb[RIGHT]; }
        }
        public int MidBX
        {
            get { return (edgesb[LEFT] + edgesb[RIGHT]) / 2; }
        }
        public int MidBY
        {
            get { return (edgesb[UP] + edgesb[DOWN]) / 2; }
        }
        public int BHeight
        {
            get { return edgesb[UP] - edgesb[DOWN] + 1; }
        }
        public int BWidth
        {
            get { return edgesb[RIGHT] - edgesb[LEFT] + 1; }
        }
        public int WTop
        {
            get { return (edgesb[UP]+1) / WALL_BY_SCALE - 1; }
        }
        public int WBottom
        {
            get { return edgesb[DOWN] / WALL_BY_SCALE; }
        }
        public int WLeft
        {
            get { return edgesb[LEFT] / WALL_BX_SCALE; }
        }
        public int WRight
        {
            get { return (edgesb[RIGHT]+1) / WALL_BX_SCALE - 1; }
        }
        public RRect BRect
        {
            get { return new RRect(room, edgesb[LEFT], edgesb[UP], BWidth, BHeight); }
        }

        public int Edge(int direction)
        {
            return edgesb[direction % 4];
        }

        /**
         * Two plots are considered equal if they share the same key
         */
        public bool equals(Plot other)
        {
            return this.key == other.key;
        }
        public override bool Equals(Object obj)
        {
            //Check for null and compare run-time types.
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                Plot p = (Plot)obj;
                return this.equals(p);
            }
        }

        /**
         * Implement a hash that returns the same value for equal plots.
         * Simply return the key.
         */
        public override int GetHashCode()
        {
            return key;
        }

        /**
         * Whether this plot touches the edge of the room
         * (consequently leading to another room).
         */
        public bool OnEdge(int direction)
        {
            direction = direction % 4;
            return edgesb[direction] == roomEdges[direction];
        }

        public bool Contains(int bx, int by)
        {
            return ((bx >= edgesb[LEFT]) && (bx <= edgesb[RIGHT]) &&
                    (by >= edgesb[DOWN]) && (by <= edgesb[UP]));
        }
        public bool Contains(int inRoom, int bx, int by)
        {
            return ((inRoom == room) && 
                    (bx >= edgesb[LEFT]) && (bx <= edgesb[RIGHT]) &&
                    (by >= edgesb[DOWN]) && (by <= edgesb[UP]));
        }
        public bool Contains(in RRect bRect)
        {
            return this.BRect.contains(bRect);
        }

        public bool Touches(int bx, int by, int bwidth, int bheight)
        {
            return !((bx > edgesb[RIGHT]) || (bx + bwidth -1 < edgesb[LEFT]) ||
                (by < edgesb[DOWN]) || (by - bheight + 1 > edgesb[UP])); 
        }
        public bool Touches(in RRect brect)
        {
            return !((brect.left > edgesb[RIGHT]) || (brect.right < edgesb[LEFT]) ||
                (brect.top < edgesb[DOWN]) || (brect.bottom > edgesb[UP]));
        }

        /**
         * Returns true if the plot contains the coordinates or they are very close
         */
        public bool RoughlyContains(int inRoom, int bx, int by)
        {
            return ((inRoom == room) &&
                    (bx >= edgesb[LEFT]-OVERLAP_EXTENT) && (bx <= edgesb[RIGHT] + OVERLAP_EXTENT) &&
                    (by >= edgesb[DOWN] - OVERLAP_EXTENT) && (by <= edgesb[UP] + OVERLAP_EXTENT));
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

        // If two plots are adjacent, this will return the line
        // segment marking where they are adjacent - it will be just outside this
        // plot but adjacent to it.
        // If the plots are not adjacent or are adjacent in the direction other
        // than the one specified, the return value is undefined.
        // If the plots are adjacent across a room switch this will work but is
        // returning a line segment just outside this plot's room (not a line
        // segment just inside the other plot's room)
        // A left/right segment will always have point1bx < point2bx and an
        // up/down segment will always have point1by < point2by.
        public RRect GetOverlapSegment(Plot otherPlot, int direction)
        {
            // We know the two edges overlap so to find the segment of intersection we put the four
            // endpoints in sorted order and make a segment between the middle two.
            int[] endpoints = new int[4]{ this.Edge(direction + 3), this.Edge(direction + 1),
                otherPlot.Edge(direction + 3), otherPlot.Edge(direction + 1)};
            Array.Sort(endpoints); // endpoints are point in slot 1 & 2

            switch (direction)
            {
                case UP:
                    return RRect.fromTRBL(otherPlot.room, this.BTop + 1, endpoints[2], this.BTop + 1, endpoints[1]);
                case DOWN:
                    return RRect.fromTRBL(otherPlot.room, this.BBottom - 1, endpoints[2], this.BBottom - 1, endpoints[1]);
                case LEFT:
                    return RRect.fromTRBL(otherPlot.room, endpoints[2], this.BLeft - 1, endpoints[1], this.BLeft - 1);
                case RIGHT:
                default:
                    return RRect.fromTRBL(otherPlot.room, endpoints[2], this.BRight + 1, endpoints[1], this.BRight + 1);
            }

        }

        public override string ToString()
        {
            return "[" + key + "=" + this.BRect + "]";
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
        public readonly Plot thisPlot;
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

        /**
         * Two map nodes are considered equal if the refer to the same plot
         */
        public override bool Equals(Object obj)
        {
            //Check for null and compare run-time types.
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                AiMapNode n = (AiMapNode)obj;
                return this.thisPlot.equals(n.thisPlot);
            }
        }

        /**
         * Implement a hash that returns the same value for equal map nodes.
         * Simply return the key.
         */
        public override int GetHashCode()
        {
            return thisPlot.GetHashCode();
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
            int height = thisNode.thisPlot.BHeight;
            int width = thisNode.thisPlot.BWidth;
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
            int height = thisNode.thisPlot.BHeight;
            int width = thisNode.thisPlot.BWidth;
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
            get
            {
                AiPathNode foundEnd = this;
                while (foundEnd.nextNode != null)
                {
                    foundEnd = foundEnd.nextNode;
                }
                return foundEnd;
            }
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

        /**
         * Create the reverse path of this path.
         * @return a path from this path's end to this path's start
         */
        public AiPathNode Reverse()
        {
            AiPathNode reversed = new AiPathNode(this.thisNode);
            AiPathNode next = this;
            while (next.nextNode != null)
            {
                reversed = reversed.Prepend(next.nextNode.thisNode, Plot.OppositeDirection(next.nextDirection));
                next = next.nextNode;
            }
            return reversed;
        }
    }

    public abstract class PathSearchGoal
    {
        public abstract bool Found(Plot plot);
    }

    /**
     * The goal when search for a path to a single plot
     */
    public class ToPlotGoal : PathSearchGoal
    {
        /** The key of the plot we want to get to */
        private int desiredPlot;

        /**
         * Create a goal of reaching a single plot
         * @param inDesiredPlot the plot we want to get to
         */
        public ToPlotGoal(int inDesiredPlot)
        {
            desiredPlot = inDesiredPlot;
        }

        public override bool Found(Plot plot)
        {
            return plot.Key == desiredPlot;
        }
    }

    /**
     * The goal when search for a path to a one of several plots
     */
    public class ToPlotsGoal : PathSearchGoal
    {
        /** The keys of the plots we want to get to */
        private int[] desiredPlots;

        /**
         * Create a goal of reaching a single plot
         * @param inDesiredPlot the plot we want to get to
         */
        public ToPlotsGoal(int[] inDesiredPlots)
        {
            desiredPlots = (int[])inDesiredPlots.Clone();
        }

        public override bool Found(Plot plot)
        {
            return desiredPlots.Contains(plot.Key);
        }
    }

    /**
     * The goal when search for a the nearest plot not in the current room
     */
    public class InRoomGoal : PathSearchGoal
    {
        /** The room we want to be in */
        private int room;

        /**
         * Create a goal of getting into a room
         * @param inRoom the room we want to get into
         */
        public InRoomGoal(int inRoom)
        {
            room = inRoom;
        }

        public override bool Found(Plot plot)
        {
            return plot.Room == room;
        }
    }

    /**
     * The goal when search for a the nearest plot not in the current room
     */
    public class NotInRoomGoal : PathSearchGoal
    {
        /** The room we don't want to be in */
        private int notInRoom;

        /**
         * Create a goal of getting out of a room
         * @param inNotInRoom the room we want to get out of
         */
        public NotInRoomGoal(int inNotInRoom)
        {
            notInRoom = inNotInRoom;
        }

        public override bool Found(Plot plot)
        {
            return plot.Room != notInRoom;
        }
    }


}
