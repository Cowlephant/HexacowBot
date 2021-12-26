namespace HexacowBot.Core.GameServerHost;

public sealed record class ServerSize()
{
	public string Slug { get; init; } = default!;
	public double Transfer { get; init; }
	public int Memory { get; init; }
	public int Vcpus { get; init; }
	public int Disk { get; init; }
	public decimal PriceHourly { get; init; }
	public decimal PriceMonthly { get; init; }
}
