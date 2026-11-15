using Woorf.Engine.DataModel;

namespace Woorf.Engine.Networking;

internal sealed record RemoteMessage(RemoteEvent Remote, RuntimeEndpoint Target, Player? Sender, object?[] Arguments);
