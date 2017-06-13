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
		readonly FERP_MairCOMS7 PLC = new FERP_MairCOMS7();
		// Wrapper
		int lastTube = 0, currentPackage = 0, id;
		const byte margin = 2;
		// New Order
		private enum ActiveTubeType { Round, Square }
		private enum ActiveWrapType { Hexagonal, Square }
		ActiveTubeType currentTubeType;
		ActiveWrapType currentWrapType;
		// UI control
		private enum ActiveLayout { Default, NewOrder, Strapper, Storage, Recipes, History }
		ActiveLayout currentLayout;
		// Strapper
		bool textChanged = false, cellsArePopulated = false, editingRecipe = false, isStrapsModifyActive = false;
		double[] ecoStraps;
		double ecoLength, ecoStrapsNumber;
		// Storage
		int loops;
		DispatcherTimer storageTimer;
		// History
		private enum ActiveDate { Initial, End }
		ActiveDate currentDate;
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
		readonly public static string systemPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)+@"\Empacotadora";
		readonly public static string path = systemPath + @"\Orders.txt";
		readonly string historyPath = systemPath + @"\PackageHistory.txt";
		readonly string pathSquareTubes = systemPath + @"\SquareTubeRecipes.txt";
		readonly string pathRectTubes = systemPath + @"\RectTubeRecipes.txt";
		readonly string pathRoundTubes = systemPath + @"\RoundTubeRecipes.txt";
		readonly int defaultRoundTubeNmbr = 37, defaultdiameter = 65;
		readonly int defaultSquareTubeNmbr = 36, defaultWidth = 60, defaultHeight = 60;

		public Win_Main() {
			// Set initial layout
			InitializeComponent();
			ShowInitialScreen();
			InitializeLayout();
		}
		/*
		* Wrapper
		* Wrapper-Jog
		* Strapper -> verificar caixas
		* Strapper-Jog
		* Storage
		* Storage-Jog
		* New Order.-.
		* 
		*/
		#region General
		private void btnEnter_Click(object sender, RoutedEventArgs e) {
			ShowMainLayout();
			SetDefaultLayout();
		}
		private void lblDateTime_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			vbCalendar.Visibility = vbCalendar.IsVisible ? Visibility.Collapsed : Visibility.Visible;
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
				catch (NullReferenceException) { /* General.currentOrder is empty */ }
			}
		}
		private void SetNewOrderEnvironment() {
			SetNewOrderLayout();
			SetSqrWrap();
			SetSqrTube();
			currentTubeType = ActiveTubeType.Square;
			tbDensity.Text = "7.65";
			try {
				string lineContent = File.ReadLines(path).Last();
				string[] array = lineContent.Split(',');
				int.TryParse(array[0], out id);
			}
			catch (FileNotFoundException) {
				UpdateStatusBar("Ficheiro que contém as ordens não foi encontrado.", 1);
			}
			lblID.Content = (++id).ToString();
		}
		private void btnSaveNewOrder_Click(object sender, RoutedEventArgs e) {
			string valuesToWrite = "";
			if (GatherTextBoxesValues() == null) return;
			foreach (string item in GatherTextBoxesValues())
				valuesToWrite += item;
			UpdateStatusBar(General.WriteToFile(path, valuesToWrite));
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
			storageTimer.Stop();
			btnEvacuatePackage.IsEnabled = true;
			OneSecondTimer_Tick(null, null);
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
			switch (currentLayout) {
				case ActiveLayout.NewOrder:
					MessageBoxResult answer = MessageBox.Show("Sair sem guardar?", "Confirmar", MessageBoxButton.YesNo);
					if (answer == MessageBoxResult.Yes)
						SetDefaultLayout();
					break;
				case ActiveLayout.History:
					SetStorageLayout();
					break;
				default:
					SetDefaultLayout();
					break;
			}
		}
		private void btnManual_Click(object sender, RoutedEventArgs e) {
			Visibility value = borderManualWrap.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
			if (value == Visibility.Visible) {
				if (currentLayout == ActiveLayout.Default) {
					tabManual.SelectedItem = tabManualWrapper;
				}
				if (currentLayout == ActiveLayout.Strapper) {
					tabManual.SelectedItem = tabManualStrapper;
				}
				if (currentLayout == ActiveLayout.Storage) {
					tabManual.SelectedItem = tabManualStorage;
				}
			}
			borderManualWrap.Visibility = value;
		}
		private void SetOneSecondTimer() {
			// Used in:
			// - date & time label
			// - update canvas (cnvAtado -> PLC_UpdateTubesOnPackage())
			//  DispatcherTimer setup
			DispatcherTimer timer = new DispatcherTimer();
			timer.Tick += new EventHandler(OneSecondTimer_Tick);
			timer.Interval = new TimeSpan(0, 0, 0, 1);
			timer.Start();
		}
		private void OneSecondTimer_Tick(object sender, EventArgs e) {
			lblDateTime.Text = DateTime.Now.ToString("HH\\hmm:ss \n ddd dd/MM/yyyy");
			// PLC_UpdateTubesOnPackage();
			// Controlli_di_pagina F900000_Kernel, "POLMONE_1"
			//if (isStrapperLayoutActive)
			//	UpdateStrapperPage();
			if (currentLayout == ActiveLayout.Storage) {
				//UpdateStorageControls();
			}
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
			currentWrapType = ActiveWrapType.Hexagonal;
		}
		private void btnSqrWrap_Click(object sender, RoutedEventArgs e) {
			SetSqrWrap();
			currentWrapType = ActiveWrapType.Square;
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
			currentWrapType = ActiveWrapType.Hexagonal;
			DrawHexagonalWrap(defaultRoundTubeNmbr, defaultdiameter);
		}
		private void SetSqrWrap() {
			btnSqrWrap.Background = Brushes.lightRed;
			btnSqrWrap.BorderBrush = Brushes.active_border;
			btnHexaWrap.ClearValue(BackgroundProperty);
			btnHexaWrap.BorderBrush = Brushes.non_active_border;
			currentWrapType = ActiveWrapType.Square;
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
			currentTubeType = ActiveTubeType.Round;
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
			currentTubeType = ActiveTubeType.Square;
		}
		// "Set" methods call "Show"/"Hide" methods to combine the desired controls on the window
		private void SetDefaultLayout() {
			ShowGeneralLayout();
			ShowDefaultLayout();
			HideNewOrderLayout();
			//DrawSquareWrap(defaultSquareTubeNmbr, defaultWidth, defaultHeight);
			DrawHexagonalWrap(defaultRoundTubeNmbr, defaultdiameter);
		}
		private void SetNewOrderLayout() {
			HideGeneralLayout();
			HideDefaultLayout();
			ShowNewOrderLayout();
		}
		private void SetStrapperLayout() {
			ShowGeneralLayout();
			HideDefaultLayout();
			ShowStrapperLayout();
		}
		private void SetStorageLayout() {
			ShowGeneralLayout();
			HideDefaultLayout();
			ShowStorageLayout();
		}
		private void SetHistoryLayout() {
			tabLayout.SelectedItem = tabItemHistory;
			FillHistoryDataGrid();
			HideGeneralLayout();
			btnReturn.Visibility = Visibility.Visible;
			currentLayout = ActiveLayout.History;
		}
		private void SetRecipesLayout() {
			lblTitle.Content = "Receitas";
			tabLayout.SelectedItem = tabItemRecipes;
			btnRecipeRoundTube.Background = Brushes.lightRed;
			btnRecipeSquareTube.ClearValue(BackgroundProperty);
			btnReturn.Visibility = Visibility.Visible;
			btnManual.Visibility = Visibility.Collapsed;
			btnOrders.Visibility = Visibility.Collapsed;
			gridRecipesSquareTube.Visibility = Visibility.Collapsed;
			gridRecipesRoundTube.Visibility = Visibility.Visible;
			datagridRecipes.ItemsSource = Recipes.ReadTubeRecipesFromFile(pathRoundTubes);
			isRoundTubeRecipeActive = true;
			currentLayout = ActiveLayout.Recipes;
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
			currentLayout = ActiveLayout.Default;
		}
		private void HideDefaultLayout() {
			btnOrders.Visibility = Visibility.Collapsed;
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
			currentLayout = ActiveLayout.NewOrder;
		}
		private void HideNewOrderLayout() {
			btnReturn.Visibility = Visibility.Collapsed;
			btnSaveNewOrder.Visibility = Visibility.Collapsed;
			borderWrapTubeType.Visibility = Visibility.Collapsed;
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
			currentLayout = ActiveLayout.Strapper;
		}
		private void ShowStorageLayout() {
			lblTitle.Content = "Armazém";
			tabLayout.SelectedItem = tabItemStorage;
			btnWrapper.ClearValue(BackgroundProperty);
			btnStrapper.ClearValue(BackgroundProperty);
			btnStorage.Background = Brushes.lightRed;
			btnPLCConnection.ClearValue(BackgroundProperty);
			borderManualWrap.Visibility = Visibility.Collapsed;
			FillLastHistory();
			currentLayout = ActiveLayout.Storage;
		}
		private void ShowPLCConnectionLayout() {
			btnWrapper.ClearValue(BackgroundProperty);
			btnStrapper.ClearValue(BackgroundProperty);
			btnStorage.ClearValue(BackgroundProperty);
			btnPLCConnection.Background = Brushes.lightRed;
			tabLayout.SelectedItem = tabItemPLCConnection;
		}
		// General Layout
		private void ShowInitialScreen() {
			borderDateTime.Visibility = Visibility.Collapsed;
			status.Visibility = Visibility.Collapsed;
			logoCalculator.Visibility = Visibility.Collapsed;
			btnOrders.Visibility = Visibility.Collapsed;
			btnWrapper.Visibility = Visibility.Collapsed;
			btnStrapper.Visibility = Visibility.Collapsed;
			btnStorage.Visibility = Visibility.Collapsed;
			btnPLCConnection.Visibility = Visibility.Collapsed;
			btnManual.Visibility = Visibility.Collapsed;
			lblTitle.Visibility = Visibility.Collapsed;
			statusBar.Visibility = Visibility.Collapsed;
			tabLayout.SelectedItem = tabItemInit;
		}
		private void ShowMainLayout() {
			borderDateTime.Visibility = Visibility.Visible;
			status.Visibility = Visibility.Visible;
			logoCalculator.Visibility = Visibility.Visible;
			btnOrders.Visibility = Visibility.Visible;
			btnWrapper.Visibility = Visibility.Visible;
			btnStrapper.Visibility = Visibility.Visible;
			btnStorage.Visibility = Visibility.Visible;
			btnPLCConnection.Visibility = Visibility.Visible;
			btnManual.Visibility = Visibility.Visible;
			lblTitle.Visibility = Visibility.Visible;
			statusBar.Visibility = Visibility.Visible;
		}
		private void InitializeLayout() {
			tabItemInit.Visibility = Visibility.Hidden;
			tabItemWrapper.Visibility = Visibility.Hidden;
			tabItemStrapper.Visibility = Visibility.Hidden;
			tabItemStorage.Visibility = Visibility.Hidden;
			tabItemWrapperMain.Visibility = Visibility.Hidden;
			tabItemWrapperNewOrder.Visibility = Visibility.Hidden;
			tabItemHistory.Visibility = Visibility.Hidden;
			tabItemRecipes.Visibility = Visibility.Hidden;
			tabItemPLCConnection.Visibility = Visibility.Hidden;
			tabManualWrapper.Visibility = Visibility.Hidden;
			tabManualStrapper.Visibility = Visibility.Hidden;
			tabManualStorage.Visibility = Visibility.Hidden;
			SetOneSecondTimer();
			SetStorageTimer();
			errorImage.Source = Imaging.CreateBitmapSourceFromHIcon(System.Drawing.SystemIcons.Error.Handle‌​, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
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
			int columns = 0, rows = 0, tubeCurrentlyDrawing = 0;
			List<Ellipse> listEllipses = new List<Ellipse>();

			if (shapeDiameter == 0)
				return;

			if (lastTube == (tubeAmount + 1)) {
				lastTube = 0;
				++currentPackage;
			}

			CreateEllipseShapesToBeDrawn(tubeAmount, tubeAmountBigLine, tubeAmountSmallLine, shapeDiameter, tubeCurrentlyDrawing, lineCap, variavel, incrementing, Vpos, Hpos, ref columns, ref rows, listEllipses);

			Hpos = hPosInit;
			Vpos = vPosInit;

			PutShapesInCanvas(listEllipses);

			double packageWidth = diameter * columns;
			double packageHeight = diameter * rows;
			UpdateLabelsValues(tubeAmount, packageWidth, packageHeight);
		}
		private void GetValuesFromRoundTubeRecipe(int tubeAmount, out int tubeAmountBigLine, out int tubeAmountSmallLine, out int vPosInit, out int hPosInit, out int shapeDiameter) {
			Dictionary<string, int> recipeValues = Recipes.GetRoundTubeRecipe(tubeAmount);
			try {
				tubeAmountBigLine = recipeValues["bigRowSize"];
				tubeAmountSmallLine = recipeValues["smallRowSize"];
				vPosInit = recipeValues["Vpos"];
				hPosInit = recipeValues["Hpos"];
				shapeDiameter = recipeValues["shapeSize"];
			}
			catch (KeyNotFoundException) {
				tubeAmountBigLine = 0;
				tubeAmountSmallLine = 0;
				vPosInit = 0;
				hPosInit = 0;
				shapeDiameter = 0;
			}
		}
		private void CreateEllipseShapesToBeDrawn(int tubeAmount, int tubeAmountBigLine, int tubeAmountSmallLine, int shapeDiameter, int tubeCurrentlyDrawing, byte lineCap, byte variavel, bool incrementing, int Vpos, int Hpos, ref int columns, ref int rows, List<Ellipse> listEllipses) {
			int hPosLineInit;
			for (byte i = 0; i < tubeAmountBigLine; i++) {
				++rows;
				hPosLineInit = Hpos;
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
				switch (incrementing) {
					case true:
						Hpos = hPosLineInit - ((shapeDiameter / 2) + (margin / 2));
						break;
					case false:
						Hpos = hPosLineInit + ((shapeDiameter / 2) + (margin / 2));
						break;
				}
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
			try {
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
			catch (KeyNotFoundException) {
				shapeWidth = 0;
				shapeHeight = 0;
				Vpos_init = 0;
				Hpos_init = 0;
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
			CheckValidityOfTextBoxesValues(sender, tb);
			string[] array = GatherTextBoxesValues();
			double weight = 0;
			double diameter = 0;
			int tubes = 0, width = 0, height = 0;
			try {
				double.TryParse(array[3], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out diameter);
				int.TryParse(array[5], out width);
				int.TryParse(array[4], out height);
				weight = GetWeight(array, diameter, width, height);
				int.TryParse(tbTubeNmbr.Text, out tubes);
			}
			catch (NullReferenceException) { }
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
			if (sender != tbTubeNmbr) return;
			if (currentTubeType == ActiveTubeType.Round)
				DrawHexagonalWrap(tubes, diameter);
			if (currentTubeType == ActiveTubeType.Square)
				DrawSquareWrap(tubes, width, height);
		}
		private void CheckValidityOfTextBoxesValues(object sender, TextBox tb) {
			if (sender != tbDiam && sender != tbWidth && sender != tbHeight) return;
			if (double.TryParse(tb.Text, out double value))
				tb.ClearValue(BackgroundProperty);
			else {
				tb.Background = Brushes.non_active_back;
				if (tb.Text != "")
					MessageBox.Show("- Apenas são aceites números\n" +
									"- Medida não pode ser igual 0", "Valor inserido inválido");
			}
			if (value != 0.00) return;
			tb.Background = Brushes.non_active_back;
			if (tb.Text != "")
				MessageBox.Show("Medida não pode ser igual a 0");
		}
		private static double GetWeight(string[] array, double diameter, int width, int height) {
			double weight;
			double.TryParse(array[6], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double thickness);
			double.TryParse(array[7], out double length);
			double.TryParse(array[8], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double density);
			double diameterOut = diameter, diameterIn = diameter - thickness;
			// Existe um metodo na classe "OrderDetails" chamado "CalculateWeight"... porque calcular aqui?
			// Não calcula bem o peso para tubos redondos
			//if (diameter == 0)
			weight = (((height * width * length) - (((height - (2 * thickness)) * (width - (2 * thickness))) * length)) * (density * 1000) * 0.000000001);
			//else
			//    weight = ((Math.PI * ((Math.Pow((0.5 * diameter_out), 2)) - (Math.Pow((0.5 * diameter_in), 2)))) * length * (density * 0.000001));
			return weight;
		}
		private void tb_isEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) {
			TextBox tb = (TextBox)sender;
			tb.ClearValue(BackgroundProperty);
			tb.Clear();
		}
		private string[] GatherTextBoxesValues() {
			// Gathers value of New Order textboxes and concatenates them into a string
			OrderDetails newOrder = new OrderDetails {
				active = "1",
				Name = tbNrOrdem.Text,
				Diameter = tbDiam.Text,
				Width = tbWidth.Text,
				Height = tbHeight.Text,
				Thick = tbThickness.Text,
				Length = tbLength.Text,
				Density = tbDensity.Text,
				//newOrder.Hardness = "";
				TubeAm = tbTubeNmbr.Text,
				PackageAm = tbPackageAmount.Text,
				TubeType = (currentTubeType == ActiveTubeType.Round ? "R" : "Q"),
				PackageType = (currentWrapType == ActiveWrapType.Hexagonal ? "H" : "Q"),
				Created = DateTime.Now.ToString("dd/MM/yyyy HH\\hmm")
			};
			if (CheckEmptyTextBoxes()) {
				string[] emptyString = null;
				return emptyString;
			}
			try {
				switch (currentTubeType) {
					case ActiveTubeType.Round:
						newOrder.Weight = Math.Round(newOrder.CalculateWeight(newOrder.Diameter, newOrder.Thick, newOrder.Length, newOrder.Density)).ToString();
						break;
					case ActiveTubeType.Square:
						newOrder.Weight = Math.Round(newOrder.CalculateWeight(newOrder.Width, newOrder.Height, newOrder.Thick, newOrder.Length, newOrder.Density)).ToString();
						break;
				}
			}
			catch (Exception) {
				UpdateStatusBar("Cálculo do peso falhou", 1);
			}
			List<string> stringToWrite = new List<string> {
				id.ToString(), newOrder.active, newOrder.Name, newOrder.Diameter, newOrder.Width, newOrder.Height,
				newOrder.Thick, newOrder.Length, newOrder.Density, /*newOrder.Hardness,*/ newOrder.TubeAm,
				newOrder.TubeType, newOrder.PackageAm, newOrder.PackageType, newOrder.Weight, newOrder.Created };
			return stringToWrite.ToArray();
		}
		private bool CheckEmptyTextBoxes() {
			bool boxIsEmpty = false;
			List<TextBox> textBoxes = new List<TextBox>() {tbNrOrdem, tbDiam, tbWidth, tbHeight, tbThickness, tbLength,
															tbTubeNmbr, tbPackageAmount };
			foreach (TextBox box in textBoxes) {
				if (box.Text == "")
					boxIsEmpty = true;
			}
			return boxIsEmpty;
		}
		#endregion

		#region Strapper
		// Number of straps, its position and modification
		private void UpdateImageAndNumberOfTextBoxes() {
			int.TryParse(numKeypadUpDown.Value.ToString(), out int straps);
			List<Grid> boxesGrids = new List<Grid>() { grid2Straps, grid3Straps, grid4Straps, grid5Straps, grid6Straps };
			// Show/Hide grid according to number of straps on the text box
			switch (straps) {
				case 2:
					imgStrap.Source = new BitmapImage(new Uri(@"/Resources/atado2.png", UriKind.Relative));
					foreach (Grid item in boxesGrids)
						item.Visibility = (item.Name == "grid2Straps" ? Visibility.Visible : Visibility.Collapsed);
					break;
				case 3:
					imgStrap.Source = new BitmapImage(new Uri(@"/Resources/atado3.png", UriKind.Relative));
					foreach (Grid item in boxesGrids)
						item.Visibility = (item.Name == "grid3Straps" ? Visibility.Visible : Visibility.Collapsed);
					break;
				case 4:
					imgStrap.Source = new BitmapImage(new Uri(@"/Resources/atado4.png", UriKind.Relative));
					foreach (Grid item in boxesGrids)
						item.Visibility = (item.Name == "grid3Straps" ? Visibility.Visible : Visibility.Collapsed);
					break;
				case 5:
					imgStrap.Source = new BitmapImage(new Uri(@"/Resources/atado5.png", UriKind.Relative));
					foreach (Grid item in boxesGrids)
						item.Visibility = (item.Name == "grid5Straps" ? Visibility.Visible : Visibility.Collapsed);
					break;
				case 6:
					imgStrap.Source = new BitmapImage(new Uri(@"/Resources/atado6.png", UriKind.Relative));
					foreach (Grid item in boxesGrids)
						item.Visibility = (item.Name == "grid6Straps" ? Visibility.Visible : Visibility.Collapsed);
					break;
				default:
					break;
			}
		}
		private void tbStrapPosition_LostFocus(object sender, RoutedEventArgs e) {
			// Finds number of active textboxes (nmbr of straps)
			// and calls method passing the nmbr of straps
			byte.TryParse(numKeypadUpDown.Value.ToString(), out byte value);
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
					int.TryParse(tbstrap2_1.Text, out array[0]);
					int.TryParse(tbstrap2_2.Text, out array[1]);
					values = string.Join(",", array);
					break;
				case 3:
					array = new int[3];
					int.TryParse(tbstrap3_1.Text, out array[0]);
					int.TryParse(tbstrap3_2.Text, out array[1]);
					int.TryParse(tbstrap3_3.Text, out array[2]);
					values = string.Join(",", array);
					break;
				case 4:
					array = new int[4];
					int.TryParse(tbstrap4_1.Text, out array[0]);
					int.TryParse(tbstrap4_2.Text, out array[1]);
					int.TryParse(tbstrap4_3.Text, out array[2]);
					int.TryParse(tbstrap4_4.Text, out array[3]);
					values = string.Join(",", array);
					break;
				case 5:
					array = new int[5];
					int.TryParse(tbstrap5_1.Text, out array[0]);
					int.TryParse(tbstrap5_2.Text, out array[1]);
					int.TryParse(tbstrap5_3.Text, out array[2]);
					int.TryParse(tbstrap5_4.Text, out array[3]);
					int.TryParse(tbstrap5_5.Text, out array[4]);
					values = string.Join(",", array);
					break;
				case 6:
					array = new int[6];
					int.TryParse(tbstrap6_1.Text, out array[0]);
					int.TryParse(tbstrap6_2.Text, out array[1]);
					int.TryParse(tbstrap6_3.Text, out array[2]);
					int.TryParse(tbstrap6_4.Text, out array[3]);
					int.TryParse(tbstrap6_5.Text, out array[4]);
					int.TryParse(tbstrap6_6.Text, out array[5]);
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
		private void numKeypadUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e) {
			textChanged = true;
			if (!numKeypadUpDown.IsInitialized) return;
			UpdateImageAndNumberOfTextBoxes();
			ToogleModifyStrapsTextBoxes();
		}
		private void ToogleModifyStrapsTextBoxes() {
			// Changes strap position textboxes (activate or deactivate modification)
			// according to current state of program
			if (currentLayout == ActiveLayout.Strapper) {
				byte.TryParse(numKeypadUpDown.Value.ToString(), out byte value);
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
					foreach (TextBox item in controlsCollection) {
						item.Background = Brushes.modifyStrapsBrush;
						item.IsReadOnly = false;
						item.Focusable = true;
					}
				}
				else {
					foreach (TextBox item in controlsCollection) {
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
			if (currentLayout != ActiveLayout.Strapper)
				return;
			byte.TryParse(numKeypadUpDown.Value.ToString(), out byte nmbr);
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
			foreach (TextBox item in controlsCollection) {
				item.Text = values[i].ToString();
				++i;
			}
		}
		private void Button_Click(object sender, RoutedEventArgs e) {
			UpdateStrapsValues(6000);
		}
		private void UpdateStrapperPage() {
			// From old program -> update_controlli_standard()
			double oldWidth = new double();
			double oldHeight = new double();
			double oldThickness = new double();
			bool oldTubeType = new bool(), toChange;

			if (oldWidth.ToString() != PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeWidth.Item1) ||
				oldHeight.ToString() != PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeHeight.Item1) ||
				oldThickness.ToString() != PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeThickness.Item1) ||
				oldTubeType != Convert.ToBoolean(PLC.ReadBool(Strapper.DBNumber, Strapper.Order.Tube.bRoundTube))) {
				double.TryParse(PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeWidth.Item1), out oldWidth);
				double.TryParse(PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeHeight.Item1), out oldHeight);
				double.TryParse(PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeThickness.Item1), out oldThickness);
				oldTubeType = Convert.ToBoolean(PLC.ReadBool(Strapper.DBNumber, Strapper.Order.Tube.bRoundTube));
			}
			lblPackPosition.Content = PLC.ReadReal(LateralConveyor.DBNumber, LateralConveyor.PCData.rPackagePositionInStrapper.Item1);
			numKeypadUpDown.Value = Convert.ToDouble(PLC.ReadInt(Strapper.DBNumber, Strapper.Strap.iNumberOfStraps.Item1));

			toChange = ((ecoStraps != PLC.ReadArrayReal(Strapper.DBNumber, Strapper.Strap.aStrapsPosition.Item1, Strapper.Strap.aStrapsPosition.Item2)) ||
						(ecoLength.ToString() != PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeLength.Item1)) ||
						(ecoStrapsNumber.ToString() != PLC.ReadInt(LateralConveyor.DBNumber, LateralConveyor.PCData.iNumberOfRegimentsExecuted.Item1)));

			ecoStraps = PLC.ReadArrayReal(Strapper.DBNumber, Strapper.Strap.aStrapsPosition.Item1, Strapper.Strap.aStrapsPosition.Item2);
			double.TryParse(PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeLength.Item1), out ecoLength);
			double.TryParse(PLC.ReadInt(LateralConveyor.DBNumber, LateralConveyor.PCData.iNumberOfRegimentsExecuted.Item1), out ecoStrapsNumber);

			lblCurrentStrap.Content = ecoStrapsNumber.ToString();
			if (toChange && ecoStraps.Length > 0) {
				// update package state in Strapping -> "aggiorna stato pacco in reggiatura"
			}
		}
		#endregion

		#region Storage
		// Storage
		private void FillLastHistory() {
			try {
				List<History> history = History.ReadHistoryFromFile(historyPath);
				List<Label> weightLabels = new List<Label>() { lblWeight1, lblWeight2, lblWeight3 };
				lblTubesHistory.Content = history[(history.Count) - 1].TubeAm;
				for (byte i = 1; i <= 3; i++)
					weightLabels[i - 1].Content = history[(history.Count) - i].Weight;
				List<Label> dateLabels = new List<Label>() { lblDate1, lblDate2, lblDate3 };
				for (byte i = 1; i <= 3; i++)
					dateLabels[i - 1].Content = history[(history.Count) - i].Created;
			}
			catch (Exception exc) {
				if (exc is FileNotFoundException || exc is ArgumentOutOfRangeException)
					UpdateStatusBar("Ficheiro do histórico não encontrado", 1);
			}
		}
		private void UpdateStorageControls() {
			FillLastHistory();
			lblPackageHistory.Content = PLC.ReadInt(PCPLC.DBNumber, PCPLC.Archive.Package.iTubesPresent.Item1);
			lblTubesHistory.Content = PLC.ReadInt(PCPLC.DBNumber, PCPLC.Archive.Package.iProgressiveNumber.Item1);
			UpdateWeightLabel();
			UpdateDrainLabel();
		}
		private void UpdateWeightLabel() {
			if (/*PLC.ReadBool(setup) &&*/ Convert.ToBoolean(PLC.ReadBool(PCPLC.DBNumber, PCPLC.Weight.bInsertedWeight))) {
				lblWeightHistory.Content = Convert.ToDouble(PLC.ReadReal(PCPLC.DBNumber, PCPLC.Weight.rPackageWeight.Item1)) == -9999 ? "BAD" :
											PLC.ReadReal(PCPLC.DBNumber, PCPLC.Weight.rPackageWeight.Item1).ToString();
			}
			else
				lblWeightHistory.Content = "OFF";
		}
		private void UpdateDrainLabel() {
			if (Convert.ToBoolean(PLC.ReadBool(Storage.DBNumber, Storage.Setup.bEnableDrain))) {
				lblDrain.Content = "ON";
				lblDrain.Foreground = Brushes.green;
			}
			else {
				lblDrain.Content = "OFF";
				lblDrain.Foreground = Brushes.lightRed;
			}
		}
		private void SetStorageTimer() {
			storageTimer = new DispatcherTimer();
			storageTimer.Tick += new EventHandler(StorageTimer_Tick);
			storageTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
		}
		private void StorageTimer_Tick(object sender, EventArgs e) {
			if (currentLayout == ActiveLayout.Storage)
				EvacuatePackageFromScale();
			else
				storageTimer.Stop();
		}
		private void btnEvacuatePackage_Click(object sender, RoutedEventArgs e) {
			PLC.WriteBool(Storage.DBNumber, Storage.Setup.bEvacuateLastPackage, true);
			StorageTimer_Tick(null, null);
			storageTimer.Stop();
		}
		private void EvacuatePackageFromScale() {
			btnEvacuatePackage.IsEnabled = false;
			loops += 1;
			if (loops <= 9) return;
			loops = 0;
			storageTimer.Stop();
			btnEvacuatePackage.IsEnabled = true;
		}
		// History
		private void btnHistory_Click(object sender, RoutedEventArgs e) {
			SetHistoryLayout();
			tbHistoryDayInit.Text = DateTime.Now.ToString("dd");
			tbHistoryMonthInit.Text = DateTime.Now.ToString("MM");
			tbHistoryYearInit.Text = DateTime.Now.ToString("yyyy");
			rbNoFilter.IsChecked = true;
			comboboxShift.SelectedIndex = 0;
			calHistory.SelectedDate = DateTime.Today;
		}
		private void calHistory_SelectedDatesChanged(object sender, SelectionChangedEventArgs e) {
			if ((bool)rbSelectedDate.IsChecked) {
				tbHistoryDayInit.Text = calHistory.SelectedDate.Value.Day.ToString();
				tbHistoryMonthInit.Text = calHistory.SelectedDate.Value.Month.ToString();
				tbHistoryYearInit.Text = calHistory.SelectedDate.Value.Year.ToString();
			}
			else if ((bool)rbInitialFinal.IsChecked) {
				if (currentDate == ActiveDate.Initial) {
					tbHistoryDayInit.Text = calHistory.SelectedDate.Value.Day.ToString();
					tbHistoryMonthInit.Text = calHistory.SelectedDate.Value.Month.ToString();
					tbHistoryYearInit.Text = calHistory.SelectedDate.Value.Year.ToString();
				}
				else {
					tbHistoryDayEnd.Text = calHistory.SelectedDate.Value.Day.ToString();
					tbHistoryMonthEnd.Text = calHistory.SelectedDate.Value.Month.ToString();
					tbHistoryYearEnd.Text = calHistory.SelectedDate.Value.Year.ToString();
				}
			}
			FillHistoryDataGrid();
		}
		private void comboboxShift_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			FillHistoryDataGrid();
		}
		private void rbNoFilter_Checked(object sender, RoutedEventArgs e) {
			lblInitialDate.Content = "Data";
			tbHistoryDayInit.IsEnabled = false;
			tbHistoryMonthInit.IsEnabled = false;
			tbHistoryYearInit.IsEnabled = false;
			gridFinalDate.Visibility = Visibility.Collapsed;
			comboboxShift.IsEnabled = false;
			FillHistoryDataGrid();
		}
		private void rbSelectedDate_Checked(object sender, RoutedEventArgs e) {
			lblInitialDate.Content = "Data";
			tbHistoryDayInit.IsEnabled = true;
			tbHistoryMonthInit.IsEnabled = true;
			tbHistoryYearInit.IsEnabled = true;
			gridFinalDate.Visibility = Visibility.Collapsed;
			comboboxShift.IsEnabled = true;
			FillHistoryDataGrid();
		}
		private void rbInitialFinal_Checked(object sender, RoutedEventArgs e) {
			lblInitialDate.Content = "Data inicial";
			tbHistoryDayInit.IsEnabled = true;
			tbHistoryMonthInit.IsEnabled = true;
			tbHistoryYearInit.IsEnabled = true;
			comboboxShift.IsEnabled = true;
			gridFinalDate.Visibility = Visibility.Visible;
		}
		private void InitialDate_GotFocus(object sender, RoutedEventArgs e) {
			currentDate = ActiveDate.Initial;
		}
		private void FinalDate_GotFocus(object sender, RoutedEventArgs e) {
			currentDate = ActiveDate.End;
		}
		private void FillHistoryDataGrid() {
			try {
				if ((bool)rbNoFilter.IsChecked)
					datagridHistory.ItemsSource = History.ReadHistoryFromFile(historyPath);
				else if ((bool)rbSelectedDate.IsChecked) {
					try {
						if (comboboxShift.SelectedIndex == 0)
							datagridHistory.ItemsSource = History.ReadHistoryFromFile(historyPath, calHistory.SelectedDate.Value.Date);
						else
							datagridHistory.ItemsSource = History.ReadHistoryFromFile(historyPath, calHistory.SelectedDate.Value.Date, calHistory.SelectedDate.Value.Date, (byte)comboboxShift.SelectedIndex);
					}
					catch (InvalidOperationException) { }
				}
				else if ((bool)rbInitialFinal.IsChecked) {
					string sInitDate = tbHistoryDayInit.Text + " " + tbHistoryMonthInit.Text + " " + tbHistoryYearInit.Text;
					string sEndDate = tbHistoryDayEnd.Text + " " + tbHistoryMonthEnd.Text + " " + tbHistoryYearEnd.Text;
					DateTime.TryParse(sInitDate, out DateTime initialDate);
					try {
						DateTime endDate = DateTime.Parse(sEndDate);
						if (comboboxShift.SelectedIndex == 0)
							datagridHistory.ItemsSource = History.ReadHistoryFromFile(historyPath, initialDate, endDate);
						else
							datagridHistory.ItemsSource = History.ReadHistoryFromFile(historyPath, initialDate, endDate, (byte)comboboxShift.SelectedIndex);
					}
					catch (FormatException) {
						UpdateStatusBar("Escolha um intervalo de datas");
					}
				}
			}
			catch (FileNotFoundException) {
				UpdateStatusBar("History file not found", 1);
			}
		}
		#endregion

		#region Recipes
		// Recipes
		private void btnRecipeRoundTube_Click(object sender, RoutedEventArgs e) {
			ShowTubeRecipesOnDataGrid(pathRoundTubes);
			IEnumerable<TextBox> textBoxes = GetTextBoxesFromGrids();
			foreach (TextBox item in textBoxes) {
				item.Text = "";
			}
			isRoundTubeRecipeActive = true;
		}
		private void btnRecipeSquareTube_Click(object sender, RoutedEventArgs e) {
			ShowTubeRecipesOnDataGrid(pathSquareTubes, pathRectTubes);
			IEnumerable<TextBox> textBoxes = GetTextBoxesFromGrids();
			foreach (TextBox item in textBoxes) {
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
				foreach (TextBox item in textBoxes) {
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
			if (!editingRecipe) return;
			e.Handled = true;
			UpdateStatusBar("Para mudar de receita termine de editar a atual", 1);
		}
		private bool GetDataFromSelectedCells() {
			if (isRoundTubeRecipeActive == true) {
				RoundTubeRecipe datagridRow = GetRoundTubeRecipeFromGrid();
				if (datagridRow == null) return false;
				tbRecipeTubes.Text = datagridRow.TubeNumber;
				tbRecipeBigRow.Text = datagridRow.BigRow;
				tbRecipeSmallRow.Text = datagridRow.SmallRow;
				return true;
			}
			else {
				SquareTubeRecipe datagridRow = GetSquareTubeRecipeFromGrid();
				if (datagridRow == null) return false;
				tbRecipeTubes.Text = datagridRow.TubeNumber;
				tbRecipecolumns.Text = datagridRow.Columns;
				tbRecipeRows.Text = datagridRow.Rows;
				return true;
			}
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
				EditSquareTubeRecipesTextFile(newFileContent, pathSquareTubes, out bool found);
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
			foreach (TextBox item in textBoxes) {
				item.ClearValue(BackgroundProperty);
				item.IsReadOnly = true;
				item.Focusable = false;
			}
		}
		private void EditSquareTubeRecipesTextFile(List<string> newFileContent, string path, out bool found) {
			found = false;
			foreach (string item in File.ReadAllLines(path)) {
				string newline = "";
				string[] array = item.Split(',');
				if (array[0] == tbRecipeTubes.Text) {
					array[array.Length - 2] = tbRecipecolumns.Text;
					array[array.Length - 1] = tbRecipeRows.Text;
					if (path == pathSquareTubes)
						found = true;
					foreach (string value in array)
						newline += value + ",";
					newline = newline.Remove(newline.Length - 1);
				}
				newFileContent.Add(newline == "" ? item : newline);
			}
		}
		private void EditRoundTubeRecipesTextFile(List<string> newFileContent) {
			foreach (string item in File.ReadAllLines(pathRoundTubes)) {
				string newline = "";
				string[] array = item.Split(',');
				if (array[0] == tbRecipeTubes.Text) {
					array[1] = tbRecipeBigRow.Text;
					array[2] = tbRecipeSmallRow.Text;
					foreach (string value in array)
						newline += value + ",";
					newline = newline.Remove(newline.Length - 1);
				}
				newFileContent.Add(newline == "" ? item : newline);
			}
		}
		private void btnRecipeCancel_Click(object sender, RoutedEventArgs e) {
			List<TextBox> textBoxes = GetTextBoxesFromGrids();
			foreach (TextBox item in textBoxes) {
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
					PLC.ToogleBool(Accumulator_1.DBNumber, Accumulator_1.ManualMovement.bTransportChain);
					break;
				case "btnAlignmentRolls":
					image = (origin.Content == (Image)FindResource("AlignmentRolls") ? (Image)FindResource("A_AlignmentRolls") : (Image)FindResource("AlignmentRolls"));
					PLC.ToogleBool(Accumulator_1.DBNumber, Accumulator_1.ManualMovement.bAlignmentRolls);
					break;
				case "btnTrasportQueue":
					image = (origin.Content == (Image)FindResource("TrasportQueue") ? (Image)FindResource("A_TrasportQueue") : (Image)FindResource("TrasportQueue"));
					PLC.ToogleBool(Accumulator_1.DBNumber, Accumulator_1.ManualMovement.bTrasportQueue);
					break;
				case "btnAlignQueue":
					image = (origin.Content == (Image)FindResource("AlignQueue") ? (Image)FindResource("A_AlignQueue") : (Image)FindResource("AlignQueue"));
					PLC.ToogleBool(Accumulator_1.DBNumber, Accumulator_1.ManualMovement.bAlignQueue);
					break;
				case "btnLoader":
					image = (origin.Content == (Image)FindResource("Loader") ? (Image)FindResource("A_Loader") : (Image)FindResource("Loader"));
					PLC.ToogleBool(Accumulator_1.DBNumber, Accumulator_1.ManualMovement.bLoader);
					break;
				case "btnMechanicalCounterblocks":
					image = (origin.Content == (Image)FindResource("MechanicalCounterblocks") ? (Image)FindResource("A_MechanicalCounterblocks") : (Image)FindResource("MechanicalCounterblocks"));
					PLC.ToogleBool(Accumulator_1.DBNumber, Accumulator_1.ManualMovement.bMechanicalCounterblocks);
					break;
				case "btnLowerPneumaticCounterblocks":
					noImageUpdate = true;
					gridLowerActive.Visibility = gridLowerActive.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
					PLC.ToogleBool(Accumulator_1.DBNumber, Accumulator_1.ManualMovement.bLowerPneumaticCounterblocks);
					break;
				case "btnSuperiorPneumaticCounterblocks":
					noImageUpdate = true;
					gridUpperActive.Visibility = gridUpperActive.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
					PLC.ToogleBool(Accumulator_1.DBNumber, Accumulator_1.ManualMovement.bSuperiorPneumaticCounterblocks);
					break;
				case "btnLateralPneumaticCounterblocks":
					noImageUpdate = true;
					gridLateralActive.Visibility = gridLateralActive.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
					PLC.ToogleBool(Accumulator_1.DBNumber, Accumulator_1.ManualMovement.bLateralPneumaticCounterblocks);
					break;
				case "btnShelves":
					image = (origin.Content == (Image)FindResource("Shelves") ? (Image)FindResource("A_Shelves") : (Image)FindResource("Shelves"));
					PLC.ToogleBool(Accumulator_1.DBNumber, Accumulator_1.ManualMovement.bShelves);
					break;
				case "btnCar":
					image = (origin.Content == (Image)FindResource("Car") ? (Image)FindResource("A_Car") : (Image)FindResource("Car"));
					PLC.ToogleBool(Strapper.DBNumber, Strapper.ManualMovement.bCar);
					break;
				case "btnLateralTransp":
					image = (origin.Content == (Image)FindResource("LateralTransp") ? (Image)FindResource("A_LateralTransp") : (Image)FindResource("LateralTransp"));
					PLC.ToogleBool(Strapper.DBNumber, Strapper.ManualMovement.bLateralTransp);
					break;
				case "btnUpperRolls":
					noImageUpdate = true;
					if (tbUpperRolls.Background == Brushes.yellowBrush)
						tbUpperRolls.ClearValue(BackgroundProperty);
					else
						tbUpperRolls.Background = Brushes.yellowBrush;
					PLC.ToogleBool(Strapper.DBNumber, Strapper.ManualMovement.bUpperRolls);
					break;
				case "btnCarRolls":
					image = (origin.Content == (Image)FindResource("CarRolls") ? (Image)FindResource("A_CarRolls") : (Image)FindResource("CarRolls"));
					PLC.ToogleBool(Strapper.DBNumber, Strapper.ManualMovement.bCarRolls);
					break;
				case "btnCarRolls1_2":
					noImageUpdate = true;
					gridCarRolls1_2_Active.Visibility = gridCarRolls1_2_Active.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
					PLC.ToogleBool(Strapper.DBNumber, Strapper.ManualMovement.bCarRolls12);
					break;
				case "btnCarRolls2":
					noImageUpdate = true;
					gridCarRolls2_Active.Visibility = gridCarRolls2_Active.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
					PLC.ToogleBool(Strapper.DBNumber, Strapper.ManualMovement.bCarRolls2);
					break;
				case "btnLiftChains":
					image = (origin.Content == (Image)FindResource("LiftChains") ? (Image)FindResource("A_LiftChains") : (Image)FindResource("LiftChains"));
					PLC.ToogleBool(Storage.DBNumber, Storage.ManualMovement.bLiftingChains);
					break;
				case "btnStorageChains":
					image = (origin.Content == (Image)FindResource("StorageChains") ? (Image)FindResource("A_StorageChains") : (Image)FindResource("StorageChains"));
					PLC.ToogleBool(Storage.DBNumber, Storage.ManualMovement.bStorageChains);
					break;
				case "btnStorageChains_Withdrawal":
					image = (origin.Content == (Image)FindResource("StorageChains-Withdrawal") ? (Image)FindResource("A_StorageChains-Withdrawal") : (Image)FindResource("StorageChains-Withdrawal"));
					PLC.ToogleBool(Storage.DBNumber, Storage.ManualMovement.bStorage_LiftingChains);
					break;
				case "btnDrain12":
					noImageUpdate = true;
					gridDrain12Active.Visibility = gridDrain12Active.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
					PLC.ToogleBool(Storage.DBNumber, Storage.ManualMovement.bDrains1_2);
					break;
				case "btnDrain123":
					noImageUpdate = true;
					gridDrain123Active.Visibility = gridDrain123Active.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
					PLC.ToogleBool(Storage.DBNumber, Storage.ManualMovement.bDrains1_2_3);
					break;
				case "btnDrain1234":
					noImageUpdate = true;
					gridDrain1234Active.Visibility = gridDrain1234Active.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
					PLC.ToogleBool(Storage.DBNumber, Storage.ManualMovement.bDrains1_2_3_4);
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
			if (!status.Contains("Successful")) return;
			lblConnectionStatus.Background = Brushes.green;
			lblConnectionStatus.Content = "Ligado";
		}
		private void btnDisconnect_Click(object sender, RoutedEventArgs e) {
			string status = PLC.Disconnect();
			UpdateStatusBar(status);
			if (!status.Contains("Disconnected") && !status.Contains("Not connected")) return;
			lblConnectionStatus.Background = Brushes.lightRed;
			lblConnectionStatus.Content = "Desligado";
		}
		private void btnWriteData_Click(object sender, RoutedEventArgs e) {
			double.TryParse("220.34", NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double test);
			PLC.WriteInt(400, 52, test);
		}
		#endregion

	}
}