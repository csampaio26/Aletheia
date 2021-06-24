#include <vector>
#include <iostream>
#include "view.h"

View::View() : gridCellSize(72), frameSize(FIELD_SIZE)
{
    windowWidth = (frameSize * gridCellSize) + 1;
    windowHeight = (frameSize * gridCellSize) + 1;
}

View::~View()
{
    SDL_DestroyTexture(textureP1);
    SDL_FreeSurface(imageP1);
    SDL_DestroyTexture(textureP2);
    SDL_FreeSurface(imageP2);
    SDL_DestroyRenderer(renderer);
    SDL_DestroyWindow(window);
    SDL_Quit();
}

/*
 * Grid Design adapted from
 * https://github.com/catsocks/sdl-grid
 */
int View::initialize()
{

    // Place the grid cursor in the middle of the screen.
    gridCursor = {
        .x = ((frameSize - 1) / 2 * gridCellSize),
        .y = ((frameSize - 1) / 2 * gridCellSize),
        .w = gridCellSize,
        .h = gridCellSize,
    };

    // The cursor ghost is a cursor that always shows in the cell below the
    // mouse cursor.
    gridCursorHoover = { gridCursor.x, gridCursor.y, gridCellSize,
                        gridCellSize };

    SDL_Color gridBackground = { 255, 255, 255, 255 };  // Barely Black
    SDL_Color gridLineColor = { 22, 22, 22, 255 };      // Dark grey
    SDL_Color gridCursorColor = { 255, 255, 255, 255 }; // White

    if (SDL_Init(SDL_INIT_VIDEO) < 0)
    {
        SDL_LogError(SDL_LOG_CATEGORY_APPLICATION, "Initialize SDL: %s",
            SDL_GetError());
        return EXIT_FAILURE;
    }

    if (SDL_CreateWindowAndRenderer(windowWidth, windowHeight, 0, &window,
        &renderer) < 0)
    {
        SDL_LogError(SDL_LOG_CATEGORY_APPLICATION,
            "Create window and renderer: %s", SDL_GetError());
        return EXIT_FAILURE;
    }

    SDL_SetWindowTitle(window, GAME_TITLE.c_str());

    // Draw grid background.
    SDL_SetRenderDrawColor(renderer, gridBackground.r, gridBackground.g,
        gridBackground.b, gridBackground.a);

    imageP1 = SDL_LoadBMP((RES_ROOT_PATH + IMAGE_P1).c_str());
    textureP1 = SDL_CreateTextureFromSurface(renderer, imageP1);

    imageP2 = SDL_LoadBMP((RES_ROOT_PATH + IMAGE_P2).c_str());
    textureP2 = SDL_CreateTextureFromSurface(renderer, imageP2);

    SDL_RenderClear(renderer);
    drawGridLines();
}

void View::drawGridLines()
{
    SDL_SetRenderDrawColor(renderer, gridLineColor.r, gridLineColor.g,
        gridLineColor.b, gridLineColor.a);
    for (int x = 0; x < 1 + frameSize * gridCellSize;
        x += gridCellSize)
    {
        SDL_RenderDrawLine(renderer, x, 0, x, windowHeight);
    }

    for (int y = 0; y < 1 + frameSize * gridCellSize;
        y += gridCellSize)
    {
        SDL_RenderDrawLine(renderer, 0, y, windowWidth, y);
    }
}

void View::drawGridState(const std::vector<FieldType>& gameField, const FieldType fieldTypeP1)
{

    gridCursor.x = 0;
    gridCursor.y = 0;

    for (int i = 0; i < frameSize * frameSize; i++)
    {

        int shiftX = i % frameSize;
        int shiftY = i / frameSize;
        gridCursor.x = (gridCellSize * shiftX);
        gridCursor.y = (gridCellSize * shiftY);

        if (gameField[i] != FieldType::EMPTY)
        {

            SDL_Rect dstrect = { gridCursor.x, gridCursor.y, gridCellSize, gridCellSize };
            if (gameField[i] == fieldTypeP1)
            {
                SDL_RenderCopy(renderer, textureP1, NULL, &dstrect);
            }
            else
            {
                SDL_RenderCopy(renderer, textureP2, NULL, &dstrect);
            }

            SDL_RenderPresent(renderer);
        }
        else
        {

            SDL_SetRenderDrawColor(renderer, 255,
                255, 255,
                gridCursorColor.a);
            SDL_RenderFillRect(renderer, &gridCursor);
            SDL_RenderPresent(renderer);
        }
    }
}

void View::drawSolution(const std::vector<int>& indices)
{
    gridCursor.x = 0;
    gridCursor.y = 0;
    for (auto i : indices)
    {
        if (i >= 0)
        {
            int shiftX = i % frameSize;
            int shiftY = i / frameSize;
            gridCursor.x = (gridCellSize * shiftX);
            gridCursor.y = (gridCellSize * shiftY);

            SDL_SetRenderDrawColor(renderer, 135, 206, 250,
                gridCursorColor.a);
            SDL_RenderFillRect(renderer, &gridCursor);
            SDL_RenderPresent(renderer);
        }
    }
}

void View::waitForInput(bool& quit, bool& userPlayed)
{
    SDL_Event event;
    while (SDL_PollEvent(&event))
    {

        switch (event.type)
        {

        case SDL_MOUSEBUTTONDOWN:

            userPlayed = true;
            gridCursor.x = (event.motion.x / gridCellSize) * gridCellSize;
            gridCursor.y = (event.motion.y / gridCellSize) * gridCellSize;
            break;
        case SDL_QUIT:

            quit = true;
            break;
        }
    }
}