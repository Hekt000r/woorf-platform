#include "LuauBridge.h"

using ContextHost = int (*)(void*, lua_State*, const char*);
using ContextClock = double (*)(void*);
using ContextWrite = void (*)(void*, const char*, size_t, int);
WOORF_EXPORT void* woorf_create(WriteOutput write, HostCall host, int gameId, int workspaceId);

WOORF_EXPORT void* woorf_create_browser(ContextWrite write, ContextHost host,
    ContextClock clock, void* context, int gameId, int workspaceId)
{
    auto* runtime = static_cast<Runtime*>(woorf_create([](const char*, size_t, int) {}, nullptr, gameId, workspaceId));
    if (!runtime) return nullptr;
    runtime->write = [write, context](const char* bytes, size_t length, int isError) { write(context, bytes, length, isError); };
    runtime->host = [host, context](lua_State* state, const char* operation) { return host(context, state, operation); };
    runtime->readClock = [clock, context]() { return clock(context); };
    return runtime;
}
