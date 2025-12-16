using System.Management;
using LlamaApi.Core.Domain;

namespace LlamaApi.Services.Hardware;

public class HardwareDetectionService
{
    private HardwareInfo? _hardwareInfo;

    public void DetectHardware()
    {
        var gpuName = "Unknown";
        var vramBytes = 0UL;
        var cudaCapable = false;

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            foreach (ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString() ?? "Unknown";
                var adapterRam = obj["AdapterRAM"]?.ToString();
                if (ulong.TryParse(adapterRam, out var ram))
                {
                    gpuName = name;
                    vramBytes = ram;
                    cudaCapable = name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase);
                    break;
                }
            }
        }
        catch
        {
            // ignored
        }

        var cpuName = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Unknown";
        var cpuCores = Environment.ProcessorCount;

        _hardwareInfo = new HardwareInfo
        {
            GpuName = gpuName,
            VramBytes = vramBytes,
            CudaCapable = cudaCapable,
            CpuName = cpuName,
            CpuCores = cpuCores
        };
    }

    public HardwareInfo GetHardwareInfo() => _hardwareInfo ?? new HardwareInfo();

    public (int nGpuLayers, int nCtx, int batch) GetHeuristicDefaults(int modelSizeB = 7)
    {
        var hw = GetHardwareInfo();
        var vramGb = (int)(hw.VramBytes / (1024UL * 1024 * 1024));

        if (vramGb >= 20)
        {
            var layers = modelSizeB <= 13 ? int.MaxValue : (int)(modelSizeB * 0.65);
            return (layers, 10000, 64);
        }
        else if (vramGb >= 12)
        {
            var layers = modelSizeB <= 7 ? int.MaxValue : 40;
            return (layers, 9000, 64);
        }
        else
        {
            return (8, 6000, 32);
        }
    }
}
