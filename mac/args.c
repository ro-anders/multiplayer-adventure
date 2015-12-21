
//#import <Foundation/Foundation.h>

#include "args.h"

static int argc;

static char** argv;

void Args_SetArgs(int inArgc, char* inArgv[]) {
    argc = inArgc;
    argv = inArgv;
}


void Args_GetArgs(int* outArgc, char*** outArgv) {
    *outArgc = argc;
    *outArgv = argv;
}



