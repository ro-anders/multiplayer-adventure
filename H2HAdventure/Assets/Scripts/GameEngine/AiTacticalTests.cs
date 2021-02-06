using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{

    public class AiTacticalTests
    {
        AiTactical toTest;
        OBJECT block;
        BALL ball;
        AiPathNode path;
        AiObjective obj;

        byte[][] blockGfx = { new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF } };

        public AiTacticalTests()
        {
            UnityEngine.Debug.Log("RUNNING AI TACTICAL TESTS!!!!!!!!!!");
            UnityEngine.Debug.Log("You probably want to disable these before release");
            Map map = new Map(2, Map.MAP_LAYOUT_SMALL, false, false);
            Board board = new Board(map, null);
            OBJECT key = new OBJECT("gold key", blockGfx, new byte[0], 0, COLOR.YELLOW, OBJECT.RandomizedLocations.OUT_IN_OPEN);
            board.addObject(Board.OBJECT_YELLOWKEY, key);
            Portcullis ballsPortcullis = new Portcullis("gold gate", Map.GOLD_CASTLE, map.getRoom(Map.GOLD_FOYER), key);
            board.addObject(Board.OBJECT_YELLOW_PORT, ballsPortcullis);
            block = new OBJECT("magnet", blockGfx, new byte[0], 0, COLOR.BLACK); // Magnet is 16 pixels wide x 16 pixels high
            board.addObject(Board.OBJECT_MAGNET, block);
            block.room = 1;

            ball = new BALL(0, ballsPortcullis, false, true);
            ball.room = 1;
            toTest = new AiTactical(ball, board);
        }

        public void testAll()
        {
            test1();
            test2();
        }

        private void test1()
        {
            // Ball is on left at bottom and exit is on right at bottom.
            // Plot is L96,B64,R191,T127.  Exit is 64-95 on right edge.
            // Ball is L110, T84
            AiPathNode endOfPath = new AiPathNode(new AiMapNode(new Plot(2, 1, 24, 2, 30, 2)));
            path = endOfPath.Prepend(new AiMapNode(new Plot(1, 1, 12, 2, 23, 3)), Plot.RIGHT);
            ball.x = 110;
            ball.y = 84;
            int velX = 0, velY = 0;
            int finalX = 208;
            int finalY = 80;
            obj = new GoToObjective(1, finalX, finalY, AiObjective.CARRY_NO_OBJECT);

            // 1.0: Room is empty.  Ball has a straight line right to exit
            block.setExists(false);
            toTest.computeDirectionOnPath(path, finalX, finalY, obj, ref velX, ref velY);
            if ((velX != 6) || (velY != 0))
            {
                throw new System.Exception("Failed test 1.0 with vel (" + velX + "," + velY + ")");
            }

            // 1.1: Block touches no walls and ball can go in any direction.  
            // Block is L120,B82,R135,T97
            ball.x = 110;
            ball.y = 88;
            block.setExists(true);
            block.x = 60;
            block.y = 48;
            toTest.computeDirectionOnPath(path, finalX, finalY, obj, ref velX, ref velY);
            if ((velX != 0) || /* either 6 or -6 is ok */ (velY == 0))
            {
                throw new System.Exception("Failed test 1.1 with vel (" + velX + "," + velY + ")");
            }

            // 1.2. Block touches bottom of plot.  Ball is on corner and must go clockwise to get to cut corner and exit on right.
            // Block is L120,B64,R135,T79
            ball.x = 110;
            ball.y = 84;
            block.x = 60;
            block.y = 39;
            toTest.computeDirectionOnPath(path, finalX, finalY, obj, ref velX, ref velY);
            if ((velX != 6) || (velY != 6))
            {
                throw new System.Exception("Failed test 1.2 with vel (" + velX + "," + velY + ")");
            }

            // 1.3. Block touches bottom of plot.  Ball is on left and must go clockwise to get to exit on right.
            // Block is L120,B64,R135,T79
            ball.x = 110;
            ball.y = 78;
            block.x = 60;
            block.y = 39;
            toTest.computeDirectionOnPath(path, finalX, finalY, obj, ref velX, ref velY);
            if ((velX != 0) || (velY != 6))
            {
                throw new System.Exception("Failed test 1.3 with vel (" + velX + "," + velY + ")");
            }

        }

        // Block is in a narrow corridor and can block access around both sides if things
        // are set up right.
        private void test2()
        {
            // Ball is on left and exit is on right 
            // Plot is L96,B64,R191,T95.  Exit is 64-95 on right edge.
            AiPathNode endOfPath = new AiPathNode(new AiMapNode(new Plot(2, 1, 24, 2, 30, 2)));
            path = endOfPath.Prepend(new AiMapNode(new Plot(1, 1, 12, 2, 23, 2)), Plot.RIGHT);
            int velX = 0, velY = 0;
            int finalX = 208;
            int finalY = 80;
            obj = new GoToObjective(1, finalX, finalY, AiObjective.CARRY_NO_OBJECT);

            // 2.0: Only 8 pixels between block and bottom.  Can get around that
            // way because ball starts in the right place.
            // Block is L120,B72,R135,T87
            block.setExists(true);
            block.x = 60;
            block.y = 43;
            ball.x = 110;
            ball.y = 83;
            toTest.computeDirectionOnPath(path, finalX, finalY, obj, ref velX, ref velY);
            if ((velX != 0) || (velY != -6))
            {
                throw new System.Exception("Failed test 2.0 with vel (" + velX + "," + velY + ")");
            }

            // 2.1: Only 8 pixels between block and bottom.  Can't get around that
            // way because ball starts in the wrong place.  With no other option, just plows forward.
            block.setExists(true);
            block.x = 60;
            block.y = 43; 
            ball.x = 110;
            ball.y = 85;
            toTest.computeDirectionOnPath(path, finalX, finalY, obj, ref velX, ref velY);
            if ((velX != 6) || (velY != -6))
            {
                throw new System.Exception("Failed test 2.1 with vel (" + velX + "," + velY + ")");
            }
        }
    }
}

