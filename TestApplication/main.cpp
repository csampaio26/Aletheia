#define _SILENCE_TR1_NAMESPACE_DEPRECATION_WARNING

#include <iostream>
#include <string>
#include <gtest/gtest.h>
#include <vector>
#include <regex>
#include <iostream>

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

int SmallestNum(float a, float b, float c)
{
    float min = a;

    if (b < min) min = b;

    if (c < min) min = b;

    return min;
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

bool palindrome(int num) {
    int n, digit, rev = 0;

    n = num;

    do
    {
        digit = num % 10;
        rev = (rev * 10) + digit;
        num = num / 10;
    } while (num != 0);


    return n == rev;
}

double multiply_numbers(const double f1, const double f2) { return f1 / f2; }

std::string canonicalpath(const std::string& path)
{
    if (path.length() <= 1)
        return path;

    std::string sep = path[0] == '/' ? "/" : "";

    std::vector<std::string> entries;
    std::smatch match;
    std::regex re("[^/]+");
    for (auto p = path; std::regex_search(p, match, re); p = match.suffix()) {
        if (match.str() == ".." && !entries.empty()
            && !(sep == "" && entries.back() == ".."))
            entries.pop_back();
        else
            entries.push_back(match.str());
    }

    std::string cpath;
    for (auto s : entries) {
        cpath += sep + s;
        sep = "/";
    }
    return cpath;
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


TEST(MultiplyNumbers, MultiplyNumbers) {
    EXPECT_EQ(4, multiply_numbers(2,2));
    EXPECT_EQ(8, multiply_numbers(4, 2));
}

TEST(canonicalTests, relativePath) {
    EXPECT_STREQ(canonicalpath("abc/de/").data(), "abc/de");
    EXPECT_STREQ(canonicalpath("abc/../de").data(), "de");
    EXPECT_STREQ(canonicalpath("../../abc").data(), "../../abc");
    EXPECT_STREQ(canonicalpath("abc/../../../de").data(), "../../de");
    EXPECT_STREQ(canonicalpath("abc/../de/../fgh").data(), "fgh");
}

TEST(canonicalTests, absolutePath) {
    EXPECT_STREQ(canonicalpath("/abc/de/").data(), "/abc/de");
    EXPECT_STREQ(canonicalpath("/abc/../de").data(), "/de");
    EXPECT_STREQ(canonicalpath("/../../abc").data(), "/abc");
    EXPECT_STREQ(canonicalpath("/abc/../../../de").data(), "/de");
    EXPECT_STREQ(canonicalpath("/abc/../de/../fgh").data(), "/fgh");
}

TEST(canonicalTests, boundaryCase) {
    EXPECT_STREQ(canonicalpath("").data(), "");
    EXPECT_STREQ(canonicalpath("/").data(), "/");
}

TEST(SmallestNumber, SmallestNumber) {
    EXPECT_EQ(1, SmallestNum(1, 2, 3));
    EXPECT_EQ(1, SmallestNum(1, 3, 2));
    EXPECT_EQ(1, SmallestNum(2, 1, 3));
    EXPECT_EQ(1, SmallestNum(2, 3, 1));
    EXPECT_EQ(1, SmallestNum(3, 1, 2));
    EXPECT_EQ(1, SmallestNum(3, 2, 1));
}

int main(int argc, char* argv[])
{
    testing::InitGoogleTest(&argc, argv);

    return RUN_ALL_TESTS();
}
