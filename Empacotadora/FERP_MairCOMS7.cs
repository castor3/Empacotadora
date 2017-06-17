using System;
using System.Runtime.InteropServices;
using System.Windows;
using IBHNETLib;

namespace Empacotadora {
	/// <summary>
	/// Contains the addresses of the variables provided by the user.
	/// </summary>
	class FERP_MairCOMS7 {
		// Definition of IBHNet references
		private IIIBHnet SPS = null;
		private IIIBHnet2 SPS_2 = null;
		private IIIBHnet3 SPS_3 = null;
		bool _connected = false, _initialized = false;

		public string Message { get; set; } = "...";

		#region Connection
		/// <summary>
		/// Returns connection status
		/// </summary>
		public string Connect(string IPAddr, byte MPIAddr) {
			// Establish a connection to the control unit
			if (!_initialized)
				Init();
			if (!_connected && _initialized) {
				string sMpi = MPIAddr.ToString();
				int nMpi = Convert.ToInt32(sMpi, 10);

				//int nRack = Convert.ToInt32(tbRack.Text.ToString());	// Always 0
				//int nSlot = Convert.ToInt32(tbSlot.Text.ToString());	// Used when not using MPI
				const int nRack = 0;
				const int nSlot = 0;

				TryToConnect(IPAddr, nMpi, nRack, nSlot);
			}
			else
				Message = (_connected ? "Already connected" : "Not initialized");
			return Message;
		}
		private void Init() {
			// PLC Object Initialize and create a reference to all interfaces.
			try {
				SPS = new IIBHnet();
				SPS_2 = (IIIBHnet2)SPS;
				SPS_3 = (IIIBHnet3)SPS;
				_initialized = true;
			}
			catch (COMException exc) {
				MessageBox.Show(exc.Message);
				_initialized = false;
			}
		}
		private void TryToConnect(string IPAddr, int nMpi, int nRack, int nSlot) {
			try {
				// Since the function "Connect_DP" of interface 3 activates an
				// exception in the event of an incorrect connection setup, this function is used.
				//
				// SPS_3.Connect_DP(string Station, int DPAdr, int Rack, int Slot)
				// Station: The defined station name
				// DPAdr  :	The MPI or the Profibus address of the CPU
				// Rack   :	Always 0
				// Slot   : With MPI always 0, with Profibus the slot of the CPU

				//SPS_3.Connect_DP(IPAddr, nMpi, nRack, nSlot);
				_connected = true;
				Message = "Successful connection to IP: " + IPAddr + " -> MPI: " + nMpi;
			}
			catch (COMException exc) {
				MessageBox.Show(exc.Message);
				_connected = false;
				Message = "Error while connecting";
			}
		}

		/// <summary>
		/// Returns connection status
		/// </summary>
		public string Disconnect() {
			// Disconnect the connection to the PLC
			if (_connected)
				TryToDisconnect();
			else
				Message = "Not connected";
			return Message;
		}
		private void TryToDisconnect() {
			try {
				//SPS.Disconnect();
				_connected = false;
				Message = "Disconnected";
			}
			catch (COMException exc) {
				MessageBox.Show(exc.Message);
				Message = "Error while disconnecting";
				_connected = false;
			}
		}
		#endregion

		#region Bool
		public bool ReadBool(int DB, Tuple<int, int, string> variable) {
			int valueReadFromPLC = new int();
			if (!_connected) {
				int DBAddress = DB;
				int variableAddress = variable.Item1;
				int bitToChange = variable.Item2;
				TryToReadBool(DBAddress, variableAddress, bitToChange, ref valueReadFromPLC);
			}
			else
				Message = "Not connected";
			MessageBox.Show(Message);
			return Convert.ToBoolean(valueReadFromPLC);
		}
		private void TryToReadBool(int DBAddress, int variableAddress, int bitToChange, ref int valueReadFromPLC) {
			try {
				// SPS.get_D(int DBNr, int nr, int bit);
				// DBNr     : The number of the data block
				// nr       : The byte address within the DB				
				// bit		: Bit number within the data byte
				valueReadFromPLC = SPS.get_D(DBAddress, variableAddress, bitToChange);
				MessageBox.Show(DBAddress + ", " + variableAddress + ", " + bitToChange);
				if (valueReadFromPLC != 0)
					Message = "Success 'ReadBool()'";
			}
			catch (COMException exc) {
				MessageBox.Show(exc.Message);
				Message = "Error 'ReadBool()'";
			}
		}

