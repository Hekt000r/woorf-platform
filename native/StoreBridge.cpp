#include "LuauBridge.h"

WOORF_EXPORT int woorf_transform(lua_State* state, int callbackIndex)
{
    ExecutionScope execution(getRuntime(state));
    const int top = lua_gettop(state);
    lua_pushvalue(state, callbackIndex);
    lua_insert(state, top);
    const int status = lua_pcall(state, 1, LUA_MULTRET, 0);
    if (status != 0) return status;
    if (lua_gettop(state) == top) return 0;
    lua_settop(state, top - 1);
    lua_pushstring(state, "UpdateAsync transform must return exactly one value.");
    return 1;
}
