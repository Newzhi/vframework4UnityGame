using System.Collections.Generic;
using System.Globalization;
using BaseFramework.BaseGameRoot;

namespace BaseFramework.BaseCommandSys
{
    /// <summary>
    /// 示例：调整 <see cref="IGameTimeClock.TimeScale"/>（需已注册 GameTimeModule）。
    /// </summary>
    public sealed class TimeScaleCommand : IGameCommand
    {
        public string Name => "time.scale";
        public string Description => "Set game time scale.";
        public string Usage => "time.scale <float>";

        public string Execute(IReadOnlyList<string> args, ICommandContext context)
        {
            if (args.Count == 0)
                return "Missing scale. Usage: " + Usage;

            if (!float.TryParse(args[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float scale))
                return "Invalid scale: " + args[0];

            var clock = context.TryGetService<IGameTimeClock>();
            if (clock == null)
                return "IGameTimeClock not registered.";

            clock.TimeScale = scale;
            return $"TimeScale = {scale}";
        }
    }
}
