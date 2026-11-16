#include "LuauBridge.h"
#include "CompileSource.h"
#include <exception>
#include <memory>
#include <string>

static int printValues(lua_State* state)
{
    Runtime* runtime = getRuntime(state);
    const int count = lua_gettop(state);
    for (int index = 1; index <= count; ++index)
    {
        if (index > 1)
            runtime->write("\t", 1, 0);
        size_t length = 0;
        const char* value = luaL_tolstring(state, index, &length);
        runtime->write(value, length, 0);
        lua_pop(state, 1);
    }
    runtime->write("\n", 1, 0);
    return 0;
}

WOORF_EXPORT void* woorf_create(WriteOutput write, HostCall host, int gameId, int workspaceId)
{
    try
    {
        auto runtime = std::make_unique<Runtime>();
        runtime->write = write;
        runtime->host = host;
        runtime->state = luaL_newstate();
        if (!runtime->state)
            return nullptr;
        lua_callbacks(runtime->state)->userdata = runtime.get();
        luaL_openlibs(runtime->state);
        lua_pushcfunction(runtime->state, printValues, "print");
        lua_setglobal(runtime->state, "print");
        initializeInstances(runtime->state, gameId, workspaceId);
        initializeVector3(runtime->state);
        initializeCFrame(runtime->state);
        initializeSignals(runtime->state);
        initializeModules(runtime->state);
        initializeSchedulerHelpers(runtime->state);
        initializeColor(runtime->state);
        initializePhysicalProperties(runtime->state);
        initializeExecutionBudget(runtime->state);
        return runtime.release();
    }
    catch (const std::exception& error)
    {
        const std::string message = std::string(error.what()) + "\n";
        write(message.data(), message.size(), 1);
        return nullptr;
    }
}

static int reportFailure(Runtime* runtime, lua_State* thread)
{
    size_t length = 0;
    const char* error = lua_tolstring(thread, -1, &length);
    if (error)
        runtime->write(error, length, 1);
    else
        runtime->write("Luau execution failed", 21, 1);
    runtime->write("\n", 1, 1);
    return 1;
}

WOORF_EXPORT int woorf_run(void* handle, const char* source, size_t length,
    const char* chunkName, int scriptId)
{
    auto* runtime = static_cast<Runtime*>(handle);
    ExecutionScope execution(runtime);
    if (runtime->executionFailed) return 1;
    try
    {
        lua_State* thread = lua_newthread(runtime->state);
        luaL_sandboxthread(thread);
        if (scriptId > 0)
        {
            woorf_push_instance(thread, scriptId);
            lua_setglobal(thread, "script");
        }
        const std::string bytecode = compileSource(std::string(source, length));
        int status = luau_load(thread, chunkName, bytecode.data(), bytecode.size(), 0);
        if (status == 0)
        {
            lua_getglobal(thread, "_woorf_start");
            lua_insert(thread, -2);
            lua_getglobal(thread, "script");
            status = lua_resume(thread, nullptr, 2);
        }
        const int result = reportExecutionFailure(runtime) ? 1 : status == 0 ? 0 : reportFailure(runtime, thread);
        lua_pop(runtime->state, 1);
        return result;
    }
    catch (const std::exception& error)
    {
        lua_settop(runtime->state, 0);
        const std::string message = std::string(error.what()) + "\n";
        runtime->write(message.data(), message.size(), 1);
        return 1;
    }
}

WOORF_EXPORT void woorf_close(void* handle)
{
    delete static_cast<Runtime*>(handle);
}
