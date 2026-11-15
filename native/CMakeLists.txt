cmake_minimum_required(VERSION 3.20)
project(WoorfLuau LANGUAGES CXX C)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)
set(LUAU_BUILD_CLI OFF CACHE BOOL "" FORCE)
set(LUAU_BUILD_TESTS OFF CACHE BOOL "" FORCE)
add_subdirectory(../vendor/luau luau EXCLUDE_FROM_ALL)
add_library(woorf_luau SHARED LuauBridge.cpp InstanceBridge.cpp SignalBridge.cpp ModuleBridge.cpp SchedulerBridge.cpp Vector3Bridge.cpp CFrameBridge.cpp ValueBridge.cpp)
target_compile_features(woorf_luau PRIVATE cxx_std_17)
target_link_libraries(woorf_luau PRIVATE Luau.Compiler Luau.VM)
if(WIN32)
    set_target_properties(woorf_luau PROPERTIES PREFIX "")
endif()
if(MINGW)
    target_link_options(woorf_luau PRIVATE -static-libgcc -static-libstdc++ -static)
endif()
set_target_properties(woorf_luau PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_SOURCE_DIR}/../artifacts/native"
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_SOURCE_DIR}/../artifacts/native")
foreach(config DEBUG RELEASE RELWITHDEBINFO MINSIZEREL)
    set_target_properties(woorf_luau PROPERTIES
        RUNTIME_OUTPUT_DIRECTORY_${config} "${CMAKE_SOURCE_DIR}/../artifacts/native"
        LIBRARY_OUTPUT_DIRECTORY_${config} "${CMAKE_SOURCE_DIR}/../artifacts/native")
endforeach()
