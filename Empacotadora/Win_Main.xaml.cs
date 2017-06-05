using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO;
using System.Globalization;
using System.Windows.Interop;
using Empacotadora.Address;

namespace Empacotadora {
	/// <summary>
	/// Lógica interna para Win_Main.xaml
	/// </summary>
	public partial class Win_Main : Window {
		FERP_MairCOMS7 PLC = new FERP_MairCOMS7();
		// Wrapper
		int lastTube = 0, currentPackage = 0, id;
		const byte margin = 2;
		// New Order
		bool isRoundTubeActive = false, isSquareTubeActive = false, isHexagonalWrapActive = false, isSquareWrapActive = false;
		// UI control
		bool isDefaultLayoutActive = false, isStrapperLayoutActive = false, isNewOrderLayoutActive = false, isStorageLayoutActive = false;
		bool isRecipesLayoutActive = false, isHistoryLayoutActive = false;
		// Strapper
		bool textChanged = false, cellsArePopulated = false, editingRecipe = false, isStrapsModifyActive = false;
		// Recipe
		bool isRoundTubeRecipeActive = false;
		// PLC
		int[] PLCArrayPackageRows;
		int tubeNumber;
		bool changeOn, tubeChange, pageActive;
		struct StructTubeChange {
			public string OldLength;
			public string OldThickness;
			public string OldTi;
			public string OldWidth;
			public string OldHeight;
		}
		StructTubeChange OldTube = new StructTubeChange();
		// Diretories
		public static string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Orders.txt";
		string historyPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\PackageHistory.txt";
		string pathSquareTubes = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\SquareTubeRecipes.txt";
		string pathRectTubes = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\RectTubeRecipes.txt";
		string pathRoundTubes = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\RoundTubeRecipes.txt";

		const int defaultRoundTubeNmbr = 37, defaultdiameter = 65;
		const int defaultSquareTubeNmbr = 36, defaultWidth = 60, defaultHeight = 60;

		public Win_Main() {
			// Set initial layout
			InitializeComponent();
			InitializeLayout();
			//DrawSquareWrap(defaultSquareTubeNmbr, defaultWidth, defaultHeight);
			DrawHexagonalWrap(defaultRoundTubeNmbr, defaultdiameter);
			errorImage.Source = Imaging.CreateBitmapSourceFromHIcon(System.Drawing.SystemIcons.Error.Handle‌​, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
			//ushort DBAddress = Addresses.Storage.DBNumber;
			//double variableAddress = Addresses.Storage.iPackageNumber.Item1;
			//string size = Addresses.Storage.iPackageNumber.Item2;
			//FERP_MairCOMS7.ReadFromDB(DBAddress, variableAddress, size);
			//MessageBox.Show(double.TryParse("472", NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double test).ToString());
			//lblCurrentTubes.Content = PLC.ReadBool(Address.PackPipe.DBNumber, Address.PackPipe.Mode.bAdjustment);
		}
		#region General
		private void lblDateTime_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			if (cal.IsVisible == true)
				cal.Visibility = Visibility.Collapsed;
			else
				cal.Visibility = Visibility.Visible;
		}
		private void logoCalculator_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			Win_Calculator WNCalculator = new Win_Calculator();
			WNCalculator.ShowDialog();
		}
		private void btnOrders_Click(object sender, RoutedEventArgs e) {
			Win_Orders WNOrders = new Win_Orders();
			btnOrders.Background = Brushes.lightRed;
			btnWrapper.ClearValue(BackgroundProperty);
			WNOrders.ShowDialog();
			if (Win_Orders.flagNewOrder == true) {
				SetNewOrderEnvironment();
			}
			else if (Win_Orders.flagRecipes == true) {
				HideGeneralLayout();
				SetRecipesLayout();
			}
			else {
				SetDefaultLayout();
				try {
					int.TryParse(General.currentOrder.TubeAm, out int amount);
					if (General.currentOrder.Diameter == "") {
						gridRound.Visibility = Visibility.Collapsed;
						gridSquare.Visibility = Visibility.Visible;
						lblOrderWidth.Content = General.currentOrder.Width;
						lblOrderHeight.Content = General.currentOrder.Height;
						int.TryParse(General.currentOrder.Width, out int width);
						int.TryParse(General.currentOrder.Height, out int height);
						DrawSquareWrap(amount, width, height);
					}
					else {
						gridRound.Visibility = Visibility.Visible;
						gridSquare.Visibility = Visibility.Collapsed;
						double.TryParse(General.currentOrder.Diameter, out double diam);
						lblOrderDiam.Content = General.currentOrder.Diameter;
						DrawHexagonalWrap(amount, diam);
					}
					lblOrderName.Content = General.currentOrder.Name;
					lblOrderThick.Content = General.currentOrder.Thick;
					lblOrderLength.Content = General.currentOrder.Length;
					lblPackageLength.Content = General.currentOrder.Length;
					lblTotalPackages.Content = General.currentOrder.PackageAm;
				}
				catch (NullReferenceException) { }
			}
		}
		private void SetNewOrderEnvironment() {
			SetNewOrderLayout();
			SetSqrWrap();
			SetSqrTube();
			isSquareTubeActive = true;
			tbDensity.Text = "7.65";
			try {
				string lineContent = File.ReadLines(path).Last();
				string[] array = lineContent.Split(',');
				Int32.TryParse(array[0], out id);
			}
			catch (FileNotFoundException) {
				UpdateStatusBar("Ficheiro que contém as ordens não foi encontrado.", 1);
			}
			lblID.Content = (++id).ToString();
		}
		private void btnSaveNewOrder_Click(object sender, RoutedEventArgs e) {
			UpdateStatusBar(General.WriteToFile(path, GatherTextBoxesValues()));
			SetDefaultLayout();
		}
		private void btnWrapper_Click(object sender, RoutedEventArgs e) {
			SetDefaultLayout();
		}
		private void btnStrapper_Click(object sender, RoutedEventArgs e) {
			SetStrapperLayout();
		}
		private void btnStorage_Click(object sender, RoutedEventArgs e) {
			SetStorageLayout();
		}
		private void btnPLCConnection_Click(object sender, RoutedEventArgs e) {
			ShowPLCConnectionLayout();
		}
		private void btnExit_Click(object sender, RoutedEventArgs e) {
			//var answer = MessageBox.Show("Terminar o programa?", "Confirmar", MessageBoxButton.YesNo);
			//if(answer == MessageBoxResult.Yes)
			Application.Current.Shutdown();
		}
		private void btnAbout_Click(object sender, RoutedEventArgs e) {
			MessageBox.Show("              Desenvolvedor: Rui Santos\n" +
							"                Supervisor: José Mendes\n" +
							"      Desenvolvido no dpt. de informática\n" +
							"       em Ferpinta, S.A. - Vale de Cambra\n" +
							"                                   2017",
							"Sobre");
		}
		private void btnReturn_Click(object sender, RoutedEventArgs e) {
			if (isNewOrderLayoutActive == true) {
				var answer = MessageBox.Show("Sair sem guardar?", "Confirmar", MessageBoxButton.YesNo);
				if (answer == MessageBoxResult.Yes) {
					SetDefaultLayout();
					isNewOrderLayoutActive = false;
				}
			}
			else if (isRecipesLayoutActive == true) {
				SetDefaultLayout();
				isRecipesLayoutActive = false;
			}
			else if (isHistoryLayoutActive == true) {
				SetStorageLayout();
				isHistoryLayoutActive = false;
			}
			else
				SetDefaultLayout();
		}
		private void btnManual_Click(object sender, RoutedEventArgs e) {
			var value = borderManualWrap.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
			if (value == Visibility.Visible) {
				if (isDefaultLayoutActive == true) {
					tabManual.SelectedItem = tabManualWrapper;
				}
				if (isStrapperLayoutActive == true) {
					tabManual.SelectedItem = tabManualStrapper;
				}
				if (isStorageLayoutActive == true) {
					tabManual.SelectedItem = tabManualStorage;
				}
			}
			borderManualWrap.Visibility = value;
		}
		private void SetOneSecondTimer() {
			// Used in:
			// - date &time label
			// - update canvas (cnvAtado)
			//  DispatcherTimer setup
			DispatcherTimer timer = new DispatcherTimer();
			timer.Tick += new EventHandler(Timer_Tick);
			timer.Interval = new TimeSpan(0, 0, 0, 1);
			timer.Start();
		}
		private void Timer_Tick(object sender, EventArgs e) {
			lblDateTime.Text = DateTime.Now.ToString("HH\\hmm:ss \n ddd dd/MM/yyyy");
			// PLC_UpdateTubesOnPackage();
			// Controlli_di_pagina F900000_Kernel, "POLMONE_1"
			//if (isStrapperLayoutActive)
			//	UpdateStrapperPage();
		}
		#region StatusBar
		// Status bar update
		private void SetStatusBarTimer() {
			//  DispatcherTimer setup
			DispatcherTimer timer = new DispatcherTimer();
			timer.Tick += new EventHandler(StatusBarTimer_Tick);
			timer.Interval = new TimeSpan(0, 0, 0, 3);
			timer.Stop();
			timer.Start();
		}
		private void StatusBarTimer_Tick(object sender, EventArgs e) {
			DispatcherTimer timer = (DispatcherTimer)sender;
			timer.Stop();
			sbIcon.Visibility = Visibility.Collapsed;
			status.Content = "Pronto";
		}
		private void UpdateStatusBar(string msg) {
			status.Content = msg;
			SetStatusBarTimer();
		}
		private void UpdateStatusBar(string msg, byte error) {
			// pass any number (0 <-> 255) through "error" to show the error icon with the message
			status.Content = msg;
			sbIcon.Visibility = Visibility.Visible;
			SetStatusBarTimer();
		}
		#endregion

