﻿#nullable enable
using Neatoo.RemoteFactory.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Mapper;
/*
READONLY CODE DO NOT MODIFY!!
This code is generated by the Neatoo.RemoteFactory.MapperGenerator.
*/
public partial class MapperNullableBangObj
{
    public partial void MapTo(MapperNullableBangDto dto)
    {
        dto.Value = this.Value ?? throw new NullReferenceException("Neatoo.RemoteFactory.FactoryGeneratorTests.Mapper.MapperNullableBangObj.Value");
        dto.Reference = this.Reference ?? throw new NullReferenceException("Neatoo.RemoteFactory.FactoryGeneratorTests.Mapper.MapperNullableBangObj.Reference");
        dto.Value_ = this.Value_;
        dto.Reference_ = this.Reference_;
    }

    public partial void MapFrom(MapperNullableBangDto dto)
    {
        this.Value = dto.Value;
        this.Reference = dto.Reference;
        this.Value_ = dto.Value_ ?? throw new NullReferenceException("Neatoo.RemoteFactory.FactoryGeneratorTests.Mapper.MapperNullableBangDto.Value_");
        this.Reference_ = dto.Reference_ ?? throw new NullReferenceException("Neatoo.RemoteFactory.FactoryGeneratorTests.Mapper.MapperNullableBangDto.Reference_");
    }
}