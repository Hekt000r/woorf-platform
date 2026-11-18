# Scripted screen GUI

Woorf renders a documented Roblox-style screen GUI subset in Windows play mode
and in the browser host. UI belongs to the owning client's PlayerGui. A server
and each client retain separate worlds and Luau VMs; button callbacks run in the
client, and gameplay changes should use validated RemoteEvents.

The obby demonstrates a scripted stage/time/death HUD and a restart button:

```powershell
dotnet run --project src/Woorf.Cli --no-restore -- examples/obby --play
dotnet run --project src/Woorf.Cli --no-restore -- examples/obby --play --clients 2
```

Local play now starts an in-process server and client, including LocalScripts.
Graphical TCP clients use their own GUI and input. GUI rendering stays in a
central overlay; Instances do not own GPU resources or draw themselves.

## Scripting

Place a LocalScript in StarterPlayer/StarterPlayerScripts or StarterGui:

```luau
local player = game:GetService("Players").LocalPlayer
local screen = Instance.new("ScreenGui", player:WaitForChild("PlayerGui"))
screen.ResetOnSpawn = false
local button = Instance.new("TextButton", screen)
button.AnchorPoint = Vector2.new(0.5, 0.5)
button.Position = UDim2.fromScale(0.5, 0.5)
button.Size = UDim2.fromOffset(180, 42)
button.Text = "Press me"
button.Activated:Connect(function(input, clickCount)
    button.Text = "Clicked!"
end)
```

Vector2, UDim and UDim2 are immutable Luau datatypes with constructors, equality,
dimension arithmetic and interpolation. They cross the supported value codec,
including attributes and remotes. GUI properties validate types, publish effective
change signals and clone independently. Read-only layout results update during
GUI capture, rather than synchronously after every assignment.

## Supported classes and behavior

| Area | Implemented subset |
| --- | --- |
| Containers | ScreenGui: Enabled, DisplayOrder, ResetOnSpawn, ZIndexBehavior; Frame and CanvasGroup |
| Placement | Position/Size UDim2, AnchorPoint, Visible, ZIndex, LayoutOrder, clipping, AutomaticSize and SizeConstraint |
| Text | TextLabel, TextButton, TextBox; text/color/transparency/size, alignment, wrapping, scaling, line height, truncation, grapheme visibility and text stroke |
| Formatting | Font names use system-font substitutions; rich text supports b, i, u, s, br and font color with six-digit hex values |
| Images | ImageLabel/ImageButton with local PNG/BMP content, tint/transparency, image subrectangles and Stretch/Fit/Crop/Tile scaling |
| Scrolling | ScrollingFrame canvas dimensions/position, automatic canvas sizing, visible scroll indicators and mouse-wheel input |
| Layouts | UIListLayout and UIGridLayout: ordering, padding, fill direction and alignment; UIPadding |
| Modifiers | UICorner, UIStroke, UIScale, UISizeConstraint, UIAspectRatioConstraint, UITextSizeConstraint |
| Input | Mouse hover/movement, button press/release/click/Activated; typed immutable InputObject event payloads |
| Text entry | Focused/FocusLost, CaptureFocus/ReleaseFocus/IsFocused, editable/placeholder/multiline text, cursor and selection, keyboard navigation and deletion |
| Replication | Supported writable GUI state, local and TCP updates; unrelated server changes preserve local edits |

ScreenGui templates directly beneath StarterGui with ResetOnSpawn=false survive
respawn. Other template trees are replaced; indirect ScreenGuis reset even when
their own flag is false. Client-created direct persistent screens also retain
their identity. Existing headless startup still copies StarterGui immediately,
including projects that do not enable character spawning; this differs from
Roblox's character-dependent initial copy.

Buttons activate after a press and release within the same visible hit target.
Clipped/hidden/disabled GUI cannot receive activation. Text focus suppresses the
play viewer's character movement, jumping and reset keys. GUI events are local;
they are not automatically sent to the server.

## Boundaries

This is a working screen-GUI subset, not the complete Roblox UI engine. There is
no SurfaceGui/BillboardGui, CoreGui/core scripts, UIGradient, UIPageLayout, flex
layout, universal styling, TweenService, FontFace asset pipeline, gamepad UI
navigation, touch gestures, IME or full clipboard editing. Unsupported classes
and members raise errors. Some recognized settings only admit their supported
default mode; they do not imply the full Roblox enum behavior.

Text layout uses shared approximate glyph advances so both hosts can compute
bounds and hit targets. Windows uses system glyph rasterization and the browser
uses Canvas text; fonts, wrapping and pixel metrics are not Roblox-identical.
Rich-text font sizes/faces, localization and complex bidirectional shaping are
outside the supported markup subset. IgnoreGuiInset is accepted, but Woorf has
no Roblox top bar or device-safe-area model and currently uses the full viewport.
Rotation and clipping have narrower behavior than Roblox; rotation of an entire
nested hierarchy and complex rotated clipping require further qualification.

Custom image files must be available locally on every native client. Roblox asset
IDs/CDN delivery and server-to-client asset transfer remain unsupported. See
[browser support](browser.md) for the browser package's asset and network limits.

## Verification and references

The ordinary custom test runner includes scripting, validation, cloning, layout,
input, local/wire replication, respawn and the actual obby HUD-to-server restart
path. `--gui-graphics` checks actual OpenGL overlay pixels separately.

```powershell
dotnet run --project tests/Woorf.Cli.Tests --no-restore
dotnet run --project tests/Woorf.Cli.Tests --no-restore -- --gui-graphics
```

Implementation references were researched in the official Roblox documentation:
[GuiObject](https://create.roblox.com/docs/reference/engine/classes/GuiObject),
[ScreenGui](https://create.roblox.com/docs/reference/engine/classes/ScreenGui),
[LayerCollector](https://create.roblox.com/docs/reference/engine/classes/LayerCollector),
[GuiButton](https://create.roblox.com/docs/reference/engine/classes/GuiButton),
[TextBox](https://create.roblox.com/docs/reference/engine/classes/TextBox),
[UIListLayout](https://create.roblox.com/docs/reference/engine/classes/UIListLayout),
[size modifiers](https://create.roblox.com/docs/ui/size-modifiers) and
[scrolling frames](https://create.roblox.com/docs/ui/scrolling-frames).
These guide the API; tests establish Woorf's supported behavior, not general
Roblox conformance.

## Built-in play overlays

Desktop and browser play use the same managed overlay. Tab toggles the session
player list. Server scripts populate it using a Folder named exactly
`leaderstats` under each Player and value objects under that folder. The list
shows up to three columns and honors `IsPrimary` and `Priority` markers.
LocalScripts can control it with `StarterGui:SetCoreGuiEnabled` and
`GetCoreGuiEnabled` using `Enum.CoreGuiType.PlayerList` or `All`.

Escape opens the session menu. Escape or Resume closes it. The menu blocks local
movement and game GUI input while the shared simulation continues. LocalScripts
can observe `GuiService.MenuIsOpen`, `MenuOpened` and `MenuClosed`.

| Shortcut | Overlay |
| --- | --- |
| Shift+F3 | Network transport and connected player count. Data ping is currently unavailable. |
| Shift+F4 | Frame time, FPS, viewport size and managed heap. |
| F9 | Recent client Luau output. Also available via `StarterGui:SetCore("DevConsoleVisible", true)`. |

The player list covers the current session. The menu currently offers Resume.
The console shows client output and the diagnostics expose the metrics above.

References: [Leaderboards](https://create.roblox.com/docs/players/leaderboards),
[StarterGui](https://create.roblox.com/docs/reference/engine/classes/StarterGui),
[GuiService](https://create.roblox.com/docs/reference/engine/classes/GuiService),
[Diagnostic tools](https://create.roblox.com/docs/performance-optimization/identify).
