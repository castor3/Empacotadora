using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace Empacotadora {
	class General {
		public enum ActiveTubeType { Round, Square }
		public enum ActiveWrapType { Hexagonal, Square }
		public enum ActiveLayout { Wrapper, EditOrder, Strapper, Storage, Recipes, NewRecipe, History }
		public enum ActiveDate { Initial, End }
		public enum ActiveRecipe { RoundTube, SquareTube }
		//// Fields ////
		const byte _shapeMargin = 2;
		static readonly string _systemPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Empacotadora";
		//// Properties ////
		// Diretories
		public static string Path { get; } = _systemPath + @"\Orders.txt";
		public static string HistoryPath { get; } = _systemPath + @"\PackageHistory.txt";
		public static string PathSquareTubes { get; } = _systemPath + @"\SquareTubeRecipes.txt";
		public static string PathRectTubes { get; } = _systemPath + @"\RectTubeRecipes.txt";
		public static string PathRoundTubes { get; } = _systemPath + @"\RoundTubeRecipes.txt";
		public static string PathRopeStraps { get; } = _systemPath + @"\RopeStraps.txt";
		public static string RoundTubeRecipePath { get; } = _systemPath + @"\RoundTubeRecipes.txt";
		public static string SquareTubeRecipePath { get; } = _systemPath + @"\SquareTubeRecipes.txt";
		public static string RectTubeRecipePath { get; } = _systemPath + @"\RectTubeRecipes.txt";
		//// Methods ////
		public static IEnumerable<TextBox> GetTextBoxesFromGrid(Grid currentGrid) {
			IEnumerable<TextBox> textBoxes = Enumerable.Empty<TextBox>();
			if (currentGrid == null) return textBoxes;
			textBoxes = currentGrid.Children.OfType<TextBox>();
			return textBoxes;
		}
		public static void SetTextBoxForEdit(TextBox item) {
			item.Background = Brushes.YellowBrush;
			item.IsReadOnly = false;
			item.Focusable = true;
		}
		// Rope Straps
		public static IEnumerable<string> GetAllRopeStrapsFromFile(string pathRopeStraps) {
			if (!Document.ReadFromFile(pathRopeStraps, out IEnumerable<string> linesFromFile))
				return Enumerable.Empty<string>();
			return linesFromFile;
		}
		public static bool CheckIfRopeIsValid(int packagePerimeter, int packageWeight, string[] values) {
			bool ropeIsValid = false, ropePerimeterIsValid = false, ropeWeightIsValid = false;
			int ropePerimeter = 0, ropeWeight = 0;
			try {
				ropePerimeter = int.Parse(values[1]);
				ropeWeight = int.Parse(values[2]);
			}
			catch (Exception exc) when (exc is ArgumentNullException || exc is FormatException || exc is OverflowException) {
				return false;
			}
			ropePerimeterIsValid = (ropePerimeter >= packagePerimeter + 150 &&
									ropePerimeter <= packagePerimeter + 300);
			ropeWeightIsValid = (ropeWeight >= packageWeight + 50);
			ropeIsValid = (ropePerimeterIsValid && ropeWeightIsValid);
			return ropeIsValid;
		}
		// Round (ellipse) shape
		public static void CreateEllipseShapesToBeDrawn(int tubeAmount, int tubeAmountBigLine, int tubeAmountSmallLine, int shapeDiameter, int vPosInit, int hPosInit, ref int columns, ref int rows, ICollection<Ellipse> listEllipses) {
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
						ellip.Fill = (tubeCurrentlyDrawing < Win_Main.LastTube) ? Brushes.TomatoBrush : Brushes.GrayBrush;
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
		public static void GetValuesFromRoundTubeRecipe(int tubeAmount, out int tubeAmountBigLine, out int tubeAmountSmallLine, out int vPosInit, out int hPosInit, out int shapeDiameter) {
			Dictionary<string, int> recipeValues = Recipes.GetRoundTubeRecipe(tubeAmount);
			if (recipeValues != null) {
				tubeAmountBigLine = recipeValues["bigRowSize"];
				tubeAmountSmallLine = recipeValues["smallRowSize"];
				vPosInit = recipeValues["vPos"];
				hPosInit = recipeValues["hPos"];
				shapeDiameter = recipeValues["shapeSize"];
			}
			else {
				tubeAmountBigLine = 0;
				tubeAmountSmallLine = 0;
				vPosInit = 0;
				hPosInit = 0;
				shapeDiameter = 0;
			}
		}
		// Square (rectangle) shape
		public static void CreateRectangleShapesToBeDrawn(int tubeAmount, int shapeWidth, int shapeHeight, int hPosInit, int vPosInit, double numH, double numV, ICollection<Rectangle> listRectangles) {
			int hPos = hPosInit, vPos = vPosInit, tubeCurrentlyDrawing = 0;
			for (int i = 0; i < numV; i++) {
				for (int j = 0; j < numH; j++) {
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
						rect.Fill = (tubeCurrentlyDrawing < Win_Main.LastTube) ? Brushes.TomatoBrush : Brushes.GrayBrush;
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
		public static void GetValuesFromSquareRectTubeRecipe(int tubeAmount, int width, int height, out int shapeWidth, out int shapeHeight, out int vPosInit, out int hPosInit) {
			Dictionary<string, int> recipeValues = Recipes.GetSquareTubeRecipe(tubeAmount);
			if (recipeValues != null) {
				vPosInit = recipeValues["vPos"];
				hPosInit = recipeValues["hPos"];
				if (width != height) {
					shapeWidth = recipeValues["shapeSize"] + (recipeValues["shapeSize"] / 5);
					shapeHeight = recipeValues["shapeSize"] - (recipeValues["shapeSize"] / 5);
				}
				else {
					shapeWidth = recipeValues["shapeSize"];
					shapeHeight = recipeValues["shapeSize"];
				}
			}
			else {
				shapeWidth = 0;
				shapeHeight = 0;
				vPosInit = 0;
				hPosInit = 0;
			}
		}
		public static void CalculateNumberOfRowsAndColummsFromTubeAmount(int tubeAmount, int width, int height, out double numH, out double numV, out int packageWidth, out int packageHeight) {
			// divides the number of tubes until the number of rows and collums is even (+/-)
			double start, result = 0, temp1 = 0, temp2 = 0;

			start = (tubeAmount > 300) ? 30 : 15;   // will likely be less than 300 tubes, no need to always start on 30
			packageWidth = width * (int)(start + 1);
			packageHeight = height * (int)result;
			// packagewidth can never be higher than packageHeight
			while (packageHeight <= packageWidth) {
				temp1 = result;
				temp2 = start;
				result = tubeAmount / start;
				start--;
				packageWidth = width * (int)(start + 1);
				packageHeight = height * (int)result;
			}
			numV = temp1;
			numH = temp2 + 1;
		}
	}
}
