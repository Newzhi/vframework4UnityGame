namespace BaseLayer.Input
{
    /// <summary>
    /// 平台采集：键鼠 / 触摸等实现，写入 <see cref="InputSnapshot"/>。
    /// </summary>
    public interface IInputDeviceProvider
    {
        void Collect(ref InputSnapshot snapshot);
    }
}