		#endregion

		#region UI control methods
		// New order > define type of shape being drawn
		private void btnHexaWrap_Click(object sender, RoutedEventArgs e) {
			SetHexaWrap();
			isHexagonalWrapActive = true;
			isSquareWrapActive = false;
		}
		private void btnSqrWrap_Click(object sender, RoutedEventArgs e) {
			SetSqrWrap();
			isHexagonalWrapActive = false;
			isSquareWrapActive = true;
		}
		private void btnRoundTube_Click(object sender, RoutedEventArgs e) {
			SetRoundTube();
		}
		private void btnSqrTube_Click(object sender, RoutedEventArgs e) {
			SetSqrTube();
		}
		private void SetHexaWrap() {
			btnHexaWrap.Background = Brushes.lightRed;
			btnHexaWrap.BorderBrush = Brushes.active_border;
			btnSqrWrap.ClearValue(BackgroundProperty);
			btnSqrWrap.BorderBrush = Brushes.non_active_border;
			isHexagonalWrapActive = true;
			isSquareWrapActive = false;
			DrawHexagonalWrap(defaultRoundTubeNmbr, defaultdiameter);
		}
		private void SetSqrWrap() {
			btnSqrWrap.Background = Brushes.lightRed;
			btnSqrWrap.BorderBrush = Brushes.active_border;
			btnHexaWrap.ClearValue(BackgroundProperty);
			btnHexaWrap.BorderBrush = Brushes.non_active_border;
			isHexagonalWrapActive = false;
			isSquareWrapActive = true;
			if (tbWidth.Text != "" && tbHeight.Text != "") {
				int.TryParse(tbWidth.Text, out int width);
				int.TryParse(tbHeight.Text, out int height);
				DrawSquareWrap(defaultSquareTubeNmbr, width, height);
			}
			else
				DrawSquareWrap(defaultSquareTubeNmbr, defaultWidth, defaultHeight);
		}
		private void SetRoundTube() {
			btnRoundTube.Background = Brushes.lightRed;
			btnRoundTube.BorderBrush = Brushes.active_border;
			btnSqrTube.ClearValue(BackgroundProperty);
			btnSqrTube.BorderBrush = Brushes.non_active_border;
			tbDiam.IsEnabled = true;
			tbDiam.Focus();
			tbHeight.IsEnabled = false;
			tbWidth.IsEnabled = false;
			isSquareTubeActive = false;
			isRoundTubeActive = true;
		}
		private void SetSqrTube() {
			btnSqrTube.Background = Brushes.lightRed;
			btnSqrTube.BorderBrush = Brushes.active_border;
			btnRoundTube.ClearValue(BackgroundProperty);
			btnRoundTube.BorderBrush = Brushes.non_active_border;
			tbDiam.IsEnabled = false;
			tbWidth.IsEnabled = true;
			tbWidth.Focus();
			tbHeight.IsEnabled = true;
			isRoundTubeActive = false;
			isSquareTubeActive = true;
		}
		// "Set" methods call "Show"/"Hide" methods to combine the desired controls on the window
		private void SetDefaultLayout() {
			ShowGeneralLayout();
			ShowDefaultLayout();
			HideNewOrderLayout();
			HideStrapperLayout();
			HideStorageLayout();
		}
		private void SetNewOrderLayout() {
			HideGeneralLayout();
			HideDefaultLayout();
			ShowNewOrderLayout();
		}
		private void SetStrapperLayout() {
			ShowGeneralLayout();
			HideDefaultLayout();
			HideStorageLayout();
			ShowStrapperLayout();
		}
		private void SetStorageLayout() {
			ShowGeneralLayout();
			HideDefaultLayout();
			HideStrapperLayout();
			ShowStorageLayout();
		}
		private void SetHistoryLayout() {
			tabLayout.SelectedItem = tabItemHistory;
			try {
				datagridHistory.ItemsSource = History.ReadHistoryFromFile(historyPath);
			}
			catch (FileNotFoundException) {
				UpdateStatusBar("History file not found", 1);
			}
			HideGeneralLayout();
			btnReturn.Visibility = Visibility.Visible;
			isHistoryLayoutActive = true;
		}
		private void SetRecipesLayout() {
			lblTitle.Content = "Receitas";
			tabLayout.SelectedItem = tabItemRecipes;
			datagridRecipes.ItemsSource = Recipes.ReadTubeRecipesFromFile(pathRoundTubes);
			btnRecipeRoundTube.Background = Brushes.lightRed;
			btnRecipeSquareTube.ClearValue(BackgroundProperty);
			btnManual.Visibility = Visibility.Collapsed;
			btnOrders.Visibility = Visibility.Collapsed;
			btnReturn.Visibility = Visibility.Visible;
			gridRecipesSquareTube.Visibility = Visibility.Collapsed;
			gridRecipesRoundTube.Visibility = Visibility.Visible;
			isRoundTubeRecipeActive = true;
			isRecipesLayoutActive = true;
		}
		// "Show"/"Hide" methods show or hide layout controls
		private void ShowGeneralLayout() {
			btnWrapper.Visibility = Visibility.Visible;
			btnStrapper.Visibility = Visibility.Visible;
			btnStorage.Visibility = Visibility.Visible;
			btnExit.Visibility = Visibility.Visible;
			btnManual.Visibility = Visibility.Visible;
			btnPLCConnection.Visibility = Visibility.Visible;
		}
		private void HideGeneralLayout() {
			btnWrapper.Visibility = Visibility.Collapsed;
			btnStrapper.Visibility = Visibility.Collapsed;
			btnStorage.Visibility = Visibility.Collapsed;
			btnExit.Visibility = Visibility.Collapsed;
			btnManual.Visibility = Visibility.Collapsed;
			btnReturn.Visibility = Visibility.Collapsed;
			btnPLCConnection.Visibility = Visibility.Collapsed;
		}
		private void ShowDefaultLayout() {
			lblTitle.Content = "Empacotadora";
			btnOrders.Visibility = Visibility.Visible;
			borderCanvas.Visibility = Visibility.Visible;
			borderCanvas.Margin = new Thickness(805, 102, 79, 113);
			tabLayout.SelectedItem = tabItemWrapper;
			tabWrapper.SelectedItem = tabItemWrapperMain;
			btnOrders.ClearValue(BackgroundProperty);
			btnWrapper.Background = Brushes.lightRed;
			btnStrapper.ClearValue(BackgroundProperty);
			btnStorage.ClearValue(BackgroundProperty);
			btnPLCConnection.ClearValue(BackgroundProperty);
			borderManualWrap.Visibility = Visibility.Collapsed;
			isDefaultLayoutActive = true;
		}
		private void HideDefaultLayout() {
			btnOrders.Visibility = Visibility.Collapsed;
			borderCanvas.Visibility = Visibility.Collapsed;
			tabWrapper.SelectedItem = tabItemWrapperEmpty;
			isDefaultLayoutActive = false;
		}
		private void ShowNewOrderLayout() {
			lblTitle.Content = "Nova Ordem";
			btnReturn.Visibility = Visibility.Visible;
			btnSaveNewOrder.Visibility = Visibility.Visible;
			borderWrapTubeType.Visibility = Visibility.Visible;
			borderCanvas.Visibility = Visibility.Visible;
			borderCanvas.Margin = new Thickness(805, 202, 79, 13);
			tabWrapper.SelectedItem = tabItemWrapperNewOrder;
			tabLayout.SelectedItem = tabItemWrapperNewOrder;
			borderManualWrap.Visibility = Visibility.Collapsed;
			isNewOrderLayoutActive = true;
		}
		private void HideNewOrderLayout() {
			btnReturn.Visibility = Visibility.Collapsed;
			btnSaveNewOrder.Visibility = Visibility.Collapsed;
			borderWrapTubeType.Visibility = Visibility.Collapsed;
			isNewOrderLayoutActive = false;
		}
		private void ShowStrapperLayout() {
			lblTitle.Content = "Cintadora";
			tabLayout.SelectedItem = tabItemStrapper;
			btnWrapper.ClearValue(BackgroundProperty);
			btnStrapper.Background = Brushes.lightRed;
			btnStorage.ClearValue(BackgroundProperty);
			btnPLCConnection.ClearValue(BackgroundProperty);
			isStrapsModifyActive = false;
			ToogleModifyStrapsTextBoxes();
			borderManualWrap.Visibility = Visibility.Collapsed;
			isStrapperLayoutActive = true;
		}
		private void HideStrapperLayout() {
			isStrapperLayoutActive = false;
		}
		private void ShowStorageLayout() {
			lblTitle.Content = "Armazém";
			tabLayout.SelectedItem = tabItemStorage;
			isStrapperLayoutActive = false;
			btnWrapper.ClearValue(BackgroundProperty);
			btnStrapper.ClearValue(BackgroundProperty);
			btnStorage.Background = Brushes.lightRed;
			btnPLCConnection.ClearValue(BackgroundProperty);
			borderManualWrap.Visibility = Visibility.Collapsed;
			FillLastHistory();
			isStorageLayoutActive = true;
		}
		private void HideStorageLayout() {
			isStorageLayoutActive = false;
		}
		private void ShowPLCConnectionLayout() {
			btnWrapper.ClearValue(BackgroundProperty);
			btnStrapper.ClearValue(BackgroundProperty);
			btnStorage.ClearValue(BackgroundProperty);
			btnPLCConnection.Background = Brushes.lightRed;
			tabLayout.SelectedItem = tabItemPLCConnection;
		}
		private void InitializeLayout() {
			SetDefaultLayout();
			tabItemWrapper.Visibility = Visibility.Hidden;
			tabItemStrapper.Visibility = Visibility.Hidden;
			tabItemStorage.Visibility = Visibility.Hidden;
			tabItemWrapperMain.Visibility = Visibility.Hidden;
			tabItemWrapperNewOrder.Visibility = Visibility.Hidden;
			tabItemWrapperEmpty.Visibility = Visibility.Hidden;
			tabItemHistory.Visibility = Visibility.Hidden;
			tabItemRecipes.Visibility = Visibility.Hidden;
			tabItemPLCConnection.Visibility = Visibility.Hidden;
			tabManualWrapper.Visibility = Visibility.Hidden;
			tabManualStrapper.Visibility = Visibility.Hidden;
			tabManualStorage.Visibility = Visibility.Hidden;
			SetOneSecondTimer();
		}
		#endregion

