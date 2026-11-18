#include "LuauBridge.h"
#include "CompileSource.h"
#include <exception>

static int reportMessage(lua_State* state)
{
    size_t length = 0;
    const char* message = luaL_checklstring(state, 1, &length);
    auto* runtime = getRuntime(state);
    runtime->write(message, length, 1);
    runtime->write("\n", 1, 1);
    return 0;
}

static int readClock(lua_State* state)
{
    auto* runtime = getRuntime(state);
    lua_pushnumber(state, runtime->readClock ? runtime->readClock() : 0);
    return 1;
}

WOORF_EXPORT void woorf_set_clock(void* handle, ReadClock read)
{
    static_cast<Runtime*>(handle)->readClock = read;
}

static int newTypedProxy(lua_State* state)
{
    luaL_checktype(state, 1, LUA_TTABLE);
    lua_newuserdata(state, 1);
    lua_pushvalue(state, 1);
    lua_setmetatable(state, -2);
    return 1;
}

static int canYield(lua_State* state)
{
    lua_pushboolean(state, lua_isyieldable(state));
    return 1;
}

void initializeSchedulerHelpers(lua_State* state)
{
    lua_pushcfunction(state, canYield, "can yield");
    lua_setglobal(state, "_woorf_can_yield");
    lua_pushcfunction(state, newTypedProxy, "typed proxy");
    lua_setglobal(state, "_woorf_new_proxy");
    lua_pushcfunction(state, reportMessage, "report");
    lua_setglobal(state, "_woorf_report");
    lua_pushcfunction(state, readClock, "clock");
    lua_setglobal(state, "_woorf_now");
}

WOORF_EXPORT int woorf_initialize_scheduler(void* handle, const char* source, size_t length)
{
    auto* runtime = static_cast<Runtime*>(handle);
    const std::string bytecode = compileSource(std::string(source, length));
    int status = luau_load(runtime->state, "=WoorfScheduler", bytecode.data(), bytecode.size(), 0);
    if (status == 0) status = lua_pcall(runtime->state, 0, 0, 0);
    if (status != 0)
    {
        const char* error = lua_tostring(runtime->state, -1);
        runtime->write(error, std::char_traits<char>::length(error), 1);
        lua_pop(runtime->state, 1);
        return 1;
    }
    const char* sharedGlobals[] = {"_G", "shared"};
    for (const char* name : sharedGlobals)
    {
        lua_newtable(runtime->state);
        lua_setglobal(runtime->state, name);
    }

    luaL_sandbox(runtime->state);
    // Luau freezes global tables as libraries. Only these two hold script-owned data.
    for (const char* name : sharedGlobals)
    {
        lua_getglobal(runtime->state, name);
        lua_setreadonly(runtime->state, -1, false);
        lua_pop(runtime->state, 1);
    }
    return 0;
}

WOORF_EXPORT double woorf_step(void* handle, double now)
{
    auto* runtime = static_cast<Runtime*>(handle);
    if (runtime->executionFailed) return -1;
    ExecutionScope execution(runtime);
    const int savedTop = lua_gettop(runtime->state);
    auto* thread = lua_newthread(runtime->state);
    lua_getglobal(thread, "_woorf_step");
    lua_pushnumber(thread, now);
    const int status = lua_resume(thread, nullptr, 1);
    double deadline = -1;
    if (reportExecutionFailure(runtime)) { }
    else if (status == 0) deadline = lua_tonumber(thread, -1);
    else
    {
        const char* error = lua_tostring(thread, -1);
        if (error) runtime->write(error, std::char_traits<char>::length(error), 1);
        runtime->write("\n", 1, 1);
    }
    lua_settop(runtime->state, savedTop);
    return deadline;
}

WOORF_EXPORT int woorf_scheduler_failed(void* handle)
{
    auto* runtime = static_cast<Runtime*>(handle);
    if (runtime->executionFailed) return 1;
    lua_getglobal(runtime->state, "_woorf_scheduler_failed");
    const int status = lua_pcall(runtime->state, 0, 1, 0);
    const int failed = status != 0 || lua_toboolean(runtime->state, -1);
    lua_pop(runtime->state, 1);
    return failed;
}

WOORF_EXPORT int woorf_stop_script(void* handle, int id)
{
    auto* runtime = static_cast<Runtime*>(handle);
    if (runtime->executionFailed) return 1;
    ExecutionScope execution(runtime);
    const int top = lua_gettop(runtime->state);
    auto* thread = lua_newthread(runtime->state);
    lua_getglobal(thread, "_woorf_stop_script");
    woorf_push_instance(thread, id);
    const int status = lua_resume(thread, nullptr, 1);
    lua_settop(runtime->state, top);
    return status;
}
