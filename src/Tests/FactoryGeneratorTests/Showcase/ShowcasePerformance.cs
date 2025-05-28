using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Showcase;
public class ShowcasePerformance
{
	private static ServiceProvider? serviceProvider;
	private static object lockObj = new object();

	public class ShowcasePerformanceObj
	{
		public static uint TotalCount = 0;

		public ShowcasePerformanceObj()
		{
			this.Id = 1;
			this.Description = Guid.NewGuid().ToString();
			this.ChildA = new ShowcasePerformanceObj(2);
			this.ChildB = new ShowcasePerformanceObj(2);
			TotalCount++;
		}

		public ShowcasePerformanceObj(int id)
		{
			this.Id = id;
			this.Description = Guid.NewGuid().ToString();
			TotalCount++;
			if (id < 20)
			{
				this.ChildA = new ShowcasePerformanceObj(id + 1);
				this.ChildB = new ShowcasePerformanceObj(id + 1);
			}
		}

		public int Id { get; set; }
		public string Description { get; set; }
		public ShowcasePerformanceObj? ChildA { get; set; }
		public ShowcasePerformanceObj? ChildB { get; set; }
	}

	public interface IShowcasePerformanceDIObj
	{
		string Description { get; set; }
		int Id { get; set; }
	}

	public class ShowcasePerformanceDIObj : IShowcasePerformanceDIObj
	{
		public static uint TotalCount = 0;

		public ShowcasePerformanceDIObj(Func<int, IShowcasePerformanceDIObj> factory)
		{
			this.Id = 1;
			this.Description = Guid.NewGuid().ToString();
			this.ChildA = factory(2);
			this.ChildB = factory(2);
			TotalCount++;
		}

		public ShowcasePerformanceDIObj(int id, Func<int, IShowcasePerformanceDIObj> factory)
		{
			this.Id = id;
			this.Description = Guid.NewGuid().ToString();
			TotalCount++;
			if (id < 20)
			{
				this.ChildA = factory(id + 1);
				this.ChildB = factory(id + 1);
			}
		}

		public int Id { get; set; }
		public string Description { get; set; }
		public IShowcasePerformanceDIObj? ChildA { get; set; }
		public IShowcasePerformanceDIObj? ChildB { get; set; }

		public static IShowcasePerformanceDIObj Factory(int id)
		{
			return new ShowcasePerformanceDIObj(id, Factory);
		}
	}

	public interface IShowcasePerformanceNeatooObj
	{
		string Description { get; set; }
		int Id { get; set; }
	}

	[Factory]
	public class ShowcasePerformanceNeatooObj : IShowcasePerformanceNeatooObj
	{
		public static uint TotalCount = 0;

		public ShowcasePerformanceNeatooObj()
		{
			TotalCount++;
		}

		[Create]
		public void Create([Service] IShowcasePerformanceNeatooObjFactory factory)
		{
			this.Id = 1;
			this.Description = Guid.NewGuid().ToString();
			this.ChildA = factory.Create(2);
			this.ChildB = factory.Create(2);
		}

		[Create]
		public void Create(int id, [Service] IShowcasePerformanceNeatooObjFactory factory)
		{
			this.Id = id;
			this.Description = Guid.NewGuid().ToString();
			if (id < 20)
			{
				this.ChildA = factory.Create(id + 1);
				this.ChildB = factory.Create(id + 1);
			}
		}

		public int Id { get; set; }
		public string Description { get; set; } = null!;
		public IShowcasePerformanceNeatooObj? ChildA { get; set; }
		public IShowcasePerformanceNeatooObj? ChildB { get; set; }
	}

	private IServiceScope serviceScope = null!;

	public ShowcasePerformance()
	{
		lock (lockObj)
		{
			if (serviceProvider == null)
			{
				var serviceCollection = new ServiceCollection();

				serviceCollection.AddTransient<IShowcasePerformanceDIObj, ShowcasePerformanceDIObj>();
				serviceCollection.AddSingleton(ShowcasePerformanceDIObj.Factory);

				serviceCollection.AddScoped(typeof(IFactoryCore<>), typeof(FactoryCore<>));

				ShowcasePerformanceNeatooObjFactory.FactoryServiceRegistrar(serviceCollection, NeatooFactory.Logical);

				serviceProvider = serviceCollection.BuildServiceProvider();
			}

		}

		this.serviceScope = serviceProvider.CreateScope();
	}


	[Fact(Skip = "Optional Performance Demo")]
	public void ShowcasePerformance_Obj()
	{
		ShowcasePerformanceObj.TotalCount = 0;
		var result = new ShowcasePerformanceObj();
		Assert.True(ShowcasePerformanceObj.TotalCount == 1048575);
	}

	[Fact(Skip = "Optional Performance Demo")]
	public void ShowcasePerformance_DIObj()
	{
		ShowcasePerformanceDIObj.TotalCount = 0;
		var result = this.serviceScope.ServiceProvider.GetRequiredService<IShowcasePerformanceDIObj>();
		Assert.True(ShowcasePerformanceDIObj.TotalCount == 1048575);
	}

	[Fact(Skip = "Optional Performance Demo")]
	public void ShowcasePerformance_NeatooObj()
	{
		ShowcasePerformanceNeatooObj.TotalCount = 0;
		var factory = this.serviceScope.ServiceProvider.GetRequiredService<IShowcasePerformanceNeatooObjFactory>();
		var result = factory.Create();
		Assert.True(ShowcasePerformanceNeatooObj.TotalCount == 1048575);
	}

}