		#region Wrapper
		private void btnAddTube_Click(object sender, RoutedEventArgs e) {
			++lastTube;
			//DrawSquareWrap(defaultSquareTubeNmbr, defaultWidth, defaultHeight);
			DrawHexagonalWrap(defaultRoundTubeNmbr, defaultdiameter);
		}
		private void btnResetPackages_Click(object sender, RoutedEventArgs e) {
			currentPackage = 0;
			lblCurrentPackage.Content = currentPackage.ToString();
		}
		private void PLC_UpdateTubesAndPackageData() {
			// checks if tubes per row has changed
			changeOn = (PLCArrayPackageRows != PLC.ReadArrayInt(Accumulator_1.DBNumber, Accumulator_1.Rows.Item1, Accumulator_1.Rows.Item2));
			PLCArrayPackageRows = PLC.ReadArrayInt(Accumulator_1.DBNumber, Accumulator_1.Rows.Item1, Accumulator_1.Rows.Item2);
			// check if tube data has changed
			tubeChange = false;
			if (OldTube.OldHeight != PLC.ReadReal(Accumulator_1.DBNumber, Accumulator_1.Order.Tube.rHeight.Item1) ||
				OldTube.OldWidth != PLC.ReadReal(Accumulator_1.DBNumber, Accumulator_1.Order.Tube.rWidth.Item1) ||
				OldTube.OldThickness != PLC.ReadReal(Accumulator_1.DBNumber, Accumulator_1.Order.Tube.rThickness.Item1)) {
				tubeChange = true;
			}

			OldTube.OldHeight = PLC.ReadReal(Accumulator_1.DBNumber, Accumulator_1.Order.Tube.rHeight.Item1);
			OldTube.OldWidth = PLC.ReadReal(Accumulator_1.DBNumber, Accumulator_1.Order.Tube.rWidth.Item1);
			OldTube.OldThickness = PLC.ReadReal(Accumulator_1.DBNumber, Accumulator_1.Order.Tube.rThickness.Item1);

			// tube number in package
			lblCurrentTubes.Content = PLC.ReadInt(PackPipe.DBNumber, PackPipe.PC.iTubesOnPackage.Item1);
			lblCurrentPackage.Content = PLC.ReadInt(PackPipe.DBNumber, PackPipe.PC.iPackageNumber.Item1);
			lblTotalTubes.Content = PLC.ReadInt(Accumulator_1.DBNumber, Accumulator_1.Order.Package.bTubeNumber.Item1);
			/*
			da lasciare??
			bundleCounter = tools.OpenTextFile(path_Reports & "BundleNumber.txt", False)
			BundlesTotalDisplay.Caption = bundleCounter
			*/

			if (tubeNumber.ToString() != PLC.ReadInt(PackPipe.DBNumber, PackPipe.PC.iTubesOnPackage.Item1) ||
				/*pageActive ||*/ changeOn || tubeChange) {
				RefreshCanvas();
				//pageActive = false;
			}
		}
		private void RefreshCanvas() {
			//##//
		}
		#region Draw shapes in canvas
		// Shapes
		private void DrawHexagonalWrap(int tubeAmount, double diameter) {
			byte lineCap = 0, variavel = 0;
			bool incrementing = false;
			GetValuesFromRoundTubeRecipe(tubeAmount, out int tubeAmountBigLine, out int tubeAmountSmallLine, out int vPosInit, out int hPosInit, out int shapeDiameter);
			int Vpos = vPosInit, Hpos = hPosInit;
			int colums = 0, rows = 0, tubeCurrentlyDrawing = 0;
			List<Ellipse> listEllipses = new List<Ellipse>();

			if (shapeDiameter == 0)
				return;

			if (lastTube == (tubeAmount + 1)) {
				lastTube = 0;
				++currentPackage;
			}

			CreateEllipseShapesToBeDrawn(tubeAmount, tubeAmountBigLine, tubeAmountSmallLine, shapeDiameter, tubeCurrentlyDrawing, lineCap, variavel, incrementing, Vpos, Hpos, colums, rows, listEllipses);

			Hpos = hPosInit;
			Vpos = vPosInit;

			PutShapesInCanvas(listEllipses);

			double packageWidth = diameter * colums;
			double packageHeight = diameter * rows;
			UpdateLabelsValues(tubeAmount, packageWidth, packageHeight);
		}
		private void GetValuesFromRoundTubeRecipe(int tubeAmount, out int tubeAmountBigLine, out int tubeAmountSmallLine, out int vPosInit, out int hPosInit, out int shapeDiameter) {
			Dictionary<string, int> recipeValues = Recipes.GetRoundTubeRecipe(tubeAmount);
			tubeAmountBigLine = recipeValues["bigRowSize"];
			tubeAmountSmallLine = recipeValues["smallRowSize"];
			vPosInit = recipeValues["Vpos"];
			hPosInit = recipeValues["Hpos"];
			shapeDiameter = recipeValues["shapeSize"];
		}
		private void CreateEllipseShapesToBeDrawn(int tubeAmount, int tubeAmountBigLine, int tubeAmountSmallLine, int shapeDiameter, int tubeCurrentlyDrawing, byte lineCap, byte variavel, bool incrementing, int Vpos, int Hpos, int colums, int rows, List<Ellipse> listEllipses) {
			int HposLineInit;
			for (byte i = 0; i < tubeAmountBigLine; i++) {
				++rows;
				HposLineInit = Hpos;
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
					if (lineCap >= colums)
						++colums;
					Ellipse ellip = new Ellipse() {
						Stroke = Brushes.blackBrush,
						Width = shapeDiameter,
						Height = shapeDiameter
					};
					Canvas.SetLeft(ellip, Hpos);
					Canvas.SetTop(ellip, (Vpos - ellip.Height));
					// prevent shape from being drawn if total number of tubes was reached
					if (tubeCurrentlyDrawing < tubeAmount) {
						ellip.StrokeThickness = 2;
						ellip.Fill = (tubeCurrentlyDrawing < lastTube) ? Brushes.tomatoBrush : Brushes.grayBrush;
					}
					else
						ellip.StrokeThickness = 0;
					listEllipses.Add(ellip);
					Hpos += shapeDiameter + margin;
					++tubeCurrentlyDrawing;
				}
				if (incrementing == true)
					Hpos = HposLineInit - ((shapeDiameter / 2) + (margin / 2));
				else if (incrementing == false)
					Hpos = HposLineInit + ((shapeDiameter / 2) + (margin / 2));
				Vpos -= shapeDiameter + (margin / 2);
			}
		}
		private void DrawSquareWrap(int tubeAmount, int width, int height) {
			if (lastTube == (tubeAmount + 1)) {
				lastTube = 0;
				++currentPackage;
			}

			GetValuesFromSquareRectTubeRecipe(tubeAmount, width, height, out int shapeWidth, out int shapeHeight, out int vPosInit, out int hPosInit);
			int Vpos = vPosInit, Hpos = hPosInit, tubeCurrentlyDrawing = 0;

			CalculateNumberOfRowsAndColummsFromTubeAmount(tubeAmount, width, height, out double numH, out double numV, out int packageWidth, out int packageHeight);

			if (shapeWidth == 0 || shapeHeight == 0)
				return;
			List<Rectangle> listRectangles = new List<Rectangle>();
			CreateRectangleShapesToBeDrawn(tubeAmount, shapeWidth, shapeHeight, hPosInit, ref Vpos, ref Hpos, ref tubeCurrentlyDrawing, numH, numV, listRectangles);
			Hpos = hPosInit;
			Vpos = vPosInit;

			PutShapesInCanvas(listRectangles);

			UpdateLabelsValues(tubeAmount, packageWidth, packageHeight);
		}
		private void CreateRectangleShapesToBeDrawn(int tubeAmount, int shapeWidth, int shapeHeight, int hPosInit, ref int Vpos, ref int Hpos, ref int tubeCurrentlyDrawing, double numH, double numV, List<Rectangle> listRectangles) {
			for (int i = 0; i < numV; i++) {
				for (int j = 0; j < numH; j++) {
					Rectangle rect = new Rectangle() {
						Stroke = Brushes.blackBrush,
						Width = shapeWidth,
						Height = shapeHeight
					};
					Canvas.SetLeft(rect, Hpos);
					Canvas.SetTop(rect, (Vpos - rect.Height));
					// prevent shape from being drawn if total number of tubes was reached
					if (tubeCurrentlyDrawing < tubeAmount) {
						rect.StrokeThickness = 2;
						rect.Fill = (tubeCurrentlyDrawing < lastTube) ? Brushes.tomatoBrush : Brushes.grayBrush;
					}
					else
						rect.StrokeThickness = 0;
					listRectangles.Add(rect);
					Hpos += shapeWidth + margin;
					++tubeCurrentlyDrawing;
				}
				Hpos = hPosInit;
				Vpos -= shapeHeight + margin;
			}
		}
		private void GetValuesFromSquareRectTubeRecipe(int tubeAmount, int width, int height, out int shapeWidth, out int shapeHeight, out int Vpos_init, out int Hpos_init) {
			Dictionary<string, int> value = Recipes.GetSquareTubeRecipe(tubeAmount);
			Vpos_init = value["Vpos"];
			Hpos_init = value["Hpos"];
			if (width != height) {
				shapeWidth = value["shapeSize"] + (value["shapeSize"] / 5);
				shapeHeight = value["shapeSize"] - (value["shapeSize"] / 5);
			}
			else {
				shapeWidth = value["shapeSize"];
				shapeHeight = value["shapeSize"];
			}
		}
		private void CalculateNumberOfRowsAndColummsFromTubeAmount(int tubeAmount, int width, int height, out double numH, out double numV, out int packageWidth, out int packageHeight) {
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
		private void PutShapesInCanvas<T>(List<T> listOfShapes) where T : Shape {
			cnvAtado.Children.Clear();
			foreach (var forma in listOfShapes)
				cnvAtado.Children.Add(forma);
		}
		private void UpdateLabelsValues(int tubeAmount, double packageWidth, double packageHeight) {
			lblPackageWidth.Content = packageWidth.ToString();
			lblPackageHeight.Content = packageHeight.ToString();

			lblCurrentTubes.Content = lastTube.ToString();
			lblTotalTubes.Content = tubeAmount.ToString();
			lblCurrentPackage.Content = currentPackage.ToString();
		}
		#endregion

		#endregion

		#region NewOrder TextBoxe's Values
		// Handle NewOrder textboxe's values
		private void tb_PreviewMouseDoubleClick_Keypad(object sender, MouseButtonEventArgs e) {
			TextBox tb = (TextBox)sender;
			Win_Keypad WNKeypad = new Win_Keypad();
			WNKeypad.ShowDialog();
			if (WNKeypad.enter == true)
				tb.Text = WNKeypad.tbResult.Text;
		}
		private void tb_LostFocus(object sender, RoutedEventArgs e) {
			// main input method is the on screen keypad
			// anyway, should be added code to prevent user
			// from typing letters (from full-size physical keyboard, if available)
			// and to handle empty text box
			TextBox tb = (TextBox)sender;
			double value;
			if (sender == tbDiam || sender == tbWidth || sender == tbHeight) {
				if (Double.TryParse(tb.Text, out value))
					tb.ClearValue(BackgroundProperty);
				else {
					tb.Background = Brushes.non_active_back;
					if (tb.Text != "")
						MessageBox.Show("- Apenas são aceites números\n" +
										"- Medida não pode ser igual 0", "Valor inserido inválido");
				}
				if (value == 0.0) {
					tb.Background = Brushes.non_active_back;
					if (tb.Text != "")
						MessageBox.Show("Medida não pode ser igual a 0");
				}
			}
			string currentString = GatherTextBoxesValues();
			string[] array = currentString.Split(',');
			Double.TryParse(array[3], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double diameter);
			int.TryParse(array[5], out int width);
			int.TryParse(array[4], out int height);
			Double.TryParse(array[6], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double thickness);
			Double.TryParse(array[7], out double length);
			Double.TryParse(array[8], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double density);
			double weight, diameter_out = diameter, diameter_in = diameter - thickness;
			// Não calcula bem o peso para tubos redondos
			//if (diameter == 0)
			weight = (((height * width * length) - (((height - (2 * thickness)) * (width - (2 * thickness))) * length)) * (density * 1000) * 0.000000001);
			//else
			//    weight = ((Math.PI * ((Math.Pow((0.5 * diameter_out), 2)) - (Math.Pow((0.5 * diameter_in), 2)))) * length * (density * 0.000001));
			int.TryParse(tbTubeNmbr.Text, out int tubes);
			if (tubes > 0)
				weight *= tubes;
			try {
				if (tbDiam.IsEnabled == false)
					lblWeight.Content = Math.Round(weight);
				else
					lblWeight.Content = "###";
			}
			catch (OverflowException) {
				UpdateStatusBar("Erro ao calcular o peso <Math.Round()><Overflow>", 1);
			}
			if (sender == tbTubeNmbr) {
				if (isRoundTubeActive)
					DrawHexagonalWrap(tubes, diameter);
				if (isSquareTubeActive)
					DrawSquareWrap(tubes, width, height);
			}
		}
		private void tb_isEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) {
			TextBox tb = (TextBox)sender;
			tb.ClearValue(BackgroundProperty);
			tb.Clear();
		}
		private string GatherTextBoxesValues() {
			// Gathers value of New Order textboxes and concatenates them into a string
			OrderDetails newOrder = new OrderDetails();
			newOrder.active = "1";
			newOrder.Name = tbNrOrdem.Text;
			newOrder.Diameter = tbDiam.Text;
			newOrder.Width = tbWidth.Text;
			newOrder.Height = tbHeight.Text;
			newOrder.Thick = tbThickness.Text;
			newOrder.Length = tbLength.Text;
			newOrder.Density = tbDensity.Text;
			//newOrder.Hardness = "";
			newOrder.TubeAm = tbTubeNmbr.Text;
			if (isRoundTubeActive == true)
				newOrder.TubeType = "R";
			else
				newOrder.TubeType = "Q";
			newOrder.PackageAm = tbPackageAmount.Text;
			if (isHexagonalWrapActive == true)
				newOrder.PackageType = "H";
			else
				newOrder.PackageType = "Q";
			newOrder.Created = DateTime.Now.ToString("dd/MM/yyyy HH\\hmm");
			try {
				if (isRoundTubeActive == true)
					newOrder.Weight = Math.Round(newOrder.CalculateWeight(newOrder.Diameter, newOrder.Thick, newOrder.Length, newOrder.Density)).ToString();
				else if (isSquareTubeActive == true)
					newOrder.Weight = Math.Round(newOrder.CalculateWeight(newOrder.Width, newOrder.Height, newOrder.Thick, newOrder.Length, newOrder.Density)).ToString();
			}
			catch (Exception) {
				UpdateStatusBar("Cálculo do peso falhou", 1);
			}
			string stringToWrite = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14}\n",
				id, newOrder.active, newOrder.Name, newOrder.Diameter, newOrder.Width, newOrder.Height, newOrder.Thick,
				newOrder.Length, newOrder.Density, /*newOrder.Hardness,*/ newOrder.TubeAm, newOrder.TubeType,
				newOrder.PackageAm, newOrder.PackageType, newOrder.Weight, newOrder.Created);
			return stringToWrite;
		}
		#endregion

		#region Strapper
		// Number of straps, its position and modification
		private void tbStrapNmbr_LostFocus(object sender, RoutedEventArgs e) {
			Int32.TryParse(tbStrapNmbr.Text, out int straps);
			List<Grid> boxesGrids = new List<Grid>() { grid2Straps, grid3Straps, grid4Straps, grid5Straps, grid6Straps };
			// Show/Hide grid according to number of straps on the text box
			switch (straps) {
				case 2:
					imgStrap.Source = new BitmapImage(new Uri(@"/Resources/atado2.png", UriKind.Relative));
					foreach (var item in boxesGrids)
						item.Visibility = (item.Name == "grid2Straps" ? Visibility.Visible : Visibility.Collapsed);
					break;
				case 3:
					imgStrap.Source = new BitmapImage(new Uri(@"/Resources/atado3.png", UriKind.Relative));
					foreach (var item in boxesGrids)
						item.Visibility = (item.Name == "grid3Straps" ? Visibility.Visible : Visibility.Collapsed);
					break;
				case 4:
					imgStrap.Source = new BitmapImage(new Uri(@"/Resources/atado4.png", UriKind.Relative));
					foreach (var item in boxesGrids)
						item.Visibility = (item.Name == "grid3Straps" ? Visibility.Visible : Visibility.Collapsed);
					break;
				case 5:
					imgStrap.Source = new BitmapImage(new Uri(@"/Resources/atado5.png", UriKind.Relative));
					foreach (var item in boxesGrids)
						item.Visibility = (item.Name == "grid5Straps" ? Visibility.Visible : Visibility.Collapsed);
					break;
				case 6:
					imgStrap.Source = new BitmapImage(new Uri(@"/Resources/atado6.png", UriKind.Relative));
					foreach (var item in boxesGrids)
						item.Visibility = (item.Name == "grid6Straps" ? Visibility.Visible : Visibility.Collapsed);
					break;
				default:
					break;
			}
		}
		private void tbStrapPosition_LostFocus(object sender, RoutedEventArgs e) {
			// Finds number of active textboxes (nmbr of straps)
			// and calls method passing the nmbr of straps
			Byte.TryParse(tbStrapNmbr.Text, out byte value);
			switch (value) {
				case 2:
					if (tbstrap2_1.Text != "" && tbstrap2_2.Text != "") {
						GetStrapsPositionFromTextboxes(value);
					}
					break;
				case 3:
					if (tbstrap3_1.Text != "" && tbstrap3_2.Text != "" && tbstrap3_3.Text != "") {
						GetStrapsPositionFromTextboxes(value);
					}
					break;
				case 4:
					if (tbstrap4_1.Text != "" && tbstrap4_2.Text != "" && tbstrap4_3.Text != "" && tbstrap4_4.Text != "") {
						GetStrapsPositionFromTextboxes(value);
					}
					break;
				case 5:
					if (tbstrap5_1.Text != "" && tbstrap5_2.Text != "" && tbstrap5_3.Text != "" && tbstrap5_4.Text != "" && tbstrap5_5.Text != "") {
						GetStrapsPositionFromTextboxes(value);
					}
					break;
				case 6:
					if (tbstrap6_1.Text != "" && tbstrap6_2.Text != "" && tbstrap6_3.Text != "" && tbstrap6_4.Text != "" && tbstrap6_5.Text != "" && tbstrap6_6.Text != "") {
						GetStrapsPositionFromTextboxes(value);
					}
					break;
			}
		}
		private void GetStrapsPositionFromTextboxes(byte straps) {
			// Gets position of all straps
			int[] array;
			string values = "";
			switch (straps) {
				case 2:
					array = new int[2];
					Int32.TryParse(tbstrap2_1.Text, out array[0]);
					Int32.TryParse(tbstrap2_2.Text, out array[1]);
					values = string.Join(",", array);
					break;
				case 3:
					array = new int[3];
					Int32.TryParse(tbstrap3_1.Text, out array[0]);
					Int32.TryParse(tbstrap3_2.Text, out array[1]);
					Int32.TryParse(tbstrap3_3.Text, out array[2]);
					values = string.Join(",", array);
					break;
				case 4:
					array = new int[4];
					Int32.TryParse(tbstrap4_1.Text, out array[0]);
					Int32.TryParse(tbstrap4_2.Text, out array[1]);
					Int32.TryParse(tbstrap4_3.Text, out array[2]);
					Int32.TryParse(tbstrap4_4.Text, out array[3]);
					values = string.Join(",", array);
					break;
				case 5:
					array = new int[5];
					Int32.TryParse(tbstrap5_1.Text, out array[0]);
					Int32.TryParse(tbstrap5_2.Text, out array[1]);
					Int32.TryParse(tbstrap5_3.Text, out array[2]);
					Int32.TryParse(tbstrap5_4.Text, out array[3]);
					Int32.TryParse(tbstrap5_5.Text, out array[4]);
					values = string.Join(",", array);
					break;
				case 6:
					array = new int[6];
					Int32.TryParse(tbstrap6_1.Text, out array[0]);
					Int32.TryParse(tbstrap6_2.Text, out array[1]);
					Int32.TryParse(tbstrap6_3.Text, out array[2]);
					Int32.TryParse(tbstrap6_4.Text, out array[3]);
					Int32.TryParse(tbstrap6_5.Text, out array[4]);
					Int32.TryParse(tbstrap6_6.Text, out array[5]);
					values = string.Join(",", array);
					break;
				default:
					break;
			}
			UpdateStatusBar(values);
		}
		private void btnModifyStraps_Click(object sender, RoutedEventArgs e) {
			isStrapsModifyActive ^= true;
			ToogleModifyStrapsTextBoxes();
		}
		private void tbStrapNmbr_TextChanged(object sender, TextChangedEventArgs e) {
			isStrapsModifyActive = false;
			textChanged = true;
			ToogleModifyStrapsTextBoxes();
		}
		private void ToogleModifyStrapsTextBoxes() {
			// Changes strap position textboxes (activate or deactivate modification)
			// according to current state of program
			if (isStrapperLayoutActive == true) {
				Byte.TryParse(tbStrapNmbr.Text, out byte value);
				IEnumerable<TextBox> controlsCollection = null;
				switch (value) {
					case 2:
						controlsCollection = grid2Straps.Children.OfType<TextBox>();
						break;
					case 3:
						controlsCollection = grid3Straps.Children.OfType<TextBox>();
						break;
					case 4:
						controlsCollection = grid4Straps.Children.OfType<TextBox>();
						break;
					case 5:
						controlsCollection = grid5Straps.Children.OfType<TextBox>();
						break;
					case 6:
						controlsCollection = grid6Straps.Children.OfType<TextBox>();
						break;
				}
				if (isStrapsModifyActive == true) {
					foreach (var item in controlsCollection) {
						item.Background = Brushes.modifyStrapsBrush;
						item.IsReadOnly = false;
						item.Focusable = true;
					}
				}
				else {
					foreach (var item in controlsCollection) {
						item.ClearValue(BackgroundProperty);
						if (textChanged == true)
							item.Text = "";
						item.IsReadOnly = true;
						item.Focusable = false;
					}
				}
			}
			textChanged = false;
		}
		private void UpdateStrapsValues(int length) {
			if (isStrapperLayoutActive == false)
				return;
			Byte.TryParse(tbStrapNmbr.Text, out byte nmbr);
			IEnumerable<TextBox> controlsCollection = null;
			int[] values = null;
			byte i = 0;
			switch (nmbr) {
				case 2:
					if (grid2Straps.Visibility == Visibility.Collapsed)
						return;
					controlsCollection = grid2Straps.Children.OfType<TextBox>();
					values = Recipes.GetStrapsPositionFromRecipe(length, nmbr);
					break;
				case 3:
					if (grid3Straps.Visibility == Visibility.Collapsed)
						return;
					controlsCollection = grid3Straps.Children.OfType<TextBox>();
					values = Recipes.GetStrapsPositionFromRecipe(length, nmbr);
					break;
				case 4:
					if (grid4Straps.Visibility == Visibility.Collapsed)
						return;
					controlsCollection = grid4Straps.Children.OfType<TextBox>();
					values = Recipes.GetStrapsPositionFromRecipe(length, nmbr);
					break;
				case 5:
					if (grid5Straps.Visibility == Visibility.Collapsed)
						return;
					controlsCollection = grid5Straps.Children.OfType<TextBox>();
					values = Recipes.GetStrapsPositionFromRecipe(length, nmbr);
					break;
				case 6:
					if (grid6Straps.Visibility == Visibility.Collapsed)
						return;
					controlsCollection = grid6Straps.Children.OfType<TextBox>();
					values = Recipes.GetStrapsPositionFromRecipe(length, nmbr);
					break;
				default:
					break;
			}
			foreach (var item in controlsCollection) {
				item.Text = values[i].ToString();
				++i;
			}
		}
		private void Button_Click(object sender, RoutedEventArgs e) {
			UpdateStrapsValues(6000);
		}
		private void UpdateStrapperPage() {
			double oldWidth, oldHeight, oldThickness;
			bool oldTubeType, toChange;

			if (oldWidth.ToString() != PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeWidth.Item1) ||
				oldHeight.ToString() != PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeHeight.Item1) ||
				oldThickness.ToString() != PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeThickness.Item1) ||
				oldTubeType.ToString() != PLC.ReadBool(Strapper.DBNumber, Strapper.Order.Tube.bRoundTube))
			{
				double.TryParse(PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeWidth.Item1), out oldWidth);
				double.TryParse(PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeHeight.Item1), out oldHeight);
				double.TryParse(PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeThickness.Item1), out oldThickness);
				bool.TryParse(PLC.ReadBool(Strapper.DBNumber, Strapper.Order.Tube.bRoundTube), out oldTubeType);
			}
			lblPackPosition.Content = PLC.ReadReal(LateralConveyor.DBNumber, LateralConveyor.PCData.rPackagePositionInStrapper.Item1);
			tbStrapNmbr.Text = PLC.ReadInt(Strapper.DBNumber, Strapper.Strap.iNumberOfStraps.Item1);
			
		}
		#endregion

		#region Storage
		// Storage
		private void FillLastHistory() {
			try {
				List<History> history = History.ReadHistoryFromFile(historyPath);
				List<Label> weightLabels = new List<Label>() { lblWeight1, lblWeight2, lblWeight3 };
				lblTubesHistory.Content = history[(history.Count) - 1].TubeAm;
				for (byte i = 1; i <= 3; i++) {
					weightLabels[i - 1].Content = history[(history.Count) - i].Weight;
				}
				List<Label> dateLabels = new List<Label>() { lblDate1, lblDate2, lblDate3 };
				for (byte i = 1; i <= 3; i++) {
					dateLabels[i - 1].Content = history[(history.Count) - i].Created;
				}
			}
			catch (FileNotFoundException) {
				UpdateStatusBar("Ficheiro do histórico não encontrado", 1);
			}
		}
		// History
		private void btnHistory_Click(object sender, RoutedEventArgs e) {
			SetHistoryLayout();
		}
		#endregion

		#region Recipes
		// Recipes
		private void btnRecipeRoundTube_Click(object sender, RoutedEventArgs e) {
			ShowTubeRecipesOnDataGrid(pathRoundTubes);
			IEnumerable<TextBox> textBoxes = GetTextBoxesFromGrids();
			foreach (var item in textBoxes) {
				item.Text = "";
			}
			isRoundTubeRecipeActive = true;
		}
		private void btnRecipeSquareTube_Click(object sender, RoutedEventArgs e) {
			ShowTubeRecipesOnDataGrid(pathSquareTubes, pathRectTubes);
			IEnumerable<TextBox> textBoxes = GetTextBoxesFromGrids();
			foreach (var item in textBoxes) {
				item.Text = "";
			}
			isRoundTubeRecipeActive = false;
		}
		private void ShowTubeRecipesOnDataGrid(string pathRoundTube) {
			datagridRecipes.ItemsSource = Recipes.ReadTubeRecipesFromFile(pathRoundTube);
			btnRecipeRoundTube.Background = Brushes.lightRed;
			btnRecipeSquareTube.ClearValue(BackgroundProperty);
			gridRecipesSquareTube.Visibility = Visibility.Collapsed;
			gridRecipesRoundTube.Visibility = Visibility.Visible;
		}
		private void ShowTubeRecipesOnDataGrid(string pathSquareTube, string pathRectTube) {
			datagridRecipes.ItemsSource = Recipes.ReadTubeRecipesFromFile(pathRectTube, pathRectTube);
			btnRecipeSquareTube.Background = Brushes.lightRed;
			btnRecipeRoundTube.ClearValue(BackgroundProperty);
			gridRecipesRoundTube.Visibility = Visibility.Collapsed;
			gridRecipesSquareTube.Visibility = Visibility.Visible;
		}
		private void datagridRecipes_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e) {
			if (!editingRecipe)
				cellsArePopulated = GetDataFromSelectedCells();
		}
		private void btnRecipeEdit_Click(object sender, RoutedEventArgs e) {
			if (cellsArePopulated) {
				editingRecipe = true;
				List<TextBox> textBoxes = GetTextBoxesFromGrids();
				foreach (var item in textBoxes) {
					item.Background = Brushes.yellowBrush;
					item.IsReadOnly = false;
					item.Focusable = true;
				}
				btnRecipeEdit.IsEnabled = false;
				btnRecipeRoundTube.IsEnabled = false;
				btnRecipeSquareTube.IsEnabled = false;
				btnReturn.IsEnabled = false;
				btnRecipeSave.Visibility = Visibility.Visible;
				btnRecipeCancel.Visibility = Visibility.Visible;
			}
			else
				UpdateStatusBar("Para editar selecione uma ordem", 1);
		}
		private void datagridRecipes_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
			if (editingRecipe) {
				e.Handled = true;
				UpdateStatusBar("Para mudar de receita termine de editar a atual", 1);
			}
		}
		private bool GetDataFromSelectedCells() {
			if (isRoundTubeRecipeActive == true) {
				RoundTubeRecipe datagridRow = GetRoundTubeRecipeFromGrid();
				if (datagridRow != null) {
					tbRecipeTubes.Text = datagridRow.TubeNumber;
					tbRecipeBigRow.Text = datagridRow.BigRow;
					tbRecipeSmallRow.Text = datagridRow.SmallRow;
					return true;
				}
			}
			else {
				SquareTubeRecipe datagridRow = GetSquareTubeRecipeFromGrid();
				if (datagridRow != null) {
					tbRecipeTubes.Text = datagridRow.TubeNumber;
					tbRecipeColums.Text = datagridRow.Colums;
					tbRecipeRows.Text = datagridRow.Rows;
					return true;
				}
			}
			return false;
		}
		private RoundTubeRecipe GetRoundTubeRecipeFromGrid() {
			RoundTubeRecipe datagridRow = null;
			try {
				datagridRow = (RoundTubeRecipe)datagridRecipes.Items[datagridRecipes.SelectedIndex];
			}
			catch (ArgumentOutOfRangeException) {
				UpdateStatusBar("Nenhuma receita selecionada", 1);
			}
			return datagridRow;
		}
		private SquareTubeRecipe GetSquareTubeRecipeFromGrid() {
			SquareTubeRecipe datagridRow = null;
			try {
				datagridRow = (SquareTubeRecipe)datagridRecipes.Items[datagridRecipes.SelectedIndex];
			}
			catch (ArgumentOutOfRangeException) {
				UpdateStatusBar("Nenhuma receita selecionada", 1);
			}
			return datagridRow;
		}
		private void btnRecipeSave_Click(object sender, RoutedEventArgs e) {
			DisableTextBoxesModification();
			List<string> newFileContent = new List<string>();
			if (isRoundTubeRecipeActive == true) {
				EditRoundTubeRecipesTextFile(newFileContent);
				UpdateStatusBar(General.WriteToFile(pathRoundTubes, newFileContent));
				datagridRecipes.ItemsSource = null;
				datagridRecipes.ItemsSource = Recipes.ReadTubeRecipesFromFile(pathRoundTubes);
			}
			else {
				bool found;
				EditSquareTubeRecipesTextFile(newFileContent, pathSquareTubes, out found);
				if (found == true)
					UpdateStatusBar(General.WriteToFile(pathSquareTubes, newFileContent));
				else {
					EditSquareTubeRecipesTextFile(newFileContent, pathRectTubes, out found);
					UpdateStatusBar(General.WriteToFile(pathRectTubes, newFileContent));
				}
				datagridRecipes.ItemsSource = null;
				datagridRecipes.ItemsSource = Recipes.ReadTubeRecipesFromFile(pathSquareTubes, pathRectTubes);
			}
			btnRecipeEdit.IsEnabled = true;
			btnRecipeRoundTube.IsEnabled = true;
			btnRecipeSquareTube.IsEnabled = true;
			btnReturn.IsEnabled = true;
			btnRecipeSave.Visibility = Visibility.Collapsed;
			btnRecipeCancel.Visibility = Visibility.Collapsed;
			editingRecipe = false;
		}
		private void DisableTextBoxesModification() {
			List<TextBox> textBoxes = GetTextBoxesFromGrids();
			foreach (var item in textBoxes) {
				item.ClearValue(BackgroundProperty);
				item.IsReadOnly = true;
				item.Focusable = false;
			}
		}
		private void EditSquareTubeRecipesTextFile(List<string> newFileContent, string path, out bool found) {
			found = false;
			foreach (var item in File.ReadAllLines(path)) {
				string newline = "";
				string[] array = item.Split(',');
				if (array[0] == tbRecipeTubes.Text) {
					array[array.Length - 2] = tbRecipeColums.Text;
					array[array.Length - 1] = tbRecipeRows.Text;
					if (path == pathSquareTubes)
						found = true;
					foreach (var value in array)
						newline += value + ",";
					newline = newline.Remove(newline.Length - 1);
				}
				newFileContent.Add(newline == "" ? item : newline);
			}
		}
		private void EditRoundTubeRecipesTextFile(List<string> newFileContent) {
			foreach (var item in File.ReadAllLines(pathRoundTubes)) {
				string newline = "";
				string[] array = item.Split(',');
				if (array[0] == tbRecipeTubes.Text) {
					array[1] = tbRecipeBigRow.Text;
					array[2] = tbRecipeSmallRow.Text;
					foreach (var value in array)
						newline += value + ",";
					newline = newline.Remove(newline.Length - 1);
				}
				newFileContent.Add(newline == "" ? item : newline);
			}
		}
		private void btnRecipeCancel_Click(object sender, RoutedEventArgs e) {
			List<TextBox> textBoxes = GetTextBoxesFromGrids();
			foreach (var item in textBoxes) {
				item.ClearValue(BackgroundProperty);
				item.IsReadOnly = true;
				item.Focusable = false;
			}
			btnRecipeEdit.IsEnabled = true;
			btnRecipeRoundTube.IsEnabled = true;
			btnRecipeSquareTube.IsEnabled = true;
			btnReturn.IsEnabled = true;
			btnRecipeSave.Visibility = Visibility.Collapsed;
			btnRecipeCancel.Visibility = Visibility.Collapsed;
			editingRecipe = false;
		}
		private List<TextBox> GetTextBoxesFromGrids() {
			List<TextBox> textBoxes = new List<TextBox>();
			foreach (var item in gridRecipes.Children) {
				if (item.GetType() == typeof(TextBox)) {
					textBoxes.Add((TextBox)item);
				}
			}
			foreach (var item in gridRecipesRoundTube.Children) {
				if (item.GetType() == typeof(TextBox)) {
					textBoxes.Add((TextBox)item);
				}
			}
			foreach (var item in gridRecipesSquareTube.Children) {
				if (item.GetType() == typeof(TextBox)) {
					textBoxes.Add((TextBox)item);
				}
			}
			return textBoxes;
		}
		#endregion

		#region Manual
		//Manual
		private void btnInsideManualBorder_Click(object sender, RoutedEventArgs e) {
			Button origin = (Button)sender;
			Image image = new Image();
			string button = origin.Name.ToString();
			bool noImageUpdate = false;
			switch (button) {
				case "btnTransportChain":
					image = (origin.Content == (Image)FindResource("TransportChain") ? (Image)FindResource("A_TransportChain") : (Image)FindResource("TransportChain"));
					break;
				case "btnAlignmentRolls":
					image = (origin.Content == (Image)FindResource("AlignmentRolls") ? (Image)FindResource("A_AlignmentRolls") : (Image)FindResource("AlignmentRolls"));
					break;
				case "btnTrasportQueue":
					image = (origin.Content == (Image)FindResource("TrasportQueue") ? (Image)FindResource("A_TrasportQueue") : (Image)FindResource("TrasportQueue"));
					break;
				case "btnBatenteAllinFile":
					image = (origin.Content == (Image)FindResource("BatenteAllinFile") ? (Image)FindResource("A_BatenteAllinFile") : (Image)FindResource("BatenteAllinFile"));
					break;
				case "btnLoader":
					image = (origin.Content == (Image)FindResource("Loader") ? (Image)FindResource("A_Loader") : (Image)FindResource("Loader"));
					break;
				case "btnControsagomeMecc":
					image = (origin.Content == (Image)FindResource("ControsagomeMecc") ? (Image)FindResource("A_ControsagomeMecc") : (Image)FindResource("ControsagomeMecc"));
					break;
				case "btnControsagomePneum_lower":
					noImageUpdate = true;
					gridLowerActive.Visibility = gridLowerActive.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
					break;
				case "btnControsagomePneum_upper":
					noImageUpdate = true;
					gridUpperActive.Visibility = gridUpperActive.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
					break;
				case "btnControsagomePneum_lateral":
					noImageUpdate = true;
					gridLateralActive.Visibility = gridLateralActive.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
					break;
				case "btnShelves":
					image = (origin.Content == (Image)FindResource("Shelves") ? (Image)FindResource("A_Shelves") : (Image)FindResource("Shelves"));
					break;
				case "btnCar":
					image = (origin.Content == (Image)FindResource("Car") ? (Image)FindResource("A_Car") : (Image)FindResource("Car"));
					break;
				case "btnCarRolls":
					image = (origin.Content == (Image)FindResource("CarRolls") ? (Image)FindResource("A_CarRolls") : (Image)FindResource("CarRolls"));
					break;
				case "btnTrasportLat":
					image = (origin.Content == (Image)FindResource("TrasportLat") ? (Image)FindResource("A_TrasportLat") : (Image)FindResource("TrasportLat"));
					break;
				case "btnUpperRolls":
					noImageUpdate = true;
					if (tbUpperRolls.Background == Brushes.yellowBrush)
						tbUpperRolls.ClearValue(BackgroundProperty);
					else
						tbUpperRolls.Background = Brushes.yellowBrush;
					break;
				case "btnLiftChains":
					image = (origin.Content == (Image)FindResource("LiftChains") ? (Image)FindResource("A_LiftChains") : (Image)FindResource("LiftChains"));
					break;
				case "btnStorageChains":
					image = (origin.Content == (Image)FindResource("StorageChains") ? (Image)FindResource("A_StorageChains") : (Image)FindResource("StorageChains"));
					break;
				case "btnStorageChains_Withdrawal":
					image = (origin.Content == (Image)FindResource("StorageChains-Withdrawal") ? (Image)FindResource("A_StorageChains-Withdrawal") : (Image)FindResource("StorageChains-Withdrawal"));
					break;
				case "btnDrain12":
					noImageUpdate = true;
					gridDrain12Active.Visibility = gridDrain12Active.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
					break;
				case "btnDrain123":
					noImageUpdate = true;
					gridDrain123Active.Visibility = gridDrain123Active.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
					break;
				case "btnDrain1234":
					noImageUpdate = true;
					gridDrain1234Active.Visibility = gridDrain1234Active.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
					break;
				default:
					UpdateStatusBar("Botão não reconhecido", 1);
					break;
			}
			if (noImageUpdate == false)
				origin.Content = image;
		}
		#endregion

		#region PLC_COM
		private void btnConnect_Click(object sender, RoutedEventArgs e) {
			string status = PLC.Connect(tbIPAddress.Text, 2);
			UpdateStatusBar(status);
			//var comp = StringComparison.OrdinalIgnoreCase;
			if (status.Contains("Successful")) {
				lblConnectionStatus.Background = Brushes.green;
				lblConnectionStatus.Content = "Ligado";
			}
		}
		private void btnDisconnect_Click(object sender, RoutedEventArgs e) {
			string status = PLC.Disconnect();
			UpdateStatusBar(status);
			if (status.Contains("Disconnected") || status.Contains("Not connected")) {
				lblConnectionStatus.Background = Brushes.lightRed;
				lblConnectionStatus.Content = "Desligado";
			}
		}
		private void btnWriteData_Click(object sender, RoutedEventArgs e) {
			double.TryParse("220.34", NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double test);
			PLC.WriteInt(400, 52, test);
		}
		#endregion

	}
}