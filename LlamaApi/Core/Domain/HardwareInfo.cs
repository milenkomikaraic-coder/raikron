namespace LlamaApi.Core.Domain;

public class HardwareInfo
{
    public string GpuName { get; set; } = "Unknown";
    public ulong VramBytes { get; set; }
    public bool CudaCapable { get; set; }
    public string CpuName { get; set; } = "Unknown";
    public int CpuCores { get; set; }
}
