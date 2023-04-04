using Assets.Scripts.Data;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using UnityEngine;

#nullable enable
public class MapTile
{
	// static
	public static readonly IReadOnlyDictionary<RangeType, Color> HighlightColors = new Dictionary<RangeType, Color>()
	{
		{ RangeType.Movement, new Color(0, 0, 1, .5f) },
		{ RangeType.Attack, new Color(1, 0, 0, .5f) }
	};

	public static MapTileData[][] CreateGridFromJson(string json)
	{// anonymous types are ugly af, but are they uglier than having a separate JsonHelper type is annoying? not a rhetorical question. im genuinely struggling to decide -morgan 2023-04-04
		var deserialized = JsonConvert.DeserializeAnonymousType(json,
							new { TileDictionary =  new Dictionary<string, MapTileData>(), TileGrid = new string[0][]},
							new JsonSerializerSettings(){ DefaultValueHandling = DefaultValueHandling.Populate })!;

		(Dictionary<string, MapTileData> tileDictionary, string[][] presetGrid) = (deserialized.TileDictionary, deserialized.TileGrid);


		MapTileData[][] outputGrid = new MapTileData[presetGrid.Length][];
		for (int i = 0; i < outputGrid.Length; i++)
		{
			outputGrid[i] = new MapTileData[presetGrid[i].Length];
			for (int j = 0; j < outputGrid[i].Length; j++)
				outputGrid[i][j] = tileDictionary[presetGrid[i][j]];
		}

		return outputGrid;
	}

	// instance
	public Vector2Int Position { get; }
	public MapTileData Data { get; }

	public Unit? OccupyingUnit { get; set; }
	public Side OccupyingSide => OccupyingUnit == null ? Side.None : OccupyingUnit.Side; // TODO rename this pls -morgan 2023-04-02

	public MapTile(Vector2Int position, MapTileData? data = null)
	{
		Position = position;
		if (data is not null)
			Data = data.Value;
		else
			Data = MapTileData.Default;
	}

	public bool IsPassableBy(Unit unit) => OccupyingSide == Side.None || OccupyingSide == unit.Side;

	public override string ToString() => $"{Position} | {Data.MovementCost}mov | {OccupyingSide}";
	public string ToDebugString() => $"{Position}\n{Data.ToString().Replace(", ", "\n")}";

	public readonly struct MapTileData
	{
		public static MapTileData Default = new MapTileData(1, 0, 0, 0, 0);

		[DefaultValue(1)] public int MovementCost { get; }
		public int AvoBonus { get; }
		public int DefBonus { get; }
		public int ResBonus { get; }
		public int HealPercent { get; }

		public MapTileData(int movementCost = 1, int avoBonus = 0, int defBonus = 0, int resBonus = 0, int healPercent = 0)
		{
			MovementCost = movementCost;
			AvoBonus = avoBonus;
			DefBonus = defBonus;
			ResBonus = resBonus;
			HealPercent = healPercent;
		}
		public override string ToString()
		{
			StringBuilder output = new StringBuilder($"Movement cost: {MovementCost}");
			if (AvoBonus != Default.AvoBonus)
				output.Append($", Avo bonus: {AvoBonus}");
			if (DefBonus != Default.DefBonus)
				output.Append($", Def bonus: {DefBonus}");
			if (ResBonus != Default.ResBonus)
				output.Append($", Res bonus: {ResBonus}");
			if (HealPercent != Default.HealPercent)
				output.Append($", Heal percent: {HealPercent}");

			return output.ToString();
		}
	}
}
