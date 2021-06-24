using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Aletheia.WorksheetParser.import
{
    /// <summary>
    /// Little customized class to read FaultLocalization
    /// This class is created as the FaultLocalization file has header row to be processed separately
    /// </summary>
    public class FaultLocalizationCsvSheetReader
    {
        private readonly char separator;

        private readonly string path;

        private readonly List<string> linesOfTheSheet;
        private DataTable table;
        private string[] columnNames;
        /// <summary>
        /// the constructor initializes member variables
        /// </summary>
        /// <param name="path">Path to FaultLocalization</param>
        /// <param name="separator">Separator used in the FaultLocalization</param>
        public FaultLocalizationCsvSheetReader(string path, char separator)
        {
            this.path = path;
            linesOfTheSheet = new List<string>();
            this.separator = separator;
        }
        /// <summary>
        /// reads the FaultLocalization file and add the lines to the data row
        /// </summary>
        private void ReadFile()
        {
            string line;
            int counter = 0;

            StreamReader file = new StreamReader(path);

            while ((line = file.ReadLine()) != null)
            {
                linesOfTheSheet.Add(line);
                counter++;
            }

            file.Close();
        }
        /// <summary>
        /// get method to access member varible
        /// </summary>
        /// <returns>Returns data table</returns>
        public DataTable getDataTable()
        {
            return table;
        }
        /// <summary>
        /// Parse the FaultLocalization
        /// use the first line to generate data table template\
        /// then parse the rest of the lines
        /// </summary>
        public void ParseSheet()
        {
            if (File.Exists(path))
            {
                ReadFile();
            }
            else
            {
                throw new Exception();
            }

            GenerateDataTable(linesOfTheSheet.ElementAt(0));

            for (int i = 1; i < linesOfTheSheet.Count; i++)
            {
                ParseLine(linesOfTheSheet.ElementAt(i));
            }
        }

        /// <summary>
        /// Parse a single line and put it to data row
        /// </summary>
        /// <param name="line">Line to be parsed from FaultLocalization</param>
        private void ParseLine(string line)
        {
            string[] values = line.Split(separator);
            DataRow row = table.NewRow();

            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i];
                string columnName = columnNames[i];
 
                    row[columnName] = value;
                
            }

            table.Rows.Add(row);
        }
        /// <summary>
        /// generates data table with the column names from the first line from FaultLocalization
        /// </summary>
        /// <param name="line">Line containing column names</param>
        private void GenerateDataTable(string line)
        {
            table = new DataTable();

            columnNames = line.Split(separator);

            DataColumn col = new DataColumn();
            col.DataType = Type.GetType("System.String");
            col.ColumnName = columnNames[0].Trim();
            table.Columns.Add(col);

            for (int i = 1; i < columnNames.Length; i++)
            {
                DataColumn column = new DataColumn();
                column.DataType = Type.GetType("System.String");
                column.ColumnName = columnNames[i].Trim();

                    
                table.Columns.Add(column);
            }
        }
    }
}
