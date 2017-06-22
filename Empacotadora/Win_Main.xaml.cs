using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents.Serialization;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Globalization;
using Empacotadora.Address;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Empacotadora {
	/// <summary>
	/// Lógica interna para Win_Main.xaml
	/// </summary>
	public partial class Win_Main : Window {
		// tb -> textbox / lbl -> label / btn -> button / rb -> radiobutton / sb -> statusbar
		// cal -> calendar / vb -> viewbox / lb -> listbox
		// General
		const string SaveSuccessful = "Sucesso ao gravar no ficheiro";
		const string SaveError = "Erro ao tentar gravar no ficheiro";
		FERP_MairCOMS7 PLC = new FERP_MairCOMS7();
		public static OrderDetails CurrentOrder = null;
		Visibility _visible = Visibility.Visible;
		Visibility _collapsed = Visibility.Collapsed;
		// Wrapper
		static int _lastTube = 0;
		public static int LastTube { get => _lastTube; }
		int _currentPackage = 0;
		// New Order
		General.ActiveTubeType _currentTubeType;
		General.ActiveWrapType _currentWrapType;
		// UI control
		General.ActiveLayout _currentLayout;
		// Strapper
		bool _textChanged = false, _cellsArePopulated = false, _editingRecipe = false, _isStrapsModifyActive = false;
		double[] _ecoStraps;
		double _ecoLength, _ecoStrapsNumber;
		// History
		General.ActiveDate _currentDate;
		// Recipe
		General.ActiveRecipe _currentRecipe;
		// PLC
		int[] PLCArrayPackageRows;
		int _tubeNumber;
		bool _changeOn, _tubeChange, _pageActive;
		struct StructTubeChange {
			public string OldLength;
			public string OldThickness;
			public string OldTi;
			public string OldWidth;
			public string OldHeight;
		}
		StructTubeChange _oldTube = new StructTubeChange();

		readonly int _defaultRoundTubeNmbr = 37, _defaultDiameter = 120;
		readonly int _defaultSquareTubeNmbr = 36, _defaultWidth = 60, _defaultHeight = 60;

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
			SetWrapperLayout();
		}
		private void lblDateTime_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			vbCalendar.Visibility = vbCalendar.IsVisible ? _collapsed : _visible;
		}
		private void logoCalculator_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			Win_Calculator wnCalculator = new Win_Calculator();
			wnCalculator.ShowDialog();
		}
		private void btnOrders_Click(object sender, RoutedEventArgs e) {
			Win_Orders wnOrders = new Win_Orders();
			btnOrders.Background = Brushes.LightRed;
			btnWrapper.ClearValue(BackgroundProperty);
			wnOrders.ShowDialog();
			switch (Win_Orders.ordersLayout) {
				case Win_Orders.Layout.Default:
					SetWrapperLayout();
					ShowCurrentOrderOnWrapperLayout();
					break;
				case Win_Orders.Layout.NewOrder:
					SetNewOrderEnvironment();
					break;
				case Win_Orders.Layout.EditOrder:
					SetEditOrderLayout();
					FillEditOrderLayoutWithCurrentOrder();
					break;
				case Win_Orders.Layout.Recipes:
					HideGeneralControls();
					SetRecipesLayout();
					break;
			}
		}
		private void FillEditOrderLayoutWithCurrentOrder() {
			if (CurrentOrder.TubeType == "R")
				SetRoundTube();
			else if (CurrentOrder.TubeType == "Q")
				SetSqrTube();
			if (CurrentOrder.PackageType == "H")
				SetHexaWrap();
			else if (CurrentOrder.PackageType == "Q")
				SetSqrWrap();
			tbNrOrdem.Text = CurrentOrder.Name;
			tbDiam.Text = CurrentOrder.Diameter;
			tbWidth.Text = CurrentOrder.Width;
			tbHeight.Text = CurrentOrder.Height;
			tbThickness.Text = CurrentOrder.Thick;
			tbLength.Text = CurrentOrder.Length;
			tbDensity.Text = CurrentOrder.Density;
			tbTubeNmbr.Text = CurrentOrder.TubeAm;
			lblWeight.Content = CurrentOrder.Weight;
			lblID.Content = CurrentOrder.ID;
			lblID1.Content = "ID:";
		}
		private void SetNewOrderEnvironment() {
			SetEditOrderLayout();
			SetSqrWrap();
			SetSqrTube();
			IEnumerable<TextBox> newOrderTextBoxes = General.GetTextBoxesFromGrid(gridNewOrder);
			foreach (TextBox item in newOrderTextBoxes)
				item.Text = "";
			lblID1.Content = "ID (auto):";
			tbDensity.Text = "7.65";
			if (!Document.ReadFromFile(General.Path, out IEnumerable<string> linesFromFile)) return;
			string[] array = linesFromFile.Last().Split(',');
			int.TryParse(array[0], out int id);
			lblID.Content = (++id).ToString();
		}
		private void ShowCurrentOrderOnWrapperLayout() {
			if (CurrentOrder == null) return;
			int.TryParse(CurrentOrder.TubeAm, out int amount);
			if (CurrentOrder.Diameter == "") {
				gridRound.Visibility = _collapsed;
				gridSquare.Visibility = _visible;
				lblOrderWidth.Content = CurrentOrder.Width;
				lblOrderHeight.Content = CurrentOrder.Height;
			}
			else {
				gridRound.Visibility = _visible;
				gridSquare.Visibility = _collapsed;
				lblOrderDiam.Content = CurrentOrder.Diameter;
			}
			lblOrderName.Content = CurrentOrder.Name;
			lblOrderThick.Content = CurrentOrder.Thick;
			lblOrderLength.Content = CurrentOrder.Length;
			lblPackageLength.Content = CurrentOrder.Length;
		}
		private void btnSaveOrder_Click(object sender, RoutedEventArgs e) {
			string valuesToWrite = "";
			string msg = "";
			if (GatherOrderTextBoxesValues() == null) {
				UpdateStatusBar("Para gravar tem que preencher todos os campos");
				return;
			}
			if (Win_Orders.ordersLayout == Win_Orders.Layout.NewOrder) {
				//foreach (string item in GatherNewOrderTextBoxesValues())
				//	valuesToWrite += item;
				valuesToWrite = GatherOrderTextBoxesValues(). Aggregate("", (current, item) => current + (item + ","));
				valuesToWrite = valuesToWrite.Remove(valuesToWrite.Length - 1);
				msg = Document.AppendToFile(General.Path, valuesToWrite) ? SaveSuccessful : SaveError;
			}
			else if (Win_Orders.ordersLayout == Win_Orders.Layout.EditOrder) {
				msg = OrderDetails.EditOrder(General.Path, CurrentOrder.ID, GatherOrderTextBoxesValues().ToArray()) ?
																		SaveSuccessful : SaveError;
				ShowCurrentOrderOnWrapperLayout();
			}
			UpdateStatusBar(msg);
			SetWrapperLayout();
		}
		private void btnWrapper_Click(object sender, RoutedEventArgs e) {
			SetWrapperLayout();
		}
		private void btnStrapper_Click(object sender, RoutedEventArgs e) {
			SetStrapperLayout();
			DisableModifyStrapsTextBoxes();
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
			switch (_currentLayout) {
				case General.ActiveLayout.EditOrder:
					MessageBoxResult answer = MessageBox.Show("Sair sem guardar?", "Confirmar", MessageBoxButton.YesNo);
					if (answer == MessageBoxResult.Yes)
						SetWrapperLayout();
					break;
				case General.ActiveLayout.History:
					SetStorageLayout();
					break;
				case General.ActiveLayout.NewRecipe:
					SetRecipesLayout();
					break;
				default:
					SetWrapperLayout();
					break;
			}
		}
		private void btnManual_Click(object sender, RoutedEventArgs e) {
			Visibility value = borderManualWrap.Visibility == _visible ? _collapsed : _visible;
			if (value == _visible) {
				if (_currentLayout == General.ActiveLayout.Wrapper)
					tabManual.SelectedItem = tabManualWrapper;
				if (_currentLayout == General.ActiveLayout.Strapper)
					tabManual.SelectedItem = tabManualStrapper;
				if (_currentLayout == General.ActiveLayout.Storage)
					tabManual.SelectedItem = tabManualStorage;
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
			lblDateTime.Text = DateTime.Now.ToString("HH\\hmm:ss \n ddd dd/MM/yyyy"); ;
			// Controlli_di_pagina F900000_Kernel, "POLMONE_1"
			switch (_currentLayout) {
				case General.ActiveLayout.Wrapper:

					break;
				case General.ActiveLayout.Strapper:
					//UpdateStrapperPage();
					break;
				case General.ActiveLayout.Storage:
					//UpdateStoragePage();
					break;
				case General.ActiveLayout.History:

					break;
			}
		}
		private void tb_PreviewMouseDoubleClick_Keypad(object sender, MouseButtonEventArgs e) {
			TextBox tb = (TextBox)sender;
			Win_Keypad wnKeypad = new Win_Keypad();
			wnKeypad.ShowDialog();
			if (Win_Keypad.Enter == true)
				tb.Text = wnKeypad.tbResult.Text;
		}
		private void ResetTextBox(TextBox item) {
			item.ClearValue(BackgroundProperty);
			item.IsReadOnly = true;
			item.Focusable = false;
		}
		private void ClearButtonBackground(IEnumerable<Button> buttonsToClear) {
			foreach (Button item in buttonsToClear)
				item.ClearValue(BackgroundProperty);
		}
		#region StatusBar
		// Status bar update
		private void SetStatusBarTimer() {
			//  DispatcherTimer setup
			DispatcherTimer timer = new DispatcherTimer();
			timer.Tick += new EventHandler(StatusBarTimer_Tick);
			timer.Interval = new TimeSpan(0, 0, 0, 3, 500);
			timer.Stop();
			timer.Start();
		}
		private void StatusBarTimer_Tick(object sender, EventArgs e) {
			DispatcherTimer timer = (DispatcherTimer)sender;
			timer.Stop();
			sbIcon.Visibility = _collapsed;
			status.Content = "Pronto";
		}
		private void UpdateStatusBar(string msg) {
			status.Content = msg;
			SetStatusBarTimer();
		}
		private void UpdateStatusBar(string msg, byte error) {
			// pass any number (0 <-> 255) through "error" to show the error icon with the message
			status.Content = msg;
			sbIcon.Visibility = _visible;
			SetStatusBarTimer();
		}
		#endregion

		#endregion

		#region UI control methods
		// New order > define type of shape being drawn
		private void btnHexaWrap_Click(object sender, RoutedEventArgs e) {
			SetHexaWrap();
		}
		private void btnSqrWrap_Click(object sender, RoutedEventArgs e) {
			SetSqrWrap();
		}
		private void btnRoundTube_Click(object sender, RoutedEventArgs e) {
			SetRoundTube();
		}
		private void btnSqrTube_Click(object sender, RoutedEventArgs e) {
			SetSqrTube();
		}
		private void SetHexaWrap() {
			if (_currentLayout == General.ActiveLayout.NewRecipe) {
				gridNewRecipeHexa.Visibility = _visible;
				gridNewRecipeSquare.Visibility = _collapsed;
			}
			btnHexaWrap.Background = Brushes.LightRed;
			btnHexaWrap.BorderBrush = Brushes.ActiveBorder;
			btnSqrWrap.ClearValue(BackgroundProperty);
			btnSqrWrap.BorderBrush = Brushes.NonActiveBorder;
			_currentWrapType = General.ActiveWrapType.Hexagonal;
			DrawHexagonalWrap(_defaultRoundTubeNmbr, _defaultDiameter);
		}
		private void SetSqrWrap() {
			if (_currentLayout == General.ActiveLayout.NewRecipe) {
				gridNewRecipeHexa.Visibility = _collapsed;
				gridNewRecipeSquare.Visibility = _visible;
			}
			btnSqrWrap.Background = Brushes.LightRed;
			btnSqrWrap.BorderBrush = Brushes.ActiveBorder;
			btnHexaWrap.ClearValue(BackgroundProperty);
			btnHexaWrap.BorderBrush = Brushes.NonActiveBorder;
			_currentWrapType = General.ActiveWrapType.Square;
			if (tbWidth.Text != "" && tbHeight.Text != "") {
				int.TryParse(tbWidth.Text, out int width);
				int.TryParse(tbHeight.Text, out int height);
				DrawSquareWrap(_defaultSquareTubeNmbr, width, height);
			}
			else
				DrawSquareWrap(_defaultSquareTubeNmbr, _defaultWidth, _defaultHeight);
		}
		private void SetRoundTube() {
			btnRoundTube.Background = Brushes.LightRed;
			btnRoundTube.BorderBrush = Brushes.ActiveBorder;
			btnSqrTube.ClearValue(BackgroundProperty);
			btnSqrTube.BorderBrush = Brushes.NonActiveBorder;
			tbDiam.IsEnabled = true;
			tbDiam.Focus();
			tbHeight.IsEnabled = false;
			tbWidth.IsEnabled = false;
			_currentTubeType = General.ActiveTubeType.Round;
		}
		private void SetSqrTube() {
			btnSqrTube.Background = Brushes.LightRed;
			btnSqrTube.BorderBrush = Brushes.ActiveBorder;
			btnRoundTube.ClearValue(BackgroundProperty);
			btnRoundTube.BorderBrush = Brushes.NonActiveBorder;
			tbDiam.IsEnabled = false;
			tbWidth.IsEnabled = true;
			tbWidth.Focus();
			tbHeight.IsEnabled = true;
			_currentTubeType = General.ActiveTubeType.Square;
		}
		// "Set" methods call "Show"/"Hide" methods to combine the desired controls on the window
		private void SetWrapperLayout() {
			ShowGeneralControls();
			ShowWrapperControls();
			if (CurrentOrder != null)
				ShowCurrentOrderOnWrapperLayout();
			HideEditOrderControls();
			DrawShape();
		}
		private void SetEditOrderLayout() {
			HideGeneralControls();
			HideWrapperControls();
			ShowEditOrderControls();
		}
		private void SetStrapperLayout() {
			ShowGeneralControls();
			HideWrapperControls();
			ShowStrapperControls();
		}
		private void SetStorageLayout() {
			ShowGeneralControls();
			HideWrapperControls();
			ShowStorageControls();
		}
		private void SetHistoryLayout() {
			tabLayout.SelectedItem = tabItemHistory;
			FillHistoryDataGrid();
			HideGeneralControls();
			btnReturn.Visibility = _visible;
			_currentLayout = General.ActiveLayout.History;
		}
		private void SetRecipesLayout() {
			lblTitle.Content = "Receitas";
			tabLayout.SelectedItem = tabItemRecipes;
			btnRecipeRoundTube.Background = Brushes.LightRed;
			btnRecipeSquareTube.ClearValue(BackgroundProperty);
			btnReturn.Visibility = _visible;
			btnManual.Visibility = _collapsed;
			btnOrders.Visibility = _collapsed;
			gridRecipesSquareTube.Visibility = _collapsed;
			gridRecipesRoundTube.Visibility = _visible;
			datagridRecipes.ItemsSource = Recipes.ReadTubeRecipesFromFile(General.PathRoundTubes);
			_currentRecipe = General.ActiveRecipe.RoundTube;
			_currentLayout = General.ActiveLayout.Recipes;
		}
		// "Show"/"Hide" methods show or hide layout controls
		private void ShowGeneralControls() {
			IEnumerable<Button> buttons = new List<Button>() {
				btnWrapper, btnStrapper, btnStorage, btnExit, btnManual, btnPLCConnection};
			foreach (var item in buttons)
				item.Visibility = _visible;
		}
		private void HideGeneralControls() {
			IEnumerable<Button> buttons = new List<Button>() {
				btnWrapper, btnStrapper, btnStorage, btnExit, btnReturn, btnManual, btnPLCConnection};
			foreach (var item in buttons)
				item.Visibility = _collapsed;
		}
		private void ShowWrapperControls() {
			lblTitle.Content = "Empacotadora";
			btnOrders.Visibility = _visible;
			borderCanvas.Visibility = _visible;
			borderCanvas.Margin = new Thickness(805, 0, 79, 78);
			tabLayout.SelectedItem = tabItemWrapper;
			tabWrapper.SelectedItem = tabItemWrapperMain;
			IEnumerable<Button> buttonsToClear = new List<Button>() {
				btnOrders, btnStrapper, btnStorage, btnPLCConnection };
			ClearButtonBackground(buttonsToClear);
			btnWrapper.Background = Brushes.LightRed;
			borderManualWrap.Visibility = _collapsed;
			gridNewRecipeDrawnShapes.Visibility = _collapsed;
			_currentLayout = General.ActiveLayout.Wrapper;
		}
		private void HideWrapperControls() {
			btnOrders.Visibility = _collapsed;
		}
		private void ShowEditOrderControls() {
			btnReturn.Visibility = _visible;
			btnSaveOrder.Visibility = _visible;
			borderWrapTubeType.Visibility = _visible;
			borderCanvas.Visibility = _visible;
			borderCanvas.Margin = new Thickness(805, 0, 79, 4);
			tabLayout.SelectedItem = tabItemWrapper;
			tabWrapper.SelectedItem = tabItemWrapperEditOrder;
			borderManualWrap.Visibility = _collapsed;
			_currentLayout = General.ActiveLayout.EditOrder;
		}
		private void HideEditOrderControls() {
			btnReturn.Visibility = _collapsed;
			btnSaveOrder.Visibility = _collapsed;
			borderWrapTubeType.Visibility = _collapsed;
		}
		private void ShowStrapperControls() {
			lblTitle.Content = "Cintadora";
			tabLayout.SelectedItem = tabItemStrapper;
			IEnumerable<Button> buttonsToClear = new List<Button>() {
				btnWrapper, btnStorage, btnPLCConnection };
			ClearButtonBackground(buttonsToClear);
			if (CurrentOrder != null)
				lblPackageLength.Content = CurrentOrder.Length;
			btnStrapper.Background = Brushes.LightRed;
			_isStrapsModifyActive = false;
			ToogleModifyStrapsTextBoxes();
			borderManualWrap.Visibility = _collapsed;
			_currentLayout = General.ActiveLayout.Strapper;
		}
		private void ShowStorageControls() {
			lblTitle.Content = "Armazém";
			tabLayout.SelectedItem = tabItemStorage;
			IEnumerable<Button> buttonsToClear = new List<Button>() {
				btnWrapper, btnStrapper, btnPLCConnection };
			ClearButtonBackground(buttonsToClear);
			btnStorage.Background = Brushes.LightRed;
			borderManualWrap.Visibility = _collapsed;
			FillLastHistory();
			_currentLayout = General.ActiveLayout.Storage;
		}
		private void ShowPLCConnectionLayout() {
			IEnumerable<Button> buttonsToClear = new List<Button>() {
				btnWrapper, btnStorage, btnStorage };
			ClearButtonBackground(buttonsToClear);
			btnPLCConnection.Background = Brushes.LightRed;
			tabLayout.SelectedItem = tabItemPLCConnection;
		}
		// General Layout
		private void ShowInitialScreen() {
			IEnumerable<Button> buttons = new List<Button>() {
				btnWrapper, btnStrapper, btnStorage, btnManual, btnPLCConnection};
			foreach (var item in buttons)
				item.Visibility = _collapsed;
			borderDateTime.Visibility = _collapsed;
			status.Visibility = _collapsed;
			logoCalculator.Visibility = _collapsed;
			btnOrders.Visibility = _collapsed;
			lblTitle.Visibility = _collapsed;
			statusBar.Visibility = _collapsed;
			tabLayout.SelectedItem = tabItemInit;
		}
		private void ShowMainLayout() {
			IEnumerable<Button> buttons = new List<Button>() {
				btnWrapper, btnStrapper, btnStorage, btnManual, btnPLCConnection};
			foreach (var item in buttons)
				item.Visibility = _visible;
			borderDateTime.Visibility = _visible;
			status.Visibility = _visible;
			logoCalculator.Visibility = _visible;
			btnOrders.Visibility = _visible;
			lblTitle.Visibility = _visible;
			statusBar.Visibility = _visible;
		}
		private void InitializeLayout() {
			IEnumerable<TabItem> buttons = new List<TabItem>() {
				tabItemInit, tabItemWrapper,tabItemStrapper, tabItemStorage, tabItemWrapperMain,
				tabItemWrapperEditOrder, tabItemHistory, tabItemRecipes, tabItemPLCConnection,
				tabManualWrapper, tabManualStrapper,  tabManualStorage, tabItemWrapperNewRecipe };
			foreach (var item in buttons)
				item.Visibility = Visibility.Hidden;
			SetOneSecondTimer();
			errorImage.Source = Imaging.CreateBitmapSourceFromHIcon(System.Drawing.SystemIcons.Error.Handle‌​, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
		}
		#endregion

		#region Wrapper
		private void btnAddTube_Click(object sender, RoutedEventArgs e) {
			++_lastTube;
			DrawShape();
		}
		private void DrawShape() {
			if (CurrentOrder != null) {
				if (CurrentOrder.PackageType == "H")
					DrawHexagonalWrap(Convert.ToInt32(CurrentOrder.TubeAm), Convert.ToInt32(CurrentOrder.Diameter));
				else
					DrawSquareWrap(Convert.ToInt32(CurrentOrder.TubeAm), Convert.ToInt32(CurrentOrder.Width),
																		Convert.ToInt32(CurrentOrder.Height));
			}
			else
				//DrawSquareWrap(_defaultSquareTubeNmbr, _defaultWidth, _defaultHeight);
				DrawHexagonalWrap(_defaultRoundTubeNmbr, _defaultDiameter);
		}
		private void btnResetPackages_Click(object sender, RoutedEventArgs e) {
			_currentPackage = 0;
			lblCurrentPackage.Content = _currentPackage.ToString();
		}
		private void tbPackage_TextChanged(object sender, TextChangedEventArgs e) {
			try {
				FillListBoxOfAllowedRopeStraps(int.Parse(tbPackagePerimeter.Text), int.Parse(tbPackageWeight.Text));
			}
			catch (NullReferenceException) { }
		}
		private void tbPackage_LostFocus(object sender, RoutedEventArgs e) {
			TextBox tb = (TextBox)sender;
			if (tb.Text == "") tb.Text = "0";
		}
		private void FillListBoxOfAllowedRopeStraps(int packagePerimeter, int packageWeight) {
			ICollection<string> ropeStraps = new Collection<string>();
			foreach (string rope in General.GetAllRopeStrapsFromFile(General.PathRopeStraps)) {
				string[] values = rope.Split(',');
				bool ropeIsValid = General.CheckIfRopeIsValid(packagePerimeter, packageWeight, values);
				if (ropeIsValid)
					ropeStraps.Add(rope);
			}
			lbAllowedRopeStraps.ItemsSource = ropeStraps;
		}
		private void PLC_UpdateTubesAndPackageData() {
			// checks if tubes per row has changed
			_changeOn = (PLCArrayPackageRows != PLC.ReadArrayInt(Accumulator_1.DBNumber, Accumulator_1.Rows.Item1, Accumulator_1.Rows.Item2));
			PLCArrayPackageRows = PLC.ReadArrayInt(Accumulator_1.DBNumber, Accumulator_1.Rows.Item1, Accumulator_1.Rows.Item2);
			// check if tube data has changed
			_tubeChange = false || (_oldTube.OldHeight != PLC.ReadReal(Accumulator_1.DBNumber, Accumulator_1.Order.Tube.rHeight.Item1).ToString() ||
								   _oldTube.OldWidth != PLC.ReadReal(Accumulator_1.DBNumber, Accumulator_1.Order.Tube.rWidth.Item1).ToString() ||
								   _oldTube.OldThickness != PLC.ReadReal(Accumulator_1.DBNumber, Accumulator_1.Order.Tube.rThickness.Item1).ToString());

			_oldTube.OldHeight = PLC.ReadReal(Accumulator_1.DBNumber, Accumulator_1.Order.Tube.rHeight.Item1).ToString();
			_oldTube.OldWidth = PLC.ReadReal(Accumulator_1.DBNumber, Accumulator_1.Order.Tube.rWidth.Item1).ToString();
			_oldTube.OldThickness = PLC.ReadReal(Accumulator_1.DBNumber, Accumulator_1.Order.Tube.rThickness.Item1).ToString();

			// tube number in package
			lblCurrentTubes.Content = PLC.ReadInt(PackPipe.DBNumber, PackPipe.PC.iTubesOnPackage.Item1);
			lblCurrentPackage.Content = PLC.ReadInt(PackPipe.DBNumber, PackPipe.PC.iPackageNumber.Item1);
			lblTotalTubes.Content = PLC.ReadInt(Accumulator_1.DBNumber, Accumulator_1.Order.Package.bTubeNumber.Item1);
			/*
			da lasciare??
			bundleCounter = tools.OpenTextFile(path_Reports & "BundleNumber.txt", False)
			BundlesTotalDisplay.Caption = bundleCounter
			*/

			if (_tubeNumber != PLC.ReadInt(PackPipe.DBNumber, PackPipe.PC.iTubesOnPackage.Item1) ||
				/*pageActive ||*/ _changeOn || _tubeChange) {
				//RefreshCanvas();
				//pageActive = false;
			}
		}

		#endregion

		#region Draw shapes in canvas
		// Shapes
		private void DrawHexagonalWrap(int tubeAmount, double diameter) {
			CheckIfIsLastTubeAndIncreasesPackage(tubeAmount);
			General.GetValuesFromRoundTubeRecipe(tubeAmount, out int tubeAmountBigLine, out int tubeAmountSmallLine, out int vPosInit, out int hPosInit, out int shapeDiameter);
			if (shapeDiameter == 0) return;
			int columns = 0, rows = 0;
			ICollection<Ellipse> listEllipses = new Collection<Ellipse>();
			General.CreateEllipseShapesToBeDrawn(tubeAmount, tubeAmountBigLine, tubeAmountSmallLine, shapeDiameter, vPosInit, hPosInit, ref columns, ref rows, listEllipses);
			PutShapesInCanvas(listEllipses);
			double packageWidth = diameter * columns;
			double packageHeight = diameter * rows;
			UpdateLabelsValues(tubeAmount, packageWidth, packageHeight);
		}
		private void DrawHexagonalWrap(int tubeAmount, double diameter, int tubeAmountBigLine, int tubeAmountSmallLine, int vPosInit, int hPosInit, int shapeDiameter) {
			if (shapeDiameter == 0) return;
			int columns = 0, rows = 0;
			ICollection<Ellipse> listEllipses = new Collection<Ellipse>();
			General.CreateEllipseShapesToBeDrawn(tubeAmount, tubeAmountBigLine, tubeAmountSmallLine, shapeDiameter, vPosInit, hPosInit, ref columns, ref rows, listEllipses);
			PutShapesInCanvas(listEllipses);

			double packageWidth = diameter * columns;
			double packageHeight = diameter * rows;
			lblPackageWidth.Content = packageWidth.ToString();
			lblPackageHeight.Content = packageHeight.ToString();
		}
		private void DrawSquareWrap(int tubeAmount, int width, int height) {
			CheckIfIsLastTubeAndIncreasesPackage(tubeAmount);
			General.GetValuesFromSquareRectTubeRecipe(tubeAmount, width, height, out int shapeWidth, out int shapeHeight, out int vPosInit, out int hPosInit);
			if (shapeWidth == 0 || shapeHeight == 0) return;
			General.CalculateNumberOfRowsAndColummsFromTubeAmount(tubeAmount, width, height, out double numH, out double numV, out int packageWidth, out int packageHeight);
			ICollection<Rectangle> listRectangles = new Collection<Rectangle>();
			General.CreateRectangleShapesToBeDrawn(tubeAmount, shapeWidth, shapeHeight, hPosInit, vPosInit, numH, numV, listRectangles);
			PutShapesInCanvas(listRectangles);
			UpdateLabelsValues(tubeAmount, packageWidth, packageHeight);
		}
		private void CheckIfIsLastTubeAndIncreasesPackage(int tubeAmount) {
			if (_lastTube == (tubeAmount + 1)) {
				_lastTube = 0;
				++_currentPackage;
			}
		}
		private void PutShapesInCanvas<T>(IEnumerable<T> listOfShapes) where T : Shape {
			cnvAtado.Children.Clear();
			foreach (var shape in listOfShapes)
				cnvAtado.Children.Add(shape);
		}
		private void UpdateLabelsValues(int tubeAmount, double packageWidth, double packageHeight) {
			lblPackageWidth.Content = packageWidth.ToString();
			lblPackageHeight.Content = packageHeight.ToString();

			lblCurrentTubes.Content = _lastTube.ToString();
			lblTotalTubes.Content = tubeAmount.ToString();
			lblCurrentPackage.Content = _currentPackage.ToString();
		}
		#endregion

		#region Order TextBoxe's Values
		// Handle NewOrder textboxe's values
		private void tb_LostFocus(object sender, RoutedEventArgs e) {
			// main input method is the on screen keypad
			// anyway, should be added code to prevent user
			// from typing letters (from full-size physical keyboard, if available)
			TextBox tb = (TextBox)sender;
			CheckIfTextBoxeValueIsValid(sender, tb);
			OrderDetails order = GatherOrderTextBoxesValues();
			double weight = double.Parse(order.Weight);
			int width = int.Parse(order.Width);
			int height = int.Parse(order.Height);
			if (order == null) return;
			if (!int.TryParse(tbTubeNmbr.Text, out int tubes)) return;
			if (tubes > 0)
				weight *= tubes;
			if (tbDiam.IsEnabled == false)
				lblWeight.Content = Math.Round(weight);
			else
				lblWeight.Content = "###";
			if (sender != tbTubeNmbr) return;
			UpdateDrawnShapes(order, width, height, tubes);
		}
		private void UpdateDrawnShapes(OrderDetails order, int width, int height, int tubes) {
			double.TryParse(order.Diameter, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double diameter);
			if (_currentTubeType == General.ActiveTubeType.Round)
				DrawHexagonalWrap(tubes, diameter);
			if (_currentTubeType == General.ActiveTubeType.Square)
				DrawSquareWrap(tubes, width, height);
		}
		private void CheckIfTextBoxeValueIsValid(object sender, TextBox tb) {
			if (sender != tbDiam && sender != tbWidth && sender != tbHeight) return;
			if (double.TryParse(tb.Text, out double value))
				tb.ClearValue(BackgroundProperty);
			else {
				tb.Background = Brushes.NonActiveBack;
				if (tb.Text != "")
					MessageBox.Show("- Apenas são aceites números\n" +
									"- Medida não pode ser igual 0", "Valor inserido inválido");
			}
			if (value != 0.0) return;
			tb.Background = Brushes.NonActiveBack;
			if (tb.Text != "")
				MessageBox.Show("Medida não pode ser igual a 0");
		}
		private void tb_isEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) {
			TextBox tb = (TextBox)sender;
			tb.ClearValue(BackgroundProperty);
			tb.Clear();
		}
		private OrderDetails/*string[]*/ GatherOrderTextBoxesValues() {
			// Gathers value of Order textboxes and concatenates them into a string
			OrderDetails newOrder = new OrderDetails {
				ID = lblID.Content.ToString(),
				Active = "1",
				Name = tbNrOrdem.Text,
				Diameter = tbDiam.Text,
				Width = tbWidth.Text,
				Height = tbHeight.Text,
				Thick = tbThickness.Text,
				Length = tbLength.Text,
				Density = tbDensity.Text,
				TubeAm = tbTubeNmbr.Text,
				TubeType = (_currentTubeType == General.ActiveTubeType.Round ? "R" : "Q"),
				PackageType = (_currentWrapType == General.ActiveWrapType.Hexagonal ? "H" : "Q"),
				Created = DateTime.Now.ToString("dd/MM/yyyy HH\\hmm")
			};
			if (CheckEmptyTextBoxes()) {
				OrderDetails emptyOrder = null;
				return emptyOrder;
			}
			try {
				newOrder.Weight = Math.Round(newOrder.CalculateWeight(newOrder)).ToString();
			}
			catch (Exception exc) {
				MessageBox.Show(exc.Message);
				MessageBox.Show("Cálculo do peso falhou");
			}
			//IEnumerable<string> stringToWrite = new Collection<string> {
			//	newOrder.ID, newOrder.Active, newOrder.Name, newOrder.Diameter, newOrder.Width,
			//	newOrder.Height, newOrder.Thick, newOrder.Length, newOrder.Density, newOrder.TubeAm,
			//	newOrder.TubeType, newOrder.PackageType, newOrder.Weight, newOrder.Created };
			return newOrder/*.ToArray()*/;
		}
		private bool CheckEmptyTextBoxes() {
			bool boxIsEmpty = false;
			ICollection<TextBox> textBoxes = new Collection<TextBox>() {tbNrOrdem, tbDiam, tbWidth, tbHeight,
															tbThickness,tbLength, tbTubeNmbr };
			foreach (TextBox box in textBoxes) {
				if (_currentTubeType == General.ActiveTubeType.Square && box.Name == tbDiam.Name) continue;
				if (_currentTubeType == General.ActiveTubeType.Round &&
										(box.Name == tbHeight.Name || box.Name == tbWidth.Name)) continue;
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
			ICollection<Grid> boxesGrids = new Collection<Grid>() { grid2Straps, grid3Straps, grid4Straps, grid5Straps, grid6Straps };
			Grid currentGrid = null;
			// Show/Hide grid according to number of straps on the text box
			switch (straps) {
				case 2:
					imgStrap.Source = new BitmapImage(new Uri(@"/Resources/atado2.png", UriKind.Relative));
					currentGrid = grid2Straps;
					break;
				case 3:
					imgStrap.Source = new BitmapImage(new Uri(@"/Resources/atado3.png", UriKind.Relative));
					currentGrid = grid3Straps;
					break;
				case 4:
					imgStrap.Source = new BitmapImage(new Uri(@"/Resources/atado4.png", UriKind.Relative));
					currentGrid = grid4Straps;
					break;
				case 5:
					imgStrap.Source = new BitmapImage(new Uri(@"/Resources/atado5.png", UriKind.Relative));
					currentGrid = grid5Straps;
					break;
				case 6:
					imgStrap.Source = new BitmapImage(new Uri(@"/Resources/atado6.png", UriKind.Relative));
					currentGrid = grid6Straps;
					break;
				default:
					break;
			}
			foreach (Grid item in boxesGrids)
				item.Visibility = (item == currentGrid ? _visible : _collapsed);
		}
		private void tbStrapPosition_LostFocus(object sender, RoutedEventArgs e) {
			// Finds number of active textboxes (nmbr of straps)
			// and calls method passing the nmbr of straps
			byte.TryParse(numKeypadUpDown.Value.ToString(), out byte value);
			switch (value) {
				case 2:
					if (!(tbstrap2_1.Text != "" && tbstrap2_2.Text != "")) return;
					break;
				case 3:
					if (!(tbstrap3_1.Text != "" && tbstrap3_2.Text != "" && tbstrap3_3.Text != "")) return;
					break;
				case 4:
					if (!(tbstrap4_1.Text != "" && tbstrap4_2.Text != "" && tbstrap4_3.Text != "" && tbstrap4_4.Text != "")) return;
					break;
				case 5:
					if (!(tbstrap5_1.Text != "" && tbstrap5_2.Text != "" && tbstrap5_3.Text != "" && tbstrap5_4.Text != "" && tbstrap5_5.Text != "")) return;
					break;
				case 6:
					if (!(tbstrap6_1.Text != "" && tbstrap6_2.Text != "" && tbstrap6_3.Text != "" && tbstrap6_4.Text != "" && tbstrap6_5.Text != "" && tbstrap6_6.Text != "")) return;
					break;
				default:
					break;
			}
			GetStrapsPositionFromTextboxes(value);
		}
		private void GetStrapsPositionFromTextboxes(byte straps) {
			// Gets straps position from active grid
			string values = "";
			IEnumerable<TextBox> textBoxes = Enumerable.Empty<TextBox>();
			Grid currentGrid = null;
			switch (straps) {
				case 2:
					currentGrid = grid2Straps;
					break;
				case 3:
					currentGrid = grid3Straps;
					break;
				case 4:
					currentGrid = grid4Straps;
					break;
				case 5:
					currentGrid = grid5Straps;
					break;
				case 6:
					currentGrid = grid6Straps;
					break;
				default:
					break;
			}
			if (currentGrid == null) return;
			textBoxes = General.GetTextBoxesFromGrid(currentGrid);
			if (textBoxes == Enumerable.Empty<TextBox>()) return;
			values = GetStrapsValuesFromTextBoxes(textBoxes);
			UpdateStatusBar(values);
		}
		private string GetStrapsValuesFromTextBoxes(IEnumerable<TextBox> TextBoxes) {
			string values = "";
			foreach (var textbBox in TextBoxes)
				values += textbBox.Text + ",";
			return values;
		}
		private void numKeypadUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e) {
			_textChanged = true;
			if (!numKeypadUpDown.IsInitialized) return;
			UpdateImageAndNumberOfTextBoxes();
			ToogleModifyStrapsTextBoxes();
		}
		private void tb_PreviewMouseDoubleClickStrapper_Keypad(object sender, MouseButtonEventArgs e) {
			if (!_isStrapsModifyActive) return;
			TextBox tb = (TextBox)sender;
			Win_Keypad wnKeypad = new Win_Keypad();
			wnKeypad.ShowDialog();
			if (Win_Keypad.Enter == true)
				tb.Text = wnKeypad.tbResult.Text;
		}
		private void btnModifyStraps_Click(object sender, RoutedEventArgs e) {
			_isStrapsModifyActive ^= true;
			btnModifyStraps.Content = (_isStrapsModifyActive ? "Guardar" : "Modificar");
			ToogleModifyStrapsTextBoxes();
		}
		private void ToogleModifyStrapsTextBoxes() {
			// Changes "strap position" textboxes (activate or deactivate modification)
			// according to current state of program
			if (_currentLayout == General.ActiveLayout.Strapper) {
				foreach (TextBox item in GetCurrentActiveStrapsTextBoxes()) {
					if (_isStrapsModifyActive)
						General.SetTextBoxForEdit(item);
					else {
						ResetTextBox(item);
						if (_textChanged)
							item.Text = "";
					}
				}
			}
			_textChanged = false;
		}
		private void DisableModifyStrapsTextBoxes() {
			if (_currentLayout != General.ActiveLayout.Strapper) return;
			foreach (TextBox item in GetCurrentActiveStrapsTextBoxes())
				ResetTextBox(item);
		}
		private IEnumerable<TextBox> GetCurrentActiveStrapsTextBoxes() {
			byte.TryParse(numKeypadUpDown.Value.ToString(), out byte value);
			IEnumerable<TextBox> controlsCollection = null;
			Grid currentGrid = null;
			switch (value) {
				case 2:
					currentGrid = grid2Straps;
					break;
				case 3:
					currentGrid = grid3Straps;
					break;
				case 4:
					currentGrid = grid4Straps;
					break;
				case 5:
					currentGrid = grid5Straps;
					break;
				case 6:
					currentGrid = grid6Straps;
					break;
				default:
					break;
			}
			return controlsCollection = General.GetTextBoxesFromGrid(currentGrid);
		}
		private void UpdateStrapsValues(int length) {
			if (_currentLayout != General.ActiveLayout.Strapper) return;
			byte.TryParse(numKeypadUpDown.Value.ToString(), out byte nmbr);
			IEnumerable<TextBox> controlsCollection = Enumerable.Empty<TextBox>();
			Grid currentGrid = null;
			switch (nmbr) {
				case 2:
					if (grid2Straps.Visibility == _visible) currentGrid = grid2Straps;
					break;
				case 3:
					if (grid3Straps.Visibility == _visible) currentGrid = grid3Straps;
					break;
				case 4:
					if (grid4Straps.Visibility == _visible) currentGrid = grid4Straps;
					break;
				case 5:
					if (grid5Straps.Visibility == _visible) currentGrid = grid5Straps;
					break;
				case 6:
					if (grid6Straps.Visibility == _visible) currentGrid = grid6Straps;
					break;
				default:
					break;
			}
			if (currentGrid == null) return;
			controlsCollection = General.GetTextBoxesFromGrid(currentGrid);
			if (controlsCollection == Enumerable.Empty<TextBox>()) return;
			int[] values = Recipes.GetStrapsPositionFromRecipe(length, nmbr);
			byte i = 0;
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

			if (oldWidth != PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeWidth.Item1) ||
				oldHeight != PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeHeight.Item1) ||
				oldThickness != PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeThickness.Item1) ||
				oldTubeType != Convert.ToBoolean(PLC.ReadBool(Strapper.DBNumber, Strapper.Order.Tube.bRoundTube))) {
				oldWidth = PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeWidth.Item1);
				oldHeight = PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeHeight.Item1);
				oldThickness = PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeThickness.Item1);
				oldTubeType = Convert.ToBoolean(PLC.ReadBool(Strapper.DBNumber, Strapper.Order.Tube.bRoundTube));
			}
			lblPackPosition.Content = PLC.ReadReal(LateralConveyor.DBNumber, LateralConveyor.PCData.rPackagePositionInStrapper.Item1);
			numKeypadUpDown.Value = Convert.ToDouble(PLC.ReadInt(Strapper.DBNumber, Strapper.Strap.iNumberOfStraps.Item1));

			toChange = ((_ecoStraps != PLC.ReadArrayReal(Strapper.DBNumber, Strapper.Strap.aStrapsPosition.Item1, Strapper.Strap.aStrapsPosition.Item2)) ||
						(_ecoLength != PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeLength.Item1)) ||
						(_ecoStrapsNumber != PLC.ReadInt(LateralConveyor.DBNumber, LateralConveyor.PCData.iNumberOfRegimentsExecuted.Item1)));

			_ecoStraps = PLC.ReadArrayReal(Strapper.DBNumber, Strapper.Strap.aStrapsPosition.Item1, Strapper.Strap.aStrapsPosition.Item2);
			_ecoLength = PLC.ReadReal(Strapper.DBNumber, Strapper.Order.Tube.rTubeLength.Item1);
			_ecoStrapsNumber = PLC.ReadInt(LateralConveyor.DBNumber, LateralConveyor.PCData.iNumberOfRegimentsExecuted.Item1);

			lblCurrentStrap.Content = _ecoStrapsNumber.ToString();
			if (toChange && _ecoStraps.Length > 0) {
				// update package state in Strapping -> "aggiorna stato pacco in reggiatura"
			}
		}
		#endregion

		#region Storage
		// Storage
		private void btnEvacuatePackage_Click(object sender, RoutedEventArgs e) {
			PLC.WriteBool(Storage.DBNumber, Storage.Setup.bEvacuateLastPackage, true);
		}
		private void FillLastHistory() {
			IList<History> history = History.ReadHistoryFromFile(General.HistoryPath);
			if (history == null) return;
			IList<Label> weightLabels = new List<Label>() { lblWeight1, lblWeight2, lblWeight3 };
			try {
				lblTubesHistory.Content = history[(history.Count) - 1].TubeAm;
				for (byte i = 1; i <= 3; i++)
					weightLabels[i - 1].Content = history[(history.Count) - i].Weight;
				IList<Label> dateLabels = new List<Label>() { lblDate1, lblDate2, lblDate3 };
				for (byte i = 1; i <= 3; i++)
					dateLabels[i - 1].Content = history[(history.Count) - i].Created;
			}
			catch (ArgumentOutOfRangeException) { }
		}
		private void UpdateStoragePage() {
			FillLastHistory();
			lblPackageHistory.Content = PLC.ReadInt(PCPLC.DBNumber, PCPLC.Archive.Package.iTubesPresent.Item1);
			lblTubesHistory.Content = PLC.ReadInt(PCPLC.DBNumber, PCPLC.Archive.Package.iProgressiveNumber.Item1);
			UpdateWeightLabel();
			UpdateDrainLabel();
		}
		private void UpdateWeightLabel() {
			if (/*PAR.ReadBool(setup) &&*/ PLC.ReadBool(PCPLC.DBNumber, PCPLC.Weight.bInsertedWeight)) {
				lblWeightHistory.Content = Convert.ToDouble(PLC.ReadReal(PCPLC.DBNumber, PCPLC.Weight.rPackageWeight.Item1)) == -9999 ? "BAD" :
											PLC.ReadReal(PCPLC.DBNumber, PCPLC.Weight.rPackageWeight.Item1).ToString();
			}
			else
				lblWeightHistory.Content = "OFF";
		}
		private void UpdateDrainLabel() {
			if (Convert.ToBoolean(PLC.ReadBool(Storage.DBNumber, Storage.Setup.bEnableDrain))) {
				lblDrain.Content = "ON";
				lblDrain.Foreground = Brushes.Green;
			}
			else {
				lblDrain.Content = "OFF";
				lblDrain.Foreground = Brushes.LightRed;
			}
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
				if (_currentDate == General.ActiveDate.Initial) {
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
			gridFinalDate.Visibility = _collapsed;
			comboboxShift.SelectedIndex = 0;
			comboboxShift.IsEnabled = false;
			FillHistoryDataGrid();
		}
		private void rbSelectedDate_Checked(object sender, RoutedEventArgs e) {
			lblInitialDate.Content = "Data";
			tbHistoryDayInit.IsEnabled = true;
			tbHistoryMonthInit.IsEnabled = true;
			tbHistoryYearInit.IsEnabled = true;
			gridFinalDate.Visibility = _collapsed;
			comboboxShift.IsEnabled = true;
			FillHistoryDataGrid();
		}
		private void rbInitialFinal_Checked(object sender, RoutedEventArgs e) {
			lblInitialDate.Content = "Data inicial";
			tbHistoryDayInit.IsEnabled = true;
			tbHistoryMonthInit.IsEnabled = true;
			tbHistoryYearInit.IsEnabled = true;
			comboboxShift.IsEnabled = true;
			gridFinalDate.Visibility = _visible;
		}
		private void InitialDate_GotFocus(object sender, RoutedEventArgs e) {
			_currentDate = General.ActiveDate.Initial;
		}
		private void FinalDate_GotFocus(object sender, RoutedEventArgs e) {
			_currentDate = General.ActiveDate.End;
		}
		private void FillHistoryDataGrid() {
			if ((bool)rbNoFilter.IsChecked)
				datagridHistory.ItemsSource = History.ReadHistoryFromFile(General.HistoryPath);
			else if ((bool)rbSelectedDate.IsChecked) {
				try {
					if (comboboxShift.SelectedIndex == 0)
						datagridHistory.ItemsSource = History.ReadHistoryFromFile(General.HistoryPath, calHistory.SelectedDate.Value.Date);
					else
						datagridHistory.ItemsSource = History.ReadHistoryFromFile(General.HistoryPath, calHistory.SelectedDate.Value.Date, calHistory.SelectedDate.Value.Date, (byte)comboboxShift.SelectedIndex);
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
						datagridHistory.ItemsSource = History.ReadHistoryFromFile(General.HistoryPath, initialDate, endDate);
					else
						datagridHistory.ItemsSource = History.ReadHistoryFromFile(General.HistoryPath, initialDate, endDate, (byte)comboboxShift.SelectedIndex);
				}
				catch (FormatException) {
					UpdateStatusBar("Escolha um intervalo de datas");
				}
			}
		}
		#endregion

		#region Recipes
		// Recipes
		private void btnRecipeRoundTube_Click(object sender, RoutedEventArgs e) {
			ShowTubeRecipesOnDataGrid(General.PathRoundTubes);
			List<TextBox> textBoxes = CollectRecipeTextBoxes();
			foreach (TextBox item in textBoxes)
				item.Text = "";
			_currentRecipe = General.ActiveRecipe.RoundTube;
		}
		private void btnRecipeSquareTube_Click(object sender, RoutedEventArgs e) {
			ShowTubeRecipesOnDataGrid(General.PathSquareTubes, General.PathRectTubes);
			List<TextBox> textBoxes = CollectRecipeTextBoxes();
			foreach (TextBox item in textBoxes)
				item.Text = "";
			_currentRecipe = General.ActiveRecipe.SquareTube;
		}
		private List<TextBox> CollectRecipeTextBoxes() {
			List<TextBox> textBoxes = new List<TextBox>();
			textBoxes.AddRange(General.GetTextBoxesFromGrid(gridRecipes));
			textBoxes.AddRange(General.GetTextBoxesFromGrid(gridRecipesRoundTube));
			textBoxes.AddRange(General.GetTextBoxesFromGrid(gridRecipesSquareTube));
			return textBoxes;
		}
		private void ShowTubeRecipesOnDataGrid(string pathRoundTube) {
			datagridRecipes.ItemsSource = Recipes.ReadTubeRecipesFromFile(pathRoundTube);
			btnRecipeRoundTube.Background = Brushes.LightRed;
			btnRecipeSquareTube.ClearValue(BackgroundProperty);
			gridRecipesSquareTube.Visibility = _collapsed;
			gridRecipesRoundTube.Visibility = _visible;
		}
		private void ShowTubeRecipesOnDataGrid(string pathSquareTube, string pathRectTube) {
			datagridRecipes.ItemsSource = Recipes.ReadTubeRecipesFromFile(pathSquareTube, pathRectTube);
			btnRecipeSquareTube.Background = Brushes.LightRed;
			btnRecipeRoundTube.ClearValue(BackgroundProperty);
			gridRecipesRoundTube.Visibility = _collapsed;
			gridRecipesSquareTube.Visibility = _visible;
		}
		private void datagridRecipes_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e) {
			if (!_editingRecipe)
				_cellsArePopulated = GetDataFromSelectedCells();
		}
		private void btnRecipeEdit_Click(object sender, RoutedEventArgs e) {
			if (_cellsArePopulated) {
				_editingRecipe = true;
				List<TextBox> textBoxes = CollectRecipeTextBoxes();
				foreach (TextBox item in textBoxes)
					General.SetTextBoxForEdit(item);
				DisableRecipeUIButtons();
			}
			else
				UpdateStatusBar("Para editar selecione uma ordem", 1);
		}
		private void DisableRecipeUIButtons() {
			btnRecipeEdit.IsEnabled = false;
			btnRecipeRoundTube.IsEnabled = false;
			btnRecipeSquareTube.IsEnabled = false;
			btnReturn.IsEnabled = false;
			btnRecipeSave.Visibility = _visible;
			btnRecipeCancel.Visibility = _visible;
		}
		private void datagridRecipes_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
			if (!_editingRecipe) return;
			e.Handled = true;
			UpdateStatusBar("Para mudar de receita termine de editar a atual", 1);
		}
		private bool GetDataFromSelectedCells() {
			if (_currentRecipe == General.ActiveRecipe.RoundTube) {
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
			if (datagridRecipes.SelectedIndex < 0) return datagridRow;
			datagridRow = (RoundTubeRecipe)datagridRecipes.Items[datagridRecipes.SelectedIndex];
			return datagridRow;
		}
		private SquareTubeRecipe GetSquareTubeRecipeFromGrid() {
			SquareTubeRecipe datagridRow = null;
			if (datagridRecipes.SelectedIndex < 0) return datagridRow;
			datagridRow = (SquareTubeRecipe)datagridRecipes.Items[datagridRecipes.SelectedIndex];
			return datagridRow;
		}
		private void btnRecipeSave_Click(object sender, RoutedEventArgs e) {
			bool found = false;
			string msg = "";
			DisableTextBoxesModification();
			ICollection<string> newFileContent = new Collection<string>();
			if (_currentRecipe == General.ActiveRecipe.RoundTube) {
				EditRoundTubeRecipesTextFile(newFileContent);
				msg = Document.WriteToFile(General.PathRoundTubes, newFileContent.ToArray()) ? SaveSuccessful : SaveError;
				UpdateStatusBar(msg);
				datagridRecipes.ItemsSource = null;
				datagridRecipes.ItemsSource = Recipes.ReadTubeRecipesFromFile(General.PathRoundTubes);
			}
			else {
				found = EditSquareTubeRecipesTextFile(newFileContent, General.PathSquareTubes);
				if (found) {
					msg = Document.WriteToFile(General.PathSquareTubes, newFileContent.ToArray()) ? SaveSuccessful : SaveError;
					UpdateStatusBar(msg);
				}
				else {
					EditSquareTubeRecipesTextFile(newFileContent, General.PathRectTubes);
					msg = Document.WriteToFile(General.PathRectTubes, newFileContent.ToArray()) ? SaveSuccessful : SaveError;
					UpdateStatusBar(msg);
				}
				datagridRecipes.ItemsSource = null;
				datagridRecipes.ItemsSource = Recipes.ReadTubeRecipesFromFile(General.PathSquareTubes, General.PathRectTubes);
			}
			ResetRecipeUIButtons();
			_editingRecipe = false;
		}
		private void DisableTextBoxesModification() {
			List<TextBox> textBoxes = CollectRecipeTextBoxes();
			foreach (TextBox item in textBoxes)
				ResetTextBox(item);
		}
		private bool EditSquareTubeRecipesTextFile(ICollection<string> newFileContent, string path) {
			bool found = false;
			if (!Document.ReadFromFile(path, out IEnumerable<string> linesFromFile)) return false;
			foreach (string item in linesFromFile) {
				string newline = "";
				string[] array = item.Split(',');
				if (array[0] == tbRecipeTubes.Text) {
					array[array.Length - 2] = tbRecipecolumns.Text;
					array[array.Length - 1] = tbRecipeRows.Text;
					found = true;
					//foreach (string value in array)
					//	newline += value + ",";
					newline = array.Aggregate(newline, (current, value) => current + (value + ","));
					newline = newline.Remove(newline.Length - 1);
					newFileContent.Add(newline == "" ? item : newline);
				}
			}
			return found;
		}
		private void EditRoundTubeRecipesTextFile(ICollection<string> newFileContent) {
			if (!Document.ReadFromFile(General.PathRoundTubes, out IEnumerable<string> linesFromFile)) return;
			foreach (string item in linesFromFile) {
				string newline = "";
				string[] array = item.Split(',');
				if (array[0] == tbRecipeTubes.Text) {
					array[1] = tbRecipeBigRow.Text;
					array[2] = tbRecipeSmallRow.Text;
					//foreach (string value in array)
					//	newline += value + ",";
					newline = array.Aggregate(newline, (current, value) => current + (value + ","));
					newline = newline.Remove(newline.Length - 1);
				}
				newFileContent.Add(newline == "" ? item : newline);
			}
		}
		private void btnAddRecipe_Click(object sender, RoutedEventArgs e) {
			_currentLayout = General.ActiveLayout.NewRecipe;
			borderWrapTubeType.Visibility = _visible;
			borderCanvas.Visibility = _visible;
			borderCanvas.Margin = new Thickness(805, 0, 79, 4);
			SetSqrTube();
			SetSqrWrap();
			gridNewRecipeSquare.Visibility = _visible;
			gridNewRecipeHexa.Visibility = _collapsed;
			btnExit.Visibility = _collapsed;
			btnReturn.Visibility = _visible;
			tabLayout.SelectedItem = tabItemWrapper;
			tabWrapper.SelectedItem = tabItemWrapperNewRecipe;
			gridNewRecipeDrawnShapes.Visibility = _visible;
			lblNewRecipeDrawnShapes.Content = "";
			borderManualWrap.Visibility = _collapsed;
		}
		private void tbNewRecipe_TextChanged(object sender, TextChangedEventArgs e) {
			int.TryParse(tbNewRecipeTubeNmbr.Text, out int tubeNmbr);
			int.TryParse(tbNewRecipeX.Text, out int xValue);
			int.TryParse(tbNewRecipeY.Text, out int yValue);
			byte.TryParse(tbNewRecipeScale.Text, out byte scale);
			if (_currentWrapType == General.ActiveWrapType.Square) {
				int.TryParse(tbNewRecipeColumns.Text, out int columns);
				int.TryParse(tbNewRecipeRows.Text, out int rows);
			}
			else {
				int.TryParse(tbNewRecipeBigRow.Text, out int bigRow);
				int.TryParse(tbNewRecipeSmallRow.Text, out int smallRow);
				DrawHexagonalWrap(tubeNmbr, 65, bigRow, smallRow, yValue, xValue, scale);
				lblNewRecipeDrawnShapes.Content = cnvAtado.Children.OfType<Ellipse>().Count().ToString();
			}

		}
		private void btnRecipeCancel_Click(object sender, RoutedEventArgs e) {
			List<TextBox> textBoxes = CollectRecipeTextBoxes();
			foreach (TextBox item in textBoxes)
				ResetTextBox(item);
			ResetRecipeUIButtons();
			_editingRecipe = false;
		}
		private void ResetRecipeUIButtons() {
			btnRecipeEdit.IsEnabled = true;
			btnRecipeRoundTube.IsEnabled = true;
			btnRecipeSquareTube.IsEnabled = true;
			btnReturn.IsEnabled = true;
			btnRecipeSave.Visibility = _collapsed;
			btnRecipeCancel.Visibility = _collapsed;
		}
		#endregion

		#region Manual
		//Manual
		private void btnInsideManualBorder_Click(object sender, RoutedEventArgs e) {
			Button origin = (Button)sender;
			Image image = new Image();
			string button = origin.Name;
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
					gridLowerActive.Visibility = gridLowerActive.Visibility == _visible ? _collapsed : _visible;
					PLC.ToogleBool(Accumulator_1.DBNumber, Accumulator_1.ManualMovement.bLowerPneumaticCounterblocks);
					break;
				case "btnSuperiorPneumaticCounterblocks":
					noImageUpdate = true;
					gridUpperActive.Visibility = gridUpperActive.Visibility == _visible ? _collapsed : _visible;
					PLC.ToogleBool(Accumulator_1.DBNumber, Accumulator_1.ManualMovement.bSuperiorPneumaticCounterblocks);
					break;
				case "btnLateralPneumaticCounterblocks":
					noImageUpdate = true;
					gridLateralActive.Visibility = gridLateralActive.Visibility == _visible ? _collapsed : _visible;
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
					if (tbUpperRolls.Background == Brushes.YellowBrush)
						tbUpperRolls.ClearValue(BackgroundProperty);
					else
						tbUpperRolls.Background = Brushes.YellowBrush;
					PLC.ToogleBool(Strapper.DBNumber, Strapper.ManualMovement.bUpperRolls);
					break;
				case "btnCarRolls":
					image = (origin.Content == (Image)FindResource("CarRolls") ? (Image)FindResource("A_CarRolls") : (Image)FindResource("CarRolls"));
					PLC.ToogleBool(Strapper.DBNumber, Strapper.ManualMovement.bCarRolls);
					break;
				case "btnCarRolls1_2":
					noImageUpdate = true;
					gridCarRolls1_2_Active.Visibility = gridCarRolls1_2_Active.Visibility == _visible ? _collapsed : _visible;
					PLC.ToogleBool(Strapper.DBNumber, Strapper.ManualMovement.bCarRolls12);
					break;
				case "btnCarRolls2":
					noImageUpdate = true;
					gridCarRolls2_Active.Visibility = gridCarRolls2_Active.Visibility == _visible ? _collapsed : _visible;
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
					gridDrain12Active.Visibility = gridDrain12Active.Visibility == _visible ? _collapsed : _visible;
					PLC.ToogleBool(Storage.DBNumber, Storage.ManualMovement.bDrains1_2);
					break;
				case "btnDrain123":
					noImageUpdate = true;
					gridDrain123Active.Visibility = gridDrain123Active.Visibility == _visible ? _collapsed : _visible;
					PLC.ToogleBool(Storage.DBNumber, Storage.ManualMovement.bDrains1_2_3);
					break;
				case "btnDrain1234":
					noImageUpdate = true;
					gridDrain1234Active.Visibility = gridDrain1234Active.Visibility == _visible ? _collapsed : _visible;
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
			lblConnectionStatus.Background = Brushes.Green;
			lblConnectionStatus.Content = "Ligado";
		}
		private void btnDisconnect_Click(object sender, RoutedEventArgs e) {
			string status = PLC.Disconnect();
			UpdateStatusBar(status);
			if (!status.Contains("Disconnected") && !status.Contains("Not connected")) return;
			lblConnectionStatus.Background = Brushes.LightRed;
			lblConnectionStatus.Content = "Desligado";
		}
		private void btnWriteData_Click(object sender, RoutedEventArgs e) {
			double.TryParse("220.34", NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double test);
			PLC.WriteInt(400, 52, test);
		}
		#endregion

	}
}