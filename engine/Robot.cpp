
#include "Robot.hpp"
#include "Dragon.hpp"
#include "Ball.hpp"
#include "Sys.hpp"

/**
 * Whether the robot is on.
 */
bool Robot::isOn() {
    return false;
}

/**
 * Define the joystick moves to randomly move around the board
 */
void Robot::ControlJoystick(bool* left, bool* up, bool* right, bool* down, bool* fire) {
    static int lastXDirection = 0;
    static int lastYDirection = 0;
    const float DIRECTION_CHANGE_PROBABILITY = 0.01f;
    const float DROP_PROBABILITY = 0.0001f;
    
    if (isOn()) {
        float random = Sys::random();
        if (random < DIRECTION_CHANGE_PROBABILITY) {
            lastXDirection = (int)(Sys::random()*3)-1;
            lastYDirection = (int)(Sys::random()*3)-1;
        }
        *left = (lastXDirection < 0);
        *right = (lastXDirection > 0);
        *down = (lastYDirection < 0);
        *up = (lastYDirection > 0);
        
        random = Sys::random();
        *fire = (random < DROP_PROBABILITY);
    }
}

void Robot::ControlConsoleSwitches(bool* reset, Dragon** dragons, int numDragons, BALL* ball) {
    // TODO: DIfferent frequency for eaten vs. non-eaten
    const float RESET_PROBABILITY = 0.00001f;
    const float EATEN_RESET_PROBABILITY = 0.001f;
    
    if (isOn()) {
        bool eaten = false;
        for(int dragonCtr=0; dragonCtr<numDragons; ++dragonCtr) {
            Dragon* dragon = dragons[dragonCtr];
            eaten = eaten || ((dragon->state == Dragon::EATEN) && (dragon->eaten == ball));
        }
        float resetTarget = (eaten ? EATEN_RESET_PROBABILITY : RESET_PROBABILITY);
        float random = Sys::random();
        *reset = (random < resetTarget);
    }
}

