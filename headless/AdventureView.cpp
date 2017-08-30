#include <stdio.h>

#include "../engine/Adventure.h"

extern "C" int main(int argc, char** argv) {
  printf("hello, world!\n");

  Adventure_Setup(2, 0, NULL, 1, 0, 0);

  while (1) {
    Adventure_Run();
  }

  return 0;
}

void Platform_PaintPixel(int r, int g, int b, int x, int y, int width, int height) {}
void Platform_ReadJoystick(bool* left, bool* up, bool* right, bool* down, bool* fire) {}
void Platform_ReadConsoleSwitches(bool* reset) {}
void Platform_ReadDifficultySwitches(int* left, int* right) {}
void Platform_MuteSound(bool mute) {}
void Platform_MakeSound(int sound, float volume) {}
float Platform_Random() {return 0;}
void Platform_DisplayStatus(const char* msg, int duration) {}




