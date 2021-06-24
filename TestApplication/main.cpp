#define _SILENCE_TR1_NAMESPACE_DEPRECATION_WARNING

#include <iostream>
#include <string>
#include <Users/carla/Desktop/Aletheia/TestApplication/solver.h>

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

bool palindrome(int num) {
    int n,  digit, rev = 0;

    n = num;

    do
    {
        digit = num % 10;
        rev = (rev * 10) + digit;
        num = num / 10;
    } while (num != 0);


    return n == rev;
}