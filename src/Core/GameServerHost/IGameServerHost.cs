namespace HexacowBot.Core.GameServerHost;

public interface IGameServerHost
{
	public string ServerName { get; }

	public bool IsBusy { get; }

	public ServerSize CurrentSize { get; }

	public ServerSize HibernateSize { get; }

	public IEnumerable<ServerSize> ServerSizes { get; }

	public IEnumerable<ServerSize> AllowedSizes { get; }

	public Task<bool> CheckIsStarted();

	public Task<bool> CheckIsHibernating();

	public Task<ServerActionResult> StartServerAsync();

	public Task<ServerActionResult> StopServerAsync();

	public Task<ServerActionResult> RestartServerAsync();

	public Task<ServerActionResult> PowerCycleServerAsync();

	public Task<ServerActionResult> ScaleServerAsync(ServerSize size);

	public Task<ServerActionResult> HibernateServerAsync();

	public Task<decimal> GetMonthToDateBalanceAsync();

	public Task<ServerSize> GetCurrentSizeAsync();

	public Task<ServerSize?> GetServerSizeAsync(string size);
}
