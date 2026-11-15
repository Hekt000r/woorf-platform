#include "LuauBridge.h"

WOORF_EXPORT void woorf_push_vector3(lua_State* state, float x, float y, float z)
{
    lua_pushvector(state, x, y, z);
}

WOORF_EXPORT int woorf_get_vector3(lua_State* state, int index, float* x, float* y, float* z)
{
    const auto* value = lua_tovector(state, index);
    if (!value) return 0;
    *x = value[0];
    *y = value[1];
    *z = value[2];
    return 1;
}

WOORF_EXPORT int woorf_get_number(lua_State* state, int index, double* value)
{
    if (lua_type(state, index) != LUA_TNUMBER) return 0;
    *value = lua_tonumber(state, index);
    return 1;
}

WOORF_EXPORT void woorf_push_number(lua_State* state, double value) { lua_pushnumber(state, value); }

static void setMethod(lua_State* state, const char* member, const char* operation)
{
    woorf_push_method(state, operation);
    lua_setfield(state, -2, member);
}

static void setConstant(lua_State* state, const char* member, float x, float y, float z)
{
    woorf_push_vector3(state, x, y, z);
    lua_setfield(state, -2, member);
}

static int readRobloxType(lua_State* state)
{
    luaL_checkany(state, 1);
    lua_pushstring(state, lua_isvector(state, 1) ? "userdata" : lua_typename(state, lua_type(state, 1)));
    return 1;
}

void initializeVector3(lua_State* state)
{
    lua_pushcfunction(state, readRobloxType, "type");
    lua_setglobal(state, "type");
    lua_pushvector(state, 0, 0, 0);
    lua_newtable(state);
    setMethod(state, "__index", "vector3_get");
    setMethod(state, "__newindex", "vector3_set");
    setMethod(state, "__tostring", "vector3_tostring");
    lua_pushstring(state, "Vector3");
    lua_setfield(state, -2, "__type");
    lua_pushstring(state, "The metatable is locked");
    lua_setfield(state, -2, "__metatable");
    lua_setreadonly(state, -1, 1);
    lua_setmetatable(state, -2);
    lua_pop(state, 1);
    lua_newtable(state);
    setMethod(state, "new", "vector3_new");
    setConstant(state, "zero", 0, 0, 0);
    setConstant(state, "one", 1, 1, 1);
    setConstant(state, "xAxis", 1, 0, 0);
    setConstant(state, "yAxis", 0, 1, 0);
    setConstant(state, "zAxis", 0, 0, 1);
    lua_setreadonly(state, -1, 1);
    lua_setglobal(state, "Vector3");
}
