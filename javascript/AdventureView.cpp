#include <stdio.h>
#include <SDL/SDL.h>

#ifdef __EMSCRIPTEN__
#include <emscripten.h>
#endif


#include "../engine/Adventure.h"
#include "JSTransport.hpp"

static SDL_Surface *screen;
static   SDL_Event event;

static bool upPressed = false;
static bool downPressed = false;
static bool leftPressed = false;
static bool rightPressed = false;
static bool firePressed = false;

static Transport* transport;

void checkKeyboard() {

    while( SDL_PollEvent( &event ) ){
        switch( event.type ){
            /* Look for a keypress */
            case SDL_KEYDOWN:
                switch( event.key.keysym.sym ){
                    case SDLK_LEFT:
                        leftPressed = true;
                        break;
                    case SDLK_RIGHT:
                        rightPressed = true;
                        break;
                    case SDLK_UP:
                        upPressed = true;
                        break;
                    case SDLK_DOWN:
                        downPressed = true;
                        break;
                    case SDLK_SPACE:
                        firePressed = true;
                        break;
                    default:
                        break;
                }
                break;
            case SDL_KEYUP:
                switch( event.key.keysym.sym ){
                    case SDLK_LEFT:
                        leftPressed = false;
                        break;
                    case SDLK_RIGHT:
                        rightPressed = false;
                        break;
                    case SDLK_UP:
                        upPressed = false;
                        break;
                    case SDLK_DOWN:
                        downPressed = false;
                        break;
                    case SDLK_SPACE:
                        firePressed = false;
                        break;
                    default:
                        break;
                }
                break;
            
            default:
                break;
        }
    }
}


void one_iter() {
  Adventure_Run();
  printf(".\n");
}

extern "C" int main(int argc, char** argv) {

  printf("hello, world!\n");

  SDL_Init(SDL_INIT_VIDEO);
  screen = SDL_SetVideoMode(ADVENTURE_SCREEN_WIDTH, ADVENTURE_TOTAL_SCREEN_HEIGHT, 32, SDL_SWSURFACE);

  transport = new JSTransport();
  Adventure_Setup(2, 0, transport, 1, 0, 0);

  SDL_Flip(screen); 

  printf("Game Setup.\n");
  emscripten_set_main_loop(one_iter, 0, 1);

  SDL_Quit();

  return 0;
}

void Platform_PaintPixel(int r, int g, int b, int x, int y, int width/*=1*/, int height/*=1*/) {
  if (SDL_MUSTLOCK(screen)) SDL_LockSurface(screen);
  // PaintPixel is expecting a bottom up screen buffer, so flip the image
  int flipY = ADVENTURE_SCREEN_HEIGHT - y + ADVENTURE_OVERSCAN;
  for (int i = flipY; i > flipY-height; i--) {
    for (int j = x; j < x+width; j++) {
      *((Uint32*)screen->pixels + i *ADVENTURE_SCREEN_WIDTH + j) = SDL_MapRGBA(screen->format, r, g, b, 255);
    }
  }
  if (SDL_MUSTLOCK(screen)) SDL_UnlockSurface(screen);
}

void Platform_ReadJoystick(bool* left, bool* up, bool* right, bool* down, bool* fire) {
  *left = leftPressed;
  *up = upPressed;
  *right = rightPressed;
  *down = downPressed;
  *fire = firePressed;
}

void Platform_ReadConsoleSwitches(bool* reset) {
  // TODO: Implement
  *reset = false;
}

void Platform_ReadDifficultySwitches(int* left, int* right) {
  // TODO: Implement
  *left = 0;
  *right = 0;
}

void Platform_MuteSound(bool mute) {
  // TODO: Implement
}

void Platform_MakeSound(int sound, float volume) {
  // TODO: Implement
}

float Platform_Random() {
  // TODO: Implement
  return 0;
}




