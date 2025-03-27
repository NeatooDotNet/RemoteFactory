using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Mapper;

public class MapperIgnoreAttributeDto
{
	public string Value { get; set; } = "";
	public int Number { get; set; }
}

[Factory]
public partial class MapperIgnoreAttributeObj
{
	// If this was included it would cause an error because the types don't match
	[MapperIgnore]
	public int Value { get; set; }

	public int Number { get; set; }

	public partial void MapTo(MapperIgnoreAttributeDto mapperIgnoreAttributeDto);
	public partial void MapFrom(MapperIgnoreAttributeDto mapperIgnoreAttributeDto);
}
