#include "LuauBridge.h"
#include <exception>

using PushArguments = int (*)(lua_State*);

WOORF_EXPORT int woorf_value_kind(lua_State* state, int index)
{
    switch (lua_type(state, index))
    {
    case LUA_TNONE:
    case LUA_TNIL: return 0;
    case LUA_TBOOLEAN: return 1;
    case LUA_TSTRING: return 2;
    case LUA_TNUMBER: return 8;
    case LUA_TTABLE: return 9;
    case LUA_TVECTOR: return 10;
    case LUA_TFUNCTION: return 7;
    default: break;
    }
    if (lua_touserdatatagged(state, index, 1)) return 3;
    if (lua_touserdatatagged(state, index, 4)) return 11;
    return 4;
}

WOORF_EXPORT const void* woorf_table_identity(lua_State* state, int index) { return lua_topointer(state, index); }
WOORF_EXPORT void woorf_push_nil(lua_State* state) { lua_pushnil(state); }
WOORF_EXPORT void woorf_set_top(lua_State* state, int top) { lua_settop(state, top); }
WOORF_EXPORT int woorf_table_next(lua_State* state, int index) { return lua_next(state, index); }
WOORF_EXPORT void woorf_new_table(lua_State* state) { lua_newtable(state); }
WOORF_EXPORT void woorf_raw_set(lua_State* state, int index) { lua_rawset(state, index); }

static int reportValueFailure(Runtime* runtime, lua_State* thread)
{
    size_t length = 0;
    const char* error = lua_tolstring(thread, -1, &length);
    runtime->write("Signal callback failed: ", 24, 1);
    if (error) runtime->write(error, length, 1);
    else runtime->write("non-string error", 16, 1);
    runtime->write("\n", 1, 1);
    return 1;
}

WOORF_EXPORT int woorf_invoke_values(void* handle, int reference, PushArguments push)
{
    auto* runtime = static_cast<Runtime*>(handle);
    const int savedTop = lua_gettop(runtime->state);
    try
    {
        lua_State* thread = lua_newthread(runtime->state);
        lua_getglobal(thread, "_woorf_callback");
        lua_getref(thread, reference);
        const int count = push(thread);
        const int status = count < 0 ? 1 : lua_pcall(thread, count + 1, 0, 0);
        const int result = status == 0 ? 0 : reportValueFailure(runtime, thread);
        lua_settop(runtime->state, savedTop);
        return result;
    }
    catch (const std::exception& error)
    {
        const std::string message = std::string("Signal callback failed: ") + error.what() + "\n";
        runtime->write(message.data(), message.size(), 1);
        lua_settop(runtime->state, savedTop);
        return 1;
    }
}
