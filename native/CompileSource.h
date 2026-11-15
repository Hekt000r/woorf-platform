#pragma once

#include "Luau/Compiler.h"

inline std::string compileSource(const std::string& source)
{
    // Roblox's type() reports vectors as userdata, unlike standalone Luau's intrinsic.
    static const char* mutableGlobals[] = {"type", nullptr};
    Luau::CompileOptions options;
    options.mutableGlobals = mutableGlobals;
    return Luau::compile(source, options);
}
