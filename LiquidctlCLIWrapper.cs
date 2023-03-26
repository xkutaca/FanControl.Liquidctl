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
        private static bool hasLastCallFailed = false;

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
            string line = process.StandardOutput.ReadLine();
            // restart if liquidctl crashed
            if (line == null) {
                Initialize();
                process = RestartLiquidCtlBackend(process, address);
                process.StandardInput.WriteLine("status");
                line = process.StandardOutput.ReadLine();
                if (line == null) {
                    throw new Exception($"liquidctl returns empty line. Remaining stdout:\n{process.StandardOutput.ReadToEnd()} Last stderr output:\n{process.StandardError.ReadToEnd()}");
                }
            }
            JObject result = JObject.Parse(line);
            string status = (string)result.SelectToken("status");
            hasLastCallFailed = false;
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

        private static Process RestartLiquidCtlBackend(Process oldProcess, string address) {
            liquidctlBackends.Remove(address);
            try {
                oldProcess.StandardInput.WriteLine("exit");
                oldProcess.WaitForExit(200);
            } catch (Exception) {
                if (!oldProcess.HasExited)
                    oldProcess.Kill();
            }
            return GetLiquidCtlBackend(address);
        }

        private static Process GetLiquidCtlBackend(string address) {
            Process process = liquidctlBackends.ContainsKey(address) ? liquidctlBackends[address] : null;
            if (process != null && !process.HasExited) {
                return process;
            }

            if (process != null) {
                liquidctlBackends.Remove(address);
            }

            KeyValuePair<string, string> identifier = LiquidctlStatusJSON.GetBusAndAddress(address);

            process = new Process();

            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;

            process.StartInfo.FileName = liquidctlexe;
            switch (identifier.Key) {
                case "usb":
                    process.StartInfo.Arguments = $"--json --usb-port {identifier.Value} interactive";
                    break;
                case "hid":
                    process.StartInfo.Arguments = $"--json --address {address} interactive";
                    break;
            }

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
                // try to initialize again
                if (process.ExitCode == 1 && !hasLastCallFailed) {
                    hasLastCallFailed = true;
                    Initialize();
                    return LiquidctlCall(arguments);
                }
                throw new Exception($"liquidctl returned non-zero exit code {process.ExitCode}. Last stderr output:\n{process.StandardError.ReadToEnd()}");
            }

            hasLastCallFailed = false;

            return process;
        }
    }
}
