# CMakeList.txt : CMake project for Ahsoka.System.Client, include source and define
# project specific logic here.
#
cmake_minimum_required (VERSION 3.8)

project (Ahsoka.CPP.CAN LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
SET(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fPIC -pthread")

#Sets build output to source directory
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${AHSOKA_BUILD_OUTPUTS})
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${AHSOKA_BUILD_OUTPUTS})
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${AHSOKA_BUILD_OUTPUTS})

#Output executable links to local library
set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} -Wl,-rpath='$ORIGIN'")

#Add Ahsoka Library Subdirectory
add_subdirectory (".platform_support/$ENV{AhsokaPlatform}_TargetSupport/SDK")

message(CMAKE_SOURCE_DIR="${CMAKE_SOURCE_DIR}")
message(AhsokaSources="${AhsokaSources}")
File(GLOB Generated Generated/*.cpp)

add_executable(Ahsoka.CPP.CAN main.cpp ${Generated} ${AhsokaSources})

target_include_directories(Ahsoka.CPP.CAN PRIVATE ${AhsokaHeaders})
target_link_libraries(Ahsoka.CPP.CAN PRIVATE ${AhsokaLibs})

add_ahsoka(Ahsoka.CPP.CAN)

File(GLOB Calibration CANServiceConfiguration.json)
FILE(COPY ${Calibration} DESTINATION ${AHSOKA_BUILD_OUTPUTS})

# Required to Deploy to the Target
install(DIRECTORY ${AHSOKA_BUILD_OUTPUTS} DESTINATION Ahsoka.CPP.CAN
    PATTERN "**/*"
              PERMISSIONS OWNER_READ OWNER_EXECUTE OWNER_WRITE
    )