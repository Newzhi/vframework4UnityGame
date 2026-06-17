namespace BaseLayer.Input
{
    /// <summary>
    /// 全局输入快照服务；由 <see cref="Impt.InputModule"/> 每帧写入，其它 Module 只读。
    /// </summary>
    public interface IInputService
    {
        InputSnapshot Current { get; }
        InputSnapshot Previous { get; }
        InputContext Context { get; set; }
    }
}
