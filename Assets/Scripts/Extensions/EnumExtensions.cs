using Assets.Scripts.Data;

namespace Assets.Scripts.Extensions
{
	public static class EnumExtensions
	{
		public static string ToUnitTag(this Side side) => $"unit_{side.ToString().ToLowerInvariant()}";
	}
}
