﻿using Aletheia.CommandLine.input;
using Aletheia.CommandLine.output;
using Aletheia.HitSpectra.persistence;
using Aletheia.HitSpectra.persistence.cobertura;
using Aletheia.WorksheetParser.export;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Aletheia.HitSpectra
{   
    /// <summary>
    /// This class holds necessary variable and methods for creating hit spectra data
    /// </summary>
    public class Spectralizer
    {
        private Dictionary<string, CommandLineArgument> commandLineArguments;
        //! An Boolean.
        /*!  if DEBUG is true, it will print more log. */
        private bool DEBUG = false;
        //! A Project type object.
        /*!  it holds the object extracted from .vcxproj file. */
        private Project baseProject = null; // 
        private string projectPath = null; // path to project directory
        private string srcDir = null; // path to cource code
        private string outDir = null; // where to put the output e.g. spectra, clustering and fault localization
        private string projName = "opencv"; // project name, default is opencv, it can be changed from command line
        private string gtestPath = null; // path to gtest.exe
        /// <summary>
        /// Aletheia started with propritary closed source BMW project. After that we moved to Google
        /// Chromium project, as the OpenCppCoverage tool was unable to produce coverage for Chromium project we have to move
        /// to OpenCV project. I worked around 4 months for Google Chromium project and created a some functions. I haven't 
        /// deleted the functions hoping that some other developer might find some coverage tool to work with Google Chromium project. 
        /// For standard visual studio project , the projectMode=default, for chromium project projectMode=chromium, 
        /// OpenCV is almost standard visual studio project except all the binaries are generated in the same folder instead
        /// in their own project folder. 
        /// </summary>
        private string projectMode = "default"; 
        private static string ninjaName = "";
        private static string testType = "";
        private char separator = ';';
        private int degreeOfParallelism = 1; //Explicit: Amount of Threads used to execute tests
        private bool deleteAnalData = true; //Explicit: Indicator if the created coverage-analysis should be deleted
        private int executionTimeout = 5;  //Explicit: Amount of time used as timout for the unittest-execution
        // exportpattern
        private bool argFunctionHitSpectraMatrix = true;
        private bool argInvokedFunctionsHitSpectraMatrix = true;
        private bool argInvokedFunctionsWithParametersHitSpectraMatrix = true;
        private bool argCountingFunctionInvokationsHitSpectraMatrix = true;
        private bool argLineCoverageHitSpectraMatrix = true;


        private string workingDir;

        // variables for test execution and evaluation
        private ConcurrentDictionary<string, RunResult> testExecutionIndex;
        //private Dictionary<string, SourceFile> repository;
        private Dictionary<string, Dictionary<string, SourceFile>> grandRepo;
        private HashSet<Block> allFunctions;
        private Dictionary<Class, Dictionary<Block, bool>> sourceFileSpectras;
        private List<TestcaseContext> executedTestcases;
        private int totalNumberOfTestcases;

        private DataTable functionHitSpectraMatrix;
        private DataTable invokedFunctionsHitSpectraMatrix;
        private DataTable invokedFunctionsWithParametersHitSpectraMatrix;
        private DataTable countingFunctionInvokationsHitSpectraMatrix;
        private DataTable lineCoverageHitSpectraMatrix;
        private List<string> blackList;
        /// <summary>
        /// constructor initializes some member variables. It extracts data from the command line parameter and stores them 
        /// as per necessity
        /// </summary>
        /// <param name="commandLineArguments">Command Line argument passed from the main program</param>
        /// <param name="parentPath">Working directory</param>
        public Spectralizer(Dictionary<string, CommandLineArgument> commandLineArguments, string parentPath)
        {
            this.commandLineArguments = commandLineArguments;
            this.outDir = parentPath;
    
 

            //--> Check if there is a valid installation of OpenCppCoverage
            if (!checkIfOpenCppCoverageIsInstalledOnHostPC()) { return; }

            // Read command line arguments and store them to the corrseponding members of Spectralizer
            if (!readCommandLineInputParameters()) { return; }

            createWorkingDirectory();

            if (!validateInputParameters()) { return; }

            if (DEBUG)
                Console.WriteLine("Parameters Validated\n");
            string toPrint = null;
            //-->Sanity-Check of project-directories passed
            if (projectMode.Equals("default"))
            {
                toPrint = "Coverage-analysis for " + baseProject.Name
                    + "\n--------------------------------------------------"
                    + "\nStep 1: Using gtest-executable located at:\n"
                    + baseProject.GetExecutable() + "\n";
            }
            else if (projectMode.Equals("chromium"))
            {
                string[] tmp = gtestPath.Split('\\');
                int len = tmp.Length;
                string gtestProject = tmp[len - 1].Split('.')[0];
                toPrint = "Coverage Analysis for " + gtestProject +
                   "\n--------------------------------\n" + "\nLocated at: " + gtestPath + "\n";
            }
            else
            {
                toPrint = "Nothing to print till now";
            }

            CommandLinePrinter.printToCommandLine(toPrint);
        }
        /// <summary>
        /// get method for functionHitSpectraMatrix
        /// </summary>
        public DataTable FunctionHitSpectraMatrix
        {
            get { return functionHitSpectraMatrix; }
        }
        /// <summary>
        /// get method for invokedFunctionsHitSpectraMatrix
        /// </summary>
        public DataTable InvokedFunctionsHitSpectraMatrix
        {
            get { return invokedFunctionsHitSpectraMatrix; }
        }
        /// <summary>
        /// get method for invokedFunctionsWithParametersHitSpectraMatrix
        /// </summary>
        public DataTable InvokedFunctionsWithParametersHitSpectraMatrix
        {
            get { return invokedFunctionsWithParametersHitSpectraMatrix; }
        }
        /// <summary>
        /// get method for countingFunctionInvokationsHitSpectraMatrix
        /// </summary>
        public DataTable CountingFunctionInvokationsHitSpectraMatrix
        {
            get { return countingFunctionInvokationsHitSpectraMatrix; }
        }
        /// <summary>
        /// lineCoverageHitSpectraMatrix
        /// </summary>
        public DataTable LineCoverageHitSpectraMatrix
        {
            get { return lineCoverageHitSpectraMatrix; }
        }
        /// <summary>
        /// this method calls methods to execute all the test cases, analyzes the openCppCoverage files, 
        /// parse all the test case touched functions,build hit spectra data table, and finally deletes 
        /// the temporary coverage file if commanded so (default is to delete temporary coverage files)
        /// </summary>
        public void executeTestSuite()
        {
            //repository = new Dictionary<string, SourceFile>();
            grandRepo = new Dictionary<string, Dictionary<string, SourceFile>>();
            testExecutionIndex = new ConcurrentDictionary<string, RunResult>(degreeOfParallelism, 1);
            allFunctions = new HashSet<Block>();
            //trimTestCases();
            executeTestcases();
            if (DEBUG)
                Console.WriteLine("Execution of test cases complete\n");
            #region Generic Analysis stuff
            analyzeOpenCppCoverageFiles();
            if (DEBUG)
                Console.WriteLine("Analysis of OpenCpp Coverage file is complete\n");
            parseAllByTheTestcasesTouchedFunctions();
            if (DEBUG)
                Console.WriteLine("Parsing of blocks touched by testcases complete\n");
            #endregion

            if (argFunctionHitSpectraMatrix)
            {
                #region Function Hit Spectra Matrix
                buildFunctionHitSpectraDataTable();
                if (DEBUG)
                    Console.WriteLine("Function Hit spectra Table built\n");
                buildFunctionHitSpectraMatrix();
                if (DEBUG)
                    Console.WriteLine("Function Hit spectra matrix build\n");
                removeUselessColumns(functionHitSpectraMatrix);
                if (DEBUG)
                    Console.WriteLine("Useless column removed from function spectra\n");
                removeTEST_FColumns(functionHitSpectraMatrix);
                if (DEBUG)
                    Console.WriteLine("Test_F TEST_P column removed from function spectra\n");
                #endregion
            }

            if (argCountingFunctionInvokationsHitSpectraMatrix)
            {
                #region Counting Function Invokations HitSpectra-Matrix
                buildCountingFunctionInvokationsHitSpectraDataTable();
                if (DEBUG)
                    Console.WriteLine("Counting Function Hit spectra Table built\n");
                buildCountingFunctionInvokationsHitSpectraMatrix();
                if (DEBUG)
                    Console.WriteLine("counting Function Hit spectra matrix built\n");
                //removeUselessColumns(countingFunctionInvokationsHitSpectraMatrix);
                removeTEST_FColumns(countingFunctionInvokationsHitSpectraMatrix);
                if (DEBUG)
                    Console.WriteLine("Test_F TEST_P column removed from counting function spectra\n");
                #endregion
            }

            if (argLineCoverageHitSpectraMatrix)
            {
                #region Line Coverage Hit Spectra Matrix
                buildLineCoverageHitSpectraDataTable();
                if (DEBUG)
                    Console.WriteLine("Built line coverage HitSpectra table\n");
                buildLineCoverageHitSpectraMatrix();
                if (DEBUG)
                    Console.WriteLine("Built Line coverage Hit Spectra matrix\n");
                removeUselessColumns(lineCoverageHitSpectraMatrix);
                if (DEBUG)
                    Console.WriteLine("Removed useless column from Hit spectra of Line coverage\n");
                removeTEST_FColumns(lineCoverageHitSpectraMatrix);
                if (DEBUG)
                    Console.WriteLine("Test_F TEST_P column removed from line coverage spectra\n");
                #endregion
            }

            if (argInvokedFunctionsHitSpectraMatrix)
            {
                #region Invoked Function Hit Spectra Matrix
                buildInvokedFunctionHitSpectraDataTable();
                if (DEBUG)
                    Console.WriteLine("Built Invoked Function Hit Spectra Data Table\n");
                buildInvokedFunctionsHitSpectraMatrix();
                if (DEBUG)
                    Console.WriteLine("Built Invoked Function Hit Spectra Matrix\n");
                removeUselessColumns(invokedFunctionsHitSpectraMatrix);
                if (DEBUG)
                    Console.WriteLine("Removed useless column from Invoked Coverage\n");
                removeTEST_FColumns(invokedFunctionsHitSpectraMatrix);
                if (DEBUG)
                    Console.WriteLine("Removet Test_F Test_P from column for Invoked Function coverage\n");
                #endregion
            }

            if (argInvokedFunctionsWithParametersHitSpectraMatrix)
            {
                #region Invoked Function with Parameters Hit Spectra Matrix
                buildInvokedFunctionWithParametersHitSpectraDataTable();
                if (DEBUG)
                    Console.WriteLine("Built Invoked Function with param Hit Spectra Data Table\n");
                buildInvokedFunctionsWithParametersHitSpectraMatrix();
                if (DEBUG)
                    Console.WriteLine("Built Invoked Function with param Hit Spectra Matrix\n");
                removeUselessColumns(invokedFunctionsWithParametersHitSpectraMatrix);
                if (DEBUG)
                    Console.WriteLine("Removed useless column from Invoked function with param Coverage\n");
                removeTEST_FColumns(invokedFunctionsWithParametersHitSpectraMatrix);
                if (DEBUG)
                    Console.WriteLine("Removet Test_F Test_P from column for Invoked Function with param coverage\n");
                #endregion
            }

            if (deleteAnalData)
            {
                ProjectConfig.DeleteAnalData(workingDir);
            }
        }
        /// <summary>
        /// exports hitSpectra as csv file
        /// </summary>
        public void exportHitSpectraMatrices()
        {
            string pathFunctionHitSpectraMatrix = workingDir + "\\function_spectra.csv";
            string pathInvokedFunctionsHitSpectraMatrix = workingDir + "\\functions_call_hit_spectra.csv";
            string pathInvokedFunctionsWithParametersHitSpectraMatrix = workingDir + "\\functions_call_with_parameters_hit_spectra.csv";
            string pathCountingFunctionInvokationsHitSpectraMatrix = workingDir + "\\function_call_count_hit_spectra_matrix.csv";
            string pathLineCoverageHitSpectraMatrix = workingDir + "\\statement_hit_spectra.csv";

            CsvSheetWriter writer;
            //argFunctionHitSpectraMatrix = false;
            if (argFunctionHitSpectraMatrix)
            {
                #region Export Function Hit-Spectra-Matrix
                functionHitSpectraMatrix.Columns["Result"].SetOrdinal(functionHitSpectraMatrix.Columns.Count - 1);
                writer = new CsvSheetWriter(pathFunctionHitSpectraMatrix, separator, functionHitSpectraMatrix);
                writer.WriteToWorkSheet();
                if (DEBUG)
                    Console.WriteLine("Function Hit Spectra is written to file\n");
                #endregion
            }
            
            if (argInvokedFunctionsHitSpectraMatrix)
            {
                #region Export Invoked Functions Hit-Spectra-Matrix
                invokedFunctionsHitSpectraMatrix.Columns["Result"].SetOrdinal(invokedFunctionsHitSpectraMatrix.Columns.Count - 1);
                writer = new CsvSheetWriter(pathInvokedFunctionsHitSpectraMatrix, separator, invokedFunctionsHitSpectraMatrix);
                writer.WriteToWorkSheet();
                if (DEBUG)
                    Console.WriteLine("Invoked Function Hit Spectra is written to file\n");
                #endregion
            }

            if (argInvokedFunctionsWithParametersHitSpectraMatrix)
            {
                #region Export Invoked Functions With Parameters Hit-Spectra-Matrix
                invokedFunctionsWithParametersHitSpectraMatrix.Columns["Result"].SetOrdinal(invokedFunctionsWithParametersHitSpectraMatrix.Columns.Count - 1);
                writer = new CsvSheetWriter(pathInvokedFunctionsWithParametersHitSpectraMatrix, separator, invokedFunctionsWithParametersHitSpectraMatrix);
                writer.WriteToWorkSheet();
                if (DEBUG)
                    Console.WriteLine("Invoked Function with param Hit Spectra is written to file\n");
                #endregion
            }

            if (argCountingFunctionInvokationsHitSpectraMatrix)
            {
                #region Export Counting Function Invokations Hit-Spectra-Matrix
                countingFunctionInvokationsHitSpectraMatrix.Columns["Result"].SetOrdinal(countingFunctionInvokationsHitSpectraMatrix.Columns.Count - 1);
                writer = new CsvSheetWriter(pathCountingFunctionInvokationsHitSpectraMatrix, separator, countingFunctionInvokationsHitSpectraMatrix);
                writer.WriteToWorkSheet();
                if (DEBUG)
                    Console.WriteLine("Coungint function Hit Spectra is written to file\n");
                #endregion
            }

            if (argLineCoverageHitSpectraMatrix)
            {
                #region Export Line Coverage Hit-Spectra-Matrix
                lineCoverageHitSpectraMatrix.Columns["Result"].SetOrdinal(lineCoverageHitSpectraMatrix.Columns.Count - 1);
                writer = new CsvSheetWriter(pathLineCoverageHitSpectraMatrix, separator, lineCoverageHitSpectraMatrix);
                writer.WriteToWorkSheet();
                if (DEBUG)
                    Console.WriteLine("Line Hit Spectra is written to file\n");
                #endregion
            }

        }

        #region Build DataTable structures
        /// <summary>
        /// creates hit spectra for function level coverage
        /// </summary>
        private void buildFunctionHitSpectraDataTable()
        {
            if (functionHitSpectraMatrix == null)
                functionHitSpectraMatrix = new DataTable();

            if (!functionHitSpectraMatrix.Columns.Contains("Index"))
            {
                DataColumn testcaseColumn = new DataColumn();
                testcaseColumn.DataType = Type.GetType("System.String");
                testcaseColumn.ColumnName = "Index";
                testcaseColumn.DefaultValue = "No Index defined";
                functionHitSpectraMatrix.Columns.Add(testcaseColumn);
                if (DEBUG)
                    Console.WriteLine("Index column created (FC)\n");
            }

            if (!functionHitSpectraMatrix.Columns.Contains("Result"))
            {
                DataColumn testcaseColumn = new DataColumn();
                testcaseColumn.DataType = Type.GetType("System.Int32");
                testcaseColumn.ColumnName = "Result";
                testcaseColumn.DefaultValue = 0;
                functionHitSpectraMatrix.Columns.Add(testcaseColumn);
                if (DEBUG)
                    Console.WriteLine("Result Column created (FC)\n");
            }


            foreach (Block function in allFunctions)
            {
                string fctName = function.ConcatenatedName;

                if (!functionHitSpectraMatrix.Columns.Contains(fctName))
                {
                    DataColumn fctColumn = new DataColumn();
                    fctColumn.DataType = Type.GetType("System.Int32");
                    fctColumn.ColumnName = fctName;
                    fctColumn.ExtendedProperties.Add("Function", function);
                    fctColumn.DefaultValue = 0;
                    functionHitSpectraMatrix.Columns.Add(fctColumn);
                }
                if (DEBUG)
                    Console.WriteLine("Columns for data table created(FC)\n");
            }
        }
        /// <summary>
        /// build hit spectra for invoked function level coverage
        /// </summary>
        private void buildInvokedFunctionHitSpectraDataTable()
        {
            if (invokedFunctionsHitSpectraMatrix == null) { invokedFunctionsHitSpectraMatrix = new DataTable(); }

            if (!invokedFunctionsHitSpectraMatrix.Columns.Contains("Index"))
            {
                DataColumn testcaseColumn = new DataColumn();
                testcaseColumn.DataType = Type.GetType("System.String");
                testcaseColumn.ColumnName = "Index";
                testcaseColumn.DefaultValue = "No Index defined";
                invokedFunctionsHitSpectraMatrix.Columns.Add(testcaseColumn);
                if (DEBUG)
                    Console.WriteLine("Index column created (IFC)\n");

            }

            if (!invokedFunctionsHitSpectraMatrix.Columns.Contains("Result"))
            {
                DataColumn testcaseColumn = new DataColumn();
                testcaseColumn.DataType = Type.GetType("System.Int32");
                testcaseColumn.ColumnName = "Result";
                testcaseColumn.DefaultValue = 0;
                invokedFunctionsHitSpectraMatrix.Columns.Add(testcaseColumn);
                if (DEBUG)
                    Console.WriteLine("Result column created (IFC)\n");
            }
        }
        /// <summary>
        /// add colum to invokedFunctionHitSpectraDataTable
        /// </summary>
        /// <param name="columnName">Column name to be added</param>
        private void addColumnToInvokedFunctionHitSpectraDataTable(string columnName)
        {
            if (!invokedFunctionsHitSpectraMatrix.Columns.Contains(columnName))
            {
                DataColumn fctColumn = new DataColumn();
                fctColumn.DataType = Type.GetType("System.Int32");
                fctColumn.ColumnName = columnName;
                fctColumn.DefaultValue = 0;
                invokedFunctionsHitSpectraMatrix.Columns.Add(fctColumn);
            }
        }
        /// <summary>
        /// build InvokedFunctionWithParametersHitSpectraDataTable
        /// </summary>
        private void buildInvokedFunctionWithParametersHitSpectraDataTable()
        {
            if (invokedFunctionsWithParametersHitSpectraMatrix == null) { invokedFunctionsWithParametersHitSpectraMatrix = new DataTable(); }

            if (!invokedFunctionsWithParametersHitSpectraMatrix.Columns.Contains("Index"))
            {
                DataColumn testcaseColumn = new DataColumn();
                testcaseColumn.DataType = Type.GetType("System.String");
                testcaseColumn.ColumnName = "Index";
                testcaseColumn.DefaultValue = "No Index defined";
                invokedFunctionsWithParametersHitSpectraMatrix.Columns.Add(testcaseColumn);
                if (DEBUG)
                    Console.WriteLine("Index column created (IFCP)\n");
            }

            if (!invokedFunctionsWithParametersHitSpectraMatrix.Columns.Contains("Result"))
            {
                DataColumn testcaseColumn = new DataColumn();
                testcaseColumn.DataType = Type.GetType("System.Int32");
                testcaseColumn.ColumnName = "Result";
                testcaseColumn.DefaultValue = 0;
                invokedFunctionsWithParametersHitSpectraMatrix.Columns.Add(testcaseColumn);
                if (DEBUG)
                    Console.WriteLine("Result column created (IFCP)\n");
            }
        }
        /// <summary>
        /// add Column To InvokedFunctionWithParametersHitSpectraDatatable
        /// </summary>
        /// <param name="columnName">the column to be added</param>
        private void addColumnToInvokedFunctionWithParametersHitSpectraDatatable(string columnName)
        {
            if (!invokedFunctionsWithParametersHitSpectraMatrix.Columns.Contains(columnName))
            {
                DataColumn fctColumn = new DataColumn();
                fctColumn.DataType = Type.GetType("System.Int32");
                fctColumn.ColumnName = columnName;
                fctColumn.DefaultValue = 0;
                invokedFunctionsWithParametersHitSpectraMatrix.Columns.Add(fctColumn);
            }
        }
        /// <summary>
        /// build CountingFunctionInvokationsHitSpectraDataTable
        /// </summary>
        private void buildCountingFunctionInvokationsHitSpectraDataTable()
        {
            if (countingFunctionInvokationsHitSpectraMatrix == null) countingFunctionInvokationsHitSpectraMatrix = new DataTable();

            if (!countingFunctionInvokationsHitSpectraMatrix.Columns.Contains("Index"))
            {
                DataColumn testcaseColumn = new DataColumn();
                testcaseColumn.DataType = Type.GetType("System.String");
                testcaseColumn.ColumnName = "Index";
                testcaseColumn.DefaultValue = "No Index defined";
                countingFunctionInvokationsHitSpectraMatrix.Columns.Add(testcaseColumn);
                if (DEBUG)
                    Console.WriteLine("Index column created (CF)\n");
            }

            if (!countingFunctionInvokationsHitSpectraMatrix.Columns.Contains("Result"))
            {
                DataColumn testcaseColumn = new DataColumn();
                testcaseColumn.DataType = Type.GetType("System.Int32");
                testcaseColumn.ColumnName = "Result";
                testcaseColumn.DefaultValue = 0;
                countingFunctionInvokationsHitSpectraMatrix.Columns.Add(testcaseColumn);
                if (DEBUG)
                    Console.WriteLine("Result column created (CF)\n");
            }

            foreach (Block function in allFunctions)
            {
                string fctName = function.ConcatenatedName;

                if (!countingFunctionInvokationsHitSpectraMatrix.Columns.Contains(fctName))
                {
                    DataColumn fctColumn = new DataColumn();
                    fctColumn.DataType = Type.GetType("System.Int32");
                    fctColumn.ColumnName = fctName;
                    fctColumn.ExtendedProperties.Add("Function", function);
                    fctColumn.DefaultValue = 0;
                    countingFunctionInvokationsHitSpectraMatrix.Columns.Add(fctColumn);
                }
            }
            if (DEBUG)
                Console.WriteLine("Function column created (CF)\n");
        }
        /// <summary>
        /// build LineCoverageHitSpectraDataTable
        /// </summary>
        private void buildLineCoverageHitSpectraDataTable()
        {
            if (lineCoverageHitSpectraMatrix == null) lineCoverageHitSpectraMatrix = new DataTable();

            if (!lineCoverageHitSpectraMatrix.Columns.Contains("Index"))
            {
                DataColumn testcaseColumn = new DataColumn();
                testcaseColumn.DataType = Type.GetType("System.String");
                testcaseColumn.ColumnName = "Index";
                testcaseColumn.DefaultValue = "No Index defined";
                lineCoverageHitSpectraMatrix.Columns.Add(testcaseColumn);
                if (DEBUG)
                    Console.WriteLine("Index column created (LC)\n");
            }

            if (!lineCoverageHitSpectraMatrix.Columns.Contains("Result"))
            {
                DataColumn testcaseColumn = new DataColumn();
                testcaseColumn.DataType = Type.GetType("System.Int32");
                testcaseColumn.ColumnName = "Result";
                testcaseColumn.DefaultValue = 0;
                lineCoverageHitSpectraMatrix.Columns.Add(testcaseColumn);
                if (DEBUG)
                    Console.WriteLine("Result column created (LC)\n");
            }
        }
        /// <summary>
        /// add Column To LineCoverageHitSpectraDataTable
        /// </summary>
        /// <param name="columnName">column  to be added</param>
        private void addColumnToLineCoverageHitSpectraDataTable(string columnName)
        {
            if (!lineCoverageHitSpectraMatrix.Columns.Contains(columnName))
            {
                DataColumn lineHitColumn = new DataColumn();
                lineHitColumn.DataType = Type.GetType("System.Int32");
                lineHitColumn.ColumnName = columnName;
                lineHitColumn.DefaultValue = 0;
                lineCoverageHitSpectraMatrix.Columns.Add(lineHitColumn);
            }
        }
        #endregion

        #region Generic Analyzing stuff
        /// <summary>
        /// analyze openCppcoverage files to extract hitspectra
        /// </summary>
        private void analyzeOpenCppCoverageFiles()
        {
            Console.WriteLine("Number of context: "+executedTestcases.Count());
            foreach (TestcaseContext testcaseContext in executedTestcases)
            {
                string nextUnitTestIdent = testcaseContext.Name;
                RunResult result = testcaseContext.Result;
                Coverage testCoverage = testcaseContext.TestCoverage;
                Dictionary<string, SourceFile> repository = new Dictionary<string, SourceFile>();
                if (testCoverage.Package.Length > 0)
                {
                    Console.WriteLine("Number of Packages to be extracted: "+testCoverage.Package.Length+"\n");
                    for (int a = 0; a < testCoverage.Package.Length; a++)
                    {
                        foreach (Class tmpClass in testCoverage.Package[a].SourceFiles)
                        {
                            #region all by the testcases touched sourcefiles are stored in Dictionary 'repository'
                            //Step 2.1: Add the Source-File to the repository to prevent multiple instantiation of the same file

                            if (!repository.ContainsKey(tmpClass.FileName))
                            {
                                if (File.Exists(Path.GetPathRoot(srcDir) + tmpClass.FilePath))
                                    repository[tmpClass.FileName] = new SourceFile(Path.GetPathRoot(srcDir) + tmpClass.FilePath);
                                else
                                    Console.WriteLine("File not found: " + Path.GetPathRoot(srcDir) + tmpClass.FilePath);
                            }
                            #endregion
                        }
                    }
                    grandRepo[nextUnitTestIdent] = repository;

                }
            }
        }

        /// <summary>
        /// This function fills the HashSet allFunctions and adds to every object of type TestcaseContext in List executedTestcases
        /// the list of functions that are invoked by the testcase
        /// </summary>
        private void parseAllByTheTestcasesTouchedFunctions()
        {
            sourceFileSpectras = new Dictionary<Class, Dictionary<Block, bool>>();

            foreach (TestcaseContext testcaseContext in executedTestcases)
            {
                string nextUnitTestIdent = testcaseContext.Name;
                RunResult result = testcaseContext.Result;
                Coverage testCoverage = testcaseContext.TestCoverage;

                Dictionary<Block, bool> testcaseSpectra = new Dictionary<Block, bool>();
                if (testCoverage.Package.Length > 0)
                {
                    Dictionary<string, SourceFile> repository = grandRepo[nextUnitTestIdent];
                    for (int a = 0; a < testCoverage.Package.Length; a++)
                    {
                        foreach (Class tmpClass in testCoverage.Package[a].SourceFiles)
                        {
                            string originalFilePath = "";
                            int overlap = srcDir.ToLower().IndexOf(tmpClass.FilePath.ToLower().Split('\\').First());
                            if (overlap != -1)
                                originalFilePath = srcDir.Substring(0, overlap) + tmpClass.FilePath;
                            else
                                originalFilePath = srcDir + tmpClass.FilePath;
                            //if (File.Exists(srcDir + tmpClass.FilePath))
                            if (File.Exists(originalFilePath))
                            {
                             //   if (tmpClass.FileName.Contains("global_motion.hpp"))
                               //     Console.WriteLine("hpp found");
                                Dictionary<Block, bool> srcFileSpectra = repository[tmpClass.FileName].BuildFctSpectraFromTrace(tmpClass);
                                foreach (Block tmpFunction in srcFileSpectra.Keys)
                                {
                                    if (srcFileSpectra[tmpFunction])
                                    {
                                        allFunctions.Add(tmpFunction);
                                        testcaseContext.AddFunction(tmpFunction, tmpClass);
                                    }

                                    if (!testcaseSpectra.ContainsKey(tmpFunction))
                                    {
                                        testcaseSpectra.Add(tmpFunction, srcFileSpectra[tmpFunction]);
                                    }
                                }

                                if (!sourceFileSpectras.Keys.Contains(tmpClass))
                                {
                                    sourceFileSpectras[tmpClass] = srcFileSpectra;
                                }
                            }
                        }
                    }
                    testcaseContext.AddTestcaseSpectra(testcaseSpectra);
                }
            }
        }
#endregion

#region Build the Hit Spectra Matrices
        /// <summary>
        /// creates function hitSpectra matrix from executed test cases
        /// </summary>
        private void buildFunctionHitSpectraMatrix()
        {
            foreach (TestcaseContext testcaseContext in executedTestcases)
            {
                DataRow datarow = functionHitSpectraMatrix.NewRow();
                datarow["Index"] = testcaseContext.Name;

                Dictionary<Block, bool> testcaseSpectra = testcaseContext.TestcaseSpectra;

                foreach (Block function in testcaseSpectra.Keys)
                {
                    if (testcaseSpectra[function] == true)
                    {
                        datarow[function.ConcatenatedName] = 1;
                    }
                }
                if (testcaseContext.Result == RunResult.PASSED) { datarow["Result"] = 1; }
                functionHitSpectraMatrix.Rows.Add(datarow);
            }
        }
        /// <summary>
        /// creates invoked function hitSpectra matrix from executed test cases
        /// </summary>
        private void buildInvokedFunctionsHitSpectraMatrix()
        {
            foreach (TestcaseContext testcaseContext in executedTestcases)
            {
                string nextUnitTestIdent = testcaseContext.Name;
                RunResult result = testcaseContext.Result;
                Coverage testCoverage = testcaseContext.TestCoverage;

                DataRow row = invokedFunctionsHitSpectraMatrix.NewRow();
                row["Index"] = nextUnitTestIdent;
                if (testCoverage.Package.Length > 0)
                {
                    for (int a = 0; a < testCoverage.Package.Length; a++)
                    {
                        foreach (Class tmpClass in testCoverage.Package[a].SourceFiles)
                        {
                            string originalFilePath = "";
                            int overlap = srcDir.ToLower().IndexOf(tmpClass.FilePath.ToLower().Split('\\').First());
                            if (overlap != -1)
                                originalFilePath = srcDir.Substring(0, overlap) + tmpClass.FilePath;
                            else
                                originalFilePath = srcDir + tmpClass.FilePath;
                            //if (File.Exists(srcDir + tmpClass.FilePath))
                            if (File.Exists(originalFilePath))
                            {
                                foreach (Line line in tmpClass.LinesOfCode)
                                {
                                    if (line.hits > 0)
                                    {
                                        foreach (Block function in sourceFileSpectras[tmpClass].Keys)
                                        {
                                            if (function.ContainsLineAndIsBlockInvokation(line.number))
                                            {
                                                List<InvokedFunction> functions = function.GetListOfInvokedFuncsInLine(line.number);
                                                string filename = tmpClass.FileName;
                                                foreach (InvokedFunction fct in functions)
                                                {
                                                    string columnEntry = filename + "_" + fct.Name + "_" + line.number;

                                                    addColumnToInvokedFunctionHitSpectraDataTable(columnEntry);

                                                    row[columnEntry] = 1;
                                                }
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (testcaseContext.Result == RunResult.PASSED) { row["Result"] = 1; }
                invokedFunctionsHitSpectraMatrix.Rows.Add(row);
            }
        }
        /// <summary>
        /// creates invoked function with param hitSpectra matrix from executed test cases
        /// </summary>
        private void buildInvokedFunctionsWithParametersHitSpectraMatrix()
        {
            foreach (TestcaseContext testcaseContext in executedTestcases)
            {
                string nextUnitTestIdent = testcaseContext.Name;
                RunResult result = testcaseContext.Result;
                Coverage testCoverage = testcaseContext.TestCoverage;

                DataRow row = invokedFunctionsWithParametersHitSpectraMatrix.NewRow();
                row["Index"] = nextUnitTestIdent;
                if (testCoverage.Package.Length > 0)
                {
                    for (int a = 0; a < testCoverage.Package.Length; a++)
                    {
                        foreach (Class tmpClass in testCoverage.Package[a].SourceFiles)
                        {
                            string originalFilePath = "";
                            int overlap = srcDir.ToLower().IndexOf(tmpClass.FilePath.ToLower().Split('\\').First());
                            if (overlap != -1)
                                originalFilePath = srcDir.Substring(0, overlap) + tmpClass.FilePath;
                            else
                                originalFilePath = srcDir + tmpClass.FilePath;
                            //if (File.Exists(srcDir + tmpClass.FilePath))
                            if (File.Exists(originalFilePath))
                            {
                                foreach (Line line in tmpClass.LinesOfCode)
                                {
                                    if (line.hits > 0)
                                    {
                                        foreach (Block function in sourceFileSpectras[tmpClass].Keys)
                                        {
                                            if (function.ContainsLineAndIsBlockInvokation(line.number))
                                            {
                                                List<InvokedFunction> functions = function.GetListOfInvokedFuncsInLine(line.number);
                                                string filename = tmpClass.FileName;
                                                foreach (InvokedFunction fct in functions)
                                                {
                                                    string columnEntry = filename + "_" + fct.NameWithParameters + "_" + line.number;

                                                    addColumnToInvokedFunctionWithParametersHitSpectraDatatable(columnEntry);

                                                    row[columnEntry] = 1;
                                                }
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (testcaseContext.Result == RunResult.PASSED) { row["Result"] = 1; }
                invokedFunctionsWithParametersHitSpectraMatrix.Rows.Add(row);
            }
        }
        /// <summary>
        /// creates counting function invokation hitSpectra matrix from executed test cases
        /// </summary>
        private void buildCountingFunctionInvokationsHitSpectraMatrix()
        {
            foreach (TestcaseContext testcaseContext in executedTestcases)
            {
                string testcaseName = testcaseContext.Name;
                RunResult result = testcaseContext.Result;
                Coverage testCoverage = testcaseContext.TestCoverage;
                DataRow datarow = countingFunctionInvokationsHitSpectraMatrix.NewRow();

                datarow["Index"] = testcaseName;
                if (testCoverage.Package.Length > 0)
                {
                    for (int a = 0; a < testCoverage.Package.Length; a++)
                    {
                        foreach (Class tmpClass in testCoverage.Package[a].SourceFiles)
                        {
                            foreach (Line line in tmpClass.LinesOfCode)
                            {
                                if (line.hits > 0)
                                {
                                    foreach (Block function in testcaseContext.getListOfFunctionsFromSourceFile(tmpClass))
                                    {
                                        if (function.ContainsLineAndIsBlockInvokation(line.number))
                                        {
                                            List<InvokedFunction> functions = function.GetListOfInvokedFuncsInLine(line.number);
                                            // string filename = tmpClass.FileName;
                                            foreach (InvokedFunction invokedFunction in functions)
                                            {
                                                foreach (Block fctFromAllFunctions in allFunctions)
                                                {
                                                    if (invokedFunction.CompareFunctionInvokationToFunction(fctFromAllFunctions))
                                                    {
                                                        int numberOInvokations = (int)datarow[fctFromAllFunctions.ConcatenatedName];
                                                        datarow[fctFromAllFunctions.ConcatenatedName] = numberOInvokations + 1;
                                                        break;
                                                    }
                                                }
                                            }
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (testcaseContext.Result == RunResult.PASSED) { datarow["Result"] = 1; }
                countingFunctionInvokationsHitSpectraMatrix.Rows.Add(datarow);
            }
        }
        /// <summary>
        /// creates line coverage hitSpectra matrix from executed test cases
        /// </summary>
        private void buildLineCoverageHitSpectraMatrix()
        {
            foreach (TestcaseContext testcaseContext in executedTestcases)
            {
                string nextUnitTestIdent = testcaseContext.Name;
                RunResult result = testcaseContext.Result;
                Coverage testCoverage = testcaseContext.TestCoverage;

                DataRow row = lineCoverageHitSpectraMatrix.NewRow();
                row["Index"] = nextUnitTestIdent;
                if (testCoverage.Package.Length > 0)
                {
                    for (int a = 0; a < testCoverage.Package.Length; a++)
                    {
                        foreach (Class tmpClass in testCoverage.Package[a].SourceFiles)
                        {
                            foreach (Line loc in tmpClass.LinesOfCode)
                            {
                                string lineIdent = tmpClass.FileName + "_" + loc.number;

                                addColumnToLineCoverageHitSpectraDataTable(lineIdent);

                                row[lineIdent] = loc.hits;
                            }
                        }
                    }
                }
                if (testcaseContext.Result == RunResult.PASSED) { row["Result"] = 1; }
                lineCoverageHitSpectraMatrix.Rows.Add(row);
            }
        }
#endregion
        /// <summary>
        /// limit the number of test cases
        /// this function was written for the debugging purpose, it has no use in real application
        /// </summary>
        private void trimTestCases()
        {
            baseProject.LimitTestCase();
        }
#region Remove useless columns in Hit Spectra Matrices
        /// <summary>
        /// removed blank or unused columsn
        /// </summary>
        /// <param name="dataTable">Hit Spectra Data table</param>
        private void removeUselessColumns(DataTable dataTable)
        {
            List<DataColumn> columnsToRemove = new List<DataColumn>();

            foreach (DataColumn column in dataTable.Columns)
            {
                string columnName = column.ColumnName;
                if (column.DataType == typeof(Int32) && !columnName.Equals("Result"))
                {
                    bool removeColumn = true;

                    foreach (DataRow row in dataTable.Rows)
                    {
                        int rowValue = (int)row[columnName];

                        if (rowValue != 0)
                        {
                            removeColumn = false;
                            break;
                        }
                    }

                    if (removeColumn)
                    {
                        columnsToRemove.Add(column);
                    }
                }
            }

            foreach (DataColumn column in columnsToRemove)
            {
                dataTable.Columns.Remove(column);
            }
        }
        /// <summary>
        /// remove unnecessary TEST_F columns
        /// </summary>
        /// <param name="dataTable">Hit Spectra Data Table</param>
        private void removeTEST_FColumns(DataTable dataTable)
        {
            List<DataColumn> columnsToRemove = new List<DataColumn>();

            foreach (DataColumn column in dataTable.Columns)
            {
                string columnName = column.ColumnName;
                if (columnName.Contains("TEST_F") || columnName.Contains("TEST_P"))
                {
                    columnsToRemove.Add(column);
                }
            }

            foreach (DataColumn column in columnsToRemove)
            {
                dataTable.Columns.Remove(column);
            }
        }
#endregion
        /// <summary>
        /// It extracts coverage name from the gtest exe
        /// </summary>
        /// <param name="path">path to exe</param>
        /// <returns></returns>
        private string getCovname(string path)
        {
            string []tmp = path.Split('\\');
            string exe = tmp[tmp.Length - 1];
            string[] exes = exe.Split('.');
                return exes[0];
        }
#region Executing testcases
        /// <summary>
        /// It executes test cases in a multithreaded environment, the default number of thread is 12
        /// </summary>
        private void executeTestcases()
        {
            executedTestcases = new List<TestcaseContext>();
            SemaphoreSlim _AccessConsoleSemaphore = new SemaphoreSlim(1);
            SemaphoreSlim _AccessExecutedTestcasesSemaphore = new SemaphoreSlim(1);
            Task[] taskPool = new Task[degreeOfParallelism];


            int testCount = 0;

            testCount = baseProject.UnitTestCount;
            int executedTests = 0 ;
            for (int x = 0; x < degreeOfParallelism; x++)
            {
                try
                {
                    
                    taskPool[x] = Task.Factory.StartNew(() =>
                    {
                        //Indicator if this task should terminate
                        bool finishExecution = false;

                        while (!finishExecution)
                        {
                            //If there is no TestCase -> finish execution

                            //finishExecution = baseProject.TestCaseCount == 0;



                            if (!finishExecution)
                            {
                                string nextUnitTestIdent = null;

                                nextUnitTestIdent = baseProject.PopNextUnitTest();


                                //If there is no nextTest to execute, stop execution
                                if (nextUnitTestIdent == null)
                                    break;
                                
                                

                                //Execute unit-test
                                Task<RunResult> currentTest;
                                
                                if (projectMode.Equals("default"))
                                    currentTest = executeUnitTestVS(projName, nextUnitTestIdent, workingDir, baseProject.GetExecutable(), executionTimeout);
                                else // if(projectMode.Equals("chromium"))
                                {
                                    currentTest = executeUnitTest(projName, nextUnitTestIdent, workingDir, gtestPath, executionTimeout);

                                }
                                
                                //Wait for the test to finish
                                currentTest.Wait();

                                testExecutionIndex[nextUnitTestIdent] = currentTest.Result;

                                if ((currentTest.Result == RunResult.FAILED || currentTest.Result == RunResult.PASSED))
                                {
                                    //Step 1: Deserialize the coverage-information
                                    try
                                    {
                                        XmlSerializer serializer = new XmlSerializer(typeof(Coverage));
                                        string covName = getCovname(baseProject.GetExecutable());
                                        StreamReader reader = new StreamReader(workingDir + "\\" + nextUnitTestIdent + "\\" + covName + "Coverage.xml");

                                        Coverage testCov = (Coverage)serializer.Deserialize(reader);
                                        reader.Close();

                                        TestcaseContext testcaseContext = new TestcaseContext(nextUnitTestIdent, currentTest.Result, testCov);
                                        _AccessExecutedTestcasesSemaphore.Wait();
                                        executedTestcases.Add(testcaseContext);
                                        executedTests++;
                                        _AccessExecutedTestcasesSemaphore.Release();
                                    }
                                    catch(Exception e)
                                    {
                                        Console.Write(e.Message);
                                    }
                                    
                                }

                                _AccessConsoleSemaphore.Wait();
                                

                                string logLine = createLogExecutionResult(testCount-executedTests+1, nextUnitTestIdent, currentTest.Result);
                                Console.WriteLine(logLine);
                                _AccessConsoleSemaphore.Release();
                                //nextUnitTestIdent = null;
                                //finishExecution = true;
                            }
                        }
                    });
                }
                catch (Exception e)

                {
                    Console.WriteLine("Error");
                }
            }
            try
            {
                Task.WaitAll(taskPool);
            }

            catch (Exception e)
            {
                System.Console.WriteLine(e.Message + "...");

            }

        }
        /// <summary>
        /// Executes test case with hard coded parameters.
        /// This method was written to debug some error. It has no use in real application
        /// </summary>
        /// <param name="gTestPath">path to gtest exe</param>
        private void executeGtestWithoutTestCase(string gTestPath)
        {
            //#1 Prepare process to run OpenCppCoverage
            Process openCpp = new Process();

            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();

            string[] tmp = gtestPath.Split('\\');
            int len = tmp.Length;
            string gtestProject = tmp[len - 1].Split('.')[0];
            string sources = "C:\\Users\\sporsho\\chromium\\src\\out\\Default";
            cmd.StandardInput.WriteLine("C:\\Program Files\\OpenCppCoverage\\OpenCppCoverage.exe " + "--export_type=cobertura" + " " +
                 "--excluded_source=ZsgUtil" + " " +
                 "--sources=" + sources + " " +
                 "--modules=" + gtestProject + " "
                + gtestPath + " --gtest_filter=TaskCostEstimatorTest.BasicEstimation");
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit();
            //Console.WriteLine(cmd.StandardOutput.ReadToEnd());

            // openCpp.StartInfo.FileName = ProjectConfig.GetInstalledProgramPath("OpenCppCoverage");
            //openCpp.StartInfo.FileName = "C:\\Windows\\System32\\cmd.exe";
            //openCpp.StartInfo.Arguments = "/c "+gtestPath;
            //System.Diagnostics.Process.Start("CMD.exe", gtestPath);
            /* string[] tmp = gtestPath.Split('\\');
             int len = tmp.Length;
             string gtestProject = tmp[len - 1].Split('.')[0];
             //Set working-directory of the process
             System.IO.Directory.CreateDirectory(workingDir + "\\" + gtestProject);
             openCpp.StartInfo.WorkingDirectory = workingDir + "\\" + gtestProject;

             //hard coded soruces
             string sources = "C:\\Users\\sporsho\\chromium\\src\\out\\Default";
             //Set Arguments to Source and Google-Test executable
             openCpp.StartInfo.Arguments =
                 "--export_type=cobertura" + " " +
                 "--excluded_source=ZsgUtil" + " " +
                 "--sources " + sources + " " +
                 "--modules " + gtestProject + " " +
                 "-- " + gTestPath + " ";
                 //"--gtest_filter=" + testName + " ";


    
            //Execute Process in a Shell and redirect its output to catch the test-result and the target-directory of the coverage files
            openCpp.StartInfo.UseShellExecute = true;
            openCpp.StartInfo.RedirectStandardOutput = false;
            openCpp.StartInfo.RedirectStandardError = false;

            //Start OpenCppCoverage
            openCpp.Start();
            if (!openCpp.WaitForExit(1000 * 60 * 5))
                openCpp.Kill();

           */

        }
        /// <summary>
        /// extract source folder from gtest path
        /// </summary>
        /// <param name="gTestPath">path to gtest exe</param>
        /// <returns></returns>
        private string getSourcesOfGtest(string gTestPath)
        {
            string[] path = gTestPath.Split('\\');
            string unittestName = path[path.Length - 1];
            string[] tmp = unittestName.Split('.');
            string unitTest = tmp[0];
            int ind = unitTest.IndexOf("_unittests");
            string proj = unitTest.Substring(0, ind);
            ind = gTestPath.IndexOf(unittestName);
            string sources = gTestPath.Substring(0, ind);
            return sources;

        }
        /// <summary>
        /// get module names from gtest path
        /// </summary>
        /// <param name="gTestPath">path to gtest exe</param>
        /// <returns></returns>
        private string getModulesOfGtest(string gTestPath)
        {
            string[] path = gTestPath.Split('\\');
            string unittestName = path[path.Length - 1];
            string[] tmp = unittestName.Split('.');
            string unitTest = tmp[0];
            int ind = unitTest.IndexOf("_unittests");
            string proj = unitTest.Substring(0, ind);
            //ind = gTestPath.IndexOf(unittestName);
            //string sources = gTestPath.Substring(0, ind);
            return proj;
        }
        /// <summary>
        /// execute a single unit test and store coverage report
        /// </summary>
        /// <param name="projName">Project Name</param>
        /// <param name="testName">Test Case Name</param>
        /// <param name="workingDir">Working Directory</param>
        /// <param name="gTestPath">Path to gtest exe</param>
        /// <param name="timeout">timeout in secons</param>
        /// <returns></returns>
        private async Task<RunResult> executeUnitTest(string projName, string testName, string workingDir, string gTestPath, int timeout)
        {
            //#1 Prepare process to run OpenCppCoverage
            /* System.Diagnostics.Process openCpp = new System.Diagnostics.Process();

             // openCpp.StartInfo.FileName = ProjectConfig.GetInstalledProgramPath("OpenCppCoverage");
             openCpp.StartInfo.FileName = "OpenCppCoverage";
             //Set working-directory of the process
             System.IO.Directory.CreateDirectory(workingDir + "\\" + testName);
             openCpp.StartInfo.WorkingDirectory = workingDir + "\\" + testName;
             Console.WriteLine(openCpp.StartInfo.FileName);
             //Set Arguments to Source and Google-Test executable
             openCpp.StartInfo.Arguments =
                 "--export_type=cobertura" + " " +
                 "--excluded_source=ZsgUtil" + " " +
                 "--sources " + projName + " " +
                 "-- " + gTestPath + " " +
                 "--gtest_filter=" + testName + " ";
             Console.WriteLine("\nexecuting command OpenCppCoverage " + openCpp.StartInfo.Arguments);

             //Execute Process in a Shell and redirect its output to catch the test-result and the target-directory of the coverage files
             openCpp.StartInfo.UseShellExecute = false;
             openCpp.StartInfo.RedirectStandardOutput = true;
             openCpp.StartInfo.RedirectStandardError = true;*/
            RunResult testResult = RunResult.UNDEFINED;
            string sources = getSourcesOfGtest(gtestPath);
            string modules = getModulesOfGtest(gtestPath);
            System.IO.Directory.CreateDirectory(workingDir + "\\" + testName);
            Process openCpp = new Process()
            {
                StartInfo = new ProcessStartInfo("OpenCppCoverage")
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = workingDir + "\\" + testName,
                    Arguments = "--export_type=cobertura " +
                    "--sources " + sources +
                    " --modules=" + modules +
                    " -- " + gtestPath +
                    " --gtest_filter=" + testName + " "
                }
            };

            /* Process openCpp = new Process();
             openCpp.StartInfo.FileName = "OpenCppCoverage";
             openCpp.StartInfo.Arguments =
                  "--export_type=cobertura" + " " +
                  "--excluded_source=ZsgUtil" + " " +
                  "--sources " + sources + " " +
                  "--modules="+modules+
                  " -- " + gTestPath + " " +
                  "--gtest_filter=" + testName + " ";*/
            //Console.WriteLine("\nexecuting command OpenCppCoverage " + openCpp.StartInfo.Arguments);

            //Execute Process in a Shell and redirect its output to catch the test-result and the target-directory of the coverage files
            // openCpp.StartInfo.UseShellExecute = false;
            //openCpp.StartInfo.RedirectStandardOutput = true;
            //openCpp.StartInfo.RedirectStandardError = true;


            //Start OpenCppCoverage
            //openCpp.Start();

            //Monitor consumed runtime
            int timeout_sec = 60 * executionTimeout * 20;
            string output = "";
            string error = "";


            /* openCpp.OutputDataReceived += new DataReceivedEventHandler(
             (s, e) =>
             {
                 Console.WriteLine(e.Data);
                 output += e.Data;
             }
             );
             openCpp.ErrorDataReceived += new DataReceivedEventHandler((s, e) => {
                 Console.WriteLine(e.Data);
                 error += e.Data;
             });*/

            openCpp.Start();

            //openCpp.BeginOutputReadLine();
            //openCpp.BeginErrorReadLine();
            //openCpp.WaitForExit(timeout);
            while (!openCpp.HasExited)
            {
                if (timeout_sec == 0)
                {
                    try
                    {
                        openCpp.Kill();
                    }
                    catch (Exception ex) { Console.Error.WriteLine(ex.Message); }
                    testResult = RunResult.TIMEOUT;
                }

                // Read StandardOutput
                /*{
                     char[] buffer = new char[4096];
                     int numb = openCpp.StandardOutput.Read(buffer, 0, 4096);
                     output += new string(buffer, 0, numb);

                     char[] buf = new char[4096];
                     int numb2 = openCpp.StandardError.Read(buffer, 0, 4096);
                     error += new string(buffer, 0, numb2);


                 }*/
                error += await openCpp.StandardError.ReadToEndAsync();
                output += await openCpp.StandardOutput.ReadToEndAsync();
                await Task.Delay(50);
                timeout_sec--;
            }

            //#2 Wait for OpenCppCoverage t finish and read its output-streams parallel
            error += await openCpp.StandardError.ReadToEndAsync();
            output += await openCpp.StandardOutput.ReadToEndAsync();


            //openCpp.Kill();


            Console.WriteLine(output);
            //#3 Analyse the combined output of OpenCppCoverage

            bool expectRunResult = false;
            if (openCpp.HasExited)
            {
                foreach (string lineOfOutput in (error + output).Split('\n'))
                {
                    if (lineOfOutput.Split(']')[0].Contains("RUN"))
                    {   //-> Expect this line as predecessor to the line containing the test-result
                        expectRunResult = true;
                    }

                    else if (expectRunResult)
                    {   //-> extract the test-result
                        if (lineOfOutput.Split(']')[0].Contains("OK"))
                            testResult = RunResult.PASSED;
                        else
                            testResult = RunResult.FAILED;
                        expectRunResult = false;
                    }
                }
            }
            return testResult;
        }
        /// <summary>
        /// execute a single unit test of default visual studio project
        /// </summary>
        /// <param name="projName">Project name</param>
        /// <param name="testName">Test Case name</param>
        /// <param name="workingDir">Working direcotry</param>
        /// <param name="gTestPath">gtest path of the vs project</param>
        /// <param name="timeout">timeout in secons</param>
        /// <returns></returns>
        private async Task<RunResult> executeUnitTestVS(string projName, string testName, string workingDir, string gTestPath, int timeout)
        {
            //#1 Prepare process to run OpenCppCoverage
            /* System.Diagnostics.Process openCpp = new System.Diagnostics.Process();

             // openCpp.StartInfo.FileName = ProjectConfig.GetInstalledProgramPath("OpenCppCoverage");
             openCpp.StartInfo.FileName = "OpenCppCoverage";
             //Set working-directory of the process
             System.IO.Directory.CreateDirectory(workingDir + "\\" + testName);
             openCpp.StartInfo.WorkingDirectory = workingDir + "\\" + testName;
             Console.WriteLine(openCpp.StartInfo.FileName);
             //Set Arguments to Source and Google-Test executable
             openCpp.StartInfo.Arguments =
                 "--export_type=cobertura" + " " +
                 "--excluded_source=ZsgUtil" + " " +
                 "--sources " + projName + " " +
                 "-- " + gTestPath + " " +
                 "--gtest_filter=" + testName + " ";
             Console.WriteLine("\nexecuting command OpenCppCoverage " + openCpp.StartInfo.Arguments);

             //Execute Process in a Shell and redirect its output to catch the test-result and the target-directory of the coverage files
             openCpp.StartInfo.UseShellExecute = false;
             openCpp.StartInfo.RedirectStandardOutput = true;
             openCpp.StartInfo.RedirectStandardError = true;*/
            RunResult testResult = RunResult.UNDEFINED;
            string sources = srcDir;
            //string modules = getModulesOfGtest(gtestPath);
            System.IO.Directory.CreateDirectory(workingDir + "\\" + testName);
            Process openCpp = new Process()
            {
                StartInfo = new ProcessStartInfo("OpenCppCoverage")
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = workingDir + "\\" + testName,
                    Arguments = "--export_type=cobertura " +
                    "--sources " + sources +
                    " -- " + gtestPath +
                    " --gtest_filter=" + testName + " "
                }
            };

            /* Process openCpp = new Process();
             openCpp.StartInfo.FileName = "OpenCppCoverage";
             openCpp.StartInfo.Arguments =
                  "--export_type=cobertura" + " " +
                  "--excluded_source=ZsgUtil" + " " +
                  "--sources " + sources + " " +
                  "--modules="+modules+
                  " -- " + gTestPath + " " +
                  "--gtest_filter=" + testName + " ";*/
            //Console.WriteLine("\nexecuting command OpenCppCoverage " + openCpp.StartInfo.Arguments);

            //Execute Process in a Shell and redirect its output to catch the test-result and the target-directory of the coverage files
            // openCpp.StartInfo.UseShellExecute = false;
            //openCpp.StartInfo.RedirectStandardOutput = true;
            //openCpp.StartInfo.RedirectStandardError = true;


            //Start OpenCppCoverage
            //openCpp.Start();

            //Monitor consumed runtime
            int timeout_sec = 60 * executionTimeout * 20;
            string output = "";
            string error = "";


            /* openCpp.OutputDataReceived += new DataReceivedEventHandler(
             (s, e) =>
             {
                 Console.WriteLine(e.Data);
                 output += e.Data;
             }
             );
             openCpp.ErrorDataReceived += new DataReceivedEventHandler((s, e) => {
                 Console.WriteLine(e.Data);
                 error += e.Data;
             });*/

            openCpp.Start();

            //openCpp.BeginOutputReadLine();
            //openCpp.BeginErrorReadLine();
            //openCpp.WaitForExit(timeout);
            while (!openCpp.HasExited)
            {
                if (timeout_sec == 0)
                {
                    try
                    {
                        openCpp.Kill();
                    }
                    catch (Exception ex) { Console.Error.WriteLine(ex.Message); }
                    testResult = RunResult.TIMEOUT;
                }

                // Read StandardOutput
                /*{
                     char[] buffer = new char[4096];
                     int numb = openCpp.StandardOutput.Read(buffer, 0, 4096);
                     output += new string(buffer, 0, numb);

                     char[] buf = new char[4096];
                     int numb2 = openCpp.StandardError.Read(buffer, 0, 4096);
                     error += new string(buffer, 0, numb2);


                 }*/
                error += await openCpp.StandardError.ReadToEndAsync();
                output += await openCpp.StandardOutput.ReadToEndAsync();
                await Task.Delay(50);
                timeout_sec--;
            }

            //#2 Wait for OpenCppCoverage t finish and read its output-streams parallel
            error += await openCpp.StandardError.ReadToEndAsync();
            output += await openCpp.StandardOutput.ReadToEndAsync();


            //openCpp.Kill();


            Console.WriteLine(output);
            //#3 Analyse the combined output of OpenCppCoverage

            bool expectRunResult = false;
            string[] tmp = (error + output).Split('\n');
            if (openCpp.HasExited)
            {
                foreach (string lineOfOutput in tmp)
                {
                    if (lineOfOutput.Contains(" OK ") || lineOfOutput.Contains(" PASSED "))
                        testResult = RunResult.PASSED;
                    else if (lineOfOutput.Contains(" FAILED "))
                        testResult = RunResult.FAILED;
                   /* if (lineOfOutput.Split(']')[0].Contains("RUN"))
                    {   //-> Expect this line as predecessor to the line containing the test-result
                        expectRunResult = true;
                    }

                    else if (expectRunResult)
                    {   //-> extract the test-result
                        if (lineOfOutput.Split(']')[0].Contains("OK"))
                            testResult = RunResult.PASSED;
                        else
                            testResult = RunResult.FAILED;
                        expectRunResult = false;
                    }*/
                }
            }
            return testResult;
        }
#endregion

#region Reading and Evaluating Command Line Arguments
        /// <summary>
        /// reads command line input parameters and stores them in member variable
        /// </summary>
        /// <returns>returns True if everythign goes well</returns>
        private bool readCommandLineInputParameters()
        {
            foreach (CommandLineArgument cmdArgument in commandLineArguments.Values)
            {
                try
                {
                    string arg = cmdArgument.Key;

                    if (arg.ToLower().Equals(PossibleCommandLineArguments.PROJECT_PATH))
                        projectPath = cmdArgument.Value;
                    else if (arg.ToLower().Equals(PossibleCommandLineArguments.VISUAL_COVERAGE))
                        deleteAnalData = !Convert.ToBoolean(cmdArgument.Value);
                    else if (arg.ToLower().Equals(PossibleCommandLineArguments.DEBUG))
                        DEBUG = Convert.ToBoolean(cmdArgument.Value);
                    else if (arg.ToLower().Equals(PossibleCommandLineArguments.FUNCTION_COVERAGE))
                        argFunctionHitSpectraMatrix = Convert.ToBoolean(cmdArgument.Value);
                    else if (arg.ToLower().Equals(PossibleCommandLineArguments.FUNCTION_COVERAGE))
                        argFunctionHitSpectraMatrix = Convert.ToBoolean(cmdArgument.Value);
                    else if (arg.ToLower().Equals(PossibleCommandLineArguments.INVOKED_FUNCTION_COVERAGE))
                        argInvokedFunctionsHitSpectraMatrix = Convert.ToBoolean(cmdArgument.Value);
                    else if (arg.ToLower().Equals(PossibleCommandLineArguments.INVOKED_FUNCTION_WITH_PARAM_COVERAGE))
                        argInvokedFunctionsWithParametersHitSpectraMatrix = Convert.ToBoolean(cmdArgument.Value);
                    else if (arg.ToLower().Equals(PossibleCommandLineArguments.COUNTING_FUNCTION_INVOKATION_COVERAGE))
                        argCountingFunctionInvokationsHitSpectraMatrix = Convert.ToBoolean(cmdArgument.Value);
                    else if (arg.ToLower().Equals(PossibleCommandLineArguments.LINE_COVERAGE))
                        argLineCoverageHitSpectraMatrix = Convert.ToBoolean(cmdArgument.Value);
                    else if (arg.ToLower().Equals(PossibleCommandLineArguments.DEGREE_OF_PARALLELISM))
                        degreeOfParallelism = Convert.ToInt32(cmdArgument.Value);
                    else if (arg.ToLower().Equals(PossibleCommandLineArguments.EXECUTION_TIMEOUT))
                        executionTimeout = Convert.ToInt32(cmdArgument.Value);
                    else if (arg.ToLower().Equals(PossibleCommandLineArguments.PROJECT_NAME))
                        projName = Convert.ToString(cmdArgument.Value);
                    else if (arg.ToLower().Equals(PossibleCommandLineArguments.SOURCE_DIRECTORY))
                        srcDir = cmdArgument.Value;
                    else if (arg.ToLower().Equals(PossibleCommandLineArguments.GTEST_PATH))
                        gtestPath = cmdArgument.Value;
                    else if (arg.ToLower().Equals(PossibleCommandLineArguments.PROJECT_MODE))
                        projectMode = cmdArgument.Value;
                    else if (arg.ToLower().Equals(PossibleCommandLineArguments.SEPARATOR))
                        separator = cmdArgument.Value.Trim()[0];
                    else if (arg.ToLower().Equals(PossibleCommandLineArguments.EXPORT_PATTERN))
                    {
                        string pattern = cmdArgument.Value;
                        if (pattern.Length == 5)
                        {
                            argFunctionHitSpectraMatrix = extractBooleanValue(pattern[0]);
                            argInvokedFunctionsHitSpectraMatrix = extractBooleanValue(pattern[1]);
                            argInvokedFunctionsWithParametersHitSpectraMatrix = extractBooleanValue(pattern[2]);
                            argCountingFunctionInvokationsHitSpectraMatrix = extractBooleanValue(pattern[3]);
                            argLineCoverageHitSpectraMatrix = extractBooleanValue(pattern[4]);
                        }
                    }
                    else if (arg.ToLower().Equals(PossibleCommandLineArguments.TEST_TYPE))
                    {
                        testType = cmdArgument.Value;
                    }


                    
                }
                catch (Exception e)
                {
                    Console.Write(e.Message);
                    return false;
                }

            }
            if (projectPath != null && projectMode != null && projectMode.ToLower().Equals("default"))
                baseProject = new Project(projectPath, srcDir);
            else if (gtestPath != null && projectMode != null && projectMode.ToLower().Equals("chromium"))
            {
                if (testType.Equals("integration"))
                    baseProject = mimicVSProjectForIntegration(gtestPath);
                else
                    baseProject = mimicVSProject(gtestPath);
            }

            return true;
        }
        /// <summary>
        /// The original program was written for standard visual studio project. 
        /// This function converts the Google Chroimum project to standard visual studio like project so 
        /// our program can work on Google Chromium project
        /// </summary>
        /// <param name="gtestPath">patht to gtest exe</param>
        /// <returns></returns>
        private Project mimicVSProject(string gtestPath)
        {
            Project mimic = new Project();
            //extract project name from gtest path
            string[] path = gtestPath.Split('\\');
            string unittestName = path[path.Length - 1];
            string[] tmp = unittestName.Split('.');
            string unitTest = tmp[0];
            int ind = unitTest.IndexOf("_unittests");
            string proj = unitTest.Substring(0, ind);


            mimic.SetName(proj);
            //directory of the possible locaiton of ninja file
            ind = gtestPath.IndexOf(unitTest);
            string dir = gtestPath.Substring(0, ind);
            mimic.SetDirectory(dir);
            //set testsets
            Dictionary<string, List<string>> extractedTestSets = new Dictionary<string, List<string>>();
            Dictionary<string, List<string>> extractedTestSets2 = new Dictionary<string, List<string>>();
            //HashSet<string> extractedTestSets = new HashSet<string>();
            //find the location of the ninja file
            string ninja = getNinja(dir, unitTest + ".ninja");
            //extract unitest names from ninja file
            extractedTestSets = getExtractedTestSets(ninja, dir);
            // testcases from object files
            extractedTestSets2 = getExtractedTestSetsFromSource(ninja, dir);
            //while (extractedTestSets2.Count > 2)
              //  extractedTestSets2.Remove(extractedTestSets2.Keys.Last());

            mimic.SetTestSet(extractedTestSets2);
            //trimTestCases();

            return mimic;
        }
        /// <summary>
        /// This function was written for integration test, but was not completed for some complexity
        /// </summary>
        /// <param name="gtestPath">Path to gtest exe</param>
        /// <returns></returns>
        private Project mimicVSProjectForIntegration(string gtestPath)
        {
            Project mimic = new Project();
            return mimic;
        }
        /// <summary>
        /// extract test set from ninja file [Google Chromium project]
        /// </summary>
        /// <param name="ninjaFile">path to ninja file</param>
        /// <param name="testHome">tes home directory</param>
        /// <returns></returns>
        Dictionary<string, List<string>> getExtractedTestSetsFromSource(string ninjaFile, string testHome)
        {
            Dictionary<string, List<string>> extractedTC = new Dictionary<string, List<string>>();
            HashSet<string> fileList = getTestFileFromNinja(ninjaFile);
            foreach (string file in fileList)
            {
                HashSet<string> tmpTests = ExtractTestCasesFromSourceFile(file);
                foreach (string test in tmpTests)
                {
                    string[] tmp = test.Split('.');

                    if (extractedTC.ContainsKey(tmp[0]))
                    {
                        extractedTC[tmp[0]].Add(tmp[1]);
                    }
                    else
                    {
                        List<string> tmp2 = new List<string>();
                        tmp2.Add(tmp[1]);
                        extractedTC[tmp[0]] = tmp2;
                    }
                }


            }
            return extractedTC;
        }
        /// <summary>
        /// extract source from gtest path [Google Chromium Project]
        /// </summary>
        /// <returns>sourc root directory</returns>
        private string getSrcFromGtestPath()
        {
            int index = gtestPath.IndexOf("src\\") + 4;
            string root = gtestPath.Substring(0, index);
            return root;
        }
        /// <summary>
        /// extract test file locations from ninja file [Google Chromium Project]
        /// </summary>
        /// <param name="path">path to ninja</param>
        /// <returns></returns>
        private HashSet<string> getTestFileFromNinja(string path)
        {
            string[] lines = System.IO.File.ReadAllLines(path);
            string root = getSrcFromGtestPath();
            string testFile = "";
            HashSet<string> filePath = new HashSet<string>();
            foreach (string line in lines)
            {
                testFile = "";
                if (line.StartsWith("build "))
                {

                    if (line.Contains("cxx"))
                    {
                        int endIndex = 0;
                        int startIndex = line.IndexOf("cxx ../../") + 10; // length of "cxx ../../" is 10
                        if (line.Contains(".cpp "))
                        {
                            endIndex = line.IndexOf(".cpp") + 4; // length of ".cpp" is 4
                        }
                        else if (line.Contains(".cc "))
                        {
                            endIndex = line.IndexOf(".cc") + 3; // length of ".cc" is 3
                        }
                        else if (line.Contains(".c "))
                        {
                            endIndex = line.IndexOf(".c") + 2; // length of ".c" is 2
                        }
                        else if (line.Contains(".cxx "))
                        {
                            endIndex = line.IndexOf(".cxx") + 4; // length of ".cxx" is 2
                        }
                        else if (line.Contains(".hpp "))
                        {
                            endIndex = line.IndexOf(".hpp") + 4; // length of ".hpp" is 2
                        }
                        if (endIndex != 0)
                        {
                            testFile = line.Substring(startIndex, endIndex - startIndex + 1);
                            filePath.Add(root + testFile);
                        }

                    }
                }
            }
            return filePath;
        }

        /// <summary>
        /// extract test set from ninja file [Google Chromium Project]
        /// </summary>
        /// <param name="ninjaFile">Path to Ninja file</param>
        /// <param name="testHome">test directory</param>
        /// <returns></returns>
        private Dictionary<string, List<string>> getExtractedTestSets(string ninjaFile, string testHome)
        {
            Dictionary<string, List<string>> extractedTC = new Dictionary<string, List<string>>();
            //HashSet<string> extractedTC = new HashSet<string>();
            //first extract the list of unittest and their object file location
            string[] lines = System.IO.File.ReadAllLines(ninjaFile);
            HashSet<string> objectPath = new HashSet<string>();
            foreach (string line in lines)
            {
                //sample line : build obj/third_party/WebKit/Source/platform/blink_platform_unittests/DecimalTest.obj: cxx ../../third_party/WebKit/Source/platform/DecimalTest.cpp | obj/third_party/WebKit/Source/platform/blink_platform_unittests/Precompile-platform.cc.obj || obj/third_party/WebKit/Source/platform/blink_platform_unittests.inputdeps.stamp
                //properties of the line
                // 1. line must start with 'build'
                // 2. test object path will end with '.obj cxx'
                // 3. we have to find the location where 'build ' ends and where '.obj' ends
                if (line.StartsWith("build "))
                {
                    if (line.ToLower().Contains("test.obj: cxx"))

                    {
                        //we have got the unittest object
                        //extract the path
                        int a = "build ".Length;
                        int b = ".obj".Length;
                        int placeWhereBoccurs = line.IndexOf(".obj: cxx");
                        string tmp = testHome + line.Substring(a, placeWhereBoccurs - a + b);
                        tmp = tmp.Replace('/', '\\');
                        objectPath.Add(tmp);
                        //counter += testCases.Count;
                    }
                }
            }
            //filePath = getTestFileFromNinja(ninjaFile);
            Console.WriteLine("number of test object\n");
            Console.WriteLine(objectPath.Count);
            //then extract test cases from each object file
            Console.WriteLine("Extracting unittest object files\n");
            int counter = 0;
            foreach (string path in objectPath)
            {
                string[] parts = path.Split('\\');
                int len = parts.Length;
                string fname = parts[len - 1];
                string testName = fname.Split('.')[0];
                HashSet<string> testCases = ExtractTestCasesFromObjectFile(path);
                counter += testCases.Count;
                extractedTC.Add(testName, testCases.ToList<string>());
            }
            Console.WriteLine("Extracted number of test cases: " + counter);
            return extractedTC;
        }
        /// <summary>
        /// getNinja function takes the directory to search and the ninja file to be found and it returns the locaiton of the ninja file
        /// [Google Chromium Project]
        /// </summary>
        /// <param name="dir">working directory</param>
        /// <param name="ninja">ninja name</param>
        /// <returns>location of the ninja file</returns>
        private string getNinja(string dir, string ninja)
        {
            //search ninja in dir recursively
            DirectorySearch(dir, ninja);
            Console.WriteLine("Got ninja file\n");
            Console.WriteLine("...............................................................\n");

            return ninjaName;
        }
        /// <summary>
        /// search a file in directory [Google Chromium Project]
        /// </summary>
        /// <param name="sDir">working dir</param>
        /// <param name="fileName">file name to be searched</param>
        void DirectorySearch(string sDir, string fileName)
        {
            try
            {
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    foreach (string f in Directory.GetFiles(d, fileName))
                    {
                        ninjaName = f;
                        break;
                    }
                    if (ninjaName.Equals(""))
                        DirectorySearch(d, fileName);

                }

            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
        }
        /// <summary>
        /// validates input parameters
        /// </summary>
        /// <returns>returns true if validation succeed</returns>
        private bool validateInputParameters()
        {
            //Validation: Only proceed if there is a valid path to the testsuite 
            if (!validPathToTestsuiteExists()) { return false; }

            //Validation: Only proceed if there is a valid solution name
            if (!validSolutionNameExists()) { return false; }

            //Validation: Only proceed if there is a valid source directory
            if (!validSourceDirectoryExists()) { return false; }

            //Validation: Only proceed if there is a valid output directory
            if (!validOutputDirectoryExists()) { return false; }

            //Sanity: Ensure positive amount of tasks greater zero
            degreeOfParallelism = degreeOfParallelism > 0 ? degreeOfParallelism : 1;

            //Validation: Only proceed if there is a valid executable gtest 
            if (!validGtestExecutableExists()) { return false; }

            //GtestPath is mandatory, check for existence
            checkIfThereAreAnyTestcasesInTestSuite();
            return true;
        }

#region Functions validating the input parameters
        /// <summary>
        /// validates the path to test suite
        /// </summary>
        /// <returns>returns true if succeed</returns>
        private bool validPathToTestsuiteExists()
        {

            //Validation: Only proceed if there is a valid path to the testsuite 
            if (projectMode.Equals("default"))
            {
                if (baseProject == null)
                {
                    Console.Error.WriteLine("ERROR: Please insert the project-path\n");
                    ProjectConfig.help();
                    return false;
                }

                totalNumberOfTestcases = baseProject.UnitTestCount;
            }
            else if (projectMode.ToLower().Equals("chromium"))
            {
                if (gtestPath == null)
                {
                    Console.Error.WriteLine("ERROR, Please insert the gtest executable path");
                    return false;
                }
                else
                {
                    //fetch the test cases
                    Console.WriteLine("\nProject path is not required for chromium project\n");
                }
            }
            return true;
        }
        /// <summary>
        /// checks if a valid project exists
        /// </summary>
        /// <returns>returns true if succeed</returns>
        private bool validSolutionNameExists()
        {
            //Validation: Only proceed if there is a valid solution name
            if (projName == null)
            {
                Console.Error.WriteLine("ERROR: Please insert the Project (solution) name\n");
                ProjectConfig.help();
                return false;
            }
            return true;
        }
        /// <summary>
        /// checks if source directory exists
        /// </summary>
        /// <returns>returns true if succeeds</returns>
        private bool validSourceDirectoryExists()
        {
            //Validation: Only proceed if there is a valid source directory
            if (srcDir == null)
            {
                Console.Error.WriteLine("ERROR: Please insert the project source directory\n");
                ProjectConfig.help();
                return false;
            }
            return true;
        }
        /// <summary>
        /// checks if output directory exists
        /// </summary>
        /// <returns>returns true if succeed</returns>
        private bool validOutputDirectoryExists()
        {
            //Validation: Only proceed if there is a valid output directory
            if (outDir == null)
            {
                Console.Error.WriteLine("ERROR: Please insert your desired output directory\n");
                ProjectConfig.help();
                return false;
            }
            return true;
        }
        /// <summary>
        /// checks if gtest exe exists
        /// </summary>
        /// <returns>returns true if succeeds</returns>
        private bool validGtestExecutableExists()
        {
            if(projectMode.Equals("default"))
            {
                // check if the default exe exists
                if (System.IO.File.Exists(baseProject.GetExecutable()))
                {
                    return true;
                }else
                {
                    //check if gtest is valid and is exist
                    if(gtestPath!=null && !gtestPath.Equals("") && System.IO.File.Exists(gtestPath))
                    {
                        baseProject.SetExecutable(gtestPath);
                        return true;
                    }else
                    {
                        Console.Error.WriteLine("Could not find gtest-executable at: ");
                        Console.Error.WriteLine(baseProject.GetExecutable());
                        return false;
                    }
                    
                }
            }
            else if (projectMode.Equals("chromium"))
            {
                if(gtestPath != null && !gtestPath.Equals("") && System.IO.File.Exists(gtestPath))
                {
                    baseProject.SetExecutable(gtestPath);
                    return true;
                }
                else
                {
                    Console.Error.WriteLine("Could not find gtest-executable at: ");
                    Console.Error.WriteLine(gtestPath);
                    return false;
                }
            }
            else
            {
                Console.Error.WriteLine("undetermined projects????? ");
               // Console.Error.WriteLine(gtestPath);
                return false;
            }
            /*if (projectMode.Equals("vs"))
            {
                //Validation: Only proceed if there is a valid executable gtest
                if (!System.IO.File.Exists(baseProject.getExecutable()))
                {
                    Console.Error.WriteLine("Could not find gtest-executable at: ");
                    Console.Error.WriteLine(baseProject.getExecutable());
                    return false;
                }
               
            }
            else if (projectMode.Equals("chromium"))
            {
                if (!System.IO.File.Exists(gtestPath))
                {
                    Console.Error.WriteLine("Could not find gtest-executable at: ");
                    Console.Error.WriteLine(gtestPath);
                    return false;
                }
            }
            return true;*/
        }
#endregion
#endregion
        /// <summary>
        /// checks if test case exists
        /// </summary>
        /// <returns>returns true if succeeds</returns>
        private bool checkIfThereAreAnyTestcasesInTestSuite()
        {
            //Validation - Only proceed if there is at least one TestCase
            if (baseProject.UnitTestCount == 0)
            {
                Console.WriteLine("No test-cases to run!");
                return false;
            }

            Console.WriteLine("Test-Execution contains");
            Console.WriteLine("Test-Sets:  " + baseProject.TestCaseCount);
            Console.WriteLine("Unit-Tests: " + baseProject.UnitTestCount);
            Console.WriteLine("--------------------------------------------------");

            return true;
        }

        /// <summary>
        /// extracts true/false from char
        /// </summary>
        /// <param name="value">character</param>
        /// <returns>True/False</returns>
        private bool extractBooleanValue(char value)
        {
            if (value == '0')
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        /// <summary>
        /// creates working directory
        /// </summary>
        /// <returns>returns true on succss</returns>
        private bool createWorkingDirectory()
        {
            workingDir = createOutputDirectory();

            if ((workingDir = createOutputDirectory()) == null) { return false; }

            try
            {
                System.IO.Directory.CreateDirectory(workingDir);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                return false;
            }

            string output = "\nStep 2: Created working-directory at "
                + workingDir
                + "\nContaining execution-results and hit-spectra\n"
                + "---------------------------------------------------";

            CommandLinePrinter.printToCommandLine(output);

            return true;
        }
        /// <summary>
        /// creates outptut directory name
        /// </summary>
        /// <returns>output directory name</returns>
        private string createOutputDirectory()
        {
            try
            {
                return outDir
                + "\\TestCoverage_"
                + DateTime.Now.Year + "-"
                + (DateTime.Now.Month >= 10 ? DateTime.Now.Month + "-" : "0" + DateTime.Now.Month + "-")
                + (DateTime.Now.Day >= 10 ? DateTime.Now.Day + "_" : "0" + DateTime.Now.Day + "_")
                + (DateTime.Now.Hour >= 10 ? DateTime.Now.Hour + "-" : "0" + DateTime.Now.Hour + "-")
                + (DateTime.Now.Minute >= 10 ? DateTime.Now.Minute + "-" : "0" + DateTime.Now.Minute + "-")
                + (DateTime.Now.Second >= 10 ? DateTime.Now.Second + "" : "0" + DateTime.Now.Second);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                return null;
            }
        }
        /// <summary>
        /// check if openCppCoverge is installed 
        /// </summary>
        /// <returns>True on success</returns>
        private bool checkIfOpenCppCoverageIsInstalledOnHostPC()
        {
            if (ProjectConfig.GetInstalledProgramPath("OpenCppCoverage") == null)
            {
                Console.Error.WriteLine("Could not find a valid installation of OpenCppCoverage");
                return false;
            }

            return true;
        }
        /// <summary>
        /// create log for printing on console
        /// </summary>
        /// <param name="testCount">Number of test remaining</param>
        /// <param name="testName">Test Name</param>
        /// <param name="testResult">Test Result</param>
        /// <returns>log</returns>
        private string createLogExecutionResult(int testCount, string testName, RunResult testResult)
        {
            string result = string.Empty;

            string testCnt = testCount.ToString();
            for (int divLen = 0; divLen < 5 - testCount.ToString().Length; divLen++) testCnt += " ";
            result += testCnt + " | ";

            string tstName = testName;
            for (int divLen = 0; divLen < 90 - testName.ToString().Length; divLen++) tstName += " ";
            result += tstName + " | ";

            string tResult = testResult.ToString();
            for (int divLen = 0; divLen < 10 - testResult.ToString().Length; divLen++) tResult += " ";
            result += " " + tResult + " |";

            return result;
        }
 
        /// <summary>
        /// processLine is takes two string as input and find if there exist any testCase under the testname
        /// </summary>
        /// <param name="line"></param>
        /// <param name="testName"></param>
        /// <returns>empty string or testCase</returns>
        private string processLine(string line, string testName)
        {
            string alternateTestName = generateAlternateTestName(testName);
            string alternateMatcher = "::" + alternateTestName.ToLower() + "_";
            string matcher = "::" + testName.ToLower() + "_";
            if (line.ToLower().Contains(matcher))
            {
                int a = line.ToLower().IndexOf(matcher);
                int b = line.IndexOf("_Test::test_info_");
                int c = matcher.Length;
                if (b == 0 || b - a <= 0)
                    return "";
                else
                {
                    string ret = line.Substring(a + c, b - a - c);
                    return ret;
                }
            }
            else if (!alternateTestName.Equals("") && line.ToLower().Contains(alternateMatcher))
            {
                int a = line.ToLower().IndexOf(alternateMatcher);
                int b = line.ToLower().IndexOf("_test::test_info_");
                int c = matcher.Length;
                if (b == 0 || b - a <= 0)
                    return "";
                else
                {
                    string ret = line.Substring(a + c, b - a - c);
                    return ret;
                }
            }
            else
            {
                return "";
            }

        }
        /// <summary>
        /// creates alternate test name
        /// </summary>
        /// <param name="tName">Test Name</param>
        /// <returns>new name</returns>
        private string generateAlternateTestName(string tName)
        {
            if (tName.Contains("_"))
            {
                string[] tmp = tName.Split('_');
                return string.Join("", tmp);
            }
            else
                return "";
        }
        /// <summary>
        /// process line to find test case
        /// </summary>
        /// <param name="line">Line of code</param>
        /// <returns>test case</returns>
        private string processLineOfSource(string line)
        {
            string testcase = "";
            if (line.StartsWith("TEST"))
            {
                int start = line.IndexOf("(") + 1;
                int end = line.IndexOf(")");
                testcase = line.Substring(start, end - start);
            }
            return testcase;
        }
        /// <summary>
        /// extract test cases from source file
        /// </summary>
        /// <param name="path">path to source file</param>
        /// <returns>HashSet of test cases</returns>
        private HashSet<string> ExtractTestCasesFromSourceFile(string path)
        {
            HashSet<string> testCasesWithTest = new HashSet<string>();
            try
            {
                string[] lines = System.IO.File.ReadAllLines(path);
                foreach (string line in lines)
                {
                    string testCase = processLineOfSource(line);
                    if (!testCase.Equals(""))
                    {
                        string[] parts = testCase.Split(',');
                        string test = parts[0].Trim() + "." + parts[1].Trim();
                        testCasesWithTest.Add(test);
                        // Console.WriteLine(test);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return testCasesWithTest;
        }
        /// <summary>
        /// this function takes a path string as a input. This path should point to the object file generated by gn gen corresponding to a google test
        /// the object file links can be found in corresponding ninja file of the unitest
        /// </summary>
        /// <param name="path">Path to object file</param>
        /// <returns>HashSet of testCases</returns>
        private HashSet<string> ExtractTestCasesFromObjectFile(string path)
        {
            HashSet<string> testCases = new HashSet<string>();
            //string path = "C:/Users/sporsho/chromium/src/out/Default/obj/third_party/WebKit/Source/platform/blink_platform_unittests/DragImageTest.obj";
            string[] parts = path.Split('\\');
            int len = parts.Length;
            string fname = parts[len - 1];
            string testName = fname.Split('.')[0];

            try
            {
                //BinaryReader br = new BinaryReader(new FileStream(path, FileMode.Open));
                //byte[] B = File.ReadAllBytes(path);
                string[] lines = System.IO.File.ReadAllLines(path);
                //Console.WriteLine(lines.Length);
                foreach (string line in lines)
                {
                    string testCase = processLine(line, testName);
                    if (!testCase.Equals(""))
                    {
                        testCases.Add(testCase);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return testCases;
        }
    }

}
