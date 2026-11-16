# Model bounds

`Model:GetBoundingBox()` returns a CFrame at the center of the volume enclosing all descendant Parts, plus a Vector3 describing its size. `GetExtentsSize()` returns that same size alone. The box follows the Model pivot orientation: the PrimaryPart pivot when assigned, otherwise WorldPivot. It is an enclosing box in that orientation, rather than a search for the smallest box at any arbitrary angle.

This follows Roblox's [Model Engine API](https://create.roblox.com/docs/reference/engine/classes/Model), consulted on 2026-11-15. Tests use local mathematical fixtures, without a Studio comparison harness.

Bounds include rotated and nested Parts and reflect changes to size, transforms, parenting, and destruction. Reading them does not move geometry, publish changes, or freeze a new Model's automatic pivot. Workspace inherits the Model methods. A single Part can be measured by placing it in a Model; these methods are not added to BasePart.

The runtime remains headless. Bounds are geometric queries; the [physics solver](physics.md) separately simulates oriented-box Parts. Run `examples/bounds.luau` for a complete example.

For an empty Model, woorf returns its current pivot and zero size. Roblox's referenced docs do not specify this edge case; it is a local convention.
