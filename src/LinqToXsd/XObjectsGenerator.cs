// -----------------------------------------------------------------------
// <copyright file="XObjectsGenerator.cs" company="Ollon, LLC">
//     Copyright (c) 2018 Ollon, LLC. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;

using Microsoft.CSharp;

using Xml.Schema.Linq;
using Xml.Schema.Linq.CodeGen;

namespace XObjectsGenerator
{
    using System.Linq;

    public static class XObjectsGenerator
    {
        public static Assembly ThisAssembly = Assembly.GetExecutingAssembly();

        public static int Main(string[] args)
        {
            var set = new XmlSchemaSet();
            var veh = new ValidationEventHandler(ValidationCallback);
            set.ValidationEventHandler += veh;
            string csFileName = string.Empty;
            string configFileName = null;
            string assemblyName = string.Empty;
            bool fSourceNameProvided = false;
            bool xmlSerializable = false;
            bool nameMangler2 = false;
            if (args.Length == 0)
            {
                PrintHelp();
                return 0;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                string value = string.Empty;
                bool argument = false;
                if (arg.StartsWith("/", StringComparison.Ordinal) || arg.StartsWith("-", StringComparison.Ordinal))
                {
                    argument = true;
                    int colonPos = arg.IndexOf(":", StringComparison.Ordinal);
                    if (colonPos != -1)
                    {
                        value = arg.Substring(colonPos + 1);
                        arg = arg.Substring(0, colonPos);
                    }
                }

                arg = arg.ToLower(CultureInfo.InvariantCulture);
                if (!argument)
                {
                    try
                    {
                        set.Add(null, CreateReader(arg));
                    }
                    catch (Exception e)
                    {
                        PrintErrorMessage(e.ToString());
                        return 1;
                    }

                    if (csFileName == string.Empty)
                    {
                        csFileName = Path.ChangeExtension(arg, extension: "cs");
                    }
                }
                else if (ArgumentMatch(arg, toMatch: "?") || ArgumentMatch(arg, toMatch: "help"))
                {
                    PrintHelp();
                    return 0;
                }
                else if (ArgumentMatch(arg, toMatch: "config"))
                {
                    configFileName = value;
                }
                else if (ArgumentMatch(arg, toMatch: "filename"))
                {
                    csFileName = value;
                    fSourceNameProvided = true;
                }
                else if (ArgumentMatch(arg, toMatch: "enableservicereference"))
                {
                    xmlSerializable = true;
                }
                else if (ArgumentMatch(arg, toMatch: "lib"))
                {
                    assemblyName = value;
                }
                else if (ArgumentMatch(arg, toMatch: "namemangler2"))
                {
                    nameMangler2 = true;
                }
            }

            if (assemblyName != string.Empty && !fSourceNameProvided)
            {
                //only generate assembly
                csFileName = string.Empty;
            }

            set.Compile();
            set.ValidationEventHandler -= veh;
            if (set.Count > 0 && set.IsCompiled)
            {
                /*
                GenerateXObjects(
                    set, csFileName, configFileName, assemblyName, xmlSerializable, nameMangler2);
                */
                try
                {
                    GenerateXObjects(
                        set,
                        csFileName,
                        configFileName,
                        assemblyName,
                        xmlSerializable,
                        nameMangler2);
                }
                catch (Exception e)
                {
                    PrintErrorMessage(e.ToString());
                    return 1;
                }
            }

            return 0;
        }

        public static void GenerateXObjects(
            XmlSchemaSet set,
            string csFileName,
            string configFileName,
            string assemblyName,
            bool xmlSerializable,
            bool nameMangler2)
        {
            var configSettings = new LinqToXsdSettings(nameMangler2);
            if (configFileName != null)
            {
                configSettings.Load(configFileName);
            }

            configSettings.EnableServiceReference = xmlSerializable;
            var xsdConverter = new XsdToTypesConverter(configSettings);
            var mapping = xsdConverter.GenerateMapping(set);
            var codeGenerator = new CodeDomTypesGenerator(configSettings);
            var ccu = new CodeCompileUnit();
            foreach (var codeNs in codeGenerator.GenerateTypes(mapping))
            {
                ccu.Namespaces.Add(codeNs);
            }

            //Write to file
            var provider = new CSharpCodeProvider();
            if (!string.IsNullOrEmpty(csFileName))
            {
                using (var update =
                    new Update(csFileName, Encoding.UTF8))
                {
                    provider.GenerateCodeFromCompileUnit(
                        ccu,
                        update.Writer,
                        new CodeGeneratorOptions());
                }

                PrintMessage(csFileName);
            }

            if (assemblyName != string.Empty)
            {
                var options = new CompilerParameters
                {
                    OutputAssembly = assemblyName,
                    IncludeDebugInformation = true,
                    TreatWarningsAsErrors = true
                };
                options.TempFiles.KeepFiles = true;
                {
                    var r = options.ReferencedAssemblies;
                    r.Add(value: "System.dll");
                    r.Add(value: "System.Core.dll");
                    r.Add(value: "System.Xml.dll");
                    r.Add(value: "System.Xml.Linq.dll");
                    r.Add(value: "Xml.Schema.Linq.dll");
                }
                var results = provider.CompileAssemblyFromDom(options, ccu);
                if (results.Errors.Count > 0)
                {
                    PrintErrorMessage(e: "compilation error(s): ");
                    for (int i = 0; i < results.Errors.Count; i++)
                    {
                        PrintErrorMessage(results.Errors[i].ToString());
                    }

                    throw new Exception(message: "compilation error(s)");
                }

                PrintMessage(
                    "Generated Assembly: " +
                    results.CompiledAssembly);
            }
        }

        private static XmlReader CreateReader(string xsdFile)
        {
            return XmlReader.Create(
                xsdFile,
                new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse });
        }

        private static void PrintMessage(string csFileName)
        {
            PrintHeader();
            Console.WriteLine("Generated " + csFileName + "...");
        }

        private static void PrintErrorMessage(string e)
        {
            Console.Error.WriteLine(format: "LinqToXsd: error TX0001: {0}", arg0: e);
        }

        private static void PrintErrorMessage(ValidationEventArgs args)
        {
            Console.Error.WriteLine(
                $"{args.Exception.SourceUri.Replace(oldValue: "file:///", newValue: "").Replace('/', '\\')}({args.Exception.LineNumber},{args.Exception.LinePosition}): {(args.Severity == XmlSeverityType.Warning ? "warning" : "error")} TX0001: {args.Message}");
        }

        private static void PrintHeader()
        {
            Console.WriteLine(
                string.Format(
                    CultureInfo.CurrentCulture,
                    format: "[Microsoft (R) .NET Framework, Version {0}]",
                    arg0: ThisAssembly.ImageRuntimeVersion));
        }

        private static void PrintHelp()
        {
            PrintHeader();
            string name = ThisAssembly.GetName().Name;
            Console.WriteLine();
            Console.WriteLine(
                name +
                " - " +
                "Utility to generate typed wrapper classes from a XML Schema");
            Console.WriteLine(
                "Usage: " +
                name +
                " <schemaFile> [one or more schema files] [/fileName:<csFileName>.cs] [/lib:<assemblyName>] [/config:<configFileName>.xml] [/enableServiceReference] [/nameMangler2]");
        }

        private static void ValidationCallback(
            object sender,
            ValidationEventArgs args)
        {
            PrintErrorMessage(args);
        }

        private static bool ArgumentMatch(string arg, string toMatch)
        {
            switch (arg[0])
            {
                case '/':
                case '-':
                    return arg.Substring(1) == toMatch;
                default:
                    return false;
            }
        }
    }
}
