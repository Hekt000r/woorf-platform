#pragma once

#include "lua.h"
#include "lualib.h"
#include <cstddef>
#include <string>
#include <unordered_map>

#ifdef _WIN32
#define WOORF_EXPORT extern "C" __declspec(dllexport)
#else
#define WOORF_EXPORT extern "C" __attribute__((visibility("default")))
#endif

using WriteOutput = void (*)(const char*, size_t, int);
using HostCall = int (*)(lua_State*, const char*);
using ReadClock = double (*)();

struct ModuleResult
{
    bool isLoading = true;
    int reference = LUA_NOREF;
    std::string error;
};

struct Runtime
{
    lua_State* state = nullptr;
    WriteOutput write = nullptr;
    HostCall host = nullptr;
    ReadClock readClock = nullptr;
    std::unordered_map<int, ModuleResult> modules;

    ~Runtime()
    {
        if (state)
            lua_close(state);
    }
};

inline Runtime* getRuntime(lua_State* state)
{
    return static_cast<Runtime*>(lua_callbacks(state)->userdata);
}

void initializeInstances(lua_State* state, int gameId, int workspaceId);
void initializeVector3(lua_State* state);
void initializeCFrame(lua_State* state);
void initializeSignals(lua_State* state);
void initializeModules(lua_State* state);
void initializeSchedulerHelpers(lua_State* state);
WOORF_EXPORT int woorf_get_id(lua_State* state, int index);
WOORF_EXPORT void woorf_push_instance(lua_State* state, int id);
WOORF_EXPORT void woorf_push_method(lua_State* state, const char* operation);
