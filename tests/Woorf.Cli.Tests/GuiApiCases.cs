static class GuiApiCases
{
    internal static IEnumerable<(string Name, string? Source, int Exit, string Output, string Error)> GetCases()
    {
        yield return UnsupportedModes();
        yield return ValueTransfer();
        yield return Focus();
        yield return Datatypes();
        yield return Properties();
        yield return Components();
    }

    private static (string, string?, int, string, string) UnsupportedModes() =>
        ("GUI unsupported interaction and image modes fail explicitly", """
            local button = Instance.new("ImageButton")
            assert(not pcall(function() button.ScaleType = Enum.ScaleType.Slice end))
            assert(button.ScaleType == Enum.ScaleType.Stretch)
            assert(not pcall(function() button.Modal = true end))
            assert(not pcall(function() button.Selected = true end))
            local stroke = Instance.new("UIStroke")
            assert(not pcall(function() stroke.LineJoinMode = Enum.LineJoinMode.Bevel end))
            assert(stroke.LineJoinMode == Enum.LineJoinMode.Round)
            """, 0, "", "");
    private static (string, string?, int, string, string) ValueTransfer() =>
        ("GUI dimensions cross attributes clones and BindableEvent arguments", """
            local object = Instance.new("Folder")
            local values = {Vector2.new(10, -12), UDim.new(0.5, -10), UDim2.new(1, -10, 0, 40)}
            for index,value in values do object:SetAttribute("Dimension" .. index, value) end
            local clone = object:Clone()
            for index,value in values do assert(clone:GetAttribute("Dimension" .. index) == value) end
            local event = Instance.new("BindableEvent")
            local received = false
            event.Event:Connect(function(vector, dimension, size)
                assert(vector == values[1] and dimension == values[2] and size == values[3])
                received = true
            end)
            event:Fire(table.unpack(values))
            assert(received)
            """, 0, "", "");
    private static (string, string?, int, string, string) Focus() =>
        ("GUI text focus signals preserve submitted state and cloned focus is reset", """
            local box = Instance.new("TextBox")
            box.Text = "Initial"
            local focused, lost = 0, 0
            box.Focused:Connect(function() focused += 1 end)
            box.FocusLost:Connect(function(submitted) assert(submitted); lost += 1 end)
            box:CaptureFocus()
            assert(box:IsFocused() and focused == 1 and box.Text == "")
            box:CaptureFocus()
            assert(focused == 1)
            box.Text = "Typed"
            local clone = box:Clone()
            assert(not clone:IsFocused() and clone.Text == "Typed")
            box:ReleaseFocus(true)
            assert(not box:IsFocused() and lost == 1)
            """, 0, "", "");
    private static (string, string?, int, string, string) Datatypes() =>
        ("GUI datatypes preserve dimensions arithmetic and immutable identity", """
            local a = Vector2.new(3, 4)
            assert(typeof(a) == "Vector2" and a.Magnitude == 5 and a.Unit == Vector2.new(0.6, 0.8))
            assert(a + Vector2.one == Vector2.new(4, 5))
            assert(a:Dot(Vector2.new(2, 1)) == 10 and a:Lerp(Vector2.zero, 0.5) == Vector2.new(1.5, 2))
            local dimension = UDim.new(0.5, 12)
            assert(typeof(dimension) == "UDim" and dimension.Scale == 0.5 and dimension.Offset == 12)
            assert(dimension + UDim.new(0.25, 2) == UDim.new(0.75, 14))
            local size = UDim2.new(dimension, UDim.new(1, -5))
            assert(typeof(size) == "UDim2" and size.X == dimension and size.Y.Offset == -5)
            assert(UDim2.fromOffset(20, 30):Lerp(UDim2.fromScale(1, 1), 0.5) == UDim2.new(0.5, 10, 0.5, 15))
            assert(not pcall(function() size.X = dimension end))
            """, 0, "", "");
    private static (string, string?, int, string, string) Properties() =>
        ("GUI script properties validate publish changes and clone independently", """
            local screen = Instance.new("ScreenGui", game:GetService("StarterGui"))
            assert(screen:IsA("LayerCollector") and screen.Enabled and screen.ResetOnSpawn)
            local button = Instance.new("TextButton", screen)
            assert(button:IsA("GuiButton") and button:IsA("GuiObject"))
            assert(button.Visible and button.Text == "Button")
            local changes = 0
            button:GetPropertyChangedSignal("Size"):Connect(function() changes += 1 end)
            button.Size = UDim2.fromOffset(120, 40)
            button.Size = UDim2.fromOffset(120, 40)
            assert(changes == 1)
            button.AnchorPoint = Vector2.new(0.5, 0.5)
            button.AnchorPoint = Vector2.new(0.1, 0.2)
            assert(button.AnchorPoint == Vector2.new(0.1, 0.2), "Vector2 must preserve single-precision value equality across the host")
            button.Position = UDim2.fromScale(0.1, 0.2)
            assert(button.Position == UDim2.fromScale(0.1, 0.2), "UDim scales must preserve value equality across the host")
            button.Text = "Restart"
            button.TextColor3 = Color3.fromRGB(100, 200, 255)
            button.TextXAlignment = Enum.TextXAlignment.Left
            assert(button.TextXAlignment == Enum.TextXAlignment.Left)
            local alignments = {[Enum.TextXAlignment.Left] = "left"}
            assert(alignments[button.TextXAlignment] == "left", "EnumItem identity must survive host property lookups")
            assert(Enum.UserInputType.Keyboard.Value == 8 and Enum.UserInputType.Touch.Value == 7)
            assert(Enum.UserInputType.Focus.Value == 9)
            assert(typeof(button.Activated) == "RBXScriptSignal")
            assert(not pcall(function() button.Size = Vector3.zero end))
            assert(not pcall(function() button.Visible = 1 end))
            assert(not pcall(function() button.TextColor3 = Color3.new(0/0, 1, 1) end))
            assert(not pcall(function() button.AnchorPoint = Vector2.new(math.huge, 0) end))
            assert(not pcall(function() button.Position = UDim2.fromScale(math.huge, 0) end))
            assert(not pcall(function() button.TextXAlignment = Enum.TextYAlignment.Top end))
            assert(not pcall(function() button.AbsoluteSize = Vector2.one end))
            local clone = button:Clone()
            button.Text = "Changed"
            assert(clone.Text == "Restart" and clone.Size == UDim2.fromOffset(120, 40))
            local connection = clone.Activated:Connect(function() end)
            clone:Destroy()
            assert(not connection.Connected)
            """, 0, "", "");
    private static (string, string?, int, string, string) Components() =>
        ("GUI layout components expose validated settings and clone values", """
            local frame = Instance.new("ScrollingFrame")
            frame.CanvasSize = UDim2.fromOffset(400, 900)
            frame.CanvasPosition = Vector2.new(12, 30)
            frame.AutomaticCanvasSize = Enum.AutomaticSize.Y
            local layout = Instance.new("UIListLayout", frame)
            local grid = Instance.new("UIGridLayout")
            assert(grid.FillDirection == Enum.FillDirection.Horizontal)
            layout.Padding = UDim.new(0, 8)
            layout.SortOrder = Enum.SortOrder.LayoutOrder
            layout.FillDirection = Enum.FillDirection.Horizontal
            local padding = Instance.new("UIPadding", frame)
            padding.PaddingLeft = UDim.new(0, 10)
            local corner = Instance.new("UICorner", frame)
            corner.CornerRadius = UDim.new(0, 9)
            local stroke = Instance.new("UIStroke", frame)
            stroke.Thickness = 2
            stroke.Color = Color3.new(1, 0, 0)
            local clone = frame:Clone()
            assert(clone.CanvasPosition == Vector2.new(12, 30))
            assert(clone.UIListLayout.Padding == UDim.new(0, 8))
            assert(clone.UIPadding.PaddingLeft.Offset == 10)
            assert(not pcall(function() stroke.Thickness = -1 end))
            """, 0, "", "");
}
