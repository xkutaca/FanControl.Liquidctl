using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FanControl.Liquidctl
{
    internal static class LiquidctlCLIWrapper
    {
        public static string liquidctlexe = "Plugins\\liquidctl.exe"; //TODO extract path to executable to config

        private static Dictionary<string, Process> liquidctlBackends = new Dictionary<string, Process>();

        internal static void Initialize() {
            LiquidctlCall($"--json initialize all");
        }
        internal static List<LiquidctlStatusJSON> ReadStatus() {
            Process process = LiquidctlCall($"--json status");
            return JsonConvert.DeserializeObject<List<LiquidctlStatusJSON>>(process.StandardOutput.ReadToEnd());
        }
        internal static List<LiquidctlStatusJSON> ReadStatus(string address) {
            Process process = GetLiquidCtlBackend(address);
            process.StandardInput.WriteLine("status");
            JObject result = JObject.Parse(process.StandardOutput.ReadLine());
            string status = (string)result.SelectToken("status");
            if (status == "success")
                return result.SelectToken("data").ToObject<List<LiquidctlStatusJSON>>();
            throw new Exception((string)result.SelectToken("data"));
        }
        internal static void SetPump(string address, int value) {
            Process process = GetLiquidCtlBackend(address);
            process.StandardInput.WriteLine($"set pump speed {(value)}");
            JObject result = JObject.Parse(process.StandardOutput.ReadLine());
            string status = (string)result.SelectToken("status");
            if (status == "success")
                return;
            throw new Exception((string)result.SelectToken("data"));
        }

        private static Process GetLiquidCtlBackend(string address) {
            Process process = liquidctlBackends.ContainsKey(address) ? liquidctlBackends[address] : null;
            if (process != null && !process.HasExited) {
                return process;
            }

            if (process != null) {
                liquidctlBackends.Remove(address);
            }

            process = new Process();

            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;

            process.StartInfo.FileName = liquidctlexe;
            process.StartInfo.Arguments = $"--json --address {address} interactive";

            liquidctlBackends.Add(address, process);

            process.Start();

            return process;
        }

        private static Process LiquidctlCall(string arguments) {
            Process process = new Process();

            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.StartInfo.FileName = liquidctlexe;
            process.StartInfo.Arguments = arguments;

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0) {
                throw new Exception($"liquidctl returned non-zero exit code {process.ExitCode}. Last stderr output:\n{process.StandardError.ReadToEnd()}");
            }

            return process;
        }
    }
}
