# CFrame and Part transforms

This headless subset follows Roblox's [CFrame Engine API](https://create.roblox.com/docs/reference/engine/datatypes/CFrame) and [BasePart Engine API](https://create.roblox.com/docs/reference/engine/classes/BasePart), consulted on 2026-11-15. Local CLI integration tests use mathematical fixtures derived from those docs. There is no Studio comparison harness.

C# owns immutable CFrame values consisting of a position and a 3×3 rotation matrix. The native bridge exposes them as typed Luau userdata. Angles are in radians. Matrix columns correspond to RightVector, UpVector, and the negative of LookVector; forward is local negative Z.

The initial constructors include identity, Vector3/XYZ positions, position plus look target, quaternion and twelve-component matrix overloads, `Angles`/`fromEulerAnglesXYZ`, `fromEulerAnglesYXZ`/`fromOrientation`, `fromAxisAngle`, `fromMatrix`, `lookAt`, and `lookAlong`.

Values expose `Position`, `Rotation`, `X`, `Y`, `Z`, `RightVector`, `UpVector`, `LookVector`, and the XVector/YVector/ZVector aliases. Supported operations include frame composition, transforming a Vector3 point by multiplication, world-space translation with vector addition/subtraction, equality, `Inverse`, `Orthonormalize`, and `GetComponents`. The world/object-space frame, point, and direction methods accept multiple arguments and return corresponding multiple results. Direction transforms ignore translation.

Parts start with `CFrame.identity`. CFrame assignments orthonormalize the rotation basis as described by the Engine API. Assigning CFrame updates Position; assigning Position preserves the existing rotation. Cloning preserves both components independently. Effective changes publish Changed and property-specific signals for CFrame and, when its translation changes, Position. Reassigning the same value publishes no notification. State is updated before listeners run; ordering between different property notifications is a woorf convention rather than a Roblox compatibility guarantee.

This milestone does not implement physics, joints, collision resolution, or Part Orientation/Rotation properties. Model and Part pivots are covered in [headless pivots](pivots.md). Enum-based Euler orders, rotation extraction, interpolation, and the other CFrame methods remain unsupported unless listed above. Malformed/nonfinite rotations and degenerate look directions have not been exhaustively characterized against Roblox. Exact error wording and string formatting are not compatibility guarantees.

Run `examples/cframe.luau` to create, rotate, move, and clone a Part.
