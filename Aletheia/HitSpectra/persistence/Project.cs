using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Text.RegularExpressions;

namespace Aletheia.HitSpectra.persistence
{
    public class Project
    {
        private string _Name;
        private string _Directory;
        private Dictionary<string, List<string>> _TestSet;
        readonly SemaphoreSlim _TestSetSemaphore;
        private HashSet<string> _TestSetSimplified;
        private string Executable;


        public Project()
        {
            this._TestSetSemaphore = new SemaphoreSlim(1);
            this._TestSetSimplified = new HashSet<string>();
        }
        public Project(string configFile, string srcDir)
        {
            //Extract Projects-Name
            this._Name = new DirectoryInfo(configFile).Name;
            this._Name = this._Name.Substring(0, this._Name.Length - ".vcxproj".Length);

            //Extract Projects-Directory
            this._Directory = Path.GetDirectoryName(configFile);

            //Gather test-cases/unit-tests from project-file (*.vcxproj)
            this._TestSet = Project.GetTestSetFromProject(configFile, srcDir);

            this.Executable=this._Directory + "\\debug\\" + this._Name + ".exe";
            //Create a semaphore for parallel test-execution
            this._TestSetSemaphore = new SemaphoreSlim(1);
        }
        public void SetExecutable(string exec)
        {
            this.Executable = exec;
        }
        public string GetExecutable()
        {
            return this.Executable;
        }
        public void SetName(string proj)
        {
            this._Name = proj;
        }
        public void SetDirectory(string dir)
        {
            this._Directory = dir;
        }
        public void SetTestSet(Dictionary<string, List<string>> set)
        {
            this._TestSet = set;
        }
        public void SetTestSetSimplified(HashSet<string> set)
        {
            this._TestSetSimplified = set;
        }
        public string PopNextSimplifiedUnitTest()
        {
            this._TestSetSemaphore.Wait();
            if (this._TestSetSimplified.Count == 0)
            {
                this._TestSetSemaphore.Release();
                return null;
            }
            string test = this._TestSetSimplified.First<string>();
            this._TestSetSimplified.Remove(test);
            this._TestSetSemaphore.Release();
            return test;
        }
        public string PopNextUnitTest()
        {
            string testCaseName;
            string unitTestName;

            this._TestSetSemaphore.Wait();

            if (this._TestSet.Count == 0)
            {
                this._TestSetSemaphore.Release();
                return null;
            }
            testCaseName = this._TestSet.Keys.First();
            while (this._TestSet[testCaseName].Count == 0)
            {
                this._TestSet.Remove(testCaseName);
                testCaseName = this._TestSet.Keys.First();
            }
            unitTestName = this._TestSet[testCaseName].First();

            //Remove UnitTest
            this._TestSet[testCaseName].Remove(unitTestName);

            //If it was the last UnitTest of the TestCase -> Remove the TestCase too
            if (this._TestSet[testCaseName].Count == 0)
                this._TestSet.Remove(testCaseName);

            this._TestSetSemaphore.Release();

            return testCaseName + "." + unitTestName;

        }

        public string Name
        {
            get { return this._Name; }
        }

        public string Directory
        {
            get { return this._Directory; }
        }


        public int UnitTestCount
        {
            get
            {
                int result = 0;

                this._TestSetSemaphore.Wait();

                foreach (string testCase in this._TestSet.Keys)
                    result += _TestSet[testCase].Count;

                this._TestSetSemaphore.Release();

                return result;
            }
        }
        public int SimplifiedUnitTestCount
        {
            get
            {
                int result = 0;
                this._TestSetSemaphore.Wait();
                result = this._TestSetSimplified.Count;
                this._TestSetSemaphore.Release();
                return result;
            }
        }
        public void LimitTestCase()
        {
            this._TestSetSemaphore.Wait();
            while (this._TestSet.Count > 3)
            {
                this._TestSet.Remove(this._TestSet.Keys.First());
            }
            this._TestSetSemaphore.Release();
        }
        public int TestCaseCount
        {
            get
            {
                this._TestSetSemaphore.Wait();

                int result = _TestSet.Count;
                this._TestSetSemaphore.Release();

                return result;
            }
        }
        public int SimplifiedTestCaseCount
        {
            get
            {
                int result = 0;

                this._TestSetSemaphore.Wait();

                result = this._TestSetSimplified.Count;

                this._TestSetSemaphore.Release();

                return result;
            }
        }

