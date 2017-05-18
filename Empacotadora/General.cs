using System;
using System.IO;
using System.Collections.Generic;

namespace Empacotadora
{
	class General
    {
		public static OrderDetails currentOrder;
		/// <summary>
		/// Appends the string to the end of the file
		/// </summary>
		public static string WriteToFile(string path, string stringToSave)
        {
            string msg = "tentou gravar mas sem sucesso";
            try
            {
                File.AppendAllText(path, stringToSave);
            }
            catch (Exception exc)
            {
                if (exc is IOException || exc is DirectoryNotFoundException || exc is UnauthorizedAccessException)
                    return msg = "Erro ao gravar ficheiro";
                else
                    return msg;
            }
            return msg = "Guardado com sucesso";
        }
		/// <summary>
		/// Rewrites the entire file with the argument List<string>
		/// </summary>
		public static string WriteToFile(string path, List<string> stringToSave)
		{
			string msg = "tentou gravar mas sem sucesso";
			try
			{
				File.WriteAllLines(path, stringToSave);
			}
			catch (Exception exc)
			{
				if (exc is IOException || exc is DirectoryNotFoundException || exc is UnauthorizedAccessException)
					return msg = "Erro ao gravar ficheiro";
				else
					return msg;
			}
			return msg = "Guardado com sucesso";
		}
	}
}