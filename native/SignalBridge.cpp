#include "LuauBridge.h"
#include <exception>
#include <string>

static constexpr int SignalTag = 2;
static constexpr int ConnectionTag = 3;
static char signalCacheKey;
static char connectionCacheKey;

static void initializeSignalType(lua_State* state, int tag, const char* typeName,
    const char* getOperation, const char* setOperation, void* cacheKey)
{
    lua_newtable(state);
    lua_rawsetp(state, LUA_REGISTRYINDEX, cacheKey);
    lua_newtable(state);
    woorf_push_method(state, getOperation);
    lua_setfield(state, -2, "__index");
    woorf_push_method(state, setOperation);
    lua_setfield(state, -2, "__newindex");
    lua_pushstring(state, typeName);
    lua_setfield(state, -2, "__type");
    lua_pushstring(state, "The metatable is locked");
    lua_setfield(state, -2, "__metatable");
    lua_setreadonly(state, -1, 1);
    lua_setuserdatametatable(state, tag);
}

void initializeSignals(lua_State* state)
{
    initializeSignalType(state, SignalTag, "RBXScriptSignal", "signal_get", "signal_set", &signalCacheKey);
    initializeSignalType(state, ConnectionTag, "RBXScriptConnection", "connection_get", "connection_set", &connectionCacheKey);
}

static void pushSignalObject(lua_State* state, int id, int tag, void* cacheKey)
{
    lua_rawgetp(state, LUA_REGISTRYINDEX, cacheKey);
    lua_rawgeti(state, -1, id);
    if (lua_isnil(state, -1))
    {
        lua_pop(state, 1);
        auto* storedId = static_cast<int*>(lua_newuserdatataggedwithmetatable(state, sizeof(int), tag));
        *storedId = id;
        lua_pushvalue(state, -1);
        lua_rawseti(state, -3, id);
    }
    lua_remove(state, -2);
}

WOORF_EXPORT void woorf_push_signal(lua_State* state, int id)
{
    pushSignalObject(state, id, SignalTag, &signalCacheKey);
}

WOORF_EXPORT void woorf_push_connection(lua_State* state, int id)
{
    pushSignalObject(state, id, ConnectionTag, &connectionCacheKey);
}

static int getSignalObjectId(lua_State* state, int index, int tag)
{
    auto* id = static_cast<int*>(lua_touserdatatagged(state, index, tag));
    return id ? *id : 0;
}

WOORF_EXPORT int woorf_get_signal_id(lua_State* state, int index)
{
    return getSignalObjectId(state, index, SignalTag);
}

WOORF_EXPORT int woorf_get_connection_id(lua_State* state, int index)
{
    return getSignalObjectId(state, index, ConnectionTag);
}

WOORF_EXPORT int woorf_ref_function(lua_State* state, int index)
{
    return lua_isfunction(state, index) ? lua_ref(state, index) : LUA_NOREF;
}

WOORF_EXPORT void woorf_unref_callback(void* handle, int reference)
{
    lua_unref(static_cast<Runtime*>(handle)->state, reference);
}

static int reportSignalFailure(Runtime* runtime, lua_State* thread)
{
    runtime->write("Signal callback failed: ", 24, 1);
    size_t length = 0;
    const char* error = lua_tolstring(thread, -1, &length);
    if (error)
        runtime->write(error, length, 1);
    else
        runtime->write("non-string error", 16, 1);
    runtime->write("\n", 1, 1);
    return 1;
}

static int pushSignalArguments(lua_State* thread, int count, int firstId, int secondId,
    const char* text, size_t length)
{
    if (text)
    {
        lua_pushlstring(thread, text, length);
        return 1;
    }
    if (count > 0)
        woorf_push_instance(thread, firstId);
    if (count > 1)
        woorf_push_instance(thread, secondId);
    return count;
}

WOORF_EXPORT int woorf_invoke(void* handle, int reference, int count,
    int firstId, int secondId, const char* text, size_t length)
{
    auto* runtime = static_cast<Runtime*>(handle);
    const int savedTop = lua_gettop(runtime->state);
    try
    {
        lua_State* thread = lua_newthread(runtime->state);
        lua_getglobal(thread, "_woorf_callback");
        lua_getref(thread, reference);
        const int argumentCount = pushSignalArguments(thread, count, firstId, secondId, text, length);
        const int status = lua_pcall(thread, argumentCount + 1, 0, 0);
        const int result = status == 0 ? 0 : reportSignalFailure(runtime, thread);
        lua_settop(runtime->state, savedTop);
        return result;
    }
    catch (const std::exception& error)
    {
        lua_settop(runtime->state, savedTop);
        const std::string message = std::string("Signal callback failed: ") + error.what() + "\n";
        runtime->write(message.data(), message.size(), 1);
        return 1;
    }
}
