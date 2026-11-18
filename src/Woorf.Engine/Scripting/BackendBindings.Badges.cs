namespace Woorf.Engine.Scripting;

internal sealed partial class BackendBindings
{
    private static bool IsBadgeMethod(string method) => method is
        "AwardBadge" or "AwardBadgeAsync" or "UserHasBadge" or "UserHasBadgeAsync" or
        "CheckUserBadgesAsync" or "GetUserBadgesAsync" or "GetBadgeInfoAsync" or "IsDisabled" or "IsLegal";

    private int CallBadgeStub(nint state, string method) => method switch
    {
        "CheckUserBadgesAsync" or "GetUserBadgesAsync" => List(state, []),
        "GetBadgeInfoAsync" => Push(state, new LuaTableValue([
            new("Name", ""), new("Description", ""), new("IconImageId", 0d), new("IsEnabled", false)
        ])),
        "IsDisabled" => Push(state, true),
        _ => Push(state, false)
    };
}
