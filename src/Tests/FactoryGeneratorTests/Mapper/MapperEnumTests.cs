using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Mapper;


public enum AnimalType
{
	Dog,
	Cat,
	Bird
}

public class MapperEnumDto
{
	public MapperEnumDto(string name, int type)
	{
		this.Name = name;
		this.Type = type;
	}

	public string Name { get; set; }
	public int Type { get; set; }
	public int NullableType { get; set; }
}

[Factory]
public partial class MapperEnumObj
{
	public MapperEnumObj(string name, AnimalType type)
	{
		this.Name = name;
		this.Type = type;
		this.NullableType = type;
	}

	public string Name { get; set; }
	public AnimalType Type { get; set; }

	public AnimalType? NullableType { get; set; }

	public partial void MapTo(MapperEnumDto dto);
	public partial void MapFrom(MapperEnumDto dto);
}


public class MapperEnumTests
{

	[Fact]
	public void TestMapperEnum()
	{
		var obj = new MapperEnumObj("Dog", AnimalType.Dog);
		var dto = new MapperEnumDto("Dog", 0);
		obj.MapTo(dto);
		Assert.Equal(dto.Name, obj.Name);
		Assert.Equal((int)dto.Type, (int)obj.Type);
		obj.MapFrom(dto);
		Assert.Equal(dto.Name, obj.Name);
		Assert.Equal((int)dto.Type, (int)obj.Type);
	}
}
