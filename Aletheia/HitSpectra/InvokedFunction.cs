﻿using Aletheia.HitSpectra.persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aletheia.HitSpectra
{
    /// <summary>
    /// this class holds necessary member variable and methods for Invoked funcions
    /// </summary>
    public class InvokedFunction
    {
        private string name;
        private string sourceFileTheFunctionBelongsTo;
        private List<string> parameters;
        /// <summary>
        /// constructor sets some member variable
        /// </summary>
        /// <param name="name"></param>
        /// <param name="sourceFile"></param>
        /// <param name="parameters"></param>
        public InvokedFunction(string name, string sourceFile, List<string> parameters)
        {
            this.name = name;
            this.sourceFileTheFunctionBelongsTo = sourceFile;
            this.parameters = parameters;
        }

        public string Name
        {
            get { return name; }
        }

        public string NameWithParameters
        {
            get
            {
                string output = name;
                foreach (string parameter in parameters)
                {
                    output += "_" + parameter;
                }
                return output;
            }
        }

        public string SourceFile
        {
            get { return sourceFileTheFunctionBelongsTo; }
        }

        public bool compareFunctionInvokationToFunction(Block function)
        {
            string fctName = function.Name;
            string fctSourceFile = function.SourceFileName;

            if (name.Equals(fctName, StringComparison.OrdinalIgnoreCase))
            {
                if (String.IsNullOrEmpty(sourceFileTheFunctionBelongsTo))
                {
                    return true;
                }

                if (fctSourceFile.Equals(sourceFileTheFunctionBelongsTo, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
