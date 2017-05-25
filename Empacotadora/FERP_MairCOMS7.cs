using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Globalization;

namespace Empacotadora {
	/// <summary>
	/// Contains the addresses of the variables provided by the user.
	/// </summary>
	class FERP_MairCOMS7 {
		static IBHLinkComm comm;
		static bool connected = false;

		public static void Connect(string IPAddress, byte MPIAddress) {
			if (connected == false) {
				comm = new IBHLinkComm(MPIAddress);
				comm.Connect(IPAddress, 1099);
				connected = true;
				Win_Main WNMain = new Win_Main();
				WNMain.lblConnectionStatus.Content = "Ligado";
				WNMain.tbIPAddress.IsEnabled = false;
				WNMain.tbMPIAddress.IsEnabled = false;
			}
			else
				MessageBox.Show("Already connected.");
		}
		public static void Disconnect() {
			if (connected == true) {
				comm.Disconnect();
				connected = false;
				Win_Main WNMain = new Win_Main();
				WNMain.lblConnectionStatus.Content = "Desligado";
				WNMain.tbIPAddress.IsEnabled = true;
				WNMain.tbMPIAddress.IsEnabled = true;
			}
			else
				MessageBox.Show("Already disconnected.");
		}
		public static void WriteToDBAddress(ref ushort DBNumber, ref int address, string size, string text) {
			if (connected == true) {
				try {
					if (GetLengthInBits(ref size) == 1)
						comm.GenerateWriteDBRequest(DBNumber, address, StringToByteArray(ref text, GetLengthInBits(ref size)));
					else if ((GetLengthInBits(ref size) == 16 || GetLengthInBits(ref size) == 32) && TextIsOK(ref text))
						comm.GenerateWriteDBRequest(DBNumber, address, StringToByteArray(ref text, GetLengthInBits(ref size)));
					comm.Communicate();
				}
				catch (Exception exc) {
					MessageBox.Show(exc.Message);
				}
			}
		}
		/// <summary>
		/// Returns the value received from the communication
		/// </summary>
		public static string ReadFromDBAddress(ref ushort DBNumber, ref int address, ref string size) {
			// Read from PLC
			// Get Data from address
			string value = "";
			if (connected == true) {
				try {
					comm.GenerateReadDBRequest(DBNumber, address, GetLengthInBits(ref size));
					comm.Communicate();
					value = ByteArrayToString(comm.GetRequestData(), GetLengthInBits(ref size));
				}
				catch (Exception exc) {
					MessageBox.Show(exc.Message);
				}
			}
			return value;
		}
		private static byte GetLengthInBits(ref string size) {
			byte lengthInBits = 0;
			switch (size) {
				case "bit":
					lengthInBits = 1;
					break;
				case "integer":
					lengthInBits = 16;
					break;
				case "real":
					lengthInBits = 32;
					break;
				default:
					//Win_Main.UpdateStatusBar("Não encontrou o tipo de variável -> GetLengthInBits()", 1);
					break;
			}
			return lengthInBits;
		}
		//Converts string to Arrays of bytes and reverses them for Big Endian
		private static byte[] StringToByteArray(ref string text, byte lengthInBits) {
			byte[] data = new byte[0];
			switch (lengthInBits) {
				case 1:
					data = new byte[1];
					byte.TryParse(text, out data[0]);
					break;
				case 16:
					ushort.TryParse(text, out ushort tempU);
					data = BitConverter.GetBytes(tempU);
					break;
				case 32:
					float.TryParse(text, out float tempF);
					data = BitConverter.GetBytes(tempF);
					break;
				default:
					//Win_Main.UpdateStatusBar("Não encontrou o tipo de variável -> StringToByteArray()", 1);
					break;
			}
			Array.Reverse(data);
			//if (data != new byte[0])
			//Win_Main.UpdateStatusBar("Converteu corretamente a msg para escrever -> StringToByteArray()");
			//else
			//Win_Main.UpdateStatusBar("Não conseguiu converter a msg para escrever -> StringToByteArray()", 1);
			return data;

		}
		// Reverses Arrays of bytes for Little Endian and converts them into string
		private static string ByteArrayToString(byte[] arr, byte lengthInBits) {
			string str = "?";
			byte[] data = new byte[0];
			switch (lengthInBits) {
				case 1:
					str = Convert.ToString(arr[0]);
					break;
				case 16:
					UInt16 i16Value = BitConverter.ToUInt16(arr, 0);
					data = BitConverter.GetBytes(i16Value);
					Array.Reverse(data);
					UInt16 i16 = BitConverter.ToUInt16(data, 0);
					str = Convert.ToString(i16);
					break;
				case 32:
					UInt32 i32Value = BitConverter.ToUInt32(arr, 0);
					data = BitConverter.GetBytes(i32Value);
					Array.Reverse(data);
					float gkz = BitConverter.ToSingle(data, 0);
					str = Convert.ToString(gkz);
					break;
				default:
					//Win_Main.UpdateStatusBar("Não encontrou o tipo de variável -> ByteArrayToString()", 1);
					break;
			}
			//if (str != "?")
			//	Win_Main.UpdateStatusBar("Recebeu corretamente a msg -> ByteArrayToString()");
			//else
			//	Win_Main.UpdateStatusBar("Não conseguiu receber a msg -> ByteArrayToString()", 1);
			return str;
		}
		private bool CheckValues(string str, string size) {
			//Checks if value to be sent is small enough for chosen length
			double.TryParse(str, out double number);
			bool numberOk = false;
			switch (GetLengthInBits(ref size)) {
				case 1:
					if (str != "true" || str != "false")
						numberOk = true;
					break;
				case 16:
					if (number > 65535)
						numberOk = true;
					break;
				case 32:
					if (number > 3.402823e+38)
						numberOk = true;
					break;
			}
			return numberOk;
		}
		//Checks if the input-characters are numbers (only for "integer" and "real" variables)
		private static bool TextIsOK(ref string text) {
			bool textIsNumber = true;
			return textIsNumber = double.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double test);
		}
	}
}
