#include <gtest/gtest.h>
#include <Users/carla/Desktop/Aletheia/TestApplication/solver.h>
#include <Users/carla/Desktop/Aletheia/TestApplication/main.cpp>


// Test Suite

int main(int argc, char** argv)
{
    testing::InitGoogleTest(&argc, argv);
    return RUN_ALL_TESTS();
}


TEST(GreatestNumber, GreatestNumber) {
    EXPECT_EQ(3, GreatestNumber(1, 2, 3));
    EXPECT_EQ(3, GreatestNumber(1, 3, 2));
    EXPECT_EQ(1, GreatestNumber(2, 1, 3));
    EXPECT_EQ(3, GreatestNumber(2, 3, 1));
    EXPECT_EQ(3, GreatestNumber(3, 1, 2));
    EXPECT_EQ(3, GreatestNumber(3, 2, 1));
}

TEST(Factorial, Factorial) {
    EXPECT_EQ(120, factorial(5));
    EXPECT_EQ(6, factorial(3));
    EXPECT_EQ(24, factorial(4));
}

TEST(FullName, FullName) {
    EXPECT_EQ("SAMPAIO, Carla", fullName("Carla", "Sampaio"));
    EXPECT_EQ("SAMPAIO, Tiago", fullName("Tiago", "Sampaio"));
}

TEST(ArrayBounds, ArrayBounds) {
    EXPECT_EQ(1, arrayBounds(0));
    EXPECT_EQ(1, arrayBounds(1));
}

TEST(MiddleNumber, MiddleNumber) {
    EXPECT_EQ(2, middleOfThree(1, 2, 3));
    EXPECT_EQ(2, middleOfThree(1, 3, 2));
    EXPECT_EQ(2, middleOfThree(3, 2, 1));
    EXPECT_EQ(3, middleOfThree(3, 1, 5));
}

TEST(Palindrome, Palindrome) {
    EXPECT_EQ(true, palindrome(333444333));
    EXPECT_EQ(true, palindrome(-121));
}


TEST(SolverTest, testConstructsWithProperArguments)
{

    try
    {
        Solver s1;
    }
    catch (std::invalid_argument const& excpt)
    {
        EXPECT_EQ(excpt.what(), GAME_FIELD_ERROR);
    }

    try
    {
        Solver s2;
    }
    catch (std::invalid_argument const& excpt)
    {
        EXPECT_EQ(excpt.what(), GAME_FIELD_ERROR);
    }
}

TEST(SolverTest, testIsEmpty)
{
    Solver s;
    std::vector<FieldType> v1{ FieldType::CIRCLE, FieldType::EMPTY };

    EXPECT_TRUE(s.isEmptyField(v1, 1));
}

TEST(SolverTest, testHasWinningSizeHorizontal)
{
    Solver s;

    std::vector<FieldType> v4{ FieldType::CIRCLE, FieldType::CIRCLE, FieldType::EMPTY,
                              FieldType::CIRCLE, FieldType::CIRCLE, FieldType::EMPTY,
                              FieldType::CROSS, FieldType::CROSS, FieldType::CROSS };

    EXPECT_FALSE(s.containsWinningSize(v4, FieldType::CROSS, 1, 0, 0, 1));
    EXPECT_TRUE(s.containsWinningSize(v4, FieldType::CROSS, 2, 0, 0, 1));
    EXPECT_FALSE(s.containsWinningSize(v4, FieldType::CIRCLE, 2, 0, 0, 1));
}

TEST(SolverTest, testHasWinningSizeVertical)
{
    Solver s;

    std::vector<FieldType> v4{ FieldType::CROSS, FieldType::CIRCLE, FieldType::CIRCLE,
                              FieldType::CIRCLE, FieldType::CROSS, FieldType::CIRCLE,
                              FieldType::EMPTY, FieldType::EMPTY, FieldType::CIRCLE };

    EXPECT_TRUE(s.containsWinningSize(v4, FieldType::CIRCLE, 0, 2, 1, 0));
    EXPECT_FALSE(s.containsWinningSize(v4, FieldType::CIRCLE, 0, 1, 1, 0));
    EXPECT_FALSE(s.containsWinningSize(v4, FieldType::CROSS, 0, 3, 1, 0));
}

