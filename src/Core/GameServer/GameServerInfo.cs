namespace HexacowBot.Core.GameServer;

public sealed record class GameServerInfo
{
	public string Name { get; init; } = default!;
	public string Game { get; init; } = default!;
	public string Map { get; init; } = default!;
	public int CurrentPlayers { get; init; }
	public int MaxPlayers { get; init; }
	public long Ping { get; init; }
}