		public string WriteBool(int DBAddress, Tuple<int, int, string> variable, bool valueToWrite) {
			if (_connected) {
				int variableAddress = variable.Item1;
				int bitToChange = variable.Item2;
				TryToWriteBool(DBAddress, variableAddress, bitToChange, valueToWrite);
			}
			else
				Message = "Not connected";
			return Message;
		}
		private void TryToWriteBool(int DBAddress, int variableAddress, int bitToChange, bool valueToWrite) {
			// SPS.set_D(int DBNr, int nr, int bit, int pVal);
			// DBNr : The number of the data block
			// nr   : The byte address within the DB
			// bit	: Address of bit to change
			// pVal : The new value
			try {
				SPS.set_D(DBAddress, variableAddress, bitToChange, Convert.ToInt32(valueToWrite));
				MessageBox.Show(DBAddress + ", " + variableAddress + ", " + bitToChange + ", " + valueToWrite);
				Message = "Success 'WriteBool()'";
			}
			catch (COMException exc) {
				MessageBox.Show(exc.Message);
				Message = "Error 'WriteBool()'";
			}
		}

		public void ToogleBool(int DB, Tuple<int, int, string> variable) {
			//WriteBool(DB, variable, !ReadBool(DB, variable));
		}
		#endregion

		#region Int
		public int ReadInt(int DBAddress, int varAddress) {
			string valueReadFromPLC = "";
			if (_connected) {
				try {
					// SPS.get_DW(int DBNr, int nr);
					// DBNr     : The number of the data block
					// nr       : The byte address within the DB

					valueReadFromPLC = SPS.get_DW(DBAddress, varAddress).ToString();
					Message = "Success'ReadInt()'";
				}
				catch {
					Message = "Error 'ReadInt()'";
				}
			}
			else
				Message = "Not connected";
			MessageBox.Show(Message);
			return Convert.ToInt32(valueReadFromPLC);
		}
		public void WriteInt(int DBAddress, int varAddress, double valueToWrite) {
			if (!_connected) {
				try {
					// SPS.set_DW(int DBNr, int nr, int pVal);
					// DBNr : The number of the data block
					// nr   : The byte address within the DB
					// pVal : The new value
					byte[] bytes = BitConverter.GetBytes(valueToWrite);
					Array.Reverse(bytes);
					//foreach (var item in asd) {
					//	MessageBox.Show("Bytes: " + item);
					//}

					//SPS.set_DD(DBAddress, varAddress,)
					//SPS.set_DW(DBAddress, varAddress, Convert.ToInt32(valueToWrite));
					Message = "Success 'WriteInt()'";
				}
				catch {
					Message = "Error 'WriteInt()'";
				}
			}
			else
				Message = "Not Connected";
			//MessageBox.Show(Message);
		}
		#endregion

		#region Real
		public double ReadReal(int DBAddress, int varAddress) {
			string valueReadFromPLC = "";
			if (_connected) {
				try {
					// SPS.get_DW(int DBNr, int nr);
					// DBNr     : The number of the data block
					// nr       : The byte address within the DB

					valueReadFromPLC = SPS.get_DW(DBAddress, varAddress).ToString();
					Message = "Success'ReadReal()'";
				}
				catch {
					Message = "Error 'ReadReal()'";
				}
			}
			else
				Message = "Not Connected";
			MessageBox.Show(Message);
			return Convert.ToDouble(valueReadFromPLC);
		}
		public void WriteReal(int DBAddress, int varAddress, double valueToWrite) {
			if (_connected) {
				try {
					// SPS.set_DW(int DBNr, int nr, int pVal);
					// DBNr : The number of the data block
					// nr   : The byte address within the DB
					// pVal : The new value

					//SPS.set_DW(DBAddress, varAddress, Convert.ToInt32(valueToWrite);
					Message = "Success 'WriteReal()'";
				}
				catch {
					Message = "Error 'WriteReal()'";
				}
			}
			else
				Message = "Not Connected";
			MessageBox.Show(Message);
		}
		#endregion

		#region Array Int
		public int[] ReadArrayInt(int DBAddress, int varAddressFrom, int varAddressTo) {
			int[] arrayOfInts = new int[0];
			if (_connected) {
				try {
					// SPS.get_DW(int DBNr, int nr);
					// DBNr     : The number of the data block
					// nr       : The byte address within the DB
					for (int i = varAddressFrom; i < varAddressTo; i += 2)
						arrayOfInts[i] = SPS.get_DW(DBAddress, i);
					Message = "Success'ReadArrayInt()'";
				}
				catch {
					Message = "Error 'ReadArrayInt()'";
				}
			}
			else
				Message = "Not Connected";
			MessageBox.Show(Message);
			return arrayOfInts;
		}
		#endregion

		#region Array Real
		public double[] ReadArrayReal(int DBAddress, int varAddressFrom, int varAddressTo) {
			double[] arrayOfReals = new double[0];
			if (_connected) {
				try {
					// SPS.get_DW(int DBNr, int nr);
					// DBNr     : The number of the data block
					// nr       : The byte address within the DB
					for (int i = varAddressFrom; i < varAddressTo; i += 4)
						arrayOfReals[i] = SPS.get_DW(DBAddress, i);
					Message = "Success'ReadArrayReal()'";
				}
				catch {
					Message = "Error 'ReadArrayReal()'";
				}
			}
			else
				Message = "Not Connected";
			MessageBox.Show(Message);
			return arrayOfReals;
		}
		#endregion
	}
}
