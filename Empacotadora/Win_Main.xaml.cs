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
using System.Collections;

namespace Empacotadora {
	/// <summary>
	/// Lógica interna para Win_Main.xaml
	/// </summary>
	public partial class Win_Main : Window {
		// General
		const string SaveSuccessful = "Sucesso ao gravar";
		const string SaveError = "Erro ao tentar gravar";
		FERP_MairCOMS7 PLC = new FERP_MairCOMS7();
		public static OrderDetails CurrentOrder = new OrderDetails();
		// Wrapper
		int _lastTube = 0, _currentPackage = 0, _id;
		const byte Margin = 2;
		// New Order
		enum ActiveTubeType { Round, Square }
		enum ActiveWrapType { Hexagonal, Square }
		ActiveTubeType _currentTubeType;
		ActiveWrapType _currentWrapType;
		// UI control
		private enum ActiveLayout { Wrapper, EditOrder, Strapper, Storage, Recipes, History }
		ActiveLayout _currentLayout;
		// Strapper
		bool _textChanged = false, _cellsArePopulated = false, _editingRecipe = false, _isStrapsModifyActive = false;
		double[] _ecoStraps;
		double _ecoLength, _ecoStrapsNumber;
		// Storage
		DispatcherTimer _storageTimer;
		// History
		enum ActiveDate { Initial, End }
		ActiveDate _currentDate;
		// Recipe
		enum ActiveRecipe { RoundTube, SquareTube }
		ActiveRecipe _currentRecipe;
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
		// Diretories
		public static readonly string SystemPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Empacotadora";
		public static readonly string Path = SystemPath + @"\Orders.txt";
		readonly string _historyPath = SystemPath + @"\PackageHistory.txt";
		readonly string _pathSquareTubes = SystemPath + @"\SquareTubeRecipes.txt";
		readonly string _pathRectTubes = SystemPath + @"\RectTubeRecipes.txt";
		readonly string _pathRoundTubes = SystemPath + @"\RoundTubeRecipes.txt";
		readonly string _pathRopeStraps = SystemPath + @"\RopeStraps.txt";
		readonly int _defaultRoundTubeNmbr = 37, _defaultDiameter = 65;
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
			vbCalendar.Visibility = vbCalendar.IsVisible ? Visibility.Collapsed : Visibility.Visible;
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
			switch (Win_Orders.NextLayout) {
				case Win_Orders.Layout.Default:
					SetWrapperLayout();
					ShowCurrentOrderOnWrapperLayout();
					break;
				case Win_Orders.Layout.NewOrder:
					SetEditOrderEnvironment();
					break;
				case Win_Orders.Layout.EditOrder:
					SetEditOrderLayout();
					FillEditOrderLayoutWithCurrentOrder();
					break;
				case Win_Orders.Layout.Recipes:
					HideGeneralLayout();
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
		}
		private void SetEditOrderEnvironment() {
			SetEditOrderLayout();
			SetSqrWrap();
			SetSqrTube();
			_currentTubeType = ActiveTubeType.Square;
			tbDensity.Text = "7.65";
			try {
				string lineContent = File.ReadLines(Path).Last();
				string[] array = lineContent.Split(',');
				int.TryParse(array[0], out _id);
			}
			catch (Exception exc) when (exc is IOException || exc is FileNotFoundException || exc is DirectoryNotFoundException || exc is UnauthorizedAccessException) {
				UpdateStatusBar("Ficheiro que contém as ordens não foi encontrado.", 1);
				return;
			}
			lblID.Content = (++_id).ToString();
		}
		private void ShowCurrentOrderOnWrapperLayout() {
			try {
				int.TryParse(CurrentOrder.TubeAm, out int amount);
				if (CurrentOrder.Diameter == "") {
					gridRound.Visibility = Visibility.Collapsed;
					gridSquare.Visibility = Visibility.Visible;
					lblOrderWidth.Content = CurrentOrder.Width;
					lblOrderHeight.Content = CurrentOrder.Height;
					int.TryParse(CurrentOrder.Width, out int width);
					int.TryParse(CurrentOrder.Height, out int height);
					DrawSquareWrap(amount, width, height);
				}
				else {
					gridRound.Visibility = Visibility.Visible;
					gridSquare.Visibility = Visibility.Collapsed;
					double.TryParse(CurrentOrder.Diameter, out double diam);
					lblOrderDiam.Content = CurrentOrder.Diameter;
					DrawHexagonalWrap(amount, diam);
				}
				lblOrderName.Content = CurrentOrder.Name;
				lblOrderThick.Content = CurrentOrder.Thick;
				lblOrderLength.Content = CurrentOrder.Length;
				lblPackageLength.Content = CurrentOrder.Length;
			}
			catch (NullReferenceException) { /* currentOrder is empty */ }
		}
		private void btnSaveOrder_Click(object sender, RoutedEventArgs e) {
			string valuesToWrite = "";
			if (Win_Orders.NextLayout == Win_Orders.Layout.NewOrder)
			{
				if (GatherNewOrderTextBoxesValues() == null) {
					UpdateStatusBar("Para gravar tem que preencher todos os campos");
					return;
				}
				//foreach (string item in GatherNewOrderTextBoxesValues())
				//	valuesToWrite += item;
				valuesToWrite = GatherNewOrderTextBoxesValues().Aggregate(valuesToWrite, (current, item) => current + item);
			}
			else if (Win_Orders.NextLayout == Win_Orders.Layout.EditOrder)
				SavedEditedOrder();
			string msg = Document.AppendToFile(Path, valuesToWrite) ? "Sucesso ao gravar" : "Erro ao tentar gravar";
			UpdateStatusBar(msg);
			SetWrapperLayout();
		}
		private void btnWrapper_Click(object sender, RoutedEventArgs e) {
			SetWrapperLayout();
		}
		private void btnStrapper_Click(object sender, RoutedEventArgs e) {
			SetStrapperLayout();
		}
		private void btnStorage_Click(object sender, RoutedEventArgs e) {
			SetStorageLayout();
			_storageTimer.Start();
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
			switch (_currentLayout) {
				case ActiveLayout.EditOrder:
					MessageBoxResult answer = MessageBox.Show("Sair sem guardar?", "Confirmar", MessageBoxButton.YesNo);
					if (answer == MessageBoxResult.Yes)
						SetWrapperLayout();
					break;
				case ActiveLayout.History:
					SetStorageLayout();
					break;
				default:
					SetWrapperLayout();
					break;
			}
		}
		private void btnManual_Click(object sender, RoutedEventArgs e) {
			Visibility value = borderManualWrap.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
			if (value == Visibility.Visible) {
				if (_currentLayout == ActiveLayout.Wrapper) {
					tabManual.SelectedItem = tabManualWrapper;
				}
				if (_currentLayout == ActiveLayout.Strapper) {
					tabManual.SelectedItem = tabManualStrapper;
				}
				if (_currentLayout == ActiveLayout.Storage) {
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
			lblDateTime.Text = DateTime.Now.ToString("HH\\hmm:ss \n ddd dd/MM/yyyy"); ;
			// Controlli_di_pagina F900000_Kernel, "POLMONE_1"
			switch (_currentLayout) {
				case ActiveLayout.Wrapper:

					break;
				case ActiveLayout.Strapper:
					UpdateStrapperPage();
					break;
				case ActiveLayout.Storage:
					UpdateStoragePage();
					break;
				case ActiveLayout.History:

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
			btnHexaWrap.Background = Brushes.LightRed;
			btnHexaWrap.BorderBrush = Brushes.ActiveBorder;
			btnSqrWrap.ClearValue(BackgroundProperty);
			btnSqrWrap.BorderBrush = Brushes.NonActiveBorder;
			_currentWrapType = ActiveWrapType.Hexagonal;
			DrawHexagonalWrap(_defaultRoundTubeNmbr, _defaultDiameter);
		}
		private void SetSqrWrap() {
			btnSqrWrap.Background = Brushes.LightRed;
			btnSqrWrap.BorderBrush = Brushes.ActiveBorder;
			btnHexaWrap.ClearValue(BackgroundProperty);
			btnHexaWrap.BorderBrush = Brushes.NonActiveBorder;
			_currentWrapType = ActiveWrapType.Square;
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
			_currentTubeType = ActiveTubeType.Round;
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
			_currentTubeType = ActiveTubeType.Square;
		}
		// "Set" methods call "Show"/"Hide" methods to combine the desired controls on the window
		private void SetWrapperLayout() {
			ShowGeneralLayout();
			ShowWrapperLayout();
			HideEditOrderLayout();
			//DrawSquareWrap(defaultSquareTubeNmbr, defaultWidth, defaultHeight);
			DrawHexagonalWrap(_defaultRoundTubeNmbr, _defaultDiameter);
		}
		private void SetEditOrderLayout() {
			HideGeneralLayout();
			HideWrapperLayout();
			ShowEditOrderLayout();
		}
		private void SetStrapperLayout() {
			ShowGeneralLayout();
			HideWrapperLayout();
			ShowStrapperLayout();
		}
		private void SetStorageLayout() {
			ShowGeneralLayout();
			HideWrapperLayout();
			ShowStorageLayout();
		}
		private void SetHistoryLayout() {
			tabLayout.SelectedItem = tabItemHistory;
			FillHistoryDataGrid();
			HideGeneralLayout();
			btnReturn.Visibility = Visibility.Visible;
			_currentLayout = ActiveLayout.History;
		}
		private void SetRecipesLayout() {
			lblTitle.Content = "Receitas";
			tabLayout.SelectedItem = tabItemRecipes;
			btnRecipeRoundTube.Background = Brushes.LightRed;
			btnRecipeSquareTube.ClearValue(BackgroundProperty);
			btnReturn.Visibility = Visibility.Visible;
			btnManual.Visibility = Visibility.Collapsed;
			btnOrders.Visibility = Visibility.Collapsed;
			gridRecipesSquareTube.Visibility = Visibility.Collapsed;
			gridRecipesRoundTube.Visibility = Visibility.Visible;
			datagridRecipes.ItemsSource = Recipes.ReadTubeRecipesFromFile(_pathRoundTubes);
			_currentRecipe = ActiveRecipe.RoundTube;
			_currentLayout = ActiveLayout.Recipes;
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
		private void ShowWrapperLayout() {
			lblTitle.Content = "Empacotadora";
			btnOrders.Visibility = Visibility.Visible;
			borderCanvas.Visibility = Visibility.Visible;
			borderCanvas.Margin = new Thickness(805, 0, 79, 78);
			tabLayout.SelectedItem = tabItemWrapper;
			tabWrapper.SelectedItem = tabItemWrapperMain;
			btnOrders.ClearValue(BackgroundProperty);
			btnWrapper.Background = Brushes.LightRed;
			btnStrapper.ClearValue(BackgroundProperty);
			btnStorage.ClearValue(BackgroundProperty);
			btnPLCConnection.ClearValue(BackgroundProperty);
			borderManualWrap.Visibility = Visibility.Collapsed;
			_currentLayout = ActiveLayout.Wrapper;
		}
		private void HideWrapperLayout() {
			btnOrders.Visibility = Visibility.Collapsed;
		}
		private void ShowEditOrderLayout() {
			btnReturn.Visibility = Visibility.Visible;
			btnSaveOrder.Visibility = Visibility.Visible;
			borderWrapTubeType.Visibility = Visibility.Visible;
			borderCanvas.Visibility = Visibility.Visible;
			borderCanvas.Margin = new Thickness(805, 0, 79, 4);
			tabLayout.SelectedItem = tabItemWrapper;
			tabWrapper.SelectedItem = tabItemWrapperEditOrder;
			borderManualWrap.Visibility = Visibility.Collapsed;
			_currentLayout = ActiveLayout.EditOrder;
		}
		private void HideEditOrderLayout() {
			btnReturn.Visibility = Visibility.Collapsed;
			btnSaveOrder.Visibility = Visibility.Collapsed;
			borderWrapTubeType.Visibility = Visibility.Collapsed;
		}
		private void ShowStrapperLayout() {
			lblTitle.Content = "Cintadora";
			tabLayout.SelectedItem = tabItemStrapper;
			btnWrapper.ClearValue(BackgroundProperty);
			btnStrapper.Background = Brushes.LightRed;
			btnStorage.ClearValue(BackgroundProperty);
			btnPLCConnection.ClearValue(BackgroundProperty);
			_isStrapsModifyActive = false;
			ToogleModifyStrapsTextBoxes();
			borderManualWrap.Visibility = Visibility.Collapsed;
			_currentLayout = ActiveLayout.Strapper;
		}
		private void ShowStorageLayout() {
			lblTitle.Content = "Armazém";
			tabLayout.SelectedItem = tabItemStorage;
			btnWrapper.ClearValue(BackgroundProperty);
			btnStrapper.ClearValue(BackgroundProperty);
			btnStorage.Background = Brushes.LightRed;
			btnPLCConnection.ClearValue(BackgroundProperty);
			borderManualWrap.Visibility = Visibility.Collapsed;
			FillLastHistory();
			_currentLayout = ActiveLayout.Storage;
		}
		private void ShowPLCConnectionLayout() {
			btnWrapper.ClearValue(BackgroundProperty);
			btnStrapper.ClearValue(BackgroundProperty);
			btnStorage.ClearValue(BackgroundProperty);
			btnPLCConnection.Background = Brushes.LightRed;
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
			tabItemWrapperEditOrder.Visibility = Visibility.Hidden;
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
			++_lastTube;
			//DrawSquareWrap(defaultSquareTubeNmbr, defaultWidth, defaultHeight);
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
			catch (Exception exc) when (exc is NullReferenceException || exc is FormatException) { }
		}
		private void tbPackage_LostFocus(object sender, RoutedEventArgs e) {
			TextBox tb = (TextBox)sender;
			if (tb.Text == "") tb.Text = "0";
		}
		private void FillListBoxOfAllowedRopeStraps(int packagePerimeter, int packageWeight) {
			ICollection<string> ropeStraps = new ICollection<string>();
			foreach (string rope in GetAllRopeStrapsFromFile()) {
				string[] values = rope.Split(',');
				CheckIfRopeIsValid(packagePerimeter, packageWeight, values, out bool ropeIsValid);
				if (ropeIsValid)
					ropeStraps.Add(rope);
			}
			lbAllowedRopeStraps.ItemsSource = ropeStraps;
		}
		private void CheckIfRopeIsValid(int packagePerimeter, int packageWeight, string[] values, out bool ropeIsValid) {
			bool ropePerimeterIsValid = false, ropeWeightIsValid = false;
			int ropePerimeter = 0, ropeWeight = 0;
			try {
				ropePerimeter = int.Parse(values[1]);
				ropeWeight = int.Parse(values[2]);
			}
			catch (Exception exc) when (exc is ArgumentNullException || exc is FormatException || exc is OverflowException) {
				ropeIsValid = false;
				return;
			}
			ropePerimeterIsValid = (ropePerimeter >= packagePerimeter + 150 &&
									ropePerimeter <= packagePerimeter + 300) ? true : false;
			ropeWeightIsValid = (ropeWeight >= packageWeight + 50) ? true : false;
			ropeIsValid = (ropePerimeterIsValid && ropeWeightIsValid) ? true : false;
		}
		private IEnumerable GetAllRopeStrapsFromFile() {
			if (!Document.ReadFromFile(_pathRopeStraps, out IEnumerable<string> linesFromFile))
				return Enumerable.Empty<string>();
			return linesFromFile;
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

			GetValuesFromRoundTubeRecipe(tubeAmount, out int tubeAmountBigLine, out int tubeAmountSmallLine, out int vPosInit, out int hPosInit, out int shapeDiameter);

			int vPos = vPosInit, hPos = hPosInit;
			int columns = 0, rows = 0, tubeCurrentlyDrawing = 0;
			ICollection<Ellipse> listEllipses = new ICollection<Ellipse>();

			if (shapeDiameter == 0)
				return;

			if (_lastTube == (tubeAmount + 1)) {
				_lastTube = 0;
				++_currentPackage;
			}

			CreateEllipseShapesToBeDrawn(tubeAmount, tubeAmountBigLine, tubeAmountSmallLine, shapeDiameter, tubeCurrentlyDrawing, vPos, hPos, ref columns, ref rows, listEllipses);

			hPos = hPosInit;
			vPos = vPosInit;

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
				vPosInit = recipeValues["vPos"];
				hPosInit = recipeValues["hPos"];
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
		private void CreateEllipseShapesToBeDrawn(int tubeAmount, int tubeAmountBigLine, int tubeAmountSmallLine, int shapeDiameter, int tubeCurrentlyDrawing, int vPos, int hPos, ref int columns, ref int rows, ICollection<Ellipse> listEllipses) {
			int hPosLineInit;
			byte variavel = 0;
			bool incrementing = false;
			for (byte i = 0; i < tubeAmountBigLine; i++) {
				++rows;
				hPosLineInit = hPos;
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
						ellip.Fill = (tubeCurrentlyDrawing < _lastTube) ? Brushes.TomatoBrush : Brushes.GrayBrush;
					}
					else
						ellip.StrokeThickness = 0;
					listEllipses.Add(ellip);
					hPos += shapeDiameter + Margin;
					++tubeCurrentlyDrawing;
				}
				switch (incrementing) {
					case true:
						hPos = hPosLineInit - ((shapeDiameter / 2) + (Margin / 2));
						break;
					case false:
						hPos = hPosLineInit + ((shapeDiameter / 2) + (Margin / 2));
						break;
				}
				vPos -= shapeDiameter + (Margin / 2);
			}
		}
		private void DrawSquareWrap(int tubeAmount, int width, int height) {
			if (_lastTube == (tubeAmount + 1)) {
				_lastTube = 0;
				++_currentPackage;
			}

			GetValuesFromSquareRectTubeRecipe(tubeAmount, width, height, out int shapeWidth, out int shapeHeight, out int vPosInit, out int hPosInit);
			int vPos = vPosInit, hPos = hPosInit, tubeCurrentlyDrawing = 0;

			CalculateNumberOfRowsAndColummsFromTubeAmount(tubeAmount, width, height, out double numH, out double numV, out int packageWidth, out int packageHeight);

			if (shapeWidth == 0 || shapeHeight == 0)
				return;
			ICollection<Rectangle> listRectangles = new ICollection<Rectangle>();
			CreateRectangleShapesToBeDrawn(tubeAmount, shapeWidth, shapeHeight, hPosInit, ref vPos, ref hPos, ref tubeCurrentlyDrawing, numH, numV, listRectangles);
			hPos = hPosInit;
			vPos = vPosInit;

			PutShapesInCanvas(listRectangles);

			UpdateLabelsValues(tubeAmount, packageWidth, packageHeight);
		}
		private void CreateRectangleShapesToBeDrawn(int tubeAmount, int shapeWidth, int shapeHeight, int hPosInit, ref int vPos, ref int hPos, ref int tubeCurrentlyDrawing, double numH, double numV, ICollection<Rectangle> listRectangles) {
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
						rect.Fill = (tubeCurrentlyDrawing < _lastTube) ? Brushes.TomatoBrush : Brushes.GrayBrush;
					}
					else
						rect.StrokeThickness = 0;
					listRectangles.Add(rect);
					hPos += shapeWidth + Margin;
					++tubeCurrentlyDrawing;
				}
				hPos = hPosInit;
				vPos -= shapeHeight + Margin;
			}
		}
		private void GetValuesFromSquareRectTubeRecipe(int tubeAmount, int width, int height, out int shapeWidth, out int shapeHeight, out int vPosInit, out int hPosInit) {
			Dictionary<string, int> value = Recipes.GetSquareTubeRecipe(tubeAmount);
			try {
				vPosInit = value["vPos"];
				hPosInit = value["hPos"];
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
				vPosInit = 0;
				hPosInit = 0;
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
		private void PutShapesInCanvas<T>(ICollection<T> listOfShapes) where T : Shape {
			cnvAtado.Children.Clear();
			foreach (var forma in listOfShapes)
				cnvAtado.Children.Add(forma);
		}
		private void UpdateLabelsValues(int tubeAmount, double packageWidth, double packageHeight) {
			lblPackageWidth.Content = packageWidth.ToString();
			lblPackageHeight.Content = packageHeight.ToString();

			lblCurrentTubes.Content = _lastTube.ToString();
			lblTotalTubes.Content = tubeAmount.ToString();
			lblCurrentPackage.Content = _currentPackage.ToString();
		}
		#endregion

		#endregion

		#region EditOrder TextBoxe's Values
		// Handle NewOrder textboxe's values
		private void tb_LostFocus(object sender, RoutedEventArgs e) {
			// main input method is the on screen keypad
			// anyway, should be added code to prevent user
			// from typing letters (from full-size physical keyboard, if available)
			TextBox tb = (TextBox)sender;
			CheckIfTextBoxesValuesAreValid(sender, tb);
			string[] array = GatherNewOrderTextBoxesValues();
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
			catch (NullReferenceException) { return; }
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
			if (_currentTubeType == ActiveTubeType.Round)
				DrawHexagonalWrap(tubes, diameter);
			if (_currentTubeType == ActiveTubeType.Square)
				DrawSquareWrap(tubes, width, height);
		}
		private void CheckIfTextBoxesValuesAreValid(object sender, TextBox tb) {
			if (sender != tbDiam && sender != tbWidth && sender != tbHeight) return;
			if (double.TryParse(tb.Text, out double value))
				tb.ClearValue(BackgroundProperty);
			else {
				tb.Background = Brushes.NonActiveBack;
				if (tb.Text != "")
					MessageBox.Show("- Apenas são aceites números\n" +
									"- Medida não pode ser igual 0", "Valor inserido inválido");
			}
			if (value != 0.00) return;
			tb.Background = Brushes.NonActiveBack;
			if (tb.Text != "")
				MessageBox.Show("Medida não pode ser igual a 0");
		}
		private static double GetWeight(IReadOnlyList<string> array, double diameter, int width, int height) {
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
		private string[] GatherNewOrderTextBoxesValues() {
			// Gathers value of New Order textboxes and concatenates them into a string
			OrderDetails newOrder = new OrderDetails {
				Active = "1",
				Name = tbNrOrdem.Text,
				Diameter = tbDiam.Text,
				Width = tbWidth.Text,
				Height = tbHeight.Text,
				Thick = tbThickness.Text,
				Length = tbLength.Text,
				Density = tbDensity.Text,
				TubeAm = tbTubeNmbr.Text,
				TubeType = (_currentTubeType == ActiveTubeType.Round ? "R" : "Q"),
				PackageType = (_currentWrapType == ActiveWrapType.Hexagonal ? "H" : "Q"),
				Created = DateTime.Now.ToString("dd/MM/yyyy HH\\hmm")
			};
			if (CheckEmptyTextBoxes()) {
				string[] emptyString = null;
				return emptyString;
			}
			try {
				switch (_currentTubeType) {
					case ActiveTubeType.Round:
						newOrder.Weight = Math.Round(newOrder.CalculateWeight(newOrder.Diameter, newOrder.Thick, newOrder.Length, newOrder.Density)).ToString();
						break;
					case ActiveTubeType.Square:
						newOrder.Weight = Math.Round(newOrder.CalculateWeight(newOrder.Width, newOrder.Height, newOrder.Thick, newOrder.Length, newOrder.Density)).ToString();
						break;
				}
			}
			catch (Exception exc) {
				MessageBox.Show(exc.InnerException.ToString());
				UpdateStatusBar("Cálculo do peso falhou", 1);
			}
			ICollection<string> stringToWrite = new ICollection<string> {
				_id.ToString(), newOrder.Active, newOrder.Name, newOrder.Diameter, newOrder.Width,
				newOrder.Height, newOrder.Thick, newOrder.Length, newOrder.Density, newOrder.TubeAm,
				newOrder.TubeType, newOrder.PackageType, newOrder.Weight, newOrder.Created };
			return stringToWrite.ToArray();
		}
		private bool CheckEmptyTextBoxes() {
			bool boxIsEmpty = false;
			ICollection<TextBox> textBoxes = new ICollection<TextBox>() {tbNrOrdem, tbDiam, tbWidth, tbHeight,
															tbThickness,tbLength, tbTubeNmbr };
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
			ICollection<Grid> boxesGrids = new ICollection<Grid>() { grid2Straps, grid3Straps, grid4Straps, grid5Straps, grid6Straps };
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
					if (tbstrap2_1.Text != "" && tbstrap2_2.Text != "")
						GetStrapsPositionFromTextboxes(value);
					break;
				case 3:
					if (tbstrap3_1.Text != "" && tbstrap3_2.Text != "" && tbstrap3_3.Text != "")
						GetStrapsPositionFromTextboxes(value);
					break;
				case 4:
					if (tbstrap4_1.Text != "" && tbstrap4_2.Text != "" && tbstrap4_3.Text != "" && tbstrap4_4.Text != "")
						GetStrapsPositionFromTextboxes(value);
					break;
				case 5:
					if (tbstrap5_1.Text != "" && tbstrap5_2.Text != "" && tbstrap5_3.Text != "" && tbstrap5_4.Text != "" && tbstrap5_5.Text != "")
						GetStrapsPositionFromTextboxes(value);
					break;
				case 6:
					if (tbstrap6_1.Text != "" && tbstrap6_2.Text != "" && tbstrap6_3.Text != "" && tbstrap6_4.Text != "" && tbstrap6_5.Text != "" && tbstrap6_6.Text != "")
						GetStrapsPositionFromTextboxes(value);
					break;
					default:
						break;
			}
		}
		private void GetStrapsPositionFromTextboxes(byte straps) {
			// Gets straps position from active grid
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
			ToogleModifyStrapsTextBoxes();
		}
		private void ToogleModifyStrapsTextBoxes() {
			// Changes strap position textboxes (activate or deactivate modification)
			// according to current state of program
			if (_currentLayout == ActiveLayout.Strapper) {
				foreach (TextBox item in GetCurrentActiveStrapsTextBoxes()) {
					if (_isStrapsModifyActive)
						SetTextBoxForEdit(item);
					else {
						ResetTextBox(item);
						if (_textChanged)
							item.Text = "";
					}
				}
			}
			_textChanged = false;
		}
		private IEnumerable<TextBox> GetCurrentActiveStrapsTextBoxes() {
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
					default:
						break;
			}
			return controlsCollection;
		}
		private void UpdateStrapsValues(int length) {
			if (_currentLayout != ActiveLayout.Strapper)
				return;
			byte.TryParse(numKeypadUpDown.Value.ToString(), out byte nmbr);
			IEnumerable<TextBox> controlsCollection = null;
			switch (nmbr) {
				case 2:
					if (grid2Straps.Visibility == Visibility.Collapsed)
						return;
					controlsCollection = grid2Straps.Children.OfType<TextBox>();
					break;
				case 3:
					if (grid3Straps.Visibility == Visibility.Collapsed)
						return;
					controlsCollection = grid3Straps.Children.OfType<TextBox>();
					break;
				case 4:
					if (grid4Straps.Visibility == Visibility.Collapsed)
						return;
					controlsCollection = grid4Straps.Children.OfType<TextBox>();
					break;
				case 5:
					if (grid5Straps.Visibility == Visibility.Collapsed)
						return;
					controlsCollection = grid5Straps.Children.OfType<TextBox>();
					break;
				case 6:
					if (grid6Straps.Visibility == Visibility.Collapsed)
						return;
					controlsCollection = grid6Straps.Children.OfType<TextBox>();
					break;
				default:
					break;
			}
			int[] values = Recipes.GetStrapsPositionFromRecipe(length, nmbr);
			byte i = 0;
			if (controlsCollection != null)
				foreach (TextBox item in controlsCollection)
				{
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
		private void StorageTimer_Tick(object sender, EventArgs e) {
			UpdateStoragePage();
		}
		private void FillLastHistory() {
			ICollection<History> history = History.ReadHistoryFromFile(_historyPath);
			ICollection<Label> weightLabels = new ICollection<Label>() { lblWeight1, lblWeight2, lblWeight3 };
			try {
				lblTubesHistory.Content = history[(history.Count) - 1].TubeAm;
				for (byte i = 1; i <= 3; i++)
					weightLabels[i - 1].Content = history[(history.Count) - i].Weight;
				ICollection<Label> dateLabels = new ICollection<Label>() { lblDate1, lblDate2, lblDate3 };
				for (byte i = 1; i <= 3; i++)
					dateLabels[i - 1].Content = history[(history.Count) - i].Created;
			}
			catch (ArgumentOutOfRangeException) { }
		}
		private void SetStorageTimer() {
			_storageTimer = new DispatcherTimer();
			_storageTimer.Tick += new EventHandler(StorageTimer_Tick);
			_storageTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
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
				if (_currentDate == ActiveDate.Initial) {
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
			_currentDate = ActiveDate.Initial;
		}
		private void FinalDate_GotFocus(object sender, RoutedEventArgs e) {
			_currentDate = ActiveDate.End;
		}
		private void FillHistoryDataGrid() {
			if ((bool)rbNoFilter.IsChecked)
				datagridHistory.ItemsSource = History.ReadHistoryFromFile(_historyPath);
			else if ((bool)rbSelectedDate.IsChecked) {
				try {
					if (comboboxShift.SelectedIndex == 0)
						datagridHistory.ItemsSource = History.ReadHistoryFromFile(_historyPath, calHistory.SelectedDate.Value.Date);
					else
						datagridHistory.ItemsSource = History.ReadHistoryFromFile(_historyPath, calHistory.SelectedDate.Value.Date, calHistory.SelectedDate.Value.Date, (byte)comboboxShift.SelectedIndex);
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
						datagridHistory.ItemsSource = History.ReadHistoryFromFile(_historyPath, initialDate, endDate);
					else
						datagridHistory.ItemsSource = History.ReadHistoryFromFile(_historyPath, initialDate, endDate, (byte)comboboxShift.SelectedIndex);
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
			ShowTubeRecipesOnDataGrid(_pathRoundTubes);
			IEnumerable<TextBox> textBoxes = GetTextBoxesFromGrids();
			foreach (TextBox item in textBoxes) {
				item.Text = "";
			}
			_currentRecipe = ActiveRecipe.RoundTube;
		}
		private void btnRecipeSquareTube_Click(object sender, RoutedEventArgs e) {
			ShowTubeRecipesOnDataGrid(_pathSquareTubes, _pathRectTubes);
			IEnumerable<TextBox> textBoxes = GetTextBoxesFromGrids();
			foreach (TextBox item in textBoxes) {
				item.Text = "";
			}
			_currentRecipe = ActiveRecipe.SquareTube;
		}
		private void ShowTubeRecipesOnDataGrid(string pathRoundTube) {
			datagridRecipes.ItemsSource = Recipes.ReadTubeRecipesFromFile(pathRoundTube);
			btnRecipeRoundTube.Background = Brushes.LightRed;
			btnRecipeSquareTube.ClearValue(BackgroundProperty);
			gridRecipesSquareTube.Visibility = Visibility.Collapsed;
			gridRecipesRoundTube.Visibility = Visibility.Visible;
		}
		private void ShowTubeRecipesOnDataGrid(string pathSquareTube, string pathRectTube) {
			datagridRecipes.ItemsSource = Recipes.ReadTubeRecipesFromFile(pathRectTube, pathRectTube);
			btnRecipeSquareTube.Background = Brushes.LightRed;
			btnRecipeRoundTube.ClearValue(BackgroundProperty);
			gridRecipesRoundTube.Visibility = Visibility.Collapsed;
			gridRecipesSquareTube.Visibility = Visibility.Visible;
		}
		private void datagridRecipes_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e) {
			if (!_editingRecipe)
				_cellsArePopulated = GetDataFromSelectedCells();
		}
		private void btnRecipeEdit_Click(object sender, RoutedEventArgs e) {
			if (_cellsArePopulated) {
				_editingRecipe = true;
				ICollection<TextBox> textBoxes = GetTextBoxesFromGrids();
				foreach (TextBox item in textBoxes) {
					SetTextBoxForEdit(item);
				}
				DisableRecipeUIButtons();
			}
			else
				UpdateStatusBar("Para editar selecione uma ordem", 1);
		}
		private static void SetTextBoxForEdit(TextBox item) {
			item.Background = Brushes.YellowBrush;
			item.IsReadOnly = false;
			item.Focusable = true;
		}
		private void ResetTextBox(TextBox item) {
			item.ClearValue(BackgroundProperty);
			item.IsReadOnly = true;
			item.Focusable = false;
		}
		private void DisableRecipeUIButtons() {
			btnRecipeEdit.IsEnabled = false;
			btnRecipeRoundTube.IsEnabled = false;
			btnRecipeSquareTube.IsEnabled = false;
			btnReturn.IsEnabled = false;
			btnRecipeSave.Visibility = Visibility.Visible;
			btnRecipeCancel.Visibility = Visibility.Visible;
		}
		private void datagridRecipes_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
			if (!_editingRecipe) return;
			e.Handled = true;
			UpdateStatusBar("Para mudar de receita termine de editar a atual", 1);
		}
		private bool GetDataFromSelectedCells() {
			if (_currentRecipe == ActiveRecipe.RoundTube) {
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
			ICollection<string> newFileContent = new ICollection<string>();
			if (_currentRecipe == ActiveRecipe.RoundTube) {
				EditRoundTubeRecipesTextFile(newFileContent);
				string msg = Document.WriteToFile(_pathRoundTubes, newFileContent.ToArray()) ? SaveSuccessful : SaveError;
				UpdateStatusBar(msg);
				datagridRecipes.ItemsSource = null;
				datagridRecipes.ItemsSource = Recipes.ReadTubeRecipesFromFile(_pathRoundTubes);
			}
			else {
				EditSquareTubeRecipesTextFile(newFileContent, _pathSquareTubes, out bool found);
				if (found) {
					string msg = Document.WriteToFile(_pathSquareTubes, newFileContent.ToArray()) ? SaveSuccessful : SaveError;
					UpdateStatusBar(msg);
				}
				else {
					EditSquareTubeRecipesTextFile(newFileContent, _pathRectTubes, out found);
					string msg = Document.WriteToFile(_pathRectTubes, newFileContent.ToArray()) ? SaveSuccessful : SaveError;
					UpdateStatusBar(msg);
				}
				datagridRecipes.ItemsSource = null;
				datagridRecipes.ItemsSource = Recipes.ReadTubeRecipesFromFile(_pathSquareTubes, _pathRectTubes);
			}
			ResetRecipeUIButtons();
			_editingRecipe = false;
		}
		private void DisableTextBoxesModification() {
			ICollection<TextBox> textBoxes = GetTextBoxesFromGrids();
			foreach (TextBox item in textBoxes)
				ResetTextBox(item);
		}
		private void EditSquareTubeRecipesTextFile(ICollection<string> newFileContent, string path, out bool found) {
			found = false;
			if (!Document.ReadFromFile(path, out IEnumerable<string> linesFromFile)) return;
			foreach (string item in linesFromFile) {
				string newline = "";
				string[] array = item.Split(',');
				if (array[0] == tbRecipeTubes.Text) {
					array[array.Length - 2] = tbRecipecolumns.Text;
					array[array.Length - 1] = tbRecipeRows.Text;
					if (path == _pathSquareTubes)
						found = true;
					//foreach (string value in array)
					//	newline += value + ",";
					newline = array.Aggregate(newline, (current, value) => current + (value + ","));
					newline = newline.Remove(newline.Length - 1);
				}
				newFileContent.Add(newline == "" ? item : newline);
			}
		}
		private void EditRoundTubeRecipesTextFile(ICollection<string> newFileContent) {
			if (!Document.ReadFromFile(_pathRoundTubes, out IEnumerable<string> linesFromFile)) return;
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
		private void btnRecipeCancel_Click(object sender, RoutedEventArgs e) {
			ICollection<TextBox> textBoxes = GetTextBoxesFromGrids();
			foreach (TextBox item in textBoxes) {
				ResetTextBox(item);
			}
			ResetRecipeUIButtons();
			_editingRecipe = false;
		}
		private void ResetRecipeUIButtons() {
			btnRecipeEdit.IsEnabled = true;
			btnRecipeRoundTube.IsEnabled = true;
			btnRecipeSquareTube.IsEnabled = true;
			btnReturn.IsEnabled = true;
			btnRecipeSave.Visibility = Visibility.Collapsed;
			btnRecipeCancel.Visibility = Visibility.Collapsed;
		}
		private ICollection<TextBox> GetTextBoxesFromGrids() {
			ICollection<TextBox> textBoxes = new ICollection<TextBox>();
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