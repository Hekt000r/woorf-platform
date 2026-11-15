using Woorf.Engine.DataModel;
using GameDataModel = Woorf.Engine.DataModel.DataModel;

namespace Woorf.Engine.Projects;

public sealed record ProjectScript(Instance Instance, string Source, string FilePath);

public sealed record LoadedProject(GameDataModel Game, IReadOnlyList<ProjectScript> Scripts);