TEST(SolverTest, testHasWinningSizeDiagonal)
{

    Solver s;
    std::vector<FieldType> v1{ FieldType::CIRCLE, FieldType::CROSS, FieldType::CIRCLE,
                              FieldType::CIRCLE, FieldType::CIRCLE, FieldType::CROSS,
                              FieldType::CIRCLE, FieldType::CROSS, FieldType::CIRCLE

    };
    EXPECT_TRUE(s.containsWinningSize(v1, FieldType::CIRCLE, 0, 0, 1, 1));
    EXPECT_TRUE(s.containsWinningSize(v1, FieldType::CIRCLE, 2, 0, -1, 1));
}

TEST(SolverTest, testIsInField)
{
    const int fieldSize = 3;
    Solver s;

    EXPECT_FALSE(s.isInField(0, fieldSize));
    EXPECT_FALSE(s.isInField(0, fieldSize));
    EXPECT_FALSE(s.isInField(-1, 0));
    EXPECT_FALSE(s.isInField(2, -1));

    EXPECT_TRUE(s.isInField(0, fieldSize - 1));
    EXPECT_TRUE(s.isInField(fieldSize - 1, 0));
}

TEST(SolverTest, testIsWinningField)
{

    Solver s;
    std::vector<FieldType> v1{ FieldType::CIRCLE, FieldType::CROSS, FieldType::CIRCLE,
                              FieldType::EMPTY, FieldType::CIRCLE, FieldType::CROSS,
                              FieldType::CROSS, FieldType::CROSS, FieldType::EMPTY };

    EXPECT_FALSE(s.isWinningField(v1, 3, FieldType::CIRCLE));

    EXPECT_TRUE(s.isWinningField(v1, 8, FieldType::CIRCLE));
    EXPECT_TRUE(s.isWinningField(v1, 8, FieldType::CROSS));

    std::vector<FieldType> v2{ FieldType::CIRCLE, FieldType::CROSS, FieldType::CIRCLE,
                              FieldType::EMPTY, FieldType::CIRCLE, FieldType::CROSS,
                              FieldType::EMPTY, FieldType::CROSS, FieldType::EMPTY };

    EXPECT_TRUE(s.isWinningField(v2, 6, FieldType::CIRCLE));
}

TEST(SolverTest, testSolving)
{
    Solver s;
    std::vector<FieldType> v1{ FieldType::CIRCLE, FieldType::CROSS, FieldType::CIRCLE,
                              FieldType::CIRCLE, FieldType::CIRCLE, FieldType::CROSS,
                              FieldType::CROSS, FieldType::CROSS, FieldType::EMPTY };

    int s1 = s.solve(v1, FieldType::CROSS, 4);
    EXPECT_EQ(-5, s1);
    EXPECT_EQ(8, s.getWinningIndex());
    int s2 = s.solve(v1, FieldType::CIRCLE, 4);
    EXPECT_EQ(-5, s2);
    EXPECT_EQ(8, s.getWinningIndex());

    std::vector<FieldType> v2{ FieldType::CIRCLE, FieldType::CROSS, FieldType::CIRCLE,
                              FieldType::EMPTY, FieldType::EMPTY, FieldType::CROSS,
                              FieldType::CROSS, FieldType::CROSS, FieldType::EMPTY };

    int s3 = s.solve(v2, FieldType::CROSS, 6);
    EXPECT_EQ(-3, s3);
    EXPECT_EQ(4, s.getWinningIndex());

    std::vector<FieldType> v3{ FieldType::CIRCLE, FieldType::CROSS, FieldType::CIRCLE,
                              FieldType::EMPTY, FieldType::CIRCLE, FieldType::CROSS,
                              FieldType::CROSS, FieldType::CROSS, FieldType::CIRCLE };
    EXPECT_GT(s.solve(v3, FieldType::CROSS, 8), 0); // losing game
}