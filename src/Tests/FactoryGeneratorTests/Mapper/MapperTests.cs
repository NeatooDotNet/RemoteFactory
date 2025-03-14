using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Mapper;

[Factory]
public partial class MapperObj
{
	public MapperObj(MapperDto mapperDto)
	{
	  this.MapFrom(mapperDto);
	}

	public string Name { get; set; } = default!;
	public int Age { get; set; }

	public partial void MapTo(MapperDto mapperDto);
	public partial void MapFrom(MapperDto mapperDto);
}

public class MapperDto
{
	public string Name { get; set; } = default!;
	public int Age { get; set; }
}
