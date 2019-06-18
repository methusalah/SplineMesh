using System;
using System.Diagnostics;

namespace RockVR.Common
{
    public class CmdProcess
    {
        public static void Run(string procName, string arguments)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = procName;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.Start();
                UnityEngine.Debug.Log(process.StandardOutput.ReadToEnd());
                process.WaitForExit();
                process.Close();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e.Message);
            }
        }
    }
}