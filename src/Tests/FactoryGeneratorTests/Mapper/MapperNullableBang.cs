using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Mapper;

public class MapperNullableBangDto
{
	public int Value { get; set; }
	public List<int> Reference { get; set; } = new List<int>();

	public int? Value_ { get; set; }
	public List<int>? Reference_ { get; set; }
}

[Factory]
public partial class MapperNullableBangObj
{
	public int? Value { get; set; }
	public List<int>? Reference { get; set; }

	public int Value_ { get; set; }
	public List<int> Reference_ { get; set; } = new List<int>();

	public partial void MapTo(MapperNullableBangDto dto);

	public partial void MapFrom(MapperNullableBangDto dto);
}

