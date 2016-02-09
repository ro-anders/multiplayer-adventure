#include <stdio.h>
#include <SDL/SDL.h>

#ifdef __EMSCRIPTEN__
#include <emscripten.h>
#endif

static int ctr = 0;
static int radius = 20;
static int x = 128;
static int xvel = 0;
static int y = 128;
static int yvel = 0;
static SDL_Surface *screen;
static   SDL_Event event;

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


extern "C" void one_iter() {
  
  // Move the box
  checkKeyboard();
  x += xvel;
  y += yvel;

  // We draw a gray/flashing box at (x,y).  The rest of the screen is blue.
  if (SDL_MUSTLOCK(screen)) SDL_LockSurface(screen);
  for (int i = 0; i < 256; i++) {
    for (int j = 0; j < 256; j++) {
      int alpha = 255;
      int r = 0;
      int g = 0;
      int b = 255;
      if ((i > y-radius) && (i < y+radius) && (j > x-radius) && (j < x+radius)) {
        r = g = b = ctr;
      }
      *((Uint32*)screen->pixels + i *256 + j) = SDL_MapRGBA(screen->format, r, g, b, alpha);
    }
  }
  if (SDL_MUSTLOCK(screen)) SDL_UnlockSurface(screen);

  printf("Loop iteration #%d\n", ctr);
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
      *((Uint32*)screen->pixels + i * 256 + j) = SDL_MapRGBA(screen->format, i, j, 255-i, alpha);
    }
  }
  if (SDL_MUSTLOCK(screen)) SDL_UnlockSurface(screen);
  SDL_Flip(screen); 

  printf("you should see a smoothly-colored square - no sharp lines but the square borders!\n");
  printf("and here is some text that should be HTML-friendly: amp: |&| double-quote: |\"| quote: |'| less-than, greater-than, html-like tags: |<cheez></cheez>|\nanother line.\n");

  emscripten_set_main_loop(one_iter, 60, 1);

  SDL_Quit();

  return 0;
}



