using DigitalOcean.API.Models.Responses;

namespace HexacowBot.Core.GameServer;

public static class DigitalOceanExtensions
{
	public static ServerSize ToServerSize(this Size size)
	{
		return new ServerSize
		{
			Slug = size.Slug,
			Transfer = size.Transfer,
			Memory = size.Memory,
			Vcpus = size.Vcpus,
			Disk = size.Disk,
			PriceHourly = size.PriceHourly,
			PriceMonthly = size.PriceMonthly
		};
	}
}
