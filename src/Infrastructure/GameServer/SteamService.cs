using HexacowBot.Core.GameServer;
using SteamQueryNet;

namespace Infrastructure.GameServer;

public sealed class SteamService : IGameServerQuery
{
	public async Task<bool> CheckIsRunning(string serverIp, ushort port)
	{
		using (var serverQuery = new ServerQuery())
		{
			serverQuery.Connect(serverIp, port);

			return await Task.FromResult(serverQuery.IsConnected);
		}
	}

	public async Task<GameServerInfo> GetServerInfo(string serverIp, ushort port)
	{
		using (var serverQuery = new ServerQuery())
		{
			serverQuery.Connect(serverIp, port);
			var gameServerInfo = (await serverQuery.GetServerInfoAsync()).ToGameServerInfo();

			return gameServerInfo;
		}
	}
}
