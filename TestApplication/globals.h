#ifndef TIC_TAC_TOE_GLOBALS_H
#define TIC_TAC_TOE_GLOBALS_H

#include <string>

const std::string GAME_FIELD_ERROR{ "Winning size does not fit into the field size!" };
const std::string GAME_TITLE{ "Tic Tac Toe" };
const std::string RES_ROOT_PATH{ "../resources/" };

const std::string IMAGE_P1{ "ham.bmp" };
const std::string IMAGE_P2{ "apple.bmp" };

constexpr int FIELD_SIZE{ 3 };
constexpr int WINNING_SIZE{ 3 };

enum class FieldType
{
    EMPTY,
    CROSS,
    CIRCLE
};

#endif