#ifndef TIC_TAC_TOE_CONTROLLER_H
#define TIC_TAC_TOE_CONTROLLER_H

#include <SDL2/SDL.h>
#include "solver.h"
#include "view.h"

class Controller
{
private:
    int frameSize;
    View& view;
    Solver& solver;
    FieldType fieldTypeP1;
    FieldType fieldTypeP2;

    void waitForInput(bool& quit, bool& userPlayed);

    void makeFirstMove(Solver& solver, const FieldType type);

public:
    Controller(View& view, Solver& solver) : fieldTypeP1(FieldType::CIRCLE),
        fieldTypeP2(FieldType::CROSS), view(view),
        solver(solver), frameSize(FIELD_SIZE)
    {
    }

    void execute();
};

#endif