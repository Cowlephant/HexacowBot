namespace HexacowBot.Core.GameServerHost
{
	public interface IGameServerQuery
	{
		public Task<bool> CheckIsRunning(string serverIp, int port);
		public Task<int> GetPlayerCount();
	}
}