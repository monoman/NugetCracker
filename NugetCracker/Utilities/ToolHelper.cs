using System;
using System.Diagnostics;
using NugetCracker.Interfaces;

namespace NugetCracker.Utilities
{
	public class ToolHelper
	{
		public static bool ExecuteTool(ILogger logger, string toolName, string arguments, string workingDirectory, Action<ILogger, string> processToolOutput = null)
		{
			try {
				if (processToolOutput == null)
					processToolOutput = (l, s) => l.Info(s);
				Process p = new Process();
				p.StartInfo.UseShellExecute = false;
				p.StartInfo.RedirectStandardError = true;
				p.StartInfo.RedirectStandardOutput = true;
				p.StartInfo.FileName = toolName.FindInPathEnvironmentVariable();
				p.StartInfo.Arguments = arguments;
				p.StartInfo.WorkingDirectory = workingDirectory;
				p.StartInfo.CreateNoWindow = true;
				if (logger != null) {
					p.OutputDataReceived += (object sender, DataReceivedEventArgs e) => processToolOutput(logger, e.Data);
					p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => logger.Error(e.Data);
				}
				logger.Debug("Executing: " + p.StartInfo.FileName + " " + arguments);
				p.Start();
				p.BeginOutputReadLine();
				p.BeginErrorReadLine();
				p.WaitForExit();
				return p.ExitCode == 0;
			} catch (Exception e) {
				logger.Error(e);
			}
			return false;
		}


	}
}
