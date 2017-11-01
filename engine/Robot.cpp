
#include "Robot.hpp"
#include "Sys.hpp"

/**
 * Whether the robot is on.
 */
bool Robot::isOn() {
    return true;
}

/**
 * Define the joystick moves to randomly move around the board
 */
void Robot::ControlJoystick(bool* left, bool* up, bool* right, bool* down, bool* fire) {
    static int lastXDirection = 0;
    static int lastYDirection = 0;
    const float DIRECTION_CHANGE_PROBABILITY = 0.01;
    const float DROP_PROBABILITY = 0.0001;
    
    if (isOn()) {
        float random = Sys::random();
        if (random < DIRECTION_CHANGE_PROBABILITY) {
            lastXDirection = ((int)Sys::random()*3)-1;
            lastYDirection = ((int)Sys::random()*3)-1;
        }
        *left = (lastXDirection < 0);
        *right = (lastXDirection > 0);
        *down = (lastYDirection < 0);
        *up = (lastYDirection > 0);
        
        random = Sys::random();
        *fire = (random < DROP_PROBABILITY);
    }
    
}
