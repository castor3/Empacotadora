using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace Empacotadora {
	class Recipes {
		public static Dictionary<string, int> GetRoundTubeRecipe(int tubeNmbr) {
			string RoundTubeRecipePath = Win_Main.systemPath + @"\RoundTubeRecipes.txt";
			Dictionary<string, int> recipeValues = new Dictionary<string, int>();
			if (!Document.ReadFromFile(RoundTubeRecipePath, out IEnumerable<string> linesFromFile)) return recipeValues;
			ParseRoundTubeRecipeValues(linesFromFile, tubeNmbr, out byte bigRow, out byte smallRow, out byte shapeSize, out int Vpos, out int Hpos);
			recipeValues = new Dictionary<string, int>()
			{
				{ "bigRowSize", bigRow },
				{ "smallRowSize", smallRow },
				{ "Vpos", Vpos },
				{ "Hpos", Hpos },
				{ "shapeSize", shapeSize }
			};
			return recipeValues;
		}
		private static void ParseRoundTubeRecipeValues(IEnumerable<string> linesFromFile, int tubeNmbr, out byte bigRow, out byte smallRow, out byte shapeSize, out int Vpos, out int Hpos) {
			bigRow = smallRow = shapeSize = 0; Vpos = Hpos = 0;
			foreach (string line in linesFromFile) {
				string[] array = line.Split(',');
				try {
					if (array[0] == tubeNmbr.ToString()) {
						byte.TryParse(array[1], out bigRow);
						byte.TryParse(array[2], out smallRow);
						int.TryParse(array[3], out Vpos);
						int.TryParse(array[4], out Hpos);
						byte.TryParse(array[5], out shapeSize);
					}
				}
				catch (IndexOutOfRangeException) { return; }
			}
		}
		public static Dictionary<string, int> GetSquareTubeRecipe(int tubeNmbr) {
			bool found = false;
			string SquareTubeRecipePath = Win_Main.systemPath + @"\SquareTubeRecipes.txt";
			string RectTubeRecipePath = Win_Main.systemPath + @"\RectTubeRecipes.txt";
			Dictionary<string, int> recipeValues = new Dictionary<string, int>();
			if (!Document.ReadFromFile(SquareTubeRecipePath, out IEnumerable<string> linesFromFile)) return recipeValues;
			ParseSquareTubeRecipeValues(linesFromFile, tubeNmbr, out byte shapeSize, out int Vpos, out int Hpos, ref found);
			if (found == false) {
				if (!Document.ReadFromFile(RectTubeRecipePath, out IEnumerable<string> linesFromFileOfRectangleRecipe)) return recipeValues;
				ParseSquareTubeRecipeValues(linesFromFileOfRectangleRecipe, tubeNmbr, out shapeSize, out Vpos, out Hpos, ref found);
			}
			recipeValues = new Dictionary<string, int>()
			{
				{ "Vpos", Vpos },
				{ "Hpos", Hpos },
				{ "shapeSize", shapeSize }
			};
			return recipeValues;
		}
		private static void ParseSquareTubeRecipeValues(IEnumerable<string> linesFromFile, int tubeNmbr, out byte shapeSize, out int Vpos, out int Hpos, ref bool found) {
			shapeSize = 0; Vpos = 0; Hpos = 0;
			foreach (string line in linesFromFile) {
				string[] array = line.Split(',');
				try {
					if (array[0] == tubeNmbr.ToString()) {
						int.TryParse(array[1], out Vpos);
						int.TryParse(array[2], out Hpos);
						byte.TryParse(array[3], out shapeSize);
						found = true;
					}
				}
				catch (IndexOutOfRangeException) { return; }
			}
		}
		public static List<RoundTubeRecipe> ReadTubeRecipesFromFile(string path) {
			List<RoundTubeRecipe> recipes = new List<RoundTubeRecipe>();
			if (!Document.ReadFromFile(path, out IEnumerable<string> linesFromFile)) return recipes;
			AddRoundTubeRecipeValuesToList(recipes, linesFromFile);
			return recipes;
		}
		private static void AddRoundTubeRecipeValuesToList(List<RoundTubeRecipe> recipes, IEnumerable<string> linesFromFile) {
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
		public static List<SquareTubeRecipe> ReadTubeRecipesFromFile(string pathSquareTubes, string pathRectTubes) {
			List<SquareTubeRecipe> recipes = new List<SquareTubeRecipe>();
			if (!Document.ReadFromFile(pathSquareTubes, out IEnumerable<string> linesFromFile)) return recipes;
			AddSquareTubeRecipeValuesToList(recipes, linesFromFile);
			if (!Document.ReadFromFile(pathRectTubes, out IEnumerable<string> linesFromFileOfRectangleTubes)) return recipes;
			AddSquareTubeRecipeValuesToList(recipes, linesFromFileOfRectangleTubes);
			return recipes;
		}
		private static void AddSquareTubeRecipeValuesToList(List<SquareTubeRecipe> recipes, IEnumerable<string> linesFromFile) {
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
		public static int[] GetStrapsPositionFromRecipe(int length, byte strapsNmbr) {
			// 1st[0] and last[x] straps always sit at 400 mm off the edges
			int edge = 400;
			int[] position = new int[strapsNmbr];
			position[0] = edge;

			int trimmed_length = (length - (edge * 2));
			switch (strapsNmbr) {
				case 2:
					position[1] = length - edge;
					break;
				case 3:
					position[1] = length / 2;
					position[2] = length - edge;
					break;
				case 4:
					position[1] = edge + trimmed_length / 3;
					position[2] = trimmed_length - (trimmed_length / 3);
					position[3] = length - edge;
					break;
				case 5:
					position[1] = edge + trimmed_length / 4;
					position[2] = length / 2;
					position[3] = length - edge - (trimmed_length / 4);
					position[4] = length - edge;
					break;
				case 6:
					position[1] = edge + trimmed_length / 5;
					position[2] = edge + ((trimmed_length / 5) * 2);
					position[3] = length - edge - ((trimmed_length / 5) * 2);
					position[4] = length - edge - (trimmed_length / 5);
					position[5] = length - edge;
					break;
				default:
					MessageBox.Show("Número de cintas não reconhecido");
					break;
			}
			return position;
		}
	}
	class RoundTubeRecipe : Recipes {
		public string TubeNumber { get; set; }
		public string BigRow { get; set; }
		public string SmallRow { get; set; }
		public string Vpos { get; set; }
		public string Hpos { get; set; }
		public string ShapeSize { get; set; }
	}
	class SquareTubeRecipe : Recipes {
		public string TubeNumber { get; set; }
		public string Columns { get; set; }
		public string Rows { get; set; }
		public string Vpos { get; set; }
		public string Hpos { get; set; }
		public string ShapeSize { get; set; }
	}
}