using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.IntegrationTests.Showcase;

/// <summary>
/// Performance comparison tests demonstrating RemoteFactory overhead vs raw objects vs DI.
/// These tests are skipped by default as they are for demonstration purposes only.
/// </summary>
public class ShowcasePerformanceTests
{
    private static ServiceProvider? serviceProvider;
    private static readonly object lockObj = new();

    /// <summary>
    /// Plain object with no DI.
    /// </summary>
    public class ShowcasePerformanceObj
    {
        public static uint TotalCount = 0;

        public ShowcasePerformanceObj()
        {
            Id = 1;
            Description = Guid.NewGuid().ToString();
            ChildA = new ShowcasePerformanceObj(2);
            ChildB = new ShowcasePerformanceObj(2);
            TotalCount++;
        }

        public ShowcasePerformanceObj(int id)
        {
            Id = id;
            Description = Guid.NewGuid().ToString();
            TotalCount++;
            if (id < 20)
            {
                ChildA = new ShowcasePerformanceObj(id + 1);
                ChildB = new ShowcasePerformanceObj(id + 1);
            }
        }

        public int Id { get; set; }
        public string Description { get; set; }
        public ShowcasePerformanceObj? ChildA { get; set; }
        public ShowcasePerformanceObj? ChildB { get; set; }
    }

    /// <summary>
    /// Interface for DI-based object.
    /// </summary>
    public interface IShowcasePerformanceDIObj
    {
        string Description { get; set; }
        int Id { get; set; }
    }

    /// <summary>
    /// DI-based object using Func factory delegate.
    /// </summary>
    public class ShowcasePerformanceDIObj : IShowcasePerformanceDIObj
    {
        public static uint TotalCount = 0;

        public ShowcasePerformanceDIObj(Func<int, IShowcasePerformanceDIObj> factory)
        {
            Id = 1;
            Description = Guid.NewGuid().ToString();
            ChildA = factory(2);
            ChildB = factory(2);
            TotalCount++;
        }

        public ShowcasePerformanceDIObj(int id, Func<int, IShowcasePerformanceDIObj> factory)
        {
            Id = id;
            Description = Guid.NewGuid().ToString();
            TotalCount++;
            if (id < 20)
            {
                ChildA = factory(id + 1);
                ChildB = factory(id + 1);
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

    /// <summary>
    /// Interface for Neatoo-based object.
    /// </summary>
    public interface IShowcasePerformanceNeatooObj
    {
        string Description { get; set; }
        int Id { get; set; }
    }

    /// <summary>
    /// Neatoo RemoteFactory-based object.
    /// </summary>
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
            Id = 1;
            Description = Guid.NewGuid().ToString();
            ChildA = factory.Create(2);
            ChildB = factory.Create(2);
        }

        [Create]
        public void Create(int id, [Service] IShowcasePerformanceNeatooObjFactory factory)
        {
            Id = id;
            Description = Guid.NewGuid().ToString();
            if (id < 20)
            {
                ChildA = factory.Create(id + 1);
                ChildB = factory.Create(id + 1);
            }
        }

        public int Id { get; set; }
        public string Description { get; set; } = null!;
        public IShowcasePerformanceNeatooObj? ChildA { get; set; }
        public IShowcasePerformanceNeatooObj? ChildB { get; set; }
    }

    private IServiceScope serviceScope = null!;

    public ShowcasePerformanceTests()
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

        serviceScope = serviceProvider.CreateScope();
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
        var result = serviceScope.ServiceProvider.GetRequiredService<IShowcasePerformanceDIObj>();
        Assert.True(ShowcasePerformanceDIObj.TotalCount == 1048575);
    }

    [Fact(Skip = "Optional Performance Demo")]
    public void ShowcasePerformance_NeatooObj()
    {
        ShowcasePerformanceNeatooObj.TotalCount = 0;
        var factory = serviceScope.ServiceProvider.GetRequiredService<IShowcasePerformanceNeatooObjFactory>();
        var result = factory.Create();
        Assert.True(ShowcasePerformanceNeatooObj.TotalCount == 1048575);
    }
}
