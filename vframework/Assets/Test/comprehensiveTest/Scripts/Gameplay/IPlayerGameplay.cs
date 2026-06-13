/// <summary>玩家战斗控制（供 PlayerManager 禁用/启用操作，避免与 PlayerTest 循环引用）。</summary>
public interface IPlayerGameplay
{
    void SetGameplayEnabled(bool enabled);
}
