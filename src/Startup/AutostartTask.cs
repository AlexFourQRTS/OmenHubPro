using System;
using System.IO;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;
using OmenHub.Core.Logging;
using OmenHub.Core.Processes;

namespace OmenHub.Startup
{
    /// <summary>
    /// Autostart through an elevated scheduled task: starts at logon without a UAC prompt, also on battery.
    /// </summary>
    public static class AutostartTask
    {
        private const string TaskName = "OmenHub";
        private const int QueryTimeoutMilliseconds = 8000;
        private const int ChangeTimeoutMilliseconds = 15000;

        public static bool IsEnabled()
        {
            int exitCode;
            ProcessRunner.RunWithExitCode("schtasks", "/Query /TN " + TaskName, QueryTimeoutMilliseconds, out exitCode);
            return exitCode == 0;
        }

        /// <summary>Creates the task; returns an empty string on success or the error text.</summary>
        public static string Enable()
        {
            try
            {
                string xmlPath = Path.Combine(Path.GetTempPath(), "omenhub-task.xml");
                File.WriteAllText(xmlPath, BuildTaskXml(), Encoding.Unicode);
                int exitCode;
                string output = ProcessRunner.RunWithExitCode("schtasks",
                    "/Create /TN " + TaskName + " /XML \"" + xmlPath + "\" /F", ChangeTimeoutMilliseconds, out exitCode);
                try { File.Delete(xmlPath); } catch { }
                AppLog.Write("autostart enable -> exit=" + exitCode + " " + output);
                return exitCode == 0 ? "" : output;
            }
            catch (Exception error)
            {
                AppLog.Write("autostart enable failed: " + error.Message);
                return error.Message;
            }
        }

        public static string Disable()
        {
            int exitCode;
            string output = ProcessRunner.RunWithExitCode("schtasks", "/Delete /TN " + TaskName + " /F", ChangeTimeoutMilliseconds, out exitCode);
            AppLog.Write("autostart disable -> exit=" + exitCode + " " + output);
            return exitCode == 0 ? "" : output;
        }

        private static string BuildTaskXml()
        {
            string user = SecurityElement.Escape(WindowsIdentity.GetCurrent().Name);
            string executable = Application.ExecutablePath;
            string folder = SecurityElement.Escape(Path.GetDirectoryName(executable));
            return
"<?xml version=\"1.0\" encoding=\"UTF-16\"?>\r\n" +
"<Task version=\"1.4\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\">\r\n" +
"  <RegistrationInfo><Description>OmenHub autostart (elevated, no UAC prompt)</Description></RegistrationInfo>\r\n" +
"  <Triggers><LogonTrigger><Enabled>true</Enabled><UserId>" + user + "</UserId><Delay>PT10S</Delay></LogonTrigger></Triggers>\r\n" +
"  <Principals><Principal id=\"Author\"><UserId>" + user + "</UserId><LogonType>InteractiveToken</LogonType><RunLevel>HighestAvailable</RunLevel></Principal></Principals>\r\n" +
"  <Settings>\r\n" +
"    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>\r\n" +
"    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>\r\n" +
"    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>\r\n" +
"    <AllowHardTerminate>false</AllowHardTerminate>\r\n" +
"    <StartWhenAvailable>true</StartWhenAvailable>\r\n" +
"    <AllowStartOnDemand>true</AllowStartOnDemand>\r\n" +
"    <Enabled>true</Enabled>\r\n" +
"    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>\r\n" +
"    <Priority>1</Priority>\r\n" +
"  </Settings>\r\n" +
"  <Actions Context=\"Author\"><Exec><Command>" + SecurityElement.Escape(executable) + "</Command><Arguments>/autostart</Arguments><WorkingDirectory>" + folder + "</WorkingDirectory></Exec></Actions>\r\n" +
"</Task>\r\n";
        }
    }
}
