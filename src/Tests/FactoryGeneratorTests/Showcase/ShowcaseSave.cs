using Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Showcase;

public interface IShowcaseSave : IFactorySaveMeta
{

}

[Factory]
public class ShowcaseSave : IShowcaseSave
{
	[Create]
	public ShowcaseSave() { }

	public bool IsDeleted { get; set; } = false;

	public bool IsNew { get; set; } = true;

	[Insert]
	public void Insert([Service] IService service) { this.IsNew = false; Assert.NotNull(service); }

	[Update]
	public void Update([Service] IService service) { }

	[Delete]
	public void Delete([Service] IService service) { }

	[Insert]
	public void InsertMatchedByParamType(int a) { this.IsNew = false; }

	[Update]
	public void UpdateMatchedByParamType(int b) { }

	[Delete]
	public void DeleteMatchedByParamType(int c) { }

	[Insert]
	public void InsertNoDeleteNotNullable() { this.IsNew = false; }

	[Update]
	public void UpdateNoDeleteNotNullable() { }

	[Insert]
	public Task InsertTask() { this.IsNew = false; return Task.CompletedTask; }

	[Update]
	public Task UpdateTask() { return Task.CompletedTask; }

	[Delete]
	public Task DeleteTask() { return Task.CompletedTask; }

	[Remote]
	[Insert]
	public void InsertRemote([Service] IServerOnlyService service) { this.IsNew = false; Assert.NotNull(service); }

	[Remote]
	[Update]
	public void UpdateRemote([Service] IServerOnlyService service) { }

}

public class ShowcaseSaveTests : FactoryTestBase<IShowcaseSaveFactory>
{
	[Fact]
	public void ShowcaseSaveTests_Create()
	{
		var result = this.factory.Create();
		Assert.NotNull(result);
		Assert.True(result.IsNew);
	}

	[Fact]
	public void ShowcaseSaveTests_Save()
	{
		var result = this.factory.Create();
		var saved = this.factory.Save(result);
		Assert.False(saved!.IsNew);
	}

	[Fact]
	public void ShowcaseSaveTests_SaveNoDeleteNotNullable()
	{
		var result = this.factory.Create();
		var saved = this.factory.SaveNoDeleteNotNullable(result);
		Assert.False(saved.IsNew);
	}

	[Fact]
	public async Task ShowcaseSaveTests_SaveTask()
	{
		var result = this.factory.Create();
		var saved = await this.factory.SaveTask(result);
		Assert.False(saved!.IsNew);
	}

	[Fact]
	public void ShowcaseSaveTests_SaveMatchedByParamType()
	{
		var result = this.factory.Create();
		var saved = this.factory.SaveMatchedByParamType(result, 1)!;
		Assert.False(saved.IsNew);
	}

	[Fact]
	public async Task ShowcaseSaveTests_SaveRemote()
	{
		var result = this.factory.Create();
		var saved = await this.factory.SaveRemote(result);
		Assert.False(saved.IsNew);
	}
}