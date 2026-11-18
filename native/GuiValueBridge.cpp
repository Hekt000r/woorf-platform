#include "LuauBridge.h"
#include <cstring>

static constexpr int FirstGuiTag = 7;
static char guiEnumCacheKey;

struct SequenceValue
{
    int count;
};

WOORF_EXPORT void woorf_push_sequence(lua_State* state, int kind, const float* components, int count)
{
    auto* value = static_cast<SequenceValue*>(lua_newuserdatataggedwithmetatable(
        state, sizeof(SequenceValue) + sizeof(float) * count, 13 + kind));
    value->count = count;
    std::memcpy(reinterpret_cast<char*>(value) + sizeof(SequenceValue), components, sizeof(float) * count);
}

WOORF_EXPORT const float* woorf_get_sequence(lua_State* state, int index, int kind, int* count)
{
    auto* value = static_cast<SequenceValue*>(lua_touserdatatagged(state, index, 13 + kind));
    if (!value) return nullptr;
    *count = value->count;
    return reinterpret_cast<const float*>(value + 1);
}

static int constructSequence(lua_State* state)
{
    const int kind = luaL_checkinteger(state, 1);
    const int count = lua_gettop(state) - 1;
    if (kind < 0 || kind > 3 || count < 1 || count > 80) luaL_error(state, "Invalid sequence");
    std::vector<float> components;
    components.reserve(count);
    for (int i = 0; i < count; ++i) components.push_back(static_cast<float>(lua_tonumber(state, i + 2)));
    woorf_push_sequence(state, kind, components.data(), count);
    return 1;
}

static int readSequence(lua_State* state)
{
    const int kind = luaL_checkinteger(state, 2);
    if (kind < 0 || kind > 3) luaL_error(state, "Invalid sequence kind");
    int count = 0;
    const float* components = woorf_get_sequence(state, 1, kind, &count);
    if (!components) luaL_error(state, "Expected sequence");
    lua_createtable(state, count, 0);
    for (int i = 0; i < count; ++i)
    {
        lua_pushnumber(state, components[i]);
        lua_rawseti(state, -2, i + 1);
    }
    return 1;
}

struct FontValue
{
    size_t length;
    int weight;
    int style;
};

WOORF_EXPORT void woorf_push_font(lua_State* state, const char* family, size_t length, int weight, int style)
{
    auto* value = static_cast<FontValue*>(lua_newuserdatataggedwithmetatable(state, sizeof(FontValue) + length, FirstGuiTag + 5));
    *value = {length, weight, style};
    if (length > 0) std::memcpy(value + 1, family, length);
}

WOORF_EXPORT const char* woorf_get_font(lua_State* state, int index, size_t* length, int* weight, int* style)
{
    auto* value = static_cast<FontValue*>(lua_touserdatatagged(state, index, FirstGuiTag + 5));
    if (!value) return nullptr;
    *length = value->length;
    *weight = value->weight;
    *style = value->style;
    return reinterpret_cast<const char*>(value + 1);
}

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
    if (kind < 0 || (kind > 4 && kind != 10)) luaL_error(state, "Invalid GUI datatype");
    woorf_push_gui_value(state, kind, luaL_optnumber(state, 2, 0), luaL_optnumber(state, 3, 0),
        luaL_optnumber(state, 4, 0), luaL_optnumber(state, 5, 0));
    return 1;
}

static int readGuiValue(lua_State* state)
{
    const int kind = luaL_checkinteger(state, 2);
    if (kind < 0 || (kind > 4 && kind != 10)) luaL_error(state, "Invalid GUI datatype");
    auto* value = static_cast<double*>(lua_touserdatatagged(state, 1, FirstGuiTag + kind));
    if (!value) luaL_error(state, "Expected GUI datatype");
    for (int index = 0; index < 4; ++index) lua_pushnumber(state, value[index]);
    return 4;
}

static int setGuiMetatable(lua_State* state)
{
    const int kind = luaL_checkinteger(state, 1);
    if (kind < 0 || kind > 10) luaL_error(state, "Invalid GUI datatype");
    luaL_checktype(state, 2, LUA_TTABLE);
    lua_pushvalue(state, 2);
    lua_setreadonly(state, -1, 1);
    lua_setuserdatametatable(state, FirstGuiTag + kind);
    return 0;
}

void initializeGuiValues(lua_State* state)
{
    lua_pushcfunction(state, constructSequence, "sequence constructor"); lua_setglobal(state, "_woorf_sequence_new");
    lua_pushcfunction(state, readSequence, "sequence components"); lua_setglobal(state, "_woorf_sequence_get");
    lua_newtable(state); lua_rawsetp(state, LUA_REGISTRYINDEX, &guiEnumCacheKey);
    lua_newtable(state);
    const char* methods[] = {"new", "palette", "random", "White", "Gray", "DarkGray", "Black", "Red", "Yellow", "Green", "Blue"};
    for (const char* method : methods)
    {
        const std::string operation = std::string("brickcolor_") + method;
        woorf_push_method(state, operation.c_str());
        lua_setfield(state, -2, method);
    }
    lua_getfield(state, -1, "random"); lua_setfield(state, -2, "Random");
    lua_setreadonly(state, -1, 1);
    lua_setglobal(state, "BrickColor");
    woorf_push_method(state, "brickcolor_get"); lua_setglobal(state, "_woorf_brickcolor_get");
    lua_newtable(state);
    for (const char* method : {"new", "fromName", "fromId", "fromEnum"})
    {
        const std::string operation = std::string("font_") + method;
        woorf_push_method(state, operation.c_str());
        lua_setfield(state, -2, method);
    }
    lua_setreadonly(state, -1, 1);
    lua_setglobal(state, "Font");
    woorf_push_method(state, "font_get"); lua_setglobal(state, "_woorf_font_get");
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
