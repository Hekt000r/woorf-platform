#include "LuauBridge.h"
#include <algorithm>
#include <cmath>
#include <cstring>

static constexpr int PhysicalTag = 6;
static constexpr const char* Names[] = {"Density", "Friction", "Elasticity", "FrictionWeight", "ElasticityWeight"};

WOORF_EXPORT void woorf_push_physical_properties(lua_State* state, const float* values)
{
    auto* stored = static_cast<float*>(lua_newuserdatataggedwithmetatable(state, sizeof(float) * 5, PhysicalTag));
    std::memcpy(stored, values, sizeof(float) * 5);
}
WOORF_EXPORT int woorf_get_physical_properties(lua_State* state, int index, float* values)
{
    auto* stored = static_cast<float*>(lua_touserdatatagged(state, index, PhysicalTag));
    if (!stored) return 0;
    std::memcpy(values, stored, sizeof(float) * 5); return 1;
}
static int constructProperties(lua_State* state)
{
    const int count = lua_gettop(state);
    if (count != 3 && count != 5) luaL_error(state, "PhysicalProperties.new supports three or five numbers");
    const float maximum[] = {100, 2, 1, 100, 100};
    float values[5];
    for (int index = 0; index < 5; ++index)
    {
        const auto value = index < 3 ? luaL_checknumber(state, index + 1) : luaL_optnumber(state, index + 1, 1);
        if (!std::isfinite(value)) luaL_error(state, "Expected finite physical property");
        values[index] = static_cast<float>(std::clamp(value, index == 0 ? 0.0001 : 0.0, double(maximum[index])));
    }
    woorf_push_physical_properties(state, values); return 1;
}
static int readProperty(lua_State* state)
{
    auto* values = static_cast<float*>(lua_touserdatatagged(state, 1, PhysicalTag));
    const char* name = luaL_checkstring(state, 2);
    for (int index = 0; index < 5; ++index)
        if (std::strcmp(name, Names[index]) == 0) { lua_pushnumber(state, values[index]); return 1; }
    luaL_error(state, "Unsupported PhysicalProperties member");
}
static int rejectWrite(lua_State* state) { luaL_error(state, "PhysicalProperties is read-only"); }
static int equals(lua_State* state)
{
    float first[5], second[5];
    lua_pushboolean(state, woorf_get_physical_properties(state, 1, first) && woorf_get_physical_properties(state, 2, second)
        && std::equal(first, first + 5, second)); return 1;
}
void initializePhysicalProperties(lua_State* state)
{
    lua_newtable(state);
    lua_pushstring(state, "PhysicalProperties"); lua_setfield(state, -2, "__type");
    lua_pushstring(state, "The metatable is locked"); lua_setfield(state, -2, "__metatable");
    lua_pushcfunction(state, readProperty, "PhysicalProperties read"); lua_setfield(state, -2, "__index");
    lua_pushcfunction(state, rejectWrite, "PhysicalProperties write"); lua_setfield(state, -2, "__newindex");
    lua_pushcfunction(state, equals, "PhysicalProperties equality"); lua_setfield(state, -2, "__eq");
    lua_setreadonly(state, -1, 1); lua_setuserdatametatable(state, PhysicalTag);
    lua_newtable(state);
    lua_pushcfunction(state, constructProperties, "PhysicalProperties.new"); lua_setfield(state, -2, "new");
    lua_setreadonly(state, -1, 1); lua_setglobal(state, "PhysicalProperties");
}
