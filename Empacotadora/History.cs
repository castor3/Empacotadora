using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Windows;

namespace Empacotadora {
	public class History {
		private enum Shift { noShiftFilter, shiftOne, shiftTwo, shiftThree, nill }
		//Properties
		public string Name { get; set; }
		public string PackNmbr { get; set; }
		public string TubeAm { get; set; }
		public string Weight { get; set; }
		public string Created { get; set; }
		//Methods
		public static List<History> ReadHistoryFromFile(string path) {
			List<History> history = new List<History>();
			if (!Document.ReadFromFile(path, out IEnumerable<string> linesFromFile)) return history;
			foreach (string line in linesFromFile) {
				string[] array = line.Split(',');
				AddArrayToList(ref history, array);
			}
			return history;
		}
		public static List<History> ReadHistoryFromFile(string path, DateTime selectedDate) {
			List<History> history = new List<History>();
			if (!Document.ReadFromFile(path, out IEnumerable<string> linesFromFile)) return history;
			foreach (string line in linesFromFile) {
				string[] array = line.Split(',');
				string[] aux = array[4].Split('/');
				DateTime historyDate = DateTime.ParseExact(aux[0] + "/" + aux[1], "dd/MM", CultureInfo.InvariantCulture);
				if (selectedDate == historyDate)
					AddArrayToList(ref history, array);
			}
			return history;
		}
		public static List<History> ReadHistoryFromFile(string path, DateTime initialDate, DateTime endDate) {
			List<History> history = new List<History>();
			if (!Document.ReadFromFile(path, out IEnumerable<string> linesFromFile)) return history;
			foreach (string line in linesFromFile) {
				string[] array = line.Split(',');
				DateTime historyDate = DateTime.ParseExact(array[4], "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
				if (historyDate >= initialDate.AddHours(7) && historyDate <= endDate.AddDays(1))
					AddArrayToList(ref history, array);
			}
			return history;
		}
		public static List<History> ReadHistoryFromFile(string path, DateTime initialDate, DateTime endDate, byte shiftSelected) {
			Shift shiftOfHistoryDate = Shift.nill;
			List<History> history = new List<History>();
			if (!Document.ReadFromFile(path, out IEnumerable<string> linesFromFile)) return history;
			foreach (string line in linesFromFile) {
				string[] array = line.Split(',');
				shiftOfHistoryDate = GetShift(initialDate, endDate, array[4]);
				if (shiftSelected == 0 && shiftOfHistoryDate != Shift.nill)
					AddArrayToList(ref history, array);
				else if ((byte)(shiftOfHistoryDate) == shiftSelected)
					AddArrayToList(ref history, array);
			}
			return history;
		}
		private static void ParseDates(DateTime initialDate, DateTime endDate, string array, out DateTime historyDate, out DateTime newEndDate, out DateTime newInitialDate, out TimeSpan diff) {
			historyDate = DateTime.ParseExact(array, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
			newEndDate = endDate.AddDays(1).AddHours(7);
			newInitialDate = initialDate.AddHours(7);
			diff = newEndDate - newInitialDate;
		}
		private static Shift GetShift(DateTime initialDate, DateTime endDate, string array) {
			Shift shiftOfHistoryDate;
			ParseDates(initialDate, endDate, array, out DateTime historyDate, out DateTime newEndDate, out DateTime newInitialDate, out TimeSpan diff);
			if (initialDate == endDate)
				shiftOfHistoryDate = GetShiftFromHistoryDate(historyDate, initialDate);
			else {
				TimeSpan diffHistory = historyDate - newInitialDate;
				if ((diffHistory <= diff) && (historyDate >= newInitialDate && historyDate < newEndDate))
					shiftOfHistoryDate = GetShiftFromHistoryTime(historyDate);
				else
					shiftOfHistoryDate = Shift.nill;
			}
			return shiftOfHistoryDate;
		}
		/// <summary>
		/// Consider full dates, to calculate the shift
		/// </summary>
		private static Shift GetShiftFromHistoryDate(DateTime historyDate, DateTime initialDate) {
			Shift shiftOfHistoryDate;
			DateTime shiftOneInit = initialDate.AddHours(7);
			DateTime shiftTwoInit = shiftOneInit.AddHours(8);
			DateTime shiftThreeInit = shiftTwoInit.AddHours(8);
			DateTime shiftThreeEnd = shiftThreeInit.AddHours(8);
			if (historyDate >= shiftOneInit && historyDate < shiftTwoInit)
				shiftOfHistoryDate = Shift.shiftOne;
			else if (historyDate >= shiftTwoInit && historyDate < shiftThreeInit)
				shiftOfHistoryDate = Shift.shiftTwo;
			else if (historyDate >= shiftThreeInit && historyDate < shiftThreeEnd)
				shiftOfHistoryDate = Shift.shiftThree;
			else
				shiftOfHistoryDate = Shift.nill;
			return shiftOfHistoryDate;
		}
		/// <summary>
		/// Consider only time, not dates, to calculate the shift
		/// </summary>
		private static Shift GetShiftFromHistoryTime(DateTime historyDate) {
			Shift shiftFromHistoryTime;
			string stringHistoryTime = historyDate.ToString("HH:mm:ss");
			TimeSpan historyTime = TimeSpan.Parse(stringHistoryTime);
			TimeSpan shiftOneTimeInit = TimeSpan.Parse("07:00:00");
			TimeSpan shiftTwoTimeInit = TimeSpan.Parse("15:00:00");
			TimeSpan shiftThreeTimeInit = TimeSpan.Parse("23:00:00");
			if (historyTime >= shiftOneTimeInit && historyTime < shiftTwoTimeInit)
				shiftFromHistoryTime = Shift.shiftOne;
			else if (historyTime >= shiftTwoTimeInit && historyTime < shiftThreeTimeInit)
				shiftFromHistoryTime = Shift.shiftTwo;
			else if (historyTime >= shiftThreeTimeInit || historyTime < shiftOneTimeInit)
				shiftFromHistoryTime = Shift.shiftThree;
			else
				shiftFromHistoryTime = Shift.nill;
			return shiftFromHistoryTime;
		}
		private static void AddArrayToList(ref List<History> history, string[] array) {
			try {
				history.Add(new History() {
					Name = array[0],
					PackNmbr = array[1],
					TubeAm = array[2],
					Weight = array[3],
					Created = array[4],
				});
			}
			catch (IndexOutOfRangeException) { }
		}
	}
}
