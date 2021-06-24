using Aletheia.CommandLine.output;
using Aletheia.WorksheetParser.export;
using Aletheia.WorksheetParser.import;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aletheia.ExamScore
{

    public class ExameScoreCalculation
    {
        readonly string inputPath;
        readonly string destination;
        readonly char separator;


        public ExameScoreCalculation(string inputPath, string destination, char separator)
        {
            if (Directory.Exists(inputPath))
            {
                this.inputPath = inputPath;
                this.destination = destination;
                this.separator = separator;
            }
            else
            {
                throw new Exception("Directory doesnt exists!");
            }
        }

        public void CalculateExamScore()
        {
            DataTable ExamScoreDatatable = new DataTable();
            string[] columnNames = { "Ranking Metric", "ExamScore" };

            DataColumn col = new DataColumn
            {
                DataType = Type.GetType("System.String"),
                ColumnName = columnNames[0].Trim()
            };
            ExamScoreDatatable.Columns.Add(col);

            DataColumn col2 = new DataColumn
            {
                DataType = Type.GetType("System.String"),
                ColumnName = columnNames[1].Trim()
            };
            ExamScoreDatatable.Columns.Add(col2);

            DirectoryInfo d = new DirectoryInfo(inputPath);
            foreach (var file in d.GetFiles("*.csv"))
            {
                string toPrint = null;

                FaultLocalizationCsvSheetReader reader = new FaultLocalizationCsvSheetReader(file.FullName, separator);
                reader.ParseSheet();
                DataTable dataTable = reader.getDataTable();

                var allStatements = dataTable.AsEnumerable()
                .Select(x => x.Field<string>(0))
                .ToList();

                double examScore = allStatements.Sum(x => double.Parse(x) / allStatements.Count) / allStatements.Count;

                toPrint = "--------------------------------------------------" +
                    "\n" +
                    "Exam Score for " + file.Name + "\n" + "\n" + "\n"
                + examScore.ToString()
                + "\n--------------------------------------------------" + "\n" + "\n";

                DataRow row = ExamScoreDatatable.NewRow();
                row["Ranking Metric"] = file.Name;
                row["ExamScore"] = examScore;
                ExamScoreDatatable.Rows.Add(row);

                CommandLinePrinter.printToCommandLine(toPrint);
            }

            CsvSheetWriter writer = new CsvSheetWriter(Path.Combine(destination, "ExamScore.csv"), separator, ExamScoreDatatable);
            writer.WriteToWorkSheet();
        }
    }

}
