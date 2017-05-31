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
		private IIBHnetClass SPS = null;
		private IIIBHnet2 SPS_2 = null;
		private IIIBHnet3 SPS_3 = null;
		private bool isConnected = false;
		private string message;

		public void Init() {
			// PLC Object Initialize and create a reference to all interfaces.
			SPS1 = new IIBHnetClass();
			SPS_2 = SPS1;
			SPS_3 = SPS1;
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

					SPS_3.Connect_DP(IPAddr, nMpi, nRack, nSlot);
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
					SPS1.Disconnect();
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
		public string ReadBool(int DBAddress, int varAddress) {
			string valueRead = "";
			if (isConnected) {
				try {
					// SPS.get_DW(int DBNr, int nr);
					// DBNr     : The number of the data block
					// nr       : The byte address within the DB

					valueRead = SPS1.get_DW(DBAddress, varAddress).ToString();
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
		private void WriteBool(int DBAddress, int varAddress, bool valueToWrite) {
			if (isConnected) {
				try {
					// SPS.set_DW(int DBNr, int nr, int pVal);
					// DBNr : The number of the data block
					// nr   : The byte address within the DB
					// pVal : The new value

					//SPS.set_DW(DBAddress, varAddress, Convert.ToInt32(valueToWrite);
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

					valueRead = SPS1.get_DW(DBAddress, varAddress).ToString();
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
		private void WriteInt(int DBAddress, int varAddress, int valueToWrite) {
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

		#region Real
		public string ReadReal(int DBAddress, int varAddress) {
			string valueRead = "";
			if (isConnected) {
				try {
					// SPS.get_DW(int DBNr, int nr);
					// DBNr     : The number of the data block
					// nr       : The byte address within the DB

					valueRead = SPS1.get_DW(DBAddress, varAddress).ToString();
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
		private void WriteReal(int DBAddress, int varAddress, double valueToWrite) {
			if (isConnected) {
				try {
					// SPS.set_DW(int DBNr, int nr, int pVal);
					// DBNr : The number of the data block
					// nr   : The byte address within the DB
					// pVal : The new value

					//SPS.set_DW(DBAddress, varAddress, Convert.ToInt32(value);
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
