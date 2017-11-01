
#ifndef Robot_hpp
#define Robot_hpp

#include <stdio.h>

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
     * Define the joystick moves to randomly move around the board
     */
    static void ControlJoystick(bool* left, bool* up, bool* right, bool* down, bool* fire);

    
};
#endif /* Robot_hpp */
