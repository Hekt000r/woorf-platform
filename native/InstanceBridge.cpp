#include "LuauBridge.h"

static constexpr int InstanceTag = 1;
static char instanceCacheKey;

static int dispatchHost(lua_State* state)
{
    const char* operation = lua_tostring(state, lua_upvalueindex(1));
    if ((std::string(operation) == "get" || std::string(operation) == "set")
        && lua_type(state, 2) == LUA_TSTRING)
    {
        const std::string property = lua_tostring(state, 2);
        if (property == "OnServerInvoke" || property == "OnClientInvoke")
        {
            const int count = lua_gettop(state);
            lua_getglobal(state, std::string(operation) == "get" ? "_woorf_remote_get" : "_woorf_remote_set");
            lua_insert(state, 1);
            lua_call(state, count, LUA_MULTRET);
            return lua_gettop(state);
        }
    }
    const int result = getRuntime(state)->host(state, operation);
    // Managed exceptions are converted to stack strings before returning here.
    if (result < 0)
        lua_error(state);
    return result;
}

WOORF_EXPORT void woorf_push_method(lua_State* state, const char* operation)
{
    if (std::string(operation) == "AddItem" || std::string(operation) == "Raycast")
    {
        lua_getglobal(state, std::string(operation) == "AddItem" ? "_woorf_add_item" : "_woorf_raycast");
        return;
    }
    if (std::string(operation) == "InvokeServer" || std::string(operation) == "InvokeClient")
    {
        lua_getglobal(state, std::string(operation) == "InvokeServer" ? "_woorf_invoke_server" : "_woorf_invoke_client");
        return;
    }
    if (std::string(operation) == "Wait" || std::string(operation) == "WaitForChild")
    {
        lua_getglobal(state, std::string(operation) == "Wait" ? "_woorf_signal_wait" : "_woorf_wait_for_child");
        return;
    }
    lua_pushstring(state, operation);
    lua_pushcclosure(state, dispatchHost, "Woorf.Instance", 1);
}

WOORF_EXPORT void woorf_push_instance(lua_State* state, int id)
{
    if (id == 0)
    {
        lua_pushnil(state);
        return;
    }
    lua_rawgetp(state, LUA_REGISTRYINDEX, &instanceCacheKey);
    lua_rawgeti(state, -1, id);
    if (lua_isnil(state, -1))
    {
        lua_pop(state, 1);
        auto* storedId = static_cast<int*>(lua_newuserdatataggedwithmetatable(state, sizeof(int), InstanceTag));
        *storedId = id;
        lua_pushvalue(state, -1);
        lua_rawseti(state, -3, id);
    }
    lua_remove(state, -2);
}

void initializeInstances(lua_State* state, int gameId, int workspaceId)
{
    lua_newtable(state);
    lua_rawsetp(state, LUA_REGISTRYINDEX, &instanceCacheKey);
    lua_newtable(state);
    woorf_push_method(state, "get");
    lua_setfield(state, -2, "__index");
    woorf_push_method(state, "set");
    lua_setfield(state, -2, "__newindex");
    woorf_push_method(state, "tostring");
    lua_setfield(state, -2, "__tostring");
    lua_pushstring(state, "Instance");
    lua_setfield(state, -2, "__type");
    lua_pushstring(state, "The metatable is locked");
    lua_setfield(state, -2, "__metatable");
    lua_setreadonly(state, -1, 1);
    lua_setuserdatametatable(state, InstanceTag);
    woorf_push_instance(state, gameId);
    lua_setglobal(state, "game");
    woorf_push_instance(state, workspaceId);
    lua_setglobal(state, "workspace");
    lua_newtable(state);
    woorf_push_method(state, "new");
    lua_setfield(state, -2, "new");
    lua_setreadonly(state, -1, 1);
    lua_setglobal(state, "Instance");
}

WOORF_EXPORT int woorf_kind(lua_State* state, int index)
{
    if (lua_isnoneornil(state, index))
        return 0;
    if (lua_type(state, index) == LUA_TBOOLEAN)
        return 1;
    if (lua_type(state, index) == LUA_TSTRING)
        return 2;
    if (lua_touserdatatagged(state, index, InstanceTag))
        return 3;
    if (lua_touserdatatagged(state, index, 2))
        return 5;
    if (lua_touserdatatagged(state, index, 3))
        return 6;
    if (lua_isfunction(state, index))
        return 7;
    return 4;
}

WOORF_EXPORT int woorf_get_id(lua_State* state, int index)
{
    auto* id = static_cast<int*>(lua_touserdatatagged(state, index, InstanceTag));
    return id ? *id : 0;
}

WOORF_EXPORT const char* woorf_get_string(lua_State* state, int index, size_t* length)
{
    if (lua_type(state, index) != LUA_TSTRING)
    {
        *length = 0;
        return nullptr;
    }
    return lua_tolstring(state, index, length);
}

WOORF_EXPORT int woorf_get_bool(lua_State* state, int index)
{
    return lua_toboolean(state, index);
}

WOORF_EXPORT void woorf_push_string(lua_State* state, const char* bytes, size_t length)
{
    lua_pushlstring(state, bytes, length);
}

WOORF_EXPORT void woorf_push_bool(lua_State* state, int value)
{
    lua_pushboolean(state, value);
}

WOORF_EXPORT void woorf_push_list(lua_State* state, const int* ids, int count)
{
    lua_createtable(state, count, 0);
    for (int index = 0; index < count; ++index)
    {
        woorf_push_instance(state, ids[index]);
        lua_rawseti(state, -2, index + 1);
    }
}
