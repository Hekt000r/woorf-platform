#include "LuauBridge.h"

static constexpr int ColorTag = 5;

WOORF_EXPORT void woorf_push_color3(lua_State* state, float red, float green, float blue)
{
    auto* value = static_cast<float*>(lua_newuserdatataggedwithmetatable(state, sizeof(float) * 3, ColorTag));
    value[0] = red;
    value[1] = green;
    value[2] = blue;
}

WOORF_EXPORT int woorf_get_color3(lua_State* state, int index, float* red, float* green, float* blue)
{
    auto* value = static_cast<float*>(lua_touserdatatagged(state, index, ColorTag));
    if (!value) return 0;
    *red = value[0]; *green = value[1]; *blue = value[2];
    return 1;
}

static int constructColor(lua_State* state)
{
    woorf_push_color3(state, static_cast<float>(luaL_optnumber(state, 1, 0)),
        static_cast<float>(luaL_optnumber(state, 2, 0)), static_cast<float>(luaL_optnumber(state, 3, 0)));
    return 1;
}

static int readColor(lua_State* state)
{
    auto* value = static_cast<float*>(lua_touserdatatagged(state, 1, ColorTag));
    if (!value) luaL_error(state, "Expected Color3");
    lua_pushnumber(state, value[0]); lua_pushnumber(state, value[1]); lua_pushnumber(state, value[2]);
    return 3;
}

static int setColorMetatable(lua_State* state)
{
    luaL_checktype(state, 1, LUA_TTABLE);
    lua_pushvalue(state, 1);
    lua_setreadonly(state, -1, 1);
    lua_setuserdatametatable(state, ColorTag);
    return 0;
}

void initializeColor(lua_State* state)
{
    lua_pushcfunction(state, constructColor, "Color3.new");
    lua_setglobal(state, "_woorf_color_new");
    lua_pushcfunction(state, readColor, "Color3 components");
    lua_setglobal(state, "_woorf_color_get");
    lua_pushcfunction(state, setColorMetatable, "Color3 metatable");
    lua_setglobal(state, "_woorf_set_color_metatable");
}
