using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Empacotadora
{
	class General
	{
		public enum ActiveTubeType { Round, Square }
		public enum ActiveWrapType { Hexagonal, Square }
		public enum Layout { Wrapper, NewOrder, StrapsNewOrder, EditOrder, StrapsEditOrder, EditCurrentOrder, StrapsEditCurrentOrder, Strapper, Storage, Recipes, NewRecipe, History }
		public static Layout CurrentLayout;
		public enum ActiveDate { Initial, End }
		public enum ActiveRecipe { RoundTube, SquareTube }
		//// Fields ////
		const byte _shapeMargin = 2;
		static readonly string _systemPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Empacotadora";
		//// Properties ////
		// Diretories
		public static string OrdersPath { get; } = _systemPath + @"\Orders.txt";
		public static string HistoryPath { get; } = _systemPath + @"\PackageHistory.txt";
		public static string PathSquareTubes { get; } = _systemPath + @"\SquareTubeRecipes.txt";
		public static string PathRectTubes { get; } = _systemPath + @"\RectTubeRecipes.txt";
		public static string PathRoundTubes { get; } = _systemPath + @"\RoundTubeRecipes.txt";
		public static string PathRopeStraps { get; } = _systemPath + @"\RopeStraps.txt";
		public static string RoundTubeRecipePath { get; } = _systemPath + @"\RoundTubeRecipes.txt";
		public static string SquareTubeRecipePath { get; } = _systemPath + @"\SquareTubeRecipes.txt";
		public static string RectTubeRecipePath { get; } = _systemPath + @"\RectTubeRecipes.txt";
		//// Methods ////
		/// <summary>
		/// Gets generic type Childs from generic type Control
		/// </summary>
		public static IEnumerable<T> GetTFromControl<T>(DependencyObject depObj) where T : DependencyObject
		{
			if (depObj == null) yield break;
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++) {
				DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
				if (child != null && child is T)
					yield return (T)child;
				foreach (T childOfChild in GetTFromControl<T>(child))
					yield return childOfChild;
			}
		}
		public static void SetTextBoxForEdit(TextBox item)
		{
			item.Background = Brushes.YellowBrush;
			item.IsReadOnly = false;
			item.Focusable = true;
		}
		public static string[] GetStrapsValuesFromTextBoxes(IEnumerable<TextBox> TextBoxes)
		{
			string[] values = new string[TextBoxes.Count()];
			byte i = 0;
			foreach (var textbBox in TextBoxes) {
				values[i] = textbBox.Text;
				++i;
			}
			return values;
		}
		public static void ClearTextBoxes(Grid grid)
		{
			foreach (TextBox item in GetTFromControl<TextBox>(grid))
				item.Text = "";
		}
		// Rope Straps
		public static IEnumerable<string> GetAllRopeStrapsFromFile(string pathRopeStraps)
		{
			if (!Document.ReadFromFile(pathRopeStraps, out IEnumerable<string> linesFromFile))
				return Enumerable.Empty<string>();
			return linesFromFile;
		}
		public static bool CheckIfRopeIsValid(int packagePerimeter, int packageWeight, string[] values)
		{
			bool ropePerimeterIsValid = false, ropeWeightIsValid = false;
			if (!int.TryParse(values[1], out int ropePerimeter) ||
				!int.TryParse(values[2], out int ropeWeight)) return false;
			ropePerimeterIsValid = (ropePerimeter >= packagePerimeter + 150 &&
										ropePerimeter <= packagePerimeter + 300);
			ropeWeightIsValid = (ropeWeight >= packageWeight + 50);
			return (ropePerimeterIsValid && ropeWeightIsValid);
		}
		// Draw Shapes
		public static void PutShapesInCanvas<T>(IEnumerable<T> listOfShapes, Canvas atado) where T : Shape
		{
			atado.Children.Clear();
			foreach (var shape in listOfShapes)
				atado.Children.Add(shape);
		}
		// Round (ellipse) shape
		public static void CreateEllipseShapesToBeDrawn(int tubeAmount, Dictionary<string,int> recipeValues, ref int columns, ref int rows, ICollection<Ellipse> listEllipses)
		{
			int shapeDiameter = recipeValues["shapeSize"];
			int tubeAmountBigLine = recipeValues["bigRowSize"];
			int tubeAmountSmallLine = recipeValues["smallRowSize"];
			int vPosInit = recipeValues["vPos"];
			int hPosInit = recipeValues["hPos"];
			int hPos = hPosInit, vPos = vPosInit, tubeCurrentlyDrawing = 0;
			byte variavel = 0, lineCap = 0;
			bool incrementing = false;
			for (byte i = 0; i < tubeAmountBigLine; i++) {
				++rows;
				hPosInit = hPos;
				if ((tubeAmountSmallLine + i) < tubeAmountBigLine) {
					lineCap = (byte)(tubeAmountSmallLine + i - 1);
					incrementing = true;
				}
				else if ((tubeAmountSmallLine + i) >= tubeAmountBigLine) {
					variavel++;
					lineCap = (byte)(tubeAmountBigLine - variavel);
					incrementing = false;
				}
				for (int j = 0; j <= lineCap; j++) {
					if (lineCap >= columns)
						++columns;
					Ellipse ellip = new Ellipse() {
						Stroke = Brushes.BlackBrush,
						Width = shapeDiameter,
						Height = shapeDiameter
					};
					Canvas.SetLeft(ellip, hPos);
					Canvas.SetTop(ellip, (vPos - ellip.Height));
					// prevent shape from being drawn if total number of tubes was reached
					if (tubeCurrentlyDrawing < tubeAmount) {
						ellip.StrokeThickness = 2;
						ellip.Fill = (tubeCurrentlyDrawing < Win_Main.LastTube) ? Brushes.IndianRed : Brushes.GrayBrush;
					}
					else
						ellip.StrokeThickness = 0;
					listEllipses.Add(ellip);
					hPos += shapeDiameter + _shapeMargin;
					++tubeCurrentlyDrawing;
				}
				int hPosIncrem = hPosInit - ((shapeDiameter / 2) + (_shapeMargin / 2));
				int hPosDecrem = hPosInit + ((shapeDiameter / 2) + (_shapeMargin / 2));
				hPos = incrementing ? hPosIncrem : hPosDecrem;

				vPos -= shapeDiameter + (_shapeMargin / 2);
			}
		}
		// Rectangle shape
		public static void CreateRectangleShapesToBeDrawn(int tubeAmount, Dictionary<string, int> recipeValues, ICollection<Rectangle> listRectangles)
		{
			int shapeWidth = recipeValues["shapeWidth"];
			int shapeHeight = recipeValues["shapeHeight"];
			int vPosInit = recipeValues["vPos"];
			int hPosInit = recipeValues["hPos"];
			int columns = recipeValues["columns"];
			int rows = recipeValues["rows"];
			int hPos = hPosInit, vPos = vPosInit, tubeCurrentlyDrawing = 0;
			for (int i = 0; i < rows; i++) {
				for (int j = 0; j < columns; j++) {
					Rectangle rect = new Rectangle() {
						Stroke = Brushes.BlackBrush,
						Width = shapeWidth,
						Height = shapeHeight
					};
					Canvas.SetLeft(rect, hPos);
					Canvas.SetTop(rect, (vPos - rect.Height));
					// prevent shape from being drawn if total number of tubes was reached
					if (tubeCurrentlyDrawing < tubeAmount) {
						rect.StrokeThickness = 2;
						rect.Fill = (tubeCurrentlyDrawing < Win_Main.LastTube) ? Brushes.IndianRed : Brushes.GrayBrush;
					}
					else
						rect.StrokeThickness = 0;
					listRectangles.Add(rect);
					hPos += shapeWidth + _shapeMargin;
					++tubeCurrentlyDrawing;
				}
				hPos = hPosInit;
				vPos -= shapeHeight + _shapeMargin;
			}
		}
		public static Tuple<double, double> CalculateNumberOfRowsAndColummsFromTubeAmount(int tubeAmount, int width, int height)
		{
			// divides the number of tubes until the number of rows and collums is even (+/-)
			double start, result = 0, temp1 = 0, temp2 = 0;
			int packageWidth = 0, packageHeight = 0;
			start = (tubeAmount > 300) ? 30 : 15;
			packageWidth = width * (int)(start + 1);
			packageHeight = height * (int)result;
			// packagewidth can never be higher than packageHeight
			while (packageHeight <= packageWidth) {
				temp1 = result;
				temp2 = start;
				result = tubeAmount / start;
				packageWidth = width * (int)(--start + 1);
				packageHeight = height * (int)result;
			}
			double columns = temp1;
			double rows = temp2 + 1;
			return new Tuple<double, double>(rows, columns);
		}
		// Recipes
		public static bool EditSquareTubeRecipesTextFile(ICollection<string> newFileContent, string path, string recipeTubes, string recipeColumns, string recipeRows)
		{// This method will "out"/"ref" the newFileContent because "ICollection" is of reference type
			bool success = false;
			if (!Document.ReadFromFile(path, out IEnumerable<string> linesFromFile)) return false;
			foreach (string item in linesFromFile) {
				string newline = "";
				string[] array = item.Split(',');
				if (array[0] == recipeTubes) {
					array[array.Length - 2] = recipeColumns;
					array[array.Length - 1] = recipeRows;
					success = true;
					//foreach (string value in array)
					//	newline += value + ",";
					newline = array.Aggregate(newline, (current, value) => current + (value + ","));
					newline = newline.Remove(newline.Length - 1);
				}
				newFileContent.Add(newline == "" ? item : newline);
			}
			return success;
		}
		public static bool EditRoundTubeRecipesTextFile(ICollection<string> newFileContent, string recipeTubes, string recipeBigRow, string recipeSmallRow)
		{// This method acts as "ref" to newFileContent because "ICollection" is of reference type
			if (!Document.ReadFromFile(PathRoundTubes, out IEnumerable<string> linesFromFile)) return false;
			foreach (string item in linesFromFile) {
				string newline = "";
				string[] array = item.Split(',');
				if (array[0] == recipeTubes) {
					array[1] = recipeBigRow;
					array[2] = recipeSmallRow;
					//foreach (string value in array)
					//	newline += value + ",";
					newline = array.Aggregate(newline, (current, value) => current + (value + ","));
					newline = newline.Remove(newline.Length - 1);
				}
				newFileContent.Add(newline == "" ? item : newline);
			}
			return true;
		}
	}
}
