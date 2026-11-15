#include "LuauBridge.h"
#include <algorithm>

static constexpr int CFrameTag = 4;
static constexpr int ComponentCount = 12;

WOORF_EXPORT void woorf_push_cframe(lua_State* state, const float* components)
{
    auto* value = static_cast<float*>(lua_newuserdatataggedwithmetatable(state, sizeof(float) * ComponentCount, CFrameTag));
    std::copy_n(components, ComponentCount, value);
}

WOORF_EXPORT int woorf_get_cframe(lua_State* state, int index, float* components)
{
    const auto* value = static_cast<float*>(lua_touserdatatagged(state, index, CFrameTag));
    if (!value) return 0;
    std::copy_n(value, ComponentCount, components);
    return 1;
}

WOORF_EXPORT int woorf_get_top(lua_State* state) { return lua_gettop(state); }

static void setMethod(lua_State* state, const char* member, const char* operation)
{
    woorf_push_method(state, operation);
    lua_setfield(state, -2, member);
}

static void initializeMetatable(lua_State* state)
{
    lua_newtable(state);
    setMethod(state, "__index", "cframe_get");
    setMethod(state, "__newindex", "cframe_set");
    setMethod(state, "__tostring", "cframe_tostring");
    setMethod(state, "__eq", "cframe_eq");
    setMethod(state, "__mul", "cframe_mul");
    setMethod(state, "__add", "cframe_add");
    setMethod(state, "__sub", "cframe_sub");
    lua_pushstring(state, "CFrame");
    lua_setfield(state, -2, "__type");
    lua_pushstring(state, "The metatable is locked");
    lua_setfield(state, -2, "__metatable");
    lua_setreadonly(state, -1, 1);
    lua_setuserdatametatable(state, CFrameTag);
}

void initializeCFrame(lua_State* state)
{
    initializeMetatable(state);
    lua_newtable(state);
    const char* constructors[] = {"new", "Angles", "fromEulerAnglesXYZ", "fromEulerAnglesYXZ",
        "fromOrientation", "fromAxisAngle", "lookAt", "lookAlong", "fromMatrix"};
    for (const char* constructor : constructors)
    {
        const std::string operation = std::string("cframe_") + constructor;
        setMethod(state, constructor, operation.c_str());
    }
    const float identity[ComponentCount] = {0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1};
    woorf_push_cframe(state, identity);
    lua_setfield(state, -2, "identity");
    lua_setreadonly(state, -1, 1);
    lua_setglobal(state, "CFrame");
}
