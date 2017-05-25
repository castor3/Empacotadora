using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;

namespace Empacotadora
{
	class Recipes
	{
		public static Dictionary<string, int> GetRoundTubeRecipe(ref int tubeNmbr)
		{
			byte bigRow = 0, smallRow = 0, shapeSize = 0;
			int Vpos = 0, Hpos = 0;
			string RoundTubeRecipePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\RoundTubeRecipes.txt";

			var linesFromFile = File.ReadLines(RoundTubeRecipePath);
			foreach (var line in linesFromFile)
			{
				string[] array = line.Split(',');
				try
				{
					if (array[0] == tubeNmbr.ToString())
					{
						byte.TryParse(array[1], out bigRow);
						byte.TryParse(array[2], out smallRow);
						int.TryParse(array[3], out Vpos);
						int.TryParse(array[4], out Hpos);
						byte.TryParse(array[5], out shapeSize);
					}
				}
				catch (IndexOutOfRangeException)
				{
					MessageBox.Show("Erro de index ao ler do ficheiro");
					continue;
				}
			}
			Dictionary<string, int> details = new Dictionary<string, int>()
			{
				{ "bigRowSize", bigRow },
				{ "smallRowSize", smallRow },
				{ "Vpos", Vpos },
				{ "Hpos", Hpos },
				{ "shapeSize", shapeSize }
			};
			return details;
		}
		public static Dictionary<string, int> GetSquareTubeRecipe(ref int tubeNmbr)
		{
			byte shapeSize = 0;
			bool found = false;
			int Vpos = 0, Hpos = 0;
			string SquareTubeRecipePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\SquareTubeRecipes.txt";
			string RectTubeRecipePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\RectTubeRecipes.txt";

			var linesFromFile = File.ReadLines(SquareTubeRecipePath);
			foreach (var line in linesFromFile)
			{
				string[] array = line.Split(',');
				try
				{
					if (array[0] == tubeNmbr.ToString())
					{
						int.TryParse(array[1], out Vpos);
						int.TryParse(array[2], out Hpos);
						byte.TryParse(array[3], out shapeSize);
						found = true;
					}
				}
				catch (IndexOutOfRangeException)
				{
					MessageBox.Show("Erro de index ao ler do ficheiro");
					continue;
				}
			}
			if (found == false)
			{
				linesFromFile = File.ReadLines(RectTubeRecipePath);
				foreach (var line in linesFromFile)
				{
					string[] array = line.Split(',');
					try
					{
						if (array[0] == tubeNmbr.ToString())
						{
							int.TryParse(array[1], out Vpos);
							int.TryParse(array[2], out Hpos);
							byte.TryParse(array[3], out shapeSize);
						}
					}
					catch (IndexOutOfRangeException)
					{
						MessageBox.Show("Erro de index ao ler do ficheiro");
						continue;
					}
				}
			}

			Dictionary<string, int> values = new Dictionary<string, int>()
			{
				{ "Vpos", Vpos },
				{ "Hpos", Hpos },
				{ "shapeSize", shapeSize }
			};
			return values;
		}

		public static List<RoundTubeRecipe> ReadTubeRecipesFromFile(ref string path)
		{
			List<RoundTubeRecipe> recipes = new List<RoundTubeRecipe>();
			var linesFromFile = File.ReadLines(path);

			foreach (var line in linesFromFile)
			{
				string[] array = line.Split(',');
				try
				{
					recipes.Add(new RoundTubeRecipe()
					{
						TubeNumber = array[0],
						BigRow = array[1],
						SmallRow = array[2],
						Vpos = array[3],
						Hpos = array[4],
						ShapeSize = array[5],
					});
				}
				catch (IndexOutOfRangeException)
				{
					continue;
				}
			}
			return recipes;
		}
		public static List<SquareTubeRecipe> ReadTubeRecipesFromFile(ref string pathSquareTubes, ref string pathRectTubes)
		{
			List<SquareTubeRecipe> recipes = new List<SquareTubeRecipe>();
			IEnumerable<string> linesFromFile;

			linesFromFile = File.ReadLines(pathSquareTubes);
			foreach (var line in linesFromFile)
			{
				string[] array = line.Split(',');
				try
				{
					recipes.Add(new SquareTubeRecipe()
					{
						TubeNumber = array[0],
						Vpos = array[1],
						Hpos = array[2],
						ShapeSize = array[3],
						Colums = (array[4] != "" ? array[4] : "Auto"),
						Rows = (array[5] != "" ? array[5] : "Auto"),
					});
				}
				catch (IndexOutOfRangeException)
				{
					continue;
				}
			}

			linesFromFile = File.ReadLines(pathRectTubes);
			foreach (var line in linesFromFile)
			{
				string[] array = line.Split(',');
				try
				{
					recipes.Add(new SquareTubeRecipe()
					{
						TubeNumber = array[0],
						Vpos = array[1],
						Hpos = array[2],
						ShapeSize = array[3],
						Colums = (array[4] != "" ? array[4] : "Auto"),
						Rows = (array[5] != "" ? array[5] : "Auto"),
					});
				}
				catch (IndexOutOfRangeException)
				{
					continue;
				}
			}

			return recipes;
		}

		public static int[] GetStrapsPositionFromRecipe(ref int length, ref byte strapsNmbr)
		{
			// 1st[0] and last[x] straps always sit at 400 mm off the edges
			int edge = 400;
			int[] position = new int[strapsNmbr];
			position[0] = edge;

			int trimmed_length = (length - (edge * 2));
			switch (strapsNmbr)
			{
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
		public string Colums { get; set; }
		public string Rows { get; set; }
		public string Vpos { get; set; }
		public string Hpos { get; set; }
		public string ShapeSize { get; set; }
	}
}