# Keyboard input subset

`UserInputService` is a non-creatable client service. This milestone implements
keyboard `InputBegan(input, gameProcessedEvent)`, `InputEnded(input,
gameProcessedEvent)` and `IsKeyDown(Enum.KeyCode)`. Letter keys A–Z and Space are
supported with Roblox's numeric KeyCode values. Immutable InputObjects carry
Keyboard input type, Begin/End state and the corresponding KeyCode.

Repeated key-down messages do not fire additional began edges. Held state updates
before callbacks run. Focus loss releases held keys. Textbox focus marks input as
GUI-processed so LocalScripts can ignore gameplay shortcuts while typing. Each
client has independent input state; it is not replicated. Access to these scripting
members from a server VM raises an error.

Native polling and browser events forward to the same managed service. Adapters
do not implement game actions. The arena LocalScript handles E/Q/B and submits
validated remote requests. This is a bounded keyboard subset, not the complete
service, mouse/touch/gamepad service events, composition, device queries, action
bindings or full KeyCode enum. Existing GUI pointer events remain separate.

Implementation follows official Roblox references for
[UserInputService](https://create.roblox.com/docs/reference/engine/classes/UserInputService),
[mouse/keyboard input](https://create.roblox.com/docs/input/mouse-and-keyboard) and
[KeyCode](https://create.roblox.com/docs/reference/engine/enums/KeyCode).

NPC support extends the existing
[Humanoid:Move](https://create.roblox.com/docs/reference/engine/classes/Humanoid#Move)
subset to non-player Models in Workspace with a Humanoid and Part PrimaryPart.
They share the player controller, grounding, jumping, health and procedural posing.
Full articulated rigs, Animator and the complete Humanoid state machine remain
outside this subset.

## Context actions

Client scripts can bind and unbind actions with ContextActionService. Actions
receive their name, UserInputState, and InputObject. Higher-priority and newer
bindings run first. Return Enum.ContextActionResult.Pass to continue down the
stack or Sink to consume the input. An action that returns nothing or yields
also consumes it. Removing or displacing a held binding sends Cancel.
Supported inputs are the keyboard subset above and buttons from the first
standard gamepad. Touch-button creation, analog axes and the remaining
ContextActionService APIs are outside this subset.

[Roblox ContextActionService reference](https://create.roblox.com/docs/reference/engine/classes/ContextActionService)
