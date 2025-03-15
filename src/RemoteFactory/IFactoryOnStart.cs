namespace Neatoo.RemoteFactory;

public interface IFactoryOnStart
{
	void FactoryStart(FactoryOperation factoryOperation);
}

public interface IFactoryOnStartAsync
{
	Task FactoryStartAsync(FactoryOperation factoryOperation);
}