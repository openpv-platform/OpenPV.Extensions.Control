cmake_minimum_required(VERSION 3.5)

project(Ahsoka.QT.CAN LANGUAGES CXX)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(CMAKE_AUTOUIC ON)
set(CMAKE_AUTOMOC ON)
set(CMAKE_AUTORCC ON)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
SET(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fPIC -pthread -Wno-psabi")

#Sets build output to source directory
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_SOURCE_DIR}/Outputs)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_SOURCE_DIR}/Outputs)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_SOURCE_DIR}/Outputs)

#Output executable links to local library
set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} -Wl,-rpath='$ORIGIN'")

find_package(Qt5 COMPONENTS Widgets REQUIRED)

FILE(GLOB Headers *.h)
FILE(GLOB Sources *.cpp *.hpp)
FILE(GLOB Forms *.ui)
FILE(GLOB Resources *.qrc)
File(GLOB Generated Generated/*.cpp)


# Beginning in OpenPV 3.0, Command Line Code Generators have been simplified and now execute as part of the
# SDK Installation so this the targets (ExportCanClasses) / properties (Can Generator Properties) below are no
# longer needed. With this change, the Developer Toolkit will generate your CAN Models as part of the SDK Update
# or after Changes to the CanConfiguration.

# NOTE: For developers that prefer to keep the model generation here as part of their local build system, you can set the
# condition below to True to continue using the older style code generation method.  You
# will need to disable the Code Generation in the Associated CAN Extension as well to avoid conflict


# This Command runs the Ahsoka CAN Code Generator to Generate CAN Model Objects
# Note we also copy the Can Calibration File to the Command Line Folder
# for use in debugging on the Developer Machine (Ubuntu)
if(false)
	add_custom_command(
	  OUTPUT ${CMAKE_CURRENT_SOURCE_DIR}/Generated/CANModels.generated.h ${CMAKE_CURRENT_SOURCE_DIR}/Generated/CANModels.generated.cpp
	  COMMAND ${CMAKE_SOURCE_DIR}/Ahsoka.Client.Shared/Ubuntu64/CommandLine/Ahsoka.CommandLine --GenerateCANClasses ${CMAKE_SOURCE_DIR}/OpenLinuxST.QT.PackageInfo.json ${CMAKE_SOURCE_DIR}/Generated/CANModels.generated.h "DEMO_CAN" "CanViewModelBase" "Cpp"
	  COMMAND ${CMAKE_COMMAND} -E copy ${CMAKE_SOURCE_DIR}/STDemoPackage.1.cancalibration.json ${CMAKE_SOURCE_DIR}/Ahsoka.Client.Shared/$ENV{AhsokaPlatform}/CommandLine/
	  DEPENDS ${SOURCE_FILES} ${CMAKE_SOURCE_DIR}/Ahsoka.Client.Shared/Ubuntu64/CommandLine/Ahsoka.CommandLine main.cpp
	  WORKING_DIRECTORY ${CMAKE_CURRENT_BINARY_DIR}
	)
endif()

#Add Ahsoka Library Subdirectory
add_subdirectory ("Ahsoka.Client.Shared/$ENV{AhsokaPlatform}/SDK")

# CREATE Executable called Ahsoka.QT.CAN
add_executable(Ahsoka.QT.CAN
    ${Sources}
    ${Forms}
    ${Resources}
    ${AhsokaSources}
    ${Generated}
)

File(GLOB Calibration *.cancalibration.json)
FILE(COPY ${Calibration} DESTINATION ${CMAKE_SOURCE_DIR}/Outputs/)

target_include_directories(Ahsoka.QT.CAN PRIVATE ${AhsokaHeaders})
target_link_libraries(Ahsoka.QT.CAN PRIVATE Qt5::Widgets ${AhsokaLibs})

add_ahsoka(Ahsoka.QT.CAN)

# Required to Deploy to the Target
install(DIRECTORY ${CMAKE_SOURCE_DIR}/Outputs/ DESTINATION Ahsoka.QT.CAN
    PATTERN "**/*"
              PERMISSIONS OWNER_READ OWNER_EXECUTE OWNER_WRITE
    )
