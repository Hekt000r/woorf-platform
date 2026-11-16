#include "LuauBridge.h"

static void interruptExecution(lua_State* state, int gc)
{
    auto* runtime = getRuntime(state);
    if (gc >= 0 || runtime->executionDepth == 0) return;
    const auto now = std::chrono::steady_clock::now();
    if (!runtime->executionFailed && now < runtime->executionDeadline && now < runtime->hardExecutionDeadline) return;
    runtime->executionFailed = true;
    if (lua_isyieldable(state)) lua_break(state);
    else luaL_error(state, "Execution budget exceeded");
}

void initializeExecutionBudget(lua_State* state)
{
    lua_callbacks(state)->interrupt = interruptExecution;
}

bool reportExecutionFailure(Runtime* runtime)
{
    if (!runtime->executionFailed) return false;
    constexpr char message[] = "Execution budget exceeded (250 ms per VM entry)\n";
    runtime->write(message, sizeof(message) - 1, 1);
    return true;
}

WOORF_EXPORT int woorf_execution_failed(void* handle)
{
    return static_cast<Runtime*>(handle)->executionFailed ? 1 : 0;
}
