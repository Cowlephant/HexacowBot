using HexacowBot.Core.GameServer;
using SteamQueryNet.Models;

namespace Infrastructure.GameServer;

public static class SteamQueryNetExtensions
{
	public static GameServerInfo ToGameServerInfo(this ServerInfo serverInfo)
	{
		var gameServerInfo = new GameServerInfo
		{
			Name = serverInfo.Name,
			Game = serverInfo.Game,
			Map = serverInfo.Map,
			CurrentPlayers = serverInfo.Players,
			MaxPlayers = serverInfo.MaxPlayers,
			Ping = serverInfo.Ping
		};

		return gameServerInfo;
	}
}
