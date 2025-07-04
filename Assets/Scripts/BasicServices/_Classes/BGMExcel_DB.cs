public enum Nation : int
{
    None = 0,
    All = 1,
    JP = 2,
    GL = 3,
    KR = 4,
};
public class BGMExcel_DB
{
    public long Id { get; set; }
    public Nation Nation { get; set; }
    public string Path { get; set; } = "";
    public float Volume { get; set; }
    public float LoopStartTime { get; set; }
    public float LoopEndTime { get; set; }
    public float LoopTranstionTime { get; set; }
    public float LoopOffsetTime { get; set; }
}
