#include "LuauBridge.h"

static constexpr int FirstGuiTag = 7;
static char guiEnumCacheKey;

WOORF_EXPORT void woorf_push_gui_value(lua_State* state, int kind, double a, double b, double c, double d)
{
    const int enumKey = kind == 3 ? static_cast<int>(a) * 1024 + static_cast<int>(b) : 0;
    if (kind == 3)
    {
        lua_rawgetp(state, LUA_REGISTRYINDEX, &guiEnumCacheKey);
        lua_rawgeti(state, -1, enumKey);
        if (!lua_isnil(state, -1)) { lua_remove(state, -2); return; }
        lua_pop(state, 1);
    }
    if (kind < 3) a = static_cast<float>(a);
    if (kind == 0) b = static_cast<float>(b);
    if (kind == 2) c = static_cast<float>(c);
    auto* value = static_cast<double*>(lua_newuserdatataggedwithmetatable(state, sizeof(double) * 4, FirstGuiTag + kind));
    value[0] = a; value[1] = b; value[2] = c; value[3] = d;
    if (kind == 3)
    {
        lua_pushvalue(state, -1);
        lua_rawseti(state, -3, enumKey);
        lua_remove(state, -2);
    }
}

WOORF_EXPORT int woorf_get_gui_value(lua_State* state, int index, int kind, double* values)
{
    auto* value = static_cast<double*>(lua_touserdatatagged(state, index, FirstGuiTag + kind));
    if (!value) return 0;
    for (int component = 0; component < 4; ++component) values[component] = value[component];
    return 1;
}

static int constructGuiValue(lua_State* state)
{
    const int kind = luaL_checkinteger(state, 1);
    if (kind < 0 || kind > 3) luaL_error(state, "Invalid GUI datatype");
    woorf_push_gui_value(state, kind, luaL_optnumber(state, 2, 0), luaL_optnumber(state, 3, 0),
        luaL_optnumber(state, 4, 0), luaL_optnumber(state, 5, 0));
    return 1;
}

static int readGuiValue(lua_State* state)
{
    const int kind = luaL_checkinteger(state, 2);
    if (kind < 0 || kind > 3) luaL_error(state, "Invalid GUI datatype");
    auto* value = static_cast<double*>(lua_touserdatatagged(state, 1, FirstGuiTag + kind));
    if (!value) luaL_error(state, "Expected GUI datatype");
    for (int index = 0; index < 4; ++index) lua_pushnumber(state, value[index]);
    return 4;
}

static int setGuiMetatable(lua_State* state)
{
    const int kind = luaL_checkinteger(state, 1);
    if (kind < 0 || kind > 3) luaL_error(state, "Invalid GUI datatype");
    luaL_checktype(state, 2, LUA_TTABLE);
    lua_pushvalue(state, 2);
    lua_setreadonly(state, -1, 1);
    lua_setuserdatametatable(state, FirstGuiTag + kind);
    return 0;
}

void initializeGuiValues(lua_State* state)
{
    lua_newtable(state); lua_rawsetp(state, LUA_REGISTRYINDEX, &guiEnumCacheKey);
    lua_pushcfunction(state, constructGuiValue, "GUI datatype"); lua_setglobal(state, "_woorf_gui_new");
    lua_pushcfunction(state, readGuiValue, "GUI components"); lua_setglobal(state, "_woorf_gui_get");
    lua_pushcfunction(state, setGuiMetatable, "GUI metatable"); lua_setglobal(state, "_woorf_gui_metatable");
}

static int inputIndex(lua_State* state)
{
    lua_pushvalue(state, 2);
    lua_rawget(state, lua_upvalueindex(1));
    if (lua_isnil(state, -1)) luaL_error(state, "Unsupported InputObject member");
    return 1;
}

static int inputWrite(lua_State* state)
{
    luaL_error(state, "InputObject is read-only");
    return 0;
}

WOORF_EXPORT void woorf_wrap_input_object(lua_State* state)
{
    const int source = lua_gettop(state);
    lua_newuserdata(state, 1);
    lua_newtable(state);
    lua_pushvalue(state, source);
    lua_pushcclosure(state, inputIndex, "InputObject property", 1); lua_setfield(state, -2, "__index");
    lua_pushcfunction(state, inputWrite, "InputObject write"); lua_setfield(state, -2, "__newindex");
    lua_pushstring(state, "InputObject"); lua_setfield(state, -2, "__type");
    lua_pushstring(state, "The metatable is locked"); lua_setfield(state, -2, "__metatable");
    lua_setreadonly(state, -1, 1);
    lua_setmetatable(state, -2);
    lua_remove(state, source);
}
