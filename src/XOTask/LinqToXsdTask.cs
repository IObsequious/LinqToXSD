using System;
using System.Collections;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Xml.Schema.Linq.VS
{
    public class LinqToXsdTask : ToolTask
    {
        [Output]
        public string Filename { get; set; }

        public string LinqToXsdDir { get; set; } = string.Empty;

        public string ConfigurationFile { get; set; }

        public ITaskItem[] Sources { get; set; }

        protected override string ToolName
        {
            get
            {
                return "LinqToXsd.exe";
            }
        }

        protected override MessageImportance StandardErrorLoggingImportance
        {
            get
            {
                return MessageImportance.High;
            }
        }

        protected override string GenerateFullPathToTool()
        {
            return LinqToXsdDir + "\\LinqToXsd.exe";
        }

        protected override string GenerateCommandLineCommands()
        {
            CommandLineBuilder builder = new CommandLineBuilder();
            foreach (ITaskItem iti in Sources)
            {
                builder.AppendFileNameIfNotNull(iti);
            }

            if (ConfigurationFile != null)
            {
                builder.AppendSwitchIfNotNull(switchName: "/config:", parameters: new[] { ConfigurationFile }, delimiter: ":");
            }

            if (Filename != null)
            {
                builder.AppendSwitchIfNotNull(switchName: "/fileName:", parameters: new[] { Filename }, delimiter: ":");
            }

            Log.LogMessage(MessageImportance.High, message: "Assembling {0}", messageArgs: Sources);

            return builder.ToString();
        }
    }
}
