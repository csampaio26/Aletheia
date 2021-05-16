#define _SILENCE_TR1_NAMESPACE_DEPRECATION_WARNING

#include <iostream>
#include <gtest/gtest.h>

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
            greatest = num1;
        }
    }
    else if (num2 > num3)
        greatest = num2;
    else
        greatest = num3;

    return greatest;
}

int main(int argc, char* argv[])
{
	testing::InitGoogleTest(&argc, argv);

	return RUN_ALL_TESTS();
}

TEST(M1, InputGreatestNumber0) {
	EXPECT_EQ(3, GreatestNumber(1,2,3));
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