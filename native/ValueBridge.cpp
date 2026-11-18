#include "LuauBridge.h"
#include <exception>
#include <cstring>

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
    if (lua_touserdatatagged(state, index, 5)) return 12;
    if (lua_touserdatatagged(state, index, 6)) return 13;
    for (int kind = 0; kind < 4; ++kind)
        if (lua_touserdatatagged(state, index, 7 + kind)) return 14 + kind;
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

static int invokeValues(void* handle, int reference, const std::function<int(lua_State*)>& push)
{
    auto* runtime = static_cast<Runtime*>(handle);
    if (runtime->executionFailed) return 1;
    ExecutionScope execution(runtime);
    const int savedTop = lua_gettop(runtime->state);
    try
    {
        lua_State* thread = lua_newthread(runtime->state);
        lua_getglobal(thread, "_woorf_callback");
        lua_getref(thread, reference);
        const int count = push(thread);
        const int status = count < 0 ? 1 : lua_resume(thread, nullptr, count + 1);
        const int result = reportExecutionFailure(runtime) ? 1 : status == 0 ? 0 : reportValueFailure(runtime, thread);
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
WOORF_EXPORT int woorf_invoke_values(void* handle, int reference, PushArguments push)
{
    return invokeValues(handle, reference, push);
}

WOORF_EXPORT int woorf_invoke_values_browser(void* handle, int reference,
    int (*push)(void*, lua_State*), void* context)
{
    return invokeValues(handle, reference, [push, context](lua_State* state) { return push(context, state); });
}
WOORF_EXPORT int woorf_initialize_remote(void* handle, int id)
{
    auto* runtime = static_cast<Runtime*>(handle);
    const int savedTop = lua_gettop(runtime->state);
    lua_getglobal(runtime->state, "_woorf_remote_init");
    woorf_push_instance(runtime->state, id);
    const int status = lua_pcall(runtime->state, 1, 0, 0);
    if (status != 0) reportValueFailure(runtime, runtime->state);
    lua_settop(runtime->state, savedTop);
    return status;
}
WOORF_EXPORT int woorf_ref_shutdown(lua_State* state, int index)
{
    if (!lua_isfunction(state, index)) return -1;
    const int absolute = index > 0 ? index : lua_gettop(state) + index + 1;
    auto* root = getRuntime(state)->state;
    lua_getglobal(root, "_woorf_wrap_shutdown");
    if (root != state) lua_xmove(root, state, 1);
    lua_pushvalue(state, absolute);
    if (lua_pcall(state, 1, 1, 0) != 0) { lua_pop(state, 1); return -1; }
    const int reference = lua_ref(state, -1);
    lua_pop(state, 1);
    return reference;
}

WOORF_EXPORT int woorf_shutdown_pending(void* handle)
{
    auto* runtime = static_cast<Runtime*>(handle);
    lua_getglobal(runtime->state, "_woorf_shutdown_pending");
    const int status = lua_pcall(runtime->state, 0, 1, 0);
    const int pending = status == 0 ? lua_tointeger(runtime->state, -1) : 0;
    lua_pop(runtime->state, 1);
    return pending;
}
