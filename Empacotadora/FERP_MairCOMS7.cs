using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Globalization;
using IBHNETLib;

namespace Empacotadora {
	/// <summary>
	/// Contains the addresses of the variables provided by the user.
	/// </summary>
	class FERP_MairCOMS7 {
		// Definition of IBHNet references
		private IIIBHnet SPS = null;
		private IIIBHnet3 SPS_3 = null;
		private bool isConnected = false;
		private string message;

		public void Init() {
			// PLC Object Initialize and create a reference to all interfaces.
			SPS = new IIBHnet();
			SPS_3 = (IIIBHnet3)SPS;
		}

		#region Connection
		/// <summary>
		/// Returns connection status
		/// </summary>
		public string PLC_Connect(string IPAddr, byte MPIAddr) {
			// Establish a connection to the control unit
			if (!isConnected) {
				// Read the selected control
				string sMpi = MPIAddr.ToString();
				// Read the selected MPI address
				int nMpi = Convert.ToInt32(sMpi, 10);

				//int nRack = Convert.ToInt32(tbRack.Text.ToString());
				int nRack = 0;

				//int nSlot = Convert.ToInt32(tbSlot.Text.ToString());
				int nSlot = 0;

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
					isConnected = true;
					message = "Connected";
				}
				catch {
					isConnected = false;
					message = "Error while connecting";
				}
			}
			else
				message = "Already connected";
			return message;
		}
		/// <summary>
		/// Returns connection status
		/// </summary>
		public string Disconnect_Click(object sender, EventArgs e) {
			// Disconnect the connection to the PLC
			if (isConnected) {
				try {
					SPS.Disconnect();
					isConnected = false;
					message = "Disconnected";
				}
				catch {
					message = "Error while disconnecting";
					isConnected = false;
				}
			}
			else
				message = "Not connected";
			return message;
		}
		#endregion

		#region Bool
		public string ReadBool(int DB, Tuple<int, int, string> variable) {
			string valueRead = "";
			if (isConnected) {
				try {
					// SPS.get_DW(int DBNr, int nr);
					// DBNr     : The number of the data block
					// nr       : The byte address within the DB
					int DBAddress = DB;
					int variableAddress = variable.Item1;
					int bitToChange = variable.Item2;

					valueRead = SPS.get_D(DBAddress, variableAddress, bitToChange).ToString();
					if (valueRead.ToString() != "")
						message = "Success 'ReadBool()'";
				}
				catch {
					message = "Error 'ReadBool()'";
				}
			}
			else
				message = "Not connected";
			MessageBox.Show(message);
			return valueRead;
		}
		public void WriteBool(int DBAddress, int varAddress, int bitAddress, int valueToWrite) {
			if (isConnected) {
				try {
					// SPS.set_D(int DBNr, int nr, int bit, int pVal);
					// DBNr : The number of the data block
					// nr   : The byte address within the DB
					// bit	: Address of bit to change
					// pVal : The new value

					SPS.set_D(DBAddress, varAddress, bitAddress, valueToWrite);
					MessageBox.Show(DBAddress.ToString() + " -> " + varAddress.ToString() + " -> " +
									bitAddress.ToString() + " -> " + valueToWrite.ToString());
					message = "Success 'WriteBool()'";
				}
				catch {
					message = "Error 'WriteBool()'";
				}
			}
			else
				message = "Not connected";
			MessageBox.Show(message);
		}
		#endregion

		#region Int
		public string ReadInt(int DBAddress, int varAddress) {
			string valueRead = "";
			if (isConnected) {
				try {
					// SPS.get_DW(int DBNr, int nr);
					// DBNr     : The number of the data block
					// nr       : The byte address within the DB

					valueRead = SPS.get_DW(DBAddress, varAddress).ToString();
					message = "Success'ReadInt()'";
				}
				catch {
					message = "Error 'ReadInt()'";
				}
			}
			else
				message = "Not connected";
			MessageBox.Show(message);
			return valueRead;
		}
		public void WriteInt(int DBAddress, int varAddress, int valueToWrite) {
			if (isConnected) {
				try {
					// SPS.set_DW(int DBNr, int nr, int pVal);
					// DBNr : The number of the data block
					// nr   : The byte address within the DB
					// pVal : The new value

					SPS.set_DW(DBAddress, varAddress, Convert.ToInt32(valueToWrite));
					message = "Success 'WriteInt()'";
				}
				catch {
					message = "Error 'WriteInt()'";
				}
			}
			else
				message = "Not connected";
			MessageBox.Show(message);
		}
		#endregion

		#region Real
		public string ReadReal(int DBAddress, int varAddress) {
			string valueRead = "";
			if (isConnected) {
				try {
					// SPS.get_DW(int DBNr, int nr);
					// DBNr     : The number of the data block
					// nr       : The byte address within the DB

					valueRead = SPS.get_DW(DBAddress, varAddress).ToString();
					message = "Success'ReadInt()'";
				}
				catch {
					message = "Error 'ReadInt()'";
				}
			}
			else
				message = "Not connected";
			MessageBox.Show(message);
			return valueRead;
		}
		public void WriteReal(int DBAddress, int varAddress, double valueToWrite) {
			if (isConnected) {
				try {
					// SPS.set_DW(int DBNr, int nr, int pVal);
					// DBNr : The number of the data block
					// nr   : The byte address within the DB
					// pVal : The new value

					//SPS.set_DW(DBAddress, varAddress, Convert.ToInt32(valueToWrite);
					message = "Success 'WriteInt()'";
				}
				catch {
					message = "Error 'WriteInt()'";
				}
			}
			else
				message = "Not connected";
			MessageBox.Show(message);
		}
		#endregion
	}
}
