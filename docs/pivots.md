# Headless pivots

This milestone follows Roblox's [PVInstance](https://create.roblox.com/docs/reference/engine/classes/PVInstance), [BasePart.PivotOffset](https://create.roblox.com/docs/reference/engine/classes/BasePart#PivotOffset), [Model](https://create.roblox.com/docs/reference/engine/classes/Model), and [Instance.Clone](https://create.roblox.com/docs/reference/engine/classes/Instance#Clone) documentation, consulted on 2026-11-15. Compatibility is guided by those docs and local CLI integration tests; no Studio comparison harness is used.

Parts expose `PivotOffset`, `GetPivot()`, and `PivotTo(targetCFrame)`. PivotOffset starts at identity and changes the local pivot without moving geometry. The world pivot is `part.CFrame * part.PivotOffset`. PivotTo moves geometry so that this pivot reaches the requested target; it also transforms descendant Parts and Model pivots.

Models expose `PrimaryPart`, `WorldPivot`, `GetPivot()`, and `PivotTo(targetCFrame)`. Without a PrimaryPart, a new Model uses the center of its contents' world-aligned bounding box. The bounds account for each Part's size and rotation. Setting WorldPivot or moving with PivotTo freezes this initial automatic pivot behavior. Assigning WorldPivot changes the pivot without moving any Parts. With a PrimaryPart, GetPivot follows that Part's pivot instead of WorldPivot.

PivotTo transforms descendant Parts and nested Model pivots while preserving their relative placement. Anchored Parts can be moved. Only effective CFrame changes publish transform notifications during pivot movement; Position signals are suppressed, following the PVInstance documentation. Direct Position/CFrame writes retain their existing property notifications. PivotOffset and Model property assignments publish their own change signals.

Clones preserve pivot state. Reference properties pointing at copied instances are remapped to the corresponding copies; references to instances outside the cloned set retain their original target. Signals and movement caches remain independent. Model movement caches relative offsets to prevent accumulated drift, and rebuilds them when membership or stored transforms change.

An external PrimaryPart reference is accepted for compatibility with Roblox legacy assignments. Roblox clears such references at a simulation step; woorf has no physics simulation step, so it retains them until destruction. A previously-descendant primary reference is cleared when removed from its Model. Use descendant Parts for reliable Model movement.

The runtime remains headless: pivot movement performs no collision checks, joint solving or physics simulation. Supported transforms, pivots and PrimaryPart references replicate through the [client/server runtime](client-server.md). Scaling, legacy MoveTo/SetPrimaryPartCFrame aliases, and editor pivot properties remain unsupported. Source-directory metadata continues to support only the previously documented classes.

Model bounds are described in [Model bounds](bounds.md).

Run `examples/pivots.luau` to move and rotate a group of Parts.
