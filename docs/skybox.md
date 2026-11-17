# Skyboxes

Woorf follows the Roblox skybox arrangement: create a `Sky` beneath the `Lighting`
service and assign six face images. The cube is infinitely distant: camera
rotation changes the view, while walking never moves the sky. The obby installs
a Sky automatically, using bundled blue-sky/cloud textures.

```luau
local sky = Instance.new("Sky", game:GetService("Lighting"))
sky.SkyboxFt = "assets/sky/front.png"
sky.SkyboxBk = "assets/sky/back.png"
sky.SkyboxLf = "assets/sky/left.png"
sky.SkyboxRt = "assets/sky/right.png"
sky.SkyboxUp = "assets/sky/up.png"
sky.SkyboxDn = "assets/sky/down.png"
sky.SkyboxOrientation = Vector3.new(0, 45, 0)
```

The six legacy face property names and degree-based `SkyboxOrientation` match
the [Roblox Sky reference](https://create.roblox.com/docs/reference/engine/classes/Sky).
Orientation applies Y rotation, then X, then Z, as described by the official
[skybox construction and orientation guide](https://create.roblox.com/docs/environment/skybox).
Front faces negative Z, back positive Z, right positive X, left negative X, up
positive Y and down negative Y. Images keep their top edge above the horizon.
Faces should join seamlessly when folded into a cube.

`Sky` properties support property-change signals and independent cloning. Lighting
and Sky replicate in local client/server sessions and TCP protocol v8, including
live image/orientation edits and removal. If multiple Sky children are present,
the first direct child is used; removing it reveals the next. With no Sky, the
viewer uses its original solid background. Sky rendering adds one GPU draw call.

## Content supported by this build

The default face IDs use Roblox's `rbxasset://textures/sky/sky512_{rt,lf,up,dn,bk,ft}.tex`
names. Woorf maps these IDs to its own packaged PNG images; it does not bundle or
download Roblox artwork. The descriptive `right.png`, `left.png`, `up.png`,
`down.png`, `back.png` and `front.png` IDs under the same prefix are aliases.

Custom images can be absolute local filesystem paths or paths relative to the
process working directory. All six faces must have equal square dimensions,
between 1 and 4096 pixels. Supported files are noninterlaced 8-bit RGB/RGBA PNG
and uncompressed 24/32-bit BMP. Alpha is ignored because the sky is opaque.
Missing/unsupported images produce a diagnostic; they are not silently replaced.
Remote clients need the same custom files at the same paths. Image bytes are not
transferred from the server; packaged default assets are available to every client.

Roblox `rbxassetid://` IDs, HTTPS asset delivery, the newer Content-object aliases,
sun/moon/star rendering and their Sky properties, Lighting time-of-day controls,
Atmosphere, clouds, reflections and general Part texture mapping are not
implemented. The six-face skybox and orientation are the supported compatibility
surface; this is not a complete Roblox Lighting implementation.

## Verification

```powershell
dotnet run --project tests/Woorf.Cli.Tests --no-restore -- --sky
dotnet run --project tests/Woorf.Cli.Tests --no-restore -- --sky-graphics
```

The normal suite checks scripting and local/wire replication. The Windows OpenGL
test loads custom images through a real script, samples all six distinctly colored
faces, checks an asymmetric image's vertical direction, verifies camera translation
does not change the framebuffer, and checks degree units and Y-X-Z orientation.