        #region Static-Member
        /// <summary>
        /// Uses the project-file to extract the TestCases and its UnitTests 
        /// used int that project
        /// </summary>
        /// <param name="projectPath">Path to the project-file (*.vcxproj) of the desired test-set</param>
        /// <returns>Dictionary containing the TestCases(Key) and its set of UnitTests(Value)</returns>
        private static Dictionary<string, List<string>> GetTestSetFromProject(string projectPath, string sourceDirectory)
        {
            Dictionary<string, List<string>> tests = new Dictionary<string, List<string>>();

            if (projectPath.EndsWith(".vcxproj"))
            {
                string projectLine;
                string testCasePath;
                Dictionary<string, List<string>> currentTests;

                using (System.IO.StreamReader projectReader = new System.IO.StreamReader(projectPath))
                {
                    while ((projectLine = projectReader.ReadLine()) != null)
                    {
                        if (projectLine.Trim().StartsWith("<ClCompile "))
                        {
                            testCasePath = projectLine.Split('"')[1];

                            currentTests = new Dictionary<string, List<string>>();
                            currentTests = Project.GetUnitTestsFromTestCase(Path.Combine(sourceDirectory, testCasePath));
                            string t = "";
                            foreach (string unitTestName in currentTests.Keys)
                            {
                                if (tests.ContainsKey(unitTestName))
                                {
                                    tests[unitTestName].AddRange(currentTests[unitTestName].ToArray());
                                }
                                else
                                {
                                    tests.Add(unitTestName, currentTests[unitTestName]);
                                }
                            }
                        }
                    }
                }
            }
            return tests;
        }
        private static bool ValidateLineForTest(string lineOfCode)
        {
            if (!lineOfCode.StartsWith("#"))
            {
                if (Regex.IsMatch(lineOfCode, "\\w*TEST[^,]*\\({1}[^,]+\\s*,{1}\\s*[^,]+\\){1}"))// line matches regex
                {
                    if (lineOfCode.Contains(")"))
                    {
                        string sub = lineOfCode.Substring(0, 1 + lineOfCode.IndexOf(")"));
                        if (Regex.IsMatch(sub, "\\w*TEST[^,]*\\({1}[^,]+\\s*,{1}\\s*[^,]+\\){1}"))
                        {
                            return true;
                        }
                        else return false;
                    }
                    else return false;
                }
                else
                    return false;
            }
            else
                return false;
        }
        /// <summary>
        /// Uses the source-file to extract the contained TestCase and its UnitTests
        /// </summary>
        /// <param name="srcPath">Path to the source-file (*.cpp) of the TestCase</param>
        /// <returns>Dictionary containing the TestCases(Key) and its set of UnitTests(Value)</returns>
        private static Dictionary<string, List<string>> GetUnitTestsFromTestCase(string srcPath)
        {
            Dictionary<string, List<string>> tests = new Dictionary<string, List<string>>();

            if (!System.IO.File.Exists(srcPath)) return tests;

            string lineOfCode;
            using (System.IO.StreamReader srcReader = new System.IO.StreamReader(srcPath))
            {
                while ((lineOfCode = srcReader.ReadLine()) != null)
                {

                    //if (lineOfCode.ToLower().StartsWith("test"))
                    if(ValidateLineForTest(lineOfCode))
                    {

                        string testCase = lineOfCode.Split('(')[1].Split(',')[0];
                        string test = lineOfCode.Split('(')[1].Split(',')[1];
                        test = test.Substring(0, test.IndexOf(")"));
                        test = test.Trim();

                        if (!tests.ContainsKey(testCase))
                            tests.Add(testCase, new List<string>());

                        tests[testCase].Add(test.Trim());
                        
                    }
                }
            }
            return tests;
        }
        #endregion
    }
}
