﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.Internal.Web.Utils;

namespace NuGet
{
    public class FrameworkAssemblyReference : IFrameworkTargetable
    {
        public FrameworkAssemblyReference(string assemblyName)
            : this(assemblyName, Enumerable.Empty<FrameworkName>())
        {
        }

        public FrameworkAssemblyReference(string assemblyName, IEnumerable<FrameworkName> supportedFrameworks)
        {
            if (String.IsNullOrEmpty(assemblyName))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, CommonResources.Argument_Cannot_Be_Null_Or_Empty, "assemblyName"));
            }

            if (supportedFrameworks == null)
            {
                throw new ArgumentNullException("supportedFrameworks");
            }

            AssemblyName = assemblyName;
            SupportedFrameworks = supportedFrameworks;
        }

        public string AssemblyName { get; private set; }
        public IEnumerable<FrameworkName> SupportedFrameworks { get; private set; }
    }
}
