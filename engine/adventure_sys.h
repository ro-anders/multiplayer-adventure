

#ifndef adventure_sys_h
#define adventure_sys_h

#include "sys.h"

// Some types
typedef unsigned long color;
typedef unsigned char byte;

// Screen characteristics
#define ADVENTURE_SCREEN_WIDTH              320
#define ADVENTURE_SCREEN_HEIGHT             192
#define ADVENTURE_OVERSCAN                  16
#define ADVENTURE_TOTAL_SCREEN_HEIGHT       (ADVENTURE_SCREEN_HEIGHT + ADVENTURE_OVERSCAN + ADVENTURE_OVERSCAN)
#define ADVENTURE_FRAME_PERIOD               0.017

#define ABS(X)                           ((X)>0?(X):-(X))


#endif /* adventure_sys_h */
