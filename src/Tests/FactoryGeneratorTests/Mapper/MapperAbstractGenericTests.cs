using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Mapper;

public class MapperAbstractGenericDto
{
	public int Value { get; set; }
	public int Number { get; set; }
}

[Factory]
public abstract partial class MapperAbstractGenericObj<T>
{
	public int Value { get; set; }

	public int Number { get; set; }

	public partial void MapTo(MapperAbstractGenericDto mapperIgnoreAttributeDto);
	public partial void MapFrom(MapperAbstractGenericDto mapperIgnoreAttributeDto);
}

class MapperAbstractGenericTests
{
 }
