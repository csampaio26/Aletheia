#define _SILENCE_TR1_NAMESPACE_DEPRECATION_WARNING

#include <iostream>
#include <gtest/gtest.h>
#include <string>

using namespace std;

int GreatestNumber(int num1, int num2, int num3)
{
    int  greatest;

    if (num1 > num2)
    {
        if (num1 > num3)
        {
            greatest = num1;
        }
        else
        {
            greatest = num1; //bug
        }
    }
    else if (num2 > num3)
        greatest = num2;
    else
        greatest = num3;

    return greatest;
}

int middleOfThree(int a, int b, int c)
{
    if ((a < b && b < c) || (c < b && b < a))
        return b;

    else if ((b < a && a < c) || (c < a && a < b))
        return b; //bug

    else
        return c;
}


int factorial(int n)
{
    if (n > 1)
        return n * factorial(n - 1);
    else
        return 1;
}

string fullName(string firstName, string lastName) {
   string fullname = lastName + ", " + firstName;

    for (string::size_type i = 0; i < lastName.length(); i++)
    {
        fullname[i] = toupper(fullname[i]);
    }

    return fullname;
}

int arrayBounds(int n)
{
    int arr[] = { 1,2,3,4,5 };
    return arr[n];
}


int main(int argc, char* argv[])
{
    testing::InitGoogleTest(&argc, argv);

    return RUN_ALL_TESTS();
}

TEST(M1, InputGreatestNumber0) {
    EXPECT_EQ(3, GreatestNumber(1, 2, 3));
}

TEST(M2, InputGreatestNumber1) {
    EXPECT_EQ(3, GreatestNumber(1, 3, 2));
}

TEST(M3, InputGreatestNumber2) {
    EXPECT_EQ(1, GreatestNumber(2, 1, 3));
}

TEST(M4, InputGreatestNumber3) {
    EXPECT_EQ(3, GreatestNumber(2, 3, 1));
}

TEST(M5, InputGreatestNumber4) {
    EXPECT_EQ(3, GreatestNumber(3, 1, 2));
}

TEST(M6, InputGreatestNumber5) {
    EXPECT_EQ(3, GreatestNumber(3, 2, 1));
}

TEST(M7, Factorial5) {
    EXPECT_EQ(120, factorial(5));
}

TEST(M8, Factorial3) {
    EXPECT_EQ(6, factorial(3));
}

TEST(M9, Factorial4) {
    EXPECT_EQ(24, factorial(4));
}

TEST(M10, FullName1) {
    EXPECT_EQ("SAMPAIO, Carla", fullName("Carla", "Sampaio"));
}

TEST(M11, FullName2) {
    EXPECT_EQ("SAMPAIO, Tiago", fullName("Tiago", "Sampaio"));
}

TEST(M12, ArrayBounds1) {
    EXPECT_EQ(1, arrayBounds(0));
}

TEST(M13, ArrayBounds2) {
    EXPECT_EQ(1, arrayBounds(1));
}

TEST(M14, Middle1) {
    EXPECT_EQ(2, middleOfThree(1,2,3));
}

TEST(M15, Middle2) {
    EXPECT_EQ(2, middleOfThree(1, 3, 2));
}

TEST(M16, Middle3) {
    EXPECT_EQ(2, middleOfThree(3, 2, 1));
}

TEST(M17, Middle4) {
    EXPECT_EQ(3, middleOfThree(3, 1, 5));
}