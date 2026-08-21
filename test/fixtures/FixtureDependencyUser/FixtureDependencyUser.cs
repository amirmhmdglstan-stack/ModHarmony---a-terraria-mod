using Terraria.ModLoader;

namespace FixtureDependencyUser;

/// <summary>Test fixture: depends on FixtureDependencyBase; declares an optional dependency that is not installed.</summary>
public sealed class FixtureDependencyUser : Mod
{
	public override object Call(params object[] args) => null;
}
