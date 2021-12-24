using Ardalis.SmartEnum;

namespace HexacowBot.Core.GameServer;

public sealed class DropletStatus : SmartEnum<DropletStatus>
{
	public static DropletStatus Unknown = new DropletStatus(nameof(Unknown), 0);
	public static DropletStatus Active = new DropletStatus("active", 1);
	public static DropletStatus Off = new DropletStatus("off", 2);

	public DropletStatus(string name, int value) : base(name, value)
	{
	}
}
