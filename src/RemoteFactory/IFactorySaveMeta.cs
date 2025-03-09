namespace Neatoo.RemoteFactory;

public interface IFactorySaveMeta
{
   bool IsDeleted { get; }
   bool IsNew { get; }
}