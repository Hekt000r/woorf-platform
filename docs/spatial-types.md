# Headless spatial types

The spatial milestone follows Roblox's [Vector3 datatype](https://create.roblox.com/docs/reference/engine/datatypes/Vector3), [BasePart](https://create.roblox.com/docs/reference/engine/classes/BasePart), and [Part](https://create.roblox.com/docs/reference/engine/classes/Part) Engine API references, consulted on 2026-11-15. These references are live documentation, not a pinned compatibility snapshot. No Roblox Studio session was connected for live differential checks.

`Vector3` is an immutable value with single-precision components. C# owns Part state and implements vector methods; Luau uses its official native vector representation for primitive arithmetic, equality, and table-key behavior. Native bridge functions marshal the three components between C# and Luau. Constructors and property reads return values, rather than mutable references into a Part.

The initial surface includes `Vector3.new(x?, y?, z?)`, `zero`, `one`, `xAxis`, `yAxis`, `zAxis`, `X`, `Y`, `Z`, `Magnitude`, `Unit`, addition, subtraction, negation, multiplication, division, floor division, equality, `Dot`, `Cross`, and `Lerp`. Unsupported datatype members raise errors. Enum-based constructors and additional vector methods remain future work.

`Instance.new("Part")` creates a Part with `Position = Vector3.zero`, `Size = Vector3.new(4, 1.2, 2)`, and `Anchored = false`. The inheritance chain is `Part → FormFactorPart → BasePart → PVInstance → Instance → Object`; abstract ancestors cannot be created directly. C# owns the property values, validates their types, and clamps each Size component to the documented range of `0.001` through `2048` studs.

Explicit property assignments publish `Changed` and the corresponding `GetPropertyChangedSignal` after an effective value change. Equal assignments, including assignments that clamp to the existing Size, publish no notification. Clones preserve spatial state and have independent signals and property values.

This is a headless property subset. Anchored is stored state; there is no gravity, collision response, rendering or assembly simulation. Supported Part state replicates through the [client/server runtime](client-server.md); Workspace also supports [oriented-box raycasts](backend-apis.md). Position and CFrame are synchronized; see [CFrame and Part transforms](cframe.md). Joint movement and overlap resolution remain unimplemented. Shape and other Part-specific APIs remain unsupported. Filesystem metadata still supports only Folder, Model, and Configuration; create Parts from scripts.

Run `examples/parts.luau` for a script that creates, moves, observes, and clones a Part.

