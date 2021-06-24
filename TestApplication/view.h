#ifndef TIC_TAC_TOE_VIEW_H
#define TIC_TAC_TOE_VIEW_H

#include <vector>
#include <SDL2/SDL.h>
#include "globals.h"

class View
{
private:
    int gridCellSize;
    int frameSize;
    int windowWidth;
    int windowHeight;

    SDL_Window* window;
    SDL_Renderer* renderer;

    SDL_Rect gridCursor;
    SDL_Rect gridCursorHoover;

    SDL_Color gridBackground;
    SDL_Color gridLineColor;
    SDL_Color gridCursorHooverColor;
    SDL_Color gridCursorColor;

    SDL_Surface* imageP1;
    SDL_Texture* textureP1;
    SDL_Surface* imageP2;
    SDL_Texture* textureP2;

public:
    View();

    ~View();

    int initialize();

    void drawGridLines();

    void drawGridState(const std::vector<FieldType>& gameField, const FieldType fieldTypeP1);

    void drawSolution(const std::vector<int>& indices);

    void waitForInput(bool& quit, bool& userPlayed);

    void update() { SDL_RenderPresent(renderer); }

    const int getSelectedIndex() { return (gridCursor.x / gridCellSize) + ((gridCursor.y / gridCellSize) * (frameSize)); }
};

#endif