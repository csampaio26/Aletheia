#ifndef TIC_TAC_TOE_SOLVER_H
#define TIC_TAC_TOE_SOLVER_H

#include <vector>
#include <stdexcept>

#include "globals.h"

class Solver
{
private:
    int winningIndex;

    const int winningSize{ WINNING_SIZE };

    const int fieldSize{ FIELD_SIZE };

    std::vector<int> winningIndices;

    std::vector<FieldType> gameField;

    FieldType flipType(FieldType type);

    void dumpGameField(std::vector<FieldType>& gameFieldIn);

public:
    Solver() : gameField{ std::vector<FieldType>(fieldSize * fieldSize, FieldType::EMPTY) } {}

    bool isWinningField(std::vector<FieldType>& gameFieldIn, const int index, const FieldType type);

    bool isWinningField(const int index, const FieldType type);

    bool containsWinningSize(const std::vector<FieldType>& gameField, const FieldType type, int row, int col, const int rowIncrement, const int colIncrement);

    bool isInField(int row, int col);

    int solve(const FieldType type, const int moveCount);

    int solve(std::vector<FieldType>& gameField, const FieldType type, int moveCount);

    /* The Compiler might inline methods defined in the class */
    const int getWinningIndex() { return winningIndex; }

    const std::vector<int>& getWinningIndices() { return winningIndices; }

    const std::vector<FieldType>& getGameField() { return gameField; }

    void setFieldValue(const int index, const FieldType type) { gameField[index] = type; }

    const bool isEmptyField(const std::vector<FieldType>& gameFieldIn, const int index) { return (gameFieldIn[index] == FieldType::EMPTY); }

    bool isEmptyField(const int index) { return isEmptyField(gameField, index); }

    FieldType getFieldState(const int index) { return gameField[index]; }
};

#endif