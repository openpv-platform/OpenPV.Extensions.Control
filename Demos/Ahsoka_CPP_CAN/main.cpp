#include <string>
#include <iostream>
#include "AhsokaServices.h"
#include "CanTests.hpp"
#include <filesystem>

using namespace std;

const std::string pathToExecutable = "./CommandLine/Ahsoka.CommandLine";
 
int main(int argc, char *argv[])
{

    cout << endl << "Ahsoka CAN Tests" << endl << endl;

    CanTests canTest;
    canTest.RunTests(pathToExecutable);    

    return 0;
}


