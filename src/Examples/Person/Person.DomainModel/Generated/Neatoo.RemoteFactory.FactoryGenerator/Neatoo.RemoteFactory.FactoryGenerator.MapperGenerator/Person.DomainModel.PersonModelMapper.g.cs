#nullable enable
using Neatoo.RemoteFactory.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Neatoo.RemoteFactory;
using Person.Ef;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Person.DomainModel;
/*
Class Property Id int found
Class Property FirstName string? found
Class Property LastName string? found
Class Property Email string? found
Class Property Phone string? found
Class Property Notes string? found
Class Property Created System.DateTime found
Class Property Modified System.DateTime found
Class Property IsDeleted bool found
Class Property IsNew bool found
Method MapFrom is a Match
Parameter personEntity Person.Ef.PersonEntity found for MapFrom
Parameter Property FirstName string found
Parameter Property LastName string found
Parameter Property Email string found
Parameter Property Phone string found
Parameter Property Notes string? found
Parameter Property Created System.DateTime found
Parameter Property Modified System.DateTime found
Parameter Property Id int found
Method MapTo is a Match
Parameter personEntity Person.Ef.PersonEntity found for MapTo
Parameter Property FirstName string found
Parameter Property LastName string found
Parameter Property Email string found
Parameter Property Phone string found
Parameter Property Notes string? found
Parameter Property Created System.DateTime found
Parameter Property Modified System.DateTime found
Parameter Property Id int found

*/
internal partial class PersonModel
{
    public partial void MapFrom(PersonEntity personEntity)
    {
        this.FirstName = personEntity.FirstName;
        this.LastName = personEntity.LastName;
        this.Email = personEntity.Email;
        this.Phone = personEntity.Phone;
        this.Notes = personEntity.Notes;
        this.Created = personEntity.Created;
        this.Modified = personEntity.Modified;
        this.Id = personEntity.Id;
    }

    public partial void MapTo(PersonEntity personEntity)
    {
        personEntity.FirstName = this.FirstName ?? throw new NullReferenceException("Person.DomainModel.PersonModel.FirstName");
        personEntity.LastName = this.LastName ?? throw new NullReferenceException("Person.DomainModel.PersonModel.LastName");
        personEntity.Email = this.Email ?? throw new NullReferenceException("Person.DomainModel.PersonModel.Email");
        personEntity.Phone = this.Phone ?? throw new NullReferenceException("Person.DomainModel.PersonModel.Phone");
        personEntity.Notes = this.Notes;
        personEntity.Created = this.Created;
        personEntity.Modified = this.Modified;
        personEntity.Id = this.Id;
    }
}