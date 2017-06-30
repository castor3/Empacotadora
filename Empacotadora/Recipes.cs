using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Empacotadora
{
	class Recipes
	{
		public static Dictionary<string, int> GetRoundTubeRecipe(int tubeNmbr)
		{
			if (!Document.ReadFromFile(General.RoundTubeRecipePath, out IEnumerable<string> linesFromFile)) return null;
			var values = ParseRoundTubeRecipeValues(linesFromFile, tubeNmbr);
			Dictionary<string, int> recipeValues = new Dictionary<string, int>()
			{
				{ "bigRowSize", values.Item1 },
				{ "smallRowSize", values.Item2 },
				{ "vPos", values.Item3 },
				{ "hPos", values.Item4 },
				{ "shapeSize", values.Item5 }
			};
			return recipeValues;
		}
		public static Dictionary<string, int> GetSquareTubeRecipe(int tubeNmbr)
		{
			Dictionary<string, int> recipeValues = new Dictionary<string, int>();
			if (!Document.ReadFromFile(General.SquareTubeRecipePath, out IEnumerable<string> linesFromFile)) return null;
			var values = ParseSquareTubeRecipeValues(linesFromFile, tubeNmbr);
			recipeValues = new Dictionary<string, int>()
			{
				{ "vPos", values.Item1 },
				{ "hPos", values.Item2 },
				{ "columns", values.Item3 },
				{ "rows", values.Item4 },
				{ "shapeSize", values.Item5 }
			};
			return recipeValues;
		}
		public static Dictionary<string, int> GetRectTubeRecipe(int tubeNmbr)
		{
			Dictionary<string, int> recipeValues = new Dictionary<string, int>();
			if (!Document.ReadFromFile(General.RectTubeRecipePath, out IEnumerable<string> linesFromFile)) return null;
			var values = ParseSquareTubeRecipeValues(linesFromFile, tubeNmbr);
			recipeValues = new Dictionary<string, int>()
			{
				{ "vPos", values.Item1 },
				{ "hPos", values.Item2 },
				{ "columns", values.Item3 },
				{ "rows", values.Item4 },
				{ "shapeSize", values.Item5 }
			};
			return recipeValues;
		}
		private static Tuple<byte, byte, int, int, byte> ParseRoundTubeRecipeValues(IEnumerable<string> linesFromFile, int tubeNmbr)
		{
			byte bigRow = 0, smallRow = 0, shapeSize = 0;
			int vPos = 0, hPos = 0;
			var values = new Tuple<byte, byte, int, int, byte>(bigRow, smallRow, vPos, hPos, shapeSize);
			foreach (string line in linesFromFile) {
				string[] array = line.Split(',');
				try {
					if (array[0] == tubeNmbr.ToString()) {
						byte.TryParse(array[1], out bigRow);
						byte.TryParse(array[2], out smallRow);
						int.TryParse(array[3], out vPos);
						int.TryParse(array[4], out hPos);
						byte.TryParse(array[5], out shapeSize);
						return new Tuple<byte, byte, int, int, byte>(bigRow, smallRow, vPos, hPos, shapeSize);
					}
				}
				catch (IndexOutOfRangeException) { return values; }
			}
			return values;
		}
		private static Tuple<int, int, byte, byte, byte> ParseSquareTubeRecipeValues(IEnumerable<string> linesFromFile, int tubeNmbr)
		{
			int vPos = 0, hPos = 0;
			byte shapeSize = 0, columns = 0, rows = 0;
			var values = new Tuple<int, int, byte, byte, byte>(vPos, hPos, columns, rows, shapeSize);
			foreach (string line in linesFromFile) {
				string[] array = line.Split(',');
				try {
					if (array[0] == tubeNmbr.ToString()) {
						int.TryParse(array[1], out vPos);
						int.TryParse(array[2], out hPos);
						byte.TryParse(array[3], out shapeSize);
						byte.TryParse(array[4], out columns);
						byte.TryParse(array[5], out rows);
						return new Tuple<int, int, byte, byte, byte>(vPos, hPos, columns, rows, shapeSize);
					}
				}
				catch (IndexOutOfRangeException) { return values; }
			}
			return values;
		}
		public static ICollection<RoundTubeRecipe> ReadTubeRecipesFromFile(string path)
		{
			ICollection<RoundTubeRecipe> recipes = new Collection<RoundTubeRecipe>();
			if (!Document.ReadFromFile(path, out IEnumerable<string> linesFromFile)) return recipes;
			AddRoundTubeRecipeValuesToList(recipes, linesFromFile);
			return recipes;
		}
		public static ICollection<SquareTubeRecipe> ReadTubeRecipesFromFile(string pathSquareTubes, string pathRectTubes)
		{
			ICollection<SquareTubeRecipe> recipes = new Collection<SquareTubeRecipe>();
			if (!Document.ReadFromFile(pathSquareTubes, out IEnumerable<string> linesFromFile)) return recipes;
			AddSquareTubeRecipeValuesToList(recipes, linesFromFile);
			if (!Document.ReadFromFile(pathRectTubes, out IEnumerable<string> linesFromFileOfRectangleTubes)) return recipes;
			AddSquareTubeRecipeValuesToList(recipes, linesFromFileOfRectangleTubes);
			return recipes;
		}
		private static void AddRoundTubeRecipeValuesToList(ICollection<RoundTubeRecipe> recipes, IEnumerable<string> linesFromFile)
		{
			foreach (string line in linesFromFile) {
				string[] array = line.Split(',');
				try {
					recipes.Add(new RoundTubeRecipe() {
						TubeNumber = array[0],
						BigRow = array[1],
						SmallRow = array[2],
						Vpos = array[3],
						Hpos = array[4],
						ShapeSize = array[5],
					});
				}
				catch (IndexOutOfRangeException) { return; }
			}
		}
		private static void AddSquareTubeRecipeValuesToList(ICollection<SquareTubeRecipe> recipes, IEnumerable<string> linesFromFile)
		{
			foreach (string line in linesFromFile) {
				string[] array = line.Split(',');
				try {
					recipes.Add(new SquareTubeRecipe() {
						TubeNumber = array[0],
						Vpos = array[1],
						Hpos = array[2],
						ShapeSize = array[3],
						Columns = (array[4] != "" ? array[4] : "Auto"),
						Rows = (array[5] != "" ? array[5] : "Auto"),
					});
				}
				catch (IndexOutOfRangeException) { return; }
			}
		}
		public static int[] GetStrapsPositionFromRecipe(int length, byte strapsNmbr)
		{
			// 1st[0] and last[x] straps always sit at 400 mm off the edges
			const int edge = 400;
			int[] position = new int[strapsNmbr];
			position[0] = edge;

			int trimmedLength = (length - (edge * 2));
			switch (strapsNmbr) {
				case 2:
					position[1] = length - edge;
					break;
				case 3:
					position[1] = length / 2;
					position[2] = length - edge;
					break;
				case 4:
					position[1] = edge + trimmedLength / 3;
					position[2] = trimmedLength - (trimmedLength / 3);
					position[3] = length - edge;
					break;
				case 5:
					position[1] = edge + trimmedLength / 4;
					position[2] = length / 2;
					position[3] = length - edge - (trimmedLength / 4);
					position[4] = length - edge;
					break;
				case 6:
					position[1] = edge + trimmedLength / 5;
					position[2] = edge + ((trimmedLength / 5) * 2);
					position[3] = length - edge - ((trimmedLength / 5) * 2);
					position[4] = length - edge - (trimmedLength / 5);
					position[5] = length - edge;
					break;
				default:
					MessageBox.Show("Número de cintas não reconhecido");
					break;
			}
			return position;
		}
	}
	class RoundTubeRecipe : Recipes
	{
		public string TubeNumber { get; set; }
		public string BigRow { get; set; }
		public string SmallRow { get; set; }
		public string Vpos { get; set; }
		public string Hpos { get; set; }
		public string ShapeSize { get; set; }
	}
	class SquareTubeRecipe : Recipes
	{
		public string TubeNumber { get; set; }
		public string Columns { get; set; }
		public string Rows { get; set; }
		public string Vpos { get; set; }
		public string Hpos { get; set; }
		public string ShapeSize { get; set; }
	}
}