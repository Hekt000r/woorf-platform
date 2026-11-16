using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Physics;

internal readonly record struct PhysicsContact(BasePart Other, Vector3 Point, Vector3 Normal, float Separation);
