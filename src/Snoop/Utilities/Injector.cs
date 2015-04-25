// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Snoop.Utilities
{
	internal static class Injector
	{
		private static string Suffix(int processId)
		{
			string bitness = IntPtr.Size == 8 ? "64" : "32";
			string clr = "3.5";

			foreach (var module in WindowInfo.GetModulesByProcessId(processId))
			{
				// a process is valid to snoop if it contains a dependency on PresentationFramework, PresentationCore, or milcore (wpfgfx).
				// this includes the files:
				// PresentationFramework.dll, PresentationFramework.ni.dll
				// PresentationCore.dll, PresentationCore.ni.dll
				// wpfgfx_v0300.dll (WPF 3.0/3.5)
				// wpfgrx_v0400.dll (WPF 4.0)

				// note: sometimes PresentationFramework.dll doesn't show up in the list of modules.
				// so, it makes sense to also check for the unmanaged milcore component (wpfgfx_vxxxx.dll).
				// see for more info: http://snoopwpf.codeplex.com/Thread/View.aspx?ThreadId=236335

				// sometimes the module names aren't always the same case. compare case insensitive.
				// see for more info: http://snoopwpf.codeplex.com/workitem/6090

				if
				(
					module.szModule.StartsWith("PresentationFramework", StringComparison.OrdinalIgnoreCase) ||
                    module.szModule.StartsWith("PresentationCore", StringComparison.OrdinalIgnoreCase) ||
                    module.szModule.StartsWith("wpfgfx", StringComparison.OrdinalIgnoreCase)
				)
				{
					if (FileVersionInfo.GetVersionInfo(module.szExePath).FileMajorPart > 3)
					{
						clr = "4.0";
					}
				}
                if (module.szModule.Contains("wow64.dll"))
				{
					if (FileVersionInfo.GetVersionInfo(module.szExePath).FileMajorPart > 3)
					{
						bitness = "32";
					}
				}
			}
			return bitness + "-" + clr;
		}

		internal static void Launch(int processId, Assembly assembly, string className, string methodName)
		{
			var location = Assembly.GetEntryAssembly().Location;
			var directory = Path.GetDirectoryName(location);
		    // ReSharper disable once AssignNullToNotNullAttribute
            var file = Path.Combine(directory, "ManagedInjectorLauncher" + Suffix(processId) + ".exe");

			Process.Start(file, processId + " \"" + assembly.Location + "\" \"" + className + "\" \"" + methodName + "\"");
		}
	}
}
