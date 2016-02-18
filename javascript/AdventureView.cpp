#include <stdio.h>
#include <SDL/SDL.h>

#ifdef __EMSCRIPTEN__
#include <emscripten.h>
#endif

static int SCREEN_HEIGHT = 256;
static int SCREEN_WIDTH = 256;
static int ctr = 0;
static int radius = 20;
static int x = SCREEN_WIDTH/2;;
static int xvel = 0;
static int y = SCREEN_HEIGHT/2;
static int yvel = 0;
static SDL_Surface *screen;
static   SDL_Event event;
static Uint32* buffer = new Uint32[SCREEN_WIDTH*SCREEN_HEIGHT];

extern "C" void checkKeyboard() {

    while( SDL_PollEvent( &event ) ){
        switch( event.type ){
            /* Look for a keypress */
            case SDL_KEYDOWN:
                switch( event.key.keysym.sym ){
                    case SDLK_LEFT:
                        xvel = -1;
                        break;
                    case SDLK_RIGHT:
                        xvel =  1;
                        break;
                    case SDLK_UP:
                        yvel = -1;
                        break;
                    case SDLK_DOWN:
                        yvel =  1;
                        break;
                    default:
                        break;
                }
                break;
            case SDL_KEYUP:
                switch( event.key.keysym.sym ){
                    case SDLK_LEFT:
                        if( xvel < 0 )
                            xvel = 0;
                        break;
                    case SDLK_RIGHT:
                        if( xvel > 0 )
                            xvel = 0;
                        break;
                    case SDLK_UP:
                        if( yvel < 0 )
                            yvel = 0;
                        break;
                    case SDLK_DOWN:
                        if( yvel > 0 )
                            yvel = 0;
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

void drawPixels1(int x1, int y1, int x2, int y2, int r, int g, int b) {
  int alpha = 255;
  if (SDL_MUSTLOCK(screen)) SDL_LockSurface(screen);
  for (int xctr = x1; xctr <= x2; xctr++) {
    for (int yctr = y1; yctr <= y2; yctr++) {
      *((Uint32*)screen->pixels + yctr*SCREEN_WIDTH + xctr) = SDL_MapRGBA(screen->format, r, g, b, alpha);
    }
  }
  if (SDL_MUSTLOCK(screen)) SDL_UnlockSurface(screen);
}

void finishDraw1() {
  // Does nothing.
}

void drawPixels2(int x1, int y1, int x2, int y2, int r, int g, int b) {
  int alpha = 255;
  for (int xctr = x1; xctr <= x2; xctr++) {
    for (int yctr = y1; yctr <= y2; yctr++) {
      buffer[yctr*SCREEN_WIDTH + xctr] = SDL_MapRGBA(screen->format, r, g, b, alpha);
    }
  }
}

void finishDraw2() {

  if (SDL_MUSTLOCK(screen)) SDL_LockSurface(screen);
  for (int i = 0; i < 256; i++) {
    for (int j = 0; j < 256; j++) {
      *((Uint32*)screen->pixels + i * SCREEN_WIDTH + j) = buffer[i * SCREEN_WIDTH + j];
    }
  }
  if (SDL_MUSTLOCK(screen)) SDL_UnlockSurface(screen);
}




extern "C" void one_iter() {

  drawPixels2(0, 0, SCREEN_WIDTH, SCREEN_HEIGHT, 128, 128, 128);
  
  // Want to loop 0-7 but not in straight incremental order
  int MULT = 9;
  int VERT_BLOCKS = 16;
  int BLOCK_HT = SCREEN_HEIGHT / VERT_BLOCKS;
  int HORZ_BLOCKS = 8; //(2 x log2(16))
  int BLOCK_WD = SCREEN_WIDTH / HORZ_BLOCKS;
  for(int i=0; i<VERT_BLOCKS; ++i) {
    int mi = (i*MULT)%VERT_BLOCKS;
    int mctr = ctr/60;
    for(int j=0; j<HORZ_BLOCKS/2; ++j) {
      if (mctr % 2 == 1) {
        // Draw two blocks
        drawPixels2(j*BLOCK_WD, i*BLOCK_HT, (j+1)*BLOCK_WD-1, (i+1)*BLOCK_HT-1, 0, 0, 0); // Left
        drawPixels2((HORZ_BLOCKS-j-1)*BLOCK_WD, i*BLOCK_HT, (HORZ_BLOCKS-j)*BLOCK_WD-1, (i+1)*BLOCK_HT-1, 0, 0, 0); // Right
      }
      mctr = mctr / 2;
    }
  }

  
  // Move the box
  checkKeyboard();
  x += xvel;
  y += yvel;
  drawPixels2(x-radius, y-radius, x+radius, y+radius, 50, 0, 128);

  finishDraw2();

  printf("Loop iteration #%f\n", ctr/60.0);
  ctr++;
}

extern "C" int main(int argc, char** argv) {
  printf("hello, world!\n");

  SDL_Init(SDL_INIT_VIDEO);
  screen = SDL_SetVideoMode(256, 256, 32, SDL_SWSURFACE);

#ifdef TEST_SDL_LOCK_OPTS
  EM_ASM("SDL.defaults.copyOnLock = false; SDL.defaults.discardOnLock = true; SDL.defaults.opaqueFrontBuffer = false;");
#endif

  if (SDL_MUSTLOCK(screen)) SDL_LockSurface(screen);
  for (int i = 0; i < 256; i++) {
    for (int j = 0; j < 256; j++) {
#ifdef TEST_SDL_LOCK_OPTS
      // Alpha behaves like in the browser, so write proper opaque pixels.
      int alpha = 255;
#else
      // To emulate native behavior with blitting to screen, alpha component is ignored. Test that it is so by outputting
      // data (and testing that it does get discarded)
      int alpha = (i+j) % 255;
#endif
      *((Uint32*)screen->pixels + i * SCREEN_WIDTH + j) = SDL_MapRGBA(screen->format, i, j, 255-i, alpha);
      buffer[i * SCREEN_WIDTH + j] = SDL_MapRGBA(screen->format, i, j, 255-i, alpha);
    }
  }
  if (SDL_MUSTLOCK(screen)) SDL_UnlockSurface(screen);
  SDL_Flip(screen); 

  emscripten_set_main_loop(one_iter, 60, 1);

  SDL_Quit();

  return 0;
}



