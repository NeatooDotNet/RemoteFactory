namespace Neatoo.RemoteFactory;

/// <summary>
/// Provides state properties that RemoteFactory uses to route Save operations.
/// The generated factory's Save method examines IsNew and IsDeleted to determine
/// whether to call Insert, Update, or Delete.
/// </summary>
/// <remarks>
/// Implement this interface on types that need Save operation routing:
/// <list type="bullet">
/// <item>IsNew = true, IsDeleted = false → Insert</item>
/// <item>IsNew = false, IsDeleted = false → Update</item>
/// <item>IsNew = false, IsDeleted = true → Delete</item>
/// <item>IsNew = true, IsDeleted = true → No operation (new item deleted before save)</item>
/// </list>
/// </remarks>
public interface IFactorySaveMeta
{
   /// <summary>
   /// When true and IsNew is false, the Save method routes to the Delete operation.
   /// </summary>
   bool IsDeleted { get; }

   /// <summary>
   /// When true, the Save method routes to the Insert operation (unless IsDeleted is also true).
   /// </summary>
   bool IsNew { get; }
}