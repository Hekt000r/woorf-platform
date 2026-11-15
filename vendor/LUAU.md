# Luau provenance

- Upstream: https://github.com/luau-lang/luau
- Release: `0.745`
- Commit: `2a3d8937c19c8fd9f7b72b6ec9068c62760e6588`
- Source is vendored unchanged under `vendor/luau`; builds do not download it.
- Licenses: [LICENSE.txt](luau/LICENSE.txt) and [lua_LICENSE.txt](luau/lua_LICENSE.txt). Retain both when redistributing Luau.

The engine links Luau's compiler and VM through `native/LuauBridge.cpp`. Embedding references: https://luau.org/api/ and the upstream headers shipped with this source.
