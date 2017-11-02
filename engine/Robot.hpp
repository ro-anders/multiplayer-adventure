
#ifndef Robot_hpp
#define Robot_hpp

#include <stdio.h>

class Dragon;
class BALL;

/**
 * A robot is used for endurance testing.  It randomly moves the player around the board, picking up and putting down objects.
 * Meant to leave running for hours on end to try to find heap corruption.
 */
class Robot {
public:
    
    /**
     * Whether the robot is on.
     */
    static bool isOn();
    
    /**
     * Define how the joystick moves when the robot is randomly moving the player around the board
     */
    static void ControlJoystick(bool* left, bool* up, bool* right, bool* down, bool* fire);

    /**
     * Define when to reset when the robot is randomly moving the player around the board
     */
    static void ControlConsoleSwitches(bool* reset, Dragon** dragons, int numDragons, BALL* ball);

};
#endif /* Robot_hpp */
