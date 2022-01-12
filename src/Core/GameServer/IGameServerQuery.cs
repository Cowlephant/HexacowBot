namespace HexacowBot.Core.GameServer;

public interface IGameServerQuery
{
	public Task<bool> CheckIsRunning(string serverIp, ushort port);
	public Task<GameServerInfo> GetServerInfo(string serverIp, ushort port);
}
