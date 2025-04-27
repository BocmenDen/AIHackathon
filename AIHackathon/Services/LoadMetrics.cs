using AIHackathon.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;

namespace AIHackathon.Services
{
    [Service(ServiceType.Singleton)]
    public class LoadMetrics(IOptions<Settings> options)
    {
        public async Task<MetricResult> Load(string pathOutputFile)
        {
            using Process process = new();
            process.StartInfo.FileName = options.Value.ScriptMetricGenerator;
            process.StartInfo.Arguments = string.Format(options.Value.ScriptMetricGeneratorArgLine, pathOutputFile);
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            static MetricResult createError(string error)
            {
                return new MetricResult()
                {
                    Error = error
                };
            }

            try
            {
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                if(!string.IsNullOrWhiteSpace(error))
                    return createError(error);
                await process.WaitForExitAsync();
                var metric = JsonConvert.DeserializeObject<MetricResult?>(output);
                return metric!.Value;
            }
            catch (Exception ex)
            {
                return createError(ex.Message);
            }
        }
    }
    public struct MetricResult
    {
        public double Accuracy { get; set; }
        public string? Error { get; set; }

        public readonly bool IsError => !string.IsNullOrWhiteSpace(Error);
    }
}
