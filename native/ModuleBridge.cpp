#include "LuauBridge.h"
#include "Luau/Compiler.h"

static int loadModule(lua_State* state)
{
    auto* runtime = getRuntime(state);
    const int id = woorf_get_id(state, 1);
    const int hostResult = runtime->host(state, "module_source");
    if (hostResult < 0) lua_error(state);
    size_t sourceLength = 0;
    const char* sourceData = lua_tolstring(state, -2, &sourceLength);
    const std::string source(sourceData, sourceLength);
    const std::string name(lua_tostring(state, -1));
    const int savedTop = lua_gettop(runtime->state);
    lua_State* thread = lua_newthread(runtime->state);
    luaL_sandboxthread(thread);
    woorf_push_instance(thread, id);
    lua_setglobal(thread, "script");
    const std::string bytecode = Luau::compile(source);
    const int status = luau_load(thread, name.c_str(), bytecode.data(), bytecode.size(), 0);
    lua_xmove(thread, state, 1);
    lua_settop(runtime->state, savedTop);
    if (status != 0) lua_error(state);
    return 1;
}

void initializeModules(lua_State* state)
{
    lua_pushcfunction(state, loadModule, "load module");
    lua_setglobal(state, "_woorf_load_module");
}
