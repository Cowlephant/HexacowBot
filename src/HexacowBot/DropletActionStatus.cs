using Ardalis.SmartEnum;

namespace HexacowBot;

public sealed class DropletActionStatus : SmartEnum<DropletActionStatus>
{
	public static DropletActionStatus Completed = new DropletActionStatus("completed", 1);
	public static DropletActionStatus InProgress = new DropletActionStatus("in-progress", 2);
	public static DropletActionStatus Pending = new DropletActionStatus("pending", 3);
	public static DropletActionStatus Errored = new DropletActionStatus("errored", 4);

	public DropletActionStatus(string name, int value) : base(name, value)
	{
	}
}
