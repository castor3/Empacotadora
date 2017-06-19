using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Empacotadora {
	class Document {
		public static bool ReadFromFile(string path, out IEnumerable<string> linesFromFile) {
			linesFromFile = Enumerable.Empty<string>();
			try {
				linesFromFile = File.ReadLines(path);
				return true;
			}
			catch (Exception exc) when (exc is IOException || exc is FileNotFoundException || exc is DirectoryNotFoundException || exc is UnauthorizedAccessException) {
				MessageBox.Show(exc.Message);
				return false;
			}
		}
		/// <summary>
		/// Rewrites the entire file with the argument List<string>
		/// </summary>
		public static bool WriteToFile(string path, string[] newFileContent) {
			try {
				File.WriteAllLines(path, newFileContent);
				return true;
			}
			catch (Exception exc) when (exc is IOException || exc is FileNotFoundException || exc is DirectoryNotFoundException || exc is UnauthorizedAccessException) {
				MessageBox.Show(exc.Message);
				return false;
			}
		}
		/// <summary>
		/// Appends the string to the end of the file
		/// </summary>
		public static bool AppendToFile(string path, string stringToSave) {
			try {
				File.AppendAllText(path, stringToSave);
				return true;
			}
			catch (Exception exc) when (exc is IOException || exc is FileNotFoundException || exc is DirectoryNotFoundException || exc is UnauthorizedAccessException) {
				MessageBox.Show(exc.Message);
				return false;
			}
		}
	}
}
