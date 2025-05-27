//using Microsoft.Extensions.DependencyInjection;
//using System.Reflection;
//using System.Text.RegularExpressions;

//namespace Neatoo.RemoteFactory.FactoryGeneratorTests;

//public class AuthorizationAllCombinationTests
//{
//	public abstract class IDd
//	{
//		public string UniqueIdentifier { get; set; } = Guid.NewGuid().ToString();

//		public override bool Equals(object? obj)
//		{
//			if (obj is IDd idd)
//			{
//				return this.UniqueIdentifier == idd.UniqueIdentifier;
//			}
//			return base.Equals(obj);
//		}

//		public override int GetHashCode()
//		{
//			return this.UniqueIdentifier.GetHashCode();
//		}
//	}

//	public class VoidBool : IDd { }
//	public class VoidString : IDd { }
//	public class VoidTaskBool : IDd { }
//	public class VoidTaskString : IDd { }

//	public class TrueBoolBool : IDd { }
//	public class TrueBoolString : IDd { }
//	public class TrueBoolTaskBool : IDd { }
//	public class TrueBoolTaskString : IDd { }

//	public class FalseBoolBool : IDd { }
//	public class FalseBoolString : IDd { }
//	public class FalseBoolTaskBool : IDd { }
//	public class FalseBoolTaskString : IDd { }

//	public class TaskVoidBool : IDd { }
//	public class TaskVoidString : IDd { }
//	public class TaskVoidTaskBool : IDd { }
//	public class TaskVoidTaskString : IDd { }

//	public class TaskTrueBoolBool : IDd { }
//	public class TaskTrueBoolString : IDd { }
//	public class TaskTrueBoolTaskBool : IDd { }
//	public class TaskTrueBoolTaskString : IDd { }

//	public class TaskFalseBoolBool : IDd { }
//	public class TaskFalseBoolString : IDd { }
//	public class TaskFalseBoolTaskBool : IDd { }
//	public class TaskFalseBoolTaskString : IDd { }

//	public class VoidBoolRemote : IDd { }
//	public class VoidStringRemote : IDd { }
//	public class VoidTaskBoolRemote : IDd { }
//	public class VoidTaskStringRemote : IDd { }

//	public class TrueBoolBoolRemote : IDd { }
//	public class TrueBoolStringRemote : IDd { }
//	public class TrueBoolTaskBoolRemote : IDd { }
//	public class TrueBoolTaskStringRemote : IDd { }

//	public class FalseBoolBoolRemote : IDd { }
//	public class FalseBoolStringRemote : IDd { }
//	public class FalseBoolTaskBoolRemote : IDd { }
//	public class FalseBoolTaskStringRemote : IDd { }

//	public class TaskVoidBoolRemote : IDd { }
//	public class TaskVoidStringRemote : IDd { }
//	public class TaskVoidTaskBoolRemote : IDd { }
//	public class TaskVoidTaskStringRemote : IDd { }

//	public class TaskTrueBoolBoolRemote : IDd { }
//	public class TaskTrueBoolStringRemote : IDd { }
//	public class TaskTrueBoolTaskBoolRemote : IDd { }
//	public class TaskTrueBoolTaskStringRemote : IDd { }

//	public class TaskFalseBoolBoolRemote : IDd { }
//	public class TaskFalseBoolStringRemote : IDd { }
//	public class TaskFalseBoolTaskBoolRemote : IDd { }
//	public class TaskFalseBoolTaskStringRemote : IDd { }

//	public class RemoteVoidBool : IDd { }
//	public class RemoteVoidString : IDd { }
//	public class RemoteVoidTaskBool : IDd { }
//	public class RemoteVoidTaskString : IDd { }

//	public class RemoteTrueBoolBool : IDd { }
//	public class RemoteTrueBoolString : IDd { }
//	public class RemoteTrueBoolTaskBool : IDd { }
//	public class RemoteTrueBoolTaskString : IDd { }

//	public class RemoteFalseBoolBool : IDd { }
//	public class RemoteFalseBoolString : IDd { }
//	public class RemoteFalseBoolTaskBool : IDd { }
//	public class RemoteFalseBoolTaskString : IDd { }

//	public class RemoteTaskVoidBool : IDd { }
//	public class RemoteTaskVoidString : IDd { }
//	public class RemoteTaskVoidTaskBool : IDd { }
//	public class RemoteTaskVoidTaskString : IDd { }

//	public class RemoteTaskTrueBoolBool : IDd { }
//	public class RemoteTaskTrueBoolString : IDd { }
//	public class RemoteTaskTrueBoolTaskBool : IDd { }
//	public class RemoteTaskTrueBoolTaskString : IDd { }

//	public class RemoteTaskFalseBoolBool : IDd { }
//	public class RemoteTaskFalseBoolString : IDd { }
//	public class RemoteTaskFalseBoolTaskBool : IDd { }
//	public class RemoteTaskFalseBoolTaskString : IDd { }

//	public class RemoteVoidBoolRemote : IDd { }
//	public class RemoteVoidStringRemote : IDd { }
//	public class RemoteVoidTaskBoolRemote : IDd { }
//	public class RemoteVoidTaskStringRemote : IDd { }

//	public class RemoteTrueBoolBoolRemote : IDd { }
//	public class RemoteTrueBoolStringRemote : IDd { }
//	public class RemoteTrueBoolTaskBoolRemote : IDd { }
//	public class RemoteTrueBoolTaskStringRemote : IDd { }

//	public class RemoteFalseBoolBoolRemote : IDd { }
//	public class RemoteFalseBoolStringRemote : IDd { }
//	public class RemoteFalseBoolTaskBoolRemote : IDd { }
//	public class RemoteFalseBoolTaskStringRemote : IDd { }

//	public class RemoteTaskVoidBoolRemote : IDd { }
//	public class RemoteTaskVoidStringRemote : IDd { }
//	public class RemoteTaskVoidTaskBoolRemote : IDd { }
//	public class RemoteTaskVoidTaskStringRemote : IDd { }

//	public class RemoteTaskTrueBoolBoolRemote : IDd { }
//	public class RemoteTaskTrueBoolStringRemote : IDd { }
//	public class RemoteTaskTrueBoolTaskBoolRemote : IDd { }
//	public class RemoteTaskTrueBoolTaskStringRemote : IDd { }

//	public class RemoteTaskFalseBoolBoolRemote : IDd { }
//	public class RemoteTaskFalseBoolStringRemote : IDd { }
//	public class RemoteTaskFalseBoolTaskBoolRemote : IDd { }
//	public class RemoteTaskFalseBoolTaskStringRemote : IDd { }

//	public class VoidBoolDeny : IDd { }
//	public class VoidStringDeny : IDd { }
//	public class VoidTaskBoolDeny : IDd { }
//	public class VoidTaskStringDeny : IDd { }

//	public class TrueBoolBoolDeny : IDd { }
//	public class TrueBoolStringDeny : IDd { }
//	public class TrueBoolTaskBoolDeny : IDd { }
//	public class TrueBoolTaskStringDeny : IDd { }

//	public class FalseBoolBoolDeny : IDd { }
//	public class FalseBoolStringDeny : IDd { }
//	public class FalseBoolTaskBoolDeny : IDd { }
//	public class FalseBoolTaskStringDeny : IDd { }

//	public class TaskVoidBoolDeny : IDd { }
//	public class TaskVoidStringDeny : IDd { }
//	public class TaskVoidTaskBoolDeny : IDd { }
//	public class TaskVoidTaskStringDeny : IDd { }

//	public class TaskTrueBoolBoolDeny : IDd { }
//	public class TaskTrueBoolStringDeny : IDd { }
//	public class TaskTrueBoolTaskBoolDeny : IDd { }
//	public class TaskTrueBoolTaskStringDeny : IDd { }

//	public class TaskFalseBoolBoolDeny : IDd { }
//	public class TaskFalseBoolStringDeny : IDd { }
//	public class TaskFalseBoolTaskBoolDeny : IDd { }
//	public class TaskFalseBoolTaskStringDeny : IDd { }

//	public class VoidBoolRemoteDeny : IDd { }
//	public class VoidStringRemoteDeny : IDd { }
//	public class VoidTaskBoolRemoteDeny : IDd { }
//	public class VoidTaskStringRemoteDeny : IDd { }

//	public class TrueBoolBoolRemoteDeny : IDd { }
//	public class TrueBoolStringRemoteDeny : IDd { }
//	public class TrueBoolTaskBoolRemoteDeny : IDd { }
//	public class TrueBoolTaskStringRemoteDeny : IDd { }

//	public class FalseBoolBoolRemoteDeny : IDd { }
//	public class FalseBoolStringRemoteDeny : IDd { }
//	public class FalseBoolTaskBoolRemoteDeny : IDd { }
//	public class FalseBoolTaskStringRemoteDeny : IDd { }

//	public class TaskVoidBoolRemoteDeny : IDd { }
//	public class TaskVoidStringRemoteDeny : IDd { }
//	public class TaskVoidTaskBoolRemoteDeny : IDd { }
//	public class TaskVoidTaskStringRemoteDeny : IDd { }

//	public class TaskTrueBoolBoolRemoteDeny : IDd { }
//	public class TaskTrueBoolStringRemoteDeny : IDd { }
//	public class TaskTrueBoolTaskBoolRemoteDeny : IDd { }
//	public class TaskTrueBoolTaskStringRemoteDeny : IDd { }

//	public class TaskFalseBoolBoolRemoteDeny : IDd { }
//	public class TaskFalseBoolStringRemoteDeny : IDd { }
//	public class TaskFalseBoolTaskBoolRemoteDeny : IDd { }
//	public class TaskFalseBoolTaskStringRemoteDeny : IDd { }

//	public class RemoteVoidBoolDeny : IDd { }
//	public class RemoteVoidStringDeny : IDd { }
//	public class RemoteVoidTaskBoolDeny : IDd { }
//	public class RemoteVoidTaskStringDeny : IDd { }

//	public class RemoteTrueBoolBoolDeny : IDd { }
//	public class RemoteTrueBoolStringDeny : IDd { }
//	public class RemoteTrueBoolTaskBoolDeny : IDd { }
//	public class RemoteTrueBoolTaskStringDeny : IDd { }

//	public class RemoteFalseBoolBoolDeny : IDd { }
//	public class RemoteFalseBoolStringDeny : IDd { }
//	public class RemoteFalseBoolTaskBoolDeny : IDd { }
//	public class RemoteFalseBoolTaskStringDeny : IDd { }

//	public class RemoteTaskVoidBoolDeny : IDd { }
//	public class RemoteTaskVoidStringDeny : IDd { }
//	public class RemoteTaskVoidTaskBoolDeny : IDd { }
//	public class RemoteTaskVoidTaskStringDeny : IDd { }

//	public class RemoteTaskTrueBoolBoolDeny : IDd { }
//	public class RemoteTaskTrueBoolStringDeny : IDd { }
//	public class RemoteTaskTrueBoolTaskBoolDeny : IDd { }
//	public class RemoteTaskTrueBoolTaskStringDeny : IDd { }

//	public class RemoteTaskFalseBoolBoolDeny : IDd { }
//	public class RemoteTaskFalseBoolStringDeny : IDd { }
//	public class RemoteTaskFalseBoolTaskBoolDeny : IDd { }
//	public class RemoteTaskFalseBoolTaskStringDeny : IDd { }

//	public class RemoteVoidBoolRemoteDeny : IDd { }
//	public class RemoteVoidStringRemoteDeny : IDd { }
//	public class RemoteVoidTaskBoolRemoteDeny : IDd { }
//	public class RemoteVoidTaskStringRemoteDeny : IDd { }

//	public class RemoteTrueBoolBoolRemoteDeny : IDd { }
//	public class RemoteTrueBoolStringRemoteDeny : IDd { }
//	public class RemoteTrueBoolTaskBoolRemoteDeny : IDd { }
//	public class RemoteTrueBoolTaskStringRemoteDeny : IDd { }

//	public class RemoteFalseBoolBoolRemoteDeny : IDd { }
//	public class RemoteFalseBoolStringRemoteDeny : IDd { }
//	public class RemoteFalseBoolTaskBoolRemoteDeny : IDd { }
//	public class RemoteFalseBoolTaskStringRemoteDeny : IDd { }

//	public class RemoteTaskVoidBoolRemoteDeny : IDd { }
//	public class RemoteTaskVoidStringRemoteDeny : IDd { }
//	public class RemoteTaskVoidTaskBoolRemoteDeny : IDd { }
//	public class RemoteTaskVoidTaskStringRemoteDeny : IDd { }

//	public class RemoteTaskTrueBoolBoolRemoteDeny : IDd { }
//	public class RemoteTaskTrueBoolStringRemoteDeny : IDd { }
//	public class RemoteTaskTrueBoolTaskBoolRemoteDeny : IDd { }
//	public class RemoteTaskTrueBoolTaskStringRemoteDeny : IDd { }

//	public class RemoteTaskFalseBoolBoolRemoteDeny : IDd { }
//	public class RemoteTaskFalseBoolStringRemoteDeny : IDd { }
//	public class RemoteTaskFalseBoolTaskBoolRemoteDeny : IDd { }
//	public class RemoteTaskFalseBoolTaskStringRemoteDeny : IDd { }

//	public class AuthorizationAllCombinations
//	{
//		public List<object> ReadReceived { get; set; } = new List<object>();
//		public List<object> WriteReceived { get; set; } = new List<object>();

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(VoidBool v) { this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(VoidString v) { this.ReadReceived.Add(v); return null; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(VoidTaskBool v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(VoidTaskString v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(VoidBool v) { this.WriteReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(VoidString v) { this.WriteReceived.Add(v); return null; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(VoidTaskBool v) { await Task.Yield(); this.WriteReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(VoidTaskString v) { await Task.Yield(); this.WriteReceived.Add(v); return string.Empty; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(TrueBoolBool v) { this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(TrueBoolString v) { this.ReadReceived.Add(v); return null; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TrueBoolTaskBool v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TrueBoolTaskString v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(TrueBoolBool v) { this.WriteReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(TrueBoolString v) { this.WriteReceived.Add(v); return null; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(TrueBoolTaskBool v) { await Task.Yield(); this.WriteReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(TrueBoolTaskString v) { await Task.Yield(); this.WriteReceived.Add(v); return string.Empty; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(FalseBoolBool v) { this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(FalseBoolString v) { this.ReadReceived.Add(v); return null; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(FalseBoolTaskBool v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(FalseBoolTaskString v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(FalseBoolBool v) { this.WriteReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(FalseBoolString v) { this.WriteReceived.Add(v); return null; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(FalseBoolTaskBool v) { await Task.Yield(); this.WriteReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(FalseBoolTaskString v) { await Task.Yield(); this.WriteReceived.Add(v); return string.Empty; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(TaskVoidBool v) { this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(TaskVoidString v) { this.ReadReceived.Add(v); return null; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TaskVoidTaskBool v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TaskVoidTaskString v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(TaskVoidBool v) { this.WriteReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(TaskVoidString v) { this.WriteReceived.Add(v); return null; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(TaskVoidTaskBool v) { await Task.Yield(); this.WriteReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(TaskVoidTaskString v) { await Task.Yield(); this.WriteReceived.Add(v); return string.Empty; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(TaskTrueBoolBool v) { this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(TaskTrueBoolString v) { this.ReadReceived.Add(v); return null; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TaskTrueBoolTaskBool v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TaskTrueBoolTaskString v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(TaskTrueBoolBool v) { this.WriteReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(TaskTrueBoolString v) { this.WriteReceived.Add(v); return null; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(TaskTrueBoolTaskBool v) { await Task.Yield(); this.WriteReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(TaskTrueBoolTaskString v) { await Task.Yield(); this.WriteReceived.Add(v); return string.Empty; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(TaskFalseBoolBool v) { this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(TaskFalseBoolString v) { this.ReadReceived.Add(v); return null; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TaskFalseBoolTaskBool v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TaskFalseBoolTaskString v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(TaskFalseBoolBool v) { this.WriteReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(TaskFalseBoolString v) { this.WriteReceived.Add(v); return null; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(TaskFalseBoolTaskBool v) { await Task.Yield(); this.WriteReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(TaskFalseBoolTaskString v) { await Task.Yield(); this.WriteReceived.Add(v); return string.Empty; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(VoidBoolRemote v) { this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(VoidStringRemote v) { this.ReadReceived.Add(v); return null; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(VoidTaskBoolRemote v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(VoidTaskStringRemote v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(VoidBoolRemote v) { this.WriteReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(VoidStringRemote v) { this.WriteReceived.Add(v); return null; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(VoidTaskBoolRemote v) { await Task.Yield(); this.WriteReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(VoidTaskStringRemote v) { await Task.Yield(); this.WriteReceived.Add(v); return string.Empty; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(TrueBoolBoolRemote v) { this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(TrueBoolStringRemote v) { this.ReadReceived.Add(v); return null; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TrueBoolTaskBoolRemote v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TrueBoolTaskStringRemote v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(TrueBoolBoolRemote v) { this.WriteReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(TrueBoolStringRemote v) { this.WriteReceived.Add(v); return null; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(TrueBoolTaskBoolRemote v) { await Task.Yield(); this.WriteReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(TrueBoolTaskStringRemote v) { await Task.Yield(); this.WriteReceived.Add(v); return string.Empty; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(FalseBoolBoolRemote v) { this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(FalseBoolStringRemote v) { this.ReadReceived.Add(v); return null; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(FalseBoolTaskBoolRemote v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(FalseBoolTaskStringRemote v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(FalseBoolBoolRemote v) { this.WriteReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(FalseBoolStringRemote v) { this.WriteReceived.Add(v); return null; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(FalseBoolTaskBoolRemote v) { await Task.Yield(); this.WriteReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(FalseBoolTaskStringRemote v) { await Task.Yield(); this.WriteReceived.Add(v); return string.Empty; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(TaskVoidBoolRemote v) { this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(TaskVoidStringRemote v) { this.ReadReceived.Add(v); return null; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TaskVoidTaskBoolRemote v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TaskVoidTaskStringRemote v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(TaskVoidBoolRemote v) { this.WriteReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(TaskVoidStringRemote v) { this.WriteReceived.Add(v); return null; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(TaskVoidTaskBoolRemote v) { await Task.Yield(); this.WriteReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(TaskVoidTaskStringRemote v) { await Task.Yield(); this.WriteReceived.Add(v); return string.Empty; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(TaskTrueBoolBoolRemote v) { this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(TaskTrueBoolStringRemote v) { this.ReadReceived.Add(v); return null; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TaskTrueBoolTaskBoolRemote v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TaskTrueBoolTaskStringRemote v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(TaskTrueBoolBoolRemote v) { this.WriteReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(TaskTrueBoolStringRemote v) { this.WriteReceived.Add(v); return null; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(TaskTrueBoolTaskBoolRemote v) { await Task.Yield(); this.WriteReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(TaskTrueBoolTaskStringRemote v) { await Task.Yield(); this.WriteReceived.Add(v); return string.Empty; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(TaskFalseBoolBoolRemote v) { this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(TaskFalseBoolStringRemote v) { this.ReadReceived.Add(v); return null; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TaskFalseBoolTaskBoolRemote v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TaskFalseBoolTaskStringRemote v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(TaskFalseBoolBoolRemote v) { this.WriteReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(TaskFalseBoolStringRemote v) { this.WriteReceived.Add(v); return null; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(TaskFalseBoolTaskBoolRemote v) { await Task.Yield(); this.WriteReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(TaskFalseBoolTaskStringRemote v) { await Task.Yield(); this.WriteReceived.Add(v); return string.Empty; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(RemoteVoidBool v) { this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(RemoteVoidString v) { this.ReadReceived.Add(v); return null; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteVoidTaskBool v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteVoidTaskString v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(RemoteVoidBool v) { this.WriteReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(RemoteVoidString v) { this.WriteReceived.Add(v); return null; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(RemoteVoidTaskBool v) { await Task.Yield(); this.WriteReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(RemoteVoidTaskString v) { await Task.Yield(); this.WriteReceived.Add(v); return string.Empty; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(RemoteTrueBoolBool v) { this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(RemoteTrueBoolString v) { this.ReadReceived.Add(v); return null; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTrueBoolTaskBool v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTrueBoolTaskString v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(RemoteTrueBoolBool v) { this.WriteReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(RemoteTrueBoolString v) { this.WriteReceived.Add(v); return null; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(RemoteTrueBoolTaskBool v) { await Task.Yield(); this.WriteReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(RemoteTrueBoolTaskString v) { await Task.Yield(); this.WriteReceived.Add(v); return string.Empty; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(RemoteFalseBoolBool v) { this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(RemoteFalseBoolString v) { this.ReadReceived.Add(v); return null; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteFalseBoolTaskBool v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteFalseBoolTaskString v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(RemoteFalseBoolBool v) { this.WriteReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(RemoteFalseBoolString v) { this.WriteReceived.Add(v); return null; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(RemoteFalseBoolTaskBool v) { await Task.Yield(); this.WriteReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(RemoteFalseBoolTaskString v) { await Task.Yield(); this.WriteReceived.Add(v); return string.Empty; }


//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(RemoteTaskVoidBool v) { this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(RemoteTaskVoidString v) { this.ReadReceived.Add(v); return null; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTaskVoidTaskBool v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTaskVoidTaskString v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(RemoteTaskVoidBool v) { this.WriteReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(RemoteTaskVoidString v) { this.WriteReceived.Add(v); return null; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(RemoteTaskVoidTaskBool v) { await Task.Yield(); this.WriteReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(RemoteTaskVoidTaskString v) { await Task.Yield(); this.WriteReceived.Add(v); return string.Empty; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(RemoteTaskTrueBoolBool v) { this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(RemoteTaskTrueBoolString v) { this.ReadReceived.Add(v); return null; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTaskTrueBoolTaskBool v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTaskTrueBoolTaskString v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(RemoteTaskTrueBoolBool v) { this.WriteReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(RemoteTaskTrueBoolString v) { this.WriteReceived.Add(v); return null; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(RemoteTaskTrueBoolTaskBool v) { await Task.Yield(); this.WriteReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(RemoteTaskTrueBoolTaskString v) { await Task.Yield(); this.WriteReceived.Add(v); return string.Empty; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(RemoteTaskFalseBoolBool v) { this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(RemoteTaskFalseBoolString v) { this.ReadReceived.Add(v); return null; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTaskFalseBoolTaskBool v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTaskFalseBoolTaskString v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(RemoteTaskFalseBoolBool v) { this.WriteReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(RemoteTaskFalseBoolString v) { this.WriteReceived.Add(v); return null; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(RemoteTaskFalseBoolTaskBool v) { await Task.Yield(); this.WriteReceived.Add(v); return true; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(RemoteTaskFalseBoolTaskString v) { await Task.Yield(); this.WriteReceived.Add(v); return string.Empty; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(RemoteVoidBoolRemote v) { this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(RemoteVoidStringRemote v) { this.ReadReceived.Add(v); return null; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteVoidTaskBoolRemote v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteVoidTaskStringRemote v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(RemoteVoidBoolRemote v) { this.WriteReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(RemoteVoidStringRemote v) { this.WriteReceived.Add(v); return null; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(RemoteVoidTaskBoolRemote v) { await Task.Yield(); this.WriteReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(RemoteVoidTaskStringRemote v) { await Task.Yield(); this.WriteReceived.Add(v); return string.Empty; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(RemoteTrueBoolBoolRemote v) { this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(RemoteTrueBoolStringRemote v) { this.ReadReceived.Add(v); return null; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTrueBoolTaskBoolRemote v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTrueBoolTaskStringRemote v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(RemoteTrueBoolBoolRemote v) { this.WriteReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(RemoteTrueBoolStringRemote v) { this.WriteReceived.Add(v); return null; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(RemoteTrueBoolTaskBoolRemote v) { await Task.Yield(); this.WriteReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(RemoteTrueBoolTaskStringRemote v) { await Task.Yield(); this.WriteReceived.Add(v); return string.Empty; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(RemoteFalseBoolBoolRemote v) { this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(RemoteFalseBoolStringRemote v) { this.ReadReceived.Add(v); return null; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteFalseBoolTaskBoolRemote v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteFalseBoolTaskStringRemote v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(RemoteFalseBoolBoolRemote v) { this.WriteReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(RemoteFalseBoolStringRemote v) { this.WriteReceived.Add(v); return null; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(RemoteFalseBoolTaskBoolRemote v) { await Task.Yield(); this.WriteReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(RemoteFalseBoolTaskStringRemote v) { await Task.Yield(); this.WriteReceived.Add(v); return string.Empty; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(RemoteTaskVoidBoolRemote v) { this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(RemoteTaskVoidStringRemote v) { this.ReadReceived.Add(v); return null; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTaskVoidTaskBoolRemote v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTaskVoidTaskStringRemote v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(RemoteTaskVoidBoolRemote v) { this.WriteReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(RemoteTaskVoidStringRemote v) { this.WriteReceived.Add(v); return null; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(RemoteTaskVoidTaskBoolRemote v) { await Task.Yield(); this.WriteReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(RemoteTaskVoidTaskStringRemote v) { await Task.Yield(); this.WriteReceived.Add(v); return string.Empty; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(RemoteTaskTrueBoolBoolRemote v) { this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(RemoteTaskTrueBoolStringRemote v) { this.ReadReceived.Add(v); return null; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTaskTrueBoolTaskBoolRemote v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTaskTrueBoolTaskStringRemote v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(RemoteTaskTrueBoolBoolRemote v) { this.WriteReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(RemoteTaskTrueBoolStringRemote v) { this.WriteReceived.Add(v); return null; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(RemoteTaskTrueBoolTaskBoolRemote v) { await Task.Yield(); this.WriteReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(RemoteTaskTrueBoolTaskStringRemote v) { await Task.Yield(); this.WriteReceived.Add(v); return string.Empty; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(RemoteTaskFalseBoolBoolRemote v) { this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(RemoteTaskFalseBoolStringRemote v) { this.ReadReceived.Add(v); return null; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTaskFalseBoolTaskBoolRemote v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTaskFalseBoolTaskStringRemote v) { await Task.Yield(); this.ReadReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(RemoteTaskFalseBoolBoolRemote v) { this.WriteReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(RemoteTaskFalseBoolStringRemote v) { this.WriteReceived.Add(v); return null; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(RemoteTaskFalseBoolTaskBoolRemote v) { await Task.Yield(); this.WriteReceived.Add(v); return true; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(RemoteTaskFalseBoolTaskStringRemote v) { await Task.Yield(); this.WriteReceived.Add(v); return string.Empty; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(VoidBoolDeny v) { this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(VoidStringDeny v) { this.ReadReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(VoidTaskBoolDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(VoidTaskStringDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(VoidBoolDeny v) { this.WriteReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(VoidStringDeny v) { this.WriteReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(VoidTaskBoolDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(VoidTaskStringDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(TrueBoolBoolDeny v) { this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(TrueBoolStringDeny v) { this.ReadReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TrueBoolTaskBoolDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TrueBoolTaskStringDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(TrueBoolBoolDeny v) { this.WriteReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(TrueBoolStringDeny v) { this.WriteReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(TrueBoolTaskBoolDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(TrueBoolTaskStringDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(FalseBoolBoolDeny v) { this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(FalseBoolStringDeny v) { this.ReadReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(FalseBoolTaskBoolDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(FalseBoolTaskStringDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(FalseBoolBoolDeny v) { this.WriteReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(FalseBoolStringDeny v) { this.WriteReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(FalseBoolTaskBoolDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(FalseBoolTaskStringDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(TaskVoidBoolDeny v) { this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(TaskVoidStringDeny v) { this.ReadReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TaskVoidTaskBoolDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TaskVoidTaskStringDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(TaskVoidBoolDeny v) { this.WriteReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(TaskVoidStringDeny v) { this.WriteReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(TaskVoidTaskBoolDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(TaskVoidTaskStringDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(TaskTrueBoolBoolDeny v) { this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(TaskTrueBoolStringDeny v) { this.ReadReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TaskTrueBoolTaskBoolDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TaskTrueBoolTaskStringDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(TaskTrueBoolBoolDeny v) { this.WriteReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(TaskTrueBoolStringDeny v) { this.WriteReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(TaskTrueBoolTaskBoolDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(TaskTrueBoolTaskStringDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(TaskFalseBoolBoolDeny v) { this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(TaskFalseBoolStringDeny v) { this.ReadReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TaskFalseBoolTaskBoolDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TaskFalseBoolTaskStringDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(TaskFalseBoolBoolDeny v) { this.WriteReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(TaskFalseBoolStringDeny v) { this.WriteReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(TaskFalseBoolTaskBoolDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(TaskFalseBoolTaskStringDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(VoidBoolRemoteDeny v) { this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(VoidStringRemoteDeny v) { this.ReadReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(VoidTaskBoolRemoteDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(VoidTaskStringRemoteDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(VoidBoolRemoteDeny v) { this.WriteReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(VoidStringRemoteDeny v) { this.WriteReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(VoidTaskBoolRemoteDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(VoidTaskStringRemoteDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(TrueBoolBoolRemoteDeny v) { this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(TrueBoolStringRemoteDeny v) { this.ReadReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TrueBoolTaskBoolRemoteDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TrueBoolTaskStringRemoteDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(TrueBoolBoolRemoteDeny v) { this.WriteReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(TrueBoolStringRemoteDeny v) { this.WriteReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(TrueBoolTaskBoolRemoteDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(TrueBoolTaskStringRemoteDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(FalseBoolBoolRemoteDeny v) { this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(FalseBoolStringRemoteDeny v) { this.ReadReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(FalseBoolTaskBoolRemoteDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(FalseBoolTaskStringRemoteDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(FalseBoolBoolRemoteDeny v) { this.WriteReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(FalseBoolStringRemoteDeny v) { this.WriteReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(FalseBoolTaskBoolRemoteDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(FalseBoolTaskStringRemoteDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(TaskVoidBoolRemoteDeny v) { this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(TaskVoidStringRemoteDeny v) { this.ReadReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TaskVoidTaskBoolRemoteDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TaskVoidTaskStringRemoteDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(TaskVoidBoolRemoteDeny v) { this.WriteReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(TaskVoidStringRemoteDeny v) { this.WriteReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(TaskVoidTaskBoolRemoteDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(TaskVoidTaskStringRemoteDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(TaskTrueBoolBoolRemoteDeny v) { this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(TaskTrueBoolStringRemoteDeny v) { this.ReadReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TaskTrueBoolTaskBoolRemoteDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TaskTrueBoolTaskStringRemoteDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(TaskTrueBoolBoolRemoteDeny v) { this.WriteReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(TaskTrueBoolStringRemoteDeny v) { this.WriteReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(TaskTrueBoolTaskBoolRemoteDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(TaskTrueBoolTaskStringRemoteDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(TaskFalseBoolBoolRemoteDeny v) { this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(TaskFalseBoolStringRemoteDeny v) { this.ReadReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TaskFalseBoolTaskBoolRemoteDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(TaskFalseBoolTaskStringRemoteDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(TaskFalseBoolBoolRemoteDeny v) { this.WriteReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(TaskFalseBoolStringRemoteDeny v) { this.WriteReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(TaskFalseBoolTaskBoolRemoteDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(TaskFalseBoolTaskStringRemoteDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(RemoteVoidBoolDeny v) { this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(RemoteVoidStringDeny v) { this.ReadReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteVoidTaskBoolDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteVoidTaskStringDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(RemoteVoidBoolDeny v) { this.WriteReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(RemoteVoidStringDeny v) { this.WriteReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(RemoteVoidTaskBoolDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(RemoteVoidTaskStringDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(RemoteTrueBoolBoolDeny v) { this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(RemoteTrueBoolStringDeny v) { this.ReadReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTrueBoolTaskBoolDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTrueBoolTaskStringDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(RemoteTrueBoolBoolDeny v) { this.WriteReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(RemoteTrueBoolStringDeny v) { this.WriteReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(RemoteTrueBoolTaskBoolDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(RemoteTrueBoolTaskStringDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(RemoteFalseBoolBoolDeny v) { this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(RemoteFalseBoolStringDeny v) { this.ReadReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteFalseBoolTaskBoolDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteFalseBoolTaskStringDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(RemoteFalseBoolBoolDeny v) { this.WriteReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(RemoteFalseBoolStringDeny v) { this.WriteReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(RemoteFalseBoolTaskBoolDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(RemoteFalseBoolTaskStringDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(RemoteTaskVoidBoolDeny v) { this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(RemoteTaskVoidStringDeny v) { this.ReadReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTaskVoidTaskBoolDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTaskVoidTaskStringDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(RemoteTaskVoidBoolDeny v) { this.WriteReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(RemoteTaskVoidStringDeny v) { this.WriteReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(RemoteTaskVoidTaskBoolDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(RemoteTaskVoidTaskStringDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(RemoteTaskTrueBoolBoolDeny v) { this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(RemoteTaskTrueBoolStringDeny v) { this.ReadReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTaskTrueBoolTaskBoolDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTaskTrueBoolTaskStringDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(RemoteTaskTrueBoolBoolDeny v) { this.WriteReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(RemoteTaskTrueBoolStringDeny v) { this.WriteReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(RemoteTaskTrueBoolTaskBoolDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(RemoteTaskTrueBoolTaskStringDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(RemoteTaskFalseBoolBoolDeny v) { this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(RemoteTaskFalseBoolStringDeny v) { this.ReadReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTaskFalseBoolTaskBoolDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTaskFalseBoolTaskStringDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(RemoteTaskFalseBoolBoolDeny v) { this.WriteReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(RemoteTaskFalseBoolStringDeny v) { this.WriteReceived.Add(v); return "deny"; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(RemoteTaskFalseBoolTaskBoolDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return false; }

//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(RemoteTaskFalseBoolTaskStringDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(RemoteVoidBoolRemoteDeny v) { this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(RemoteVoidStringRemoteDeny v) { this.ReadReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteVoidTaskBoolRemoteDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteVoidTaskStringRemoteDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(RemoteVoidBoolRemoteDeny v) { this.WriteReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(RemoteVoidStringRemoteDeny v) { this.WriteReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(RemoteVoidTaskBoolRemoteDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(RemoteVoidTaskStringRemoteDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(RemoteTrueBoolBoolRemoteDeny v) { this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(RemoteTrueBoolStringRemoteDeny v) { this.ReadReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTrueBoolTaskBoolRemoteDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTrueBoolTaskStringRemoteDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(RemoteTrueBoolBoolRemoteDeny v) { this.WriteReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(RemoteTrueBoolStringRemoteDeny v) { this.WriteReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(RemoteTrueBoolTaskBoolRemoteDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(RemoteTrueBoolTaskStringRemoteDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(RemoteFalseBoolBoolRemoteDeny v) { this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(RemoteFalseBoolStringRemoteDeny v) { this.ReadReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteFalseBoolTaskBoolRemoteDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteFalseBoolTaskStringRemoteDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(RemoteFalseBoolBoolRemoteDeny v) { this.WriteReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(RemoteFalseBoolStringRemoteDeny v) { this.WriteReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(RemoteFalseBoolTaskBoolRemoteDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(RemoteFalseBoolTaskStringRemoteDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(RemoteTaskVoidBoolRemoteDeny v) { this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(RemoteTaskVoidStringRemoteDeny v) { this.ReadReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTaskVoidTaskBoolRemoteDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTaskVoidTaskStringRemoteDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(RemoteTaskVoidBoolRemoteDeny v) { this.WriteReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(RemoteTaskVoidStringRemoteDeny v) { this.WriteReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(RemoteTaskVoidTaskBoolRemoteDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(RemoteTaskVoidTaskStringRemoteDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(RemoteTaskTrueBoolBoolRemoteDeny v) { this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(RemoteTaskTrueBoolStringRemoteDeny v) { this.ReadReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTaskTrueBoolTaskBoolRemoteDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTaskTrueBoolTaskStringRemoteDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(RemoteTaskTrueBoolBoolRemoteDeny v) { this.WriteReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(RemoteTaskTrueBoolStringRemoteDeny v) { this.WriteReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(RemoteTaskTrueBoolTaskBoolRemoteDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(RemoteTaskTrueBoolTaskStringRemoteDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public bool Read(RemoteTaskFalseBoolBoolRemoteDeny v) { this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public string? Read(RemoteTaskFalseBoolStringRemoteDeny v) { this.ReadReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTaskFalseBoolTaskBoolRemoteDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
//		public async Task<bool> Read(RemoteTaskFalseBoolTaskStringRemoteDeny v) { await Task.Yield(); this.ReadReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public bool Write(RemoteTaskFalseBoolBoolRemoteDeny v) { this.WriteReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public string? Write(RemoteTaskFalseBoolStringRemoteDeny v) { this.WriteReceived.Add(v); return "deny"; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<bool> Write(RemoteTaskFalseBoolTaskBoolRemoteDeny v) { await Task.Yield(); this.WriteReceived.Add(v); return false; }

//		[Remote]
//		[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
//		public async Task<string> Write(RemoteTaskFalseBoolTaskStringRemoteDeny v)
//		{
//			await Task.Yield(); this.WriteReceived.Add(v); return "deny";
//		}
//	}

//	public interface IAuthorizedAllCombinations : IFactorySaveMeta
//	{
//		List<IDd> Received { get; set; }
//	}

//	[Factory]
//	[AuthorizeFactory<AuthorizationAllCombinations>]
//	public class AuthorizedAllCombinations : IAuthorizedAllCombinations
//	{
//		public AuthorizedAllCombinations() : base()
//		{
//			this.Received = new List<IDd>();
//		}

//		public List<IDd> Received { get; set; } = new List<IDd>();

//		public bool IsDeleted => false;

//		public bool IsNew => true;

//		[Create]
//		public void Create(VoidBool v) { this.Received.Add(v); }

//		[Create]
//		public void Create(VoidString v) { this.Received.Add(v); }

//		[Create]
//		public void Create(VoidTaskBool v) { this.Received.Add(v); }

//		[Create]
//		public void Create(VoidTaskString v) { this.Received.Add(v); }

//		[Insert]
//		public void Insert(VoidBool v) { this.Received.Add(v); }

//		[Insert]
//		public void Insert(VoidString v) { this.Received.Add(v); }

//		[Insert]
//		public void Insert(VoidTaskBool v) { this.Received.Add(v); }

//		[Insert]
//		public void Insert(VoidTaskString v) { this.Received.Add(v); }

//		[Create]
//		public bool Create(TrueBoolBool v) { this.Received.Add(v); return true; }

//		[Create]
//		public bool Create(TrueBoolString v) { this.Received.Add(v); return true; }

//		[Create]
//		public bool Create(TrueBoolTaskBool v) { this.Received.Add(v); return true; }

//		[Create]
//		public bool Create(TrueBoolTaskString v) { this.Received.Add(v); return true; }

//		[Insert]
//		public bool Insert(TrueBoolBool v) { this.Received.Add(v); return true; }

//		[Insert]
//		public bool Insert(TrueBoolString v) { this.Received.Add(v); return true; }

//		[Insert]
//		public bool Insert(TrueBoolTaskBool v) { this.Received.Add(v); return true; }

//		[Insert]
//		public bool Insert(TrueBoolTaskString v) { this.Received.Add(v); return true; }

//		[Create]
//		public bool Create(FalseBoolBool v) { this.Received.Add(v); return false; }

//		[Create]
//		public bool Create(FalseBoolString v) { this.Received.Add(v); return false; }

//		[Create]
//		public bool Create(FalseBoolTaskBool v) { this.Received.Add(v); return false; }

//		[Create]
//		public bool Create(FalseBoolTaskString v) { this.Received.Add(v); return false; }

//		[Insert]
//		public bool Insert(FalseBoolBool v) { this.Received.Add(v); return false; }

//		[Insert]
//		public bool Insert(FalseBoolString v) { this.Received.Add(v); return false; }

//		[Insert]
//		public bool Insert(FalseBoolTaskBool v) { this.Received.Add(v); return false; }

//		[Insert]
//		public bool Insert(FalseBoolTaskString v) { this.Received.Add(v); return false; }

//		[Create]
//		public async Task Create(TaskVoidBool v) { await Task.Yield(); this.Received.Add(v); }

//		[Create]
//		public async Task Create(TaskVoidString v) { await Task.Yield(); this.Received.Add(v); }

//		[Create]
//		public async Task Create(TaskVoidTaskBool v) { await Task.Yield(); this.Received.Add(v); }

//		[Create]
//		public async Task Create(TaskVoidTaskString v) { await Task.Yield(); this.Received.Add(v); }

//		[Insert]
//		public async Task Insert(TaskVoidBool v) { await Task.Yield(); this.Received.Add(v); }

//		[Insert]
//		public async Task Insert(TaskVoidString v) { await Task.Yield(); this.Received.Add(v); }

//		[Insert]
//		public async Task Insert(TaskVoidTaskBool v) { await Task.Yield(); this.Received.Add(v); }

//		[Insert]
//		public async Task Insert(TaskVoidTaskString v) { await Task.Yield(); this.Received.Add(v); }

//		[Create]
//		public async Task<bool> Create(TaskTrueBoolBool v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Create]
//		public async Task<bool> Create(TaskTrueBoolString v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Create]
//		public async Task<bool> Create(TaskTrueBoolTaskBool v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Create]
//		public async Task<bool> Create(TaskTrueBoolTaskString v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Insert]
//		public async Task<bool> Insert(TaskTrueBoolBool v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Insert]
//		public async Task<bool> Insert(TaskTrueBoolString v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Insert]
//		public async Task<bool> Insert(TaskTrueBoolTaskBool v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Insert]
//		public async Task<bool> Insert(TaskTrueBoolTaskString v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Create]
//		public async Task<bool> Create(TaskFalseBoolBool v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Create]
//		public async Task<bool> Create(TaskFalseBoolString v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Create]
//		public async Task<bool> Create(TaskFalseBoolTaskBool v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Create]
//		public async Task<bool> Create(TaskFalseBoolTaskString v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Insert]
//		public async Task<bool> Insert(TaskFalseBoolBool v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Insert]
//		public async Task<bool> Insert(TaskFalseBoolString v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Insert]
//		public async Task<bool> Insert(TaskFalseBoolTaskBool v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Insert]
//		public async Task<bool> Insert(TaskFalseBoolTaskString v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Create]
//		public void Create(VoidBoolRemote v) { this.Received.Add(v); }

//		[Create]
//		public void Create(VoidStringRemote v) { this.Received.Add(v); }

//		[Create]
//		public void Create(VoidTaskBoolRemote v) { this.Received.Add(v); }

//		[Create]
//		public void Create(VoidTaskStringRemote v) { this.Received.Add(v); }

//		[Insert]
//		public void Insert(VoidBoolRemote v) { this.Received.Add(v); }

//		[Insert]
//		public void Insert(VoidStringRemote v) { this.Received.Add(v); }

//		[Insert]
//		public void Insert(VoidTaskBoolRemote v) { this.Received.Add(v); }

//		[Insert]
//		public void Insert(VoidTaskStringRemote v) { this.Received.Add(v); }

//		[Create]
//		public bool Create(TrueBoolBoolRemote v) { this.Received.Add(v); return true; }

//		[Create]
//		public bool Create(TrueBoolStringRemote v) { this.Received.Add(v); return true; }

//		[Create]
//		public bool Create(TrueBoolTaskBoolRemote v) { this.Received.Add(v); return true; }

//		[Create]
//		public bool Create(TrueBoolTaskStringRemote v) { this.Received.Add(v); return true; }

//		[Insert]
//		public bool Insert(TrueBoolBoolRemote v) { this.Received.Add(v); return true; }

//		[Insert]
//		public bool Insert(TrueBoolStringRemote v) { this.Received.Add(v); return true; }

//		[Insert]
//		public bool Insert(TrueBoolTaskBoolRemote v) { this.Received.Add(v); return true; }

//		[Insert]
//		public bool Insert(TrueBoolTaskStringRemote v) { this.Received.Add(v); return true; }

//		[Create]
//		public bool Create(FalseBoolBoolRemote v) { this.Received.Add(v); return false; }

//		[Create]
//		public bool Create(FalseBoolStringRemote v) { this.Received.Add(v); return false; }

//		[Create]
//		public bool Create(FalseBoolTaskBoolRemote v) { this.Received.Add(v); return false; }

//		[Create]
//		public bool Create(FalseBoolTaskStringRemote v) { this.Received.Add(v); return false; }

//		[Insert]
//		public bool Insert(FalseBoolBoolRemote v) { this.Received.Add(v); return false; }

//		[Insert]
//		public bool Insert(FalseBoolStringRemote v) { this.Received.Add(v); return false; }

//		[Insert]
//		public bool Insert(FalseBoolTaskBoolRemote v) { this.Received.Add(v); return false; }

//		[Insert]
//		public bool Insert(FalseBoolTaskStringRemote v) { this.Received.Add(v); return false; }

//		[Create]
//		public async Task Create(TaskVoidBoolRemote v) { await Task.Yield(); this.Received.Add(v); }

//		[Create]
//		public async Task Create(TaskVoidStringRemote v) { await Task.Yield(); this.Received.Add(v); }

//		[Create]
//		public async Task Create(TaskVoidTaskBoolRemote v) { await Task.Yield(); this.Received.Add(v); }

//		[Create]
//		public async Task Create(TaskVoidTaskStringRemote v) { await Task.Yield(); this.Received.Add(v); }

//		[Insert]
//		public async Task Insert(TaskVoidBoolRemote v) { await Task.Yield(); this.Received.Add(v); }

//		[Insert]
//		public async Task Insert(TaskVoidStringRemote v) { await Task.Yield(); this.Received.Add(v); }

//		[Insert]
//		public async Task Insert(TaskVoidTaskBoolRemote v) { await Task.Yield(); this.Received.Add(v); }

//		[Insert]
//		public async Task Insert(TaskVoidTaskStringRemote v) { await Task.Yield(); this.Received.Add(v); }

//		[Create]
//		public async Task<bool> Create(TaskTrueBoolBoolRemote v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Create]
//		public async Task<bool> Create(TaskTrueBoolStringRemote v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Create]
//		public async Task<bool> Create(TaskTrueBoolTaskBoolRemote v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Create]
//		public async Task<bool> Create(TaskTrueBoolTaskStringRemote v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Insert]
//		public async Task<bool> Insert(TaskTrueBoolBoolRemote v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Insert]
//		public async Task<bool> Insert(TaskTrueBoolStringRemote v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Insert]
//		public async Task<bool> Insert(TaskTrueBoolTaskBoolRemote v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Insert]
//		public async Task<bool> Insert(TaskTrueBoolTaskStringRemote v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Create]
//		public async Task<bool> Create(TaskFalseBoolBoolRemote v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Create]
//		public async Task<bool> Create(TaskFalseBoolStringRemote v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Create]
//		public async Task<bool> Create(TaskFalseBoolTaskBoolRemote v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Create]
//		public async Task<bool> Create(TaskFalseBoolTaskStringRemote v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Insert]
//		public async Task<bool> Insert(TaskFalseBoolBoolRemote v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Insert]
//		public async Task<bool> Insert(TaskFalseBoolStringRemote v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Insert]
//		public async Task<bool> Insert(TaskFalseBoolTaskBoolRemote v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Insert]
//		public async Task<bool> Insert(TaskFalseBoolTaskStringRemote v) { await Task.Yield(); this.Received.Add(v); return false; }


//		[Remote]
//		[Create]
//		public void Create(RemoteVoidBool v) { this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public void Create(RemoteVoidString v) { this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public void Create(RemoteVoidTaskBool v) { this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public void Create(RemoteVoidTaskString v) { this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public void Insert(RemoteVoidBool v) { this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public void Insert(RemoteVoidString v) { this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public void Insert(RemoteVoidTaskBool v) { this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public void Insert(RemoteVoidTaskString v) { this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public bool Create(RemoteTrueBoolBool v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteTrueBoolString v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteTrueBoolTaskBool v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteTrueBoolTaskString v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteTrueBoolBool v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteTrueBoolString v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteTrueBoolTaskBool v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteTrueBoolTaskString v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteFalseBoolBool v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteFalseBoolString v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteFalseBoolTaskBool v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteFalseBoolTaskString v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteFalseBoolBool v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteFalseBoolString v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteFalseBoolTaskBool v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteFalseBoolTaskString v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public async Task Create(RemoteTaskVoidBool v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public async Task Create(RemoteTaskVoidString v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public async Task Create(RemoteTaskVoidTaskBool v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public async Task Create(RemoteTaskVoidTaskString v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public async Task Insert(RemoteTaskVoidBool v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public async Task Insert(RemoteTaskVoidString v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public async Task Insert(RemoteTaskVoidTaskBool v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public async Task Insert(RemoteTaskVoidTaskString v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskTrueBoolBool v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskTrueBoolString v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskTrueBoolTaskBool v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskTrueBoolTaskString v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskTrueBoolBool v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskTrueBoolString v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskTrueBoolTaskBool v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskTrueBoolTaskString v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskFalseBoolBool v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskFalseBoolString v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskFalseBoolTaskBool v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskFalseBoolTaskString v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskFalseBoolBool v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskFalseBoolString v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskFalseBoolTaskBool v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskFalseBoolTaskString v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public void Create(RemoteVoidBoolRemote v) { this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public void Create(RemoteVoidStringRemote v) { this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public void Create(RemoteVoidTaskBoolRemote v) { this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public void Create(RemoteVoidTaskStringRemote v) { this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public void Insert(RemoteVoidBoolRemote v) { this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public void Insert(RemoteVoidStringRemote v) { this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public void Insert(RemoteVoidTaskBoolRemote v) { this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public void Insert(RemoteVoidTaskStringRemote v) { this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public bool Create(RemoteTrueBoolBoolRemote v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteTrueBoolStringRemote v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteTrueBoolTaskBoolRemote v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteTrueBoolTaskStringRemote v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteTrueBoolBoolRemote v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteTrueBoolStringRemote v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteTrueBoolTaskBoolRemote v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteTrueBoolTaskStringRemote v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteFalseBoolBoolRemote v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteFalseBoolStringRemote v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteFalseBoolTaskBoolRemote v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteFalseBoolTaskStringRemote v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteFalseBoolBoolRemote v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteFalseBoolStringRemote v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteFalseBoolTaskBoolRemote v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteFalseBoolTaskStringRemote v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public async Task Create(RemoteTaskVoidBoolRemote v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public async Task Create(RemoteTaskVoidStringRemote v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public async Task Create(RemoteTaskVoidTaskBoolRemote v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public async Task Create(RemoteTaskVoidTaskStringRemote v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public async Task Insert(RemoteTaskVoidBoolRemote v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public async Task Insert(RemoteTaskVoidStringRemote v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public async Task Insert(RemoteTaskVoidTaskBoolRemote v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public async Task Insert(RemoteTaskVoidTaskStringRemote v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskTrueBoolBoolRemote v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskTrueBoolStringRemote v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskTrueBoolTaskBoolRemote v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskTrueBoolTaskStringRemote v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskTrueBoolBoolRemote v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskTrueBoolStringRemote v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskTrueBoolTaskBoolRemote v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskTrueBoolTaskStringRemote v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskFalseBoolBoolRemote v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskFalseBoolStringRemote v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskFalseBoolTaskBoolRemote v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskFalseBoolTaskStringRemote v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskFalseBoolBoolRemote v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskFalseBoolStringRemote v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskFalseBoolTaskBoolRemote v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskFalseBoolTaskStringRemote v) { await Task.Yield(); this.Received.Add(v); return false; }



//		[Create]
//		public void Create(VoidBoolDeny v) { this.Received.Add(v); }

//		[Create]
//		public void Create(VoidStringDeny v) { this.Received.Add(v); }

//		[Create]
//		public void Create(VoidTaskBoolDeny v) { this.Received.Add(v); }

//		[Create]
//		public void Create(VoidTaskStringDeny v) { this.Received.Add(v); }

//		[Insert]
//		public void Insert(VoidBoolDeny v) { this.Received.Add(v); }

//		[Insert]
//		public void Insert(VoidStringDeny v) { this.Received.Add(v); }

//		[Insert]
//		public void Insert(VoidTaskBoolDeny v) { this.Received.Add(v); }

//		[Insert]
//		public void Insert(VoidTaskStringDeny v) { this.Received.Add(v); }

//		[Create]
//		public bool Create(TrueBoolBoolDeny v) { this.Received.Add(v); return true; }

//		[Create]
//		public bool Create(TrueBoolStringDeny v) { this.Received.Add(v); return true; }

//		[Create]
//		public bool Create(TrueBoolTaskBoolDeny v) { this.Received.Add(v); return true; }

//		[Create]
//		public bool Create(TrueBoolTaskStringDeny v) { this.Received.Add(v); return true; }

//		[Insert]
//		public bool Insert(TrueBoolBoolDeny v) { this.Received.Add(v); return true; }

//		[Insert]
//		public bool Insert(TrueBoolStringDeny v) { this.Received.Add(v); return true; }

//		[Insert]
//		public bool Insert(TrueBoolTaskBoolDeny v) { this.Received.Add(v); return true; }

//		[Insert]
//		public bool Insert(TrueBoolTaskStringDeny v) { this.Received.Add(v); return true; }

//		[Create]
//		public bool Create(FalseBoolBoolDeny v) { this.Received.Add(v); return false; }

//		[Create]
//		public bool Create(FalseBoolStringDeny v) { this.Received.Add(v); return false; }

//		[Create]
//		public bool Create(FalseBoolTaskBoolDeny v) { this.Received.Add(v); return false; }

//		[Create]
//		public bool Create(FalseBoolTaskStringDeny v) { this.Received.Add(v); return false; }

//		[Insert]
//		public bool Insert(FalseBoolBoolDeny v) { this.Received.Add(v); return false; }

//		[Insert]
//		public bool Insert(FalseBoolStringDeny v) { this.Received.Add(v); return false; }

//		[Insert]
//		public bool Insert(FalseBoolTaskBoolDeny v) { this.Received.Add(v); return false; }

//		[Insert]
//		public bool Insert(FalseBoolTaskStringDeny v) { this.Received.Add(v); return false; }

//		[Create]
//		public async Task Create(TaskVoidBoolDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Create]
//		public async Task Create(TaskVoidStringDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Create]
//		public async Task Create(TaskVoidTaskBoolDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Create]
//		public async Task Create(TaskVoidTaskStringDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Insert]
//		public async Task Insert(TaskVoidBoolDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Insert]
//		public async Task Insert(TaskVoidStringDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Insert]
//		public async Task Insert(TaskVoidTaskBoolDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Insert]
//		public async Task Insert(TaskVoidTaskStringDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Create]
//		public async Task<bool> Create(TaskTrueBoolBoolDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Create]
//		public async Task<bool> Create(TaskTrueBoolStringDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Create]
//		public async Task<bool> Create(TaskTrueBoolTaskBoolDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Create]
//		public async Task<bool> Create(TaskTrueBoolTaskStringDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Insert]
//		public async Task<bool> Insert(TaskTrueBoolBoolDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Insert]
//		public async Task<bool> Insert(TaskTrueBoolStringDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Insert]
//		public async Task<bool> Insert(TaskTrueBoolTaskBoolDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Insert]
//		public async Task<bool> Insert(TaskTrueBoolTaskStringDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Create]
//		public async Task<bool> Create(TaskFalseBoolBoolDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Create]
//		public async Task<bool> Create(TaskFalseBoolStringDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Create]
//		public async Task<bool> Create(TaskFalseBoolTaskBoolDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Create]
//		public async Task<bool> Create(TaskFalseBoolTaskStringDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Insert]
//		public async Task<bool> Insert(TaskFalseBoolBoolDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Insert]
//		public async Task<bool> Insert(TaskFalseBoolStringDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Insert]
//		public async Task<bool> Insert(TaskFalseBoolTaskBoolDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Insert]
//		public async Task<bool> Insert(TaskFalseBoolTaskStringDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Create]
//		public void Create(VoidBoolRemoteDeny v) { this.Received.Add(v); }

//		[Create]
//		public void Create(VoidStringRemoteDeny v) { this.Received.Add(v); }

//		[Create]
//		public void Create(VoidTaskBoolRemoteDeny v) { this.Received.Add(v); }

//		[Create]
//		public void Create(VoidTaskStringRemoteDeny v) { this.Received.Add(v); }

//		[Insert]
//		public void Insert(VoidBoolRemoteDeny v) { this.Received.Add(v); }

//		[Insert]
//		public void Insert(VoidStringRemoteDeny v) { this.Received.Add(v); }

//		[Insert]
//		public void Insert(VoidTaskBoolRemoteDeny v) { this.Received.Add(v); }

//		[Insert]
//		public void Insert(VoidTaskStringRemoteDeny v) { this.Received.Add(v); }

//		[Create]
//		public bool Create(TrueBoolBoolRemoteDeny v) { this.Received.Add(v); return true; }

//		[Create]
//		public bool Create(TrueBoolStringRemoteDeny v) { this.Received.Add(v); return true; }

//		[Create]
//		public bool Create(TrueBoolTaskBoolRemoteDeny v) { this.Received.Add(v); return true; }

//		[Create]
//		public bool Create(TrueBoolTaskStringRemoteDeny v) { this.Received.Add(v); return true; }

//		[Insert]
//		public bool Insert(TrueBoolBoolRemoteDeny v) { this.Received.Add(v); return true; }

//		[Insert]
//		public bool Insert(TrueBoolStringRemoteDeny v) { this.Received.Add(v); return true; }

//		[Insert]
//		public bool Insert(TrueBoolTaskBoolRemoteDeny v) { this.Received.Add(v); return true; }

//		[Insert]
//		public bool Insert(TrueBoolTaskStringRemoteDeny v) { this.Received.Add(v); return true; }

//		[Create]
//		public bool Create(FalseBoolBoolRemoteDeny v) { this.Received.Add(v); return false; }

//		[Create]
//		public bool Create(FalseBoolStringRemoteDeny v) { this.Received.Add(v); return false; }

//		[Create]
//		public bool Create(FalseBoolTaskBoolRemoteDeny v) { this.Received.Add(v); return false; }

//		[Create]
//		public bool Create(FalseBoolTaskStringRemoteDeny v) { this.Received.Add(v); return false; }

//		[Insert]
//		public bool Insert(FalseBoolBoolRemoteDeny v) { this.Received.Add(v); return false; }

//		[Insert]
//		public bool Insert(FalseBoolStringRemoteDeny v) { this.Received.Add(v); return false; }

//		[Insert]
//		public bool Insert(FalseBoolTaskBoolRemoteDeny v) { this.Received.Add(v); return false; }

//		[Insert]
//		public bool Insert(FalseBoolTaskStringRemoteDeny v) { this.Received.Add(v); return false; }

//		[Create]
//		public async Task Create(TaskVoidBoolRemoteDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Create]
//		public async Task Create(TaskVoidStringRemoteDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Create]
//		public async Task Create(TaskVoidTaskBoolRemoteDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Create]
//		public async Task Create(TaskVoidTaskStringRemoteDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Insert]
//		public async Task Insert(TaskVoidBoolRemoteDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Insert]
//		public async Task Insert(TaskVoidStringRemoteDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Insert]
//		public async Task Insert(TaskVoidTaskBoolRemoteDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Insert]
//		public async Task Insert(TaskVoidTaskStringRemoteDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Create]
//		public async Task<bool> Create(TaskTrueBoolBoolRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Create]
//		public async Task<bool> Create(TaskTrueBoolStringRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Create]
//		public async Task<bool> Create(TaskTrueBoolTaskBoolRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Create]
//		public async Task<bool> Create(TaskTrueBoolTaskStringRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Insert]
//		public async Task<bool> Insert(TaskTrueBoolBoolRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Insert]
//		public async Task<bool> Insert(TaskTrueBoolStringRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Insert]
//		public async Task<bool> Insert(TaskTrueBoolTaskBoolRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Insert]
//		public async Task<bool> Insert(TaskTrueBoolTaskStringRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Create]
//		public async Task<bool> Create(TaskFalseBoolBoolRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Create]
//		public async Task<bool> Create(TaskFalseBoolStringRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Create]
//		public async Task<bool> Create(TaskFalseBoolTaskBoolRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Create]
//		public async Task<bool> Create(TaskFalseBoolTaskStringRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Insert]
//		public async Task<bool> Insert(TaskFalseBoolBoolRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Insert]
//		public async Task<bool> Insert(TaskFalseBoolStringRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Insert]
//		public async Task<bool> Insert(TaskFalseBoolTaskBoolRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Insert]
//		public async Task<bool> Insert(TaskFalseBoolTaskStringRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return false; }


//		[Remote]
//		[Create]
//		public void Create(RemoteVoidBoolDeny v) { this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public void Create(RemoteVoidStringDeny v) { this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public void Create(RemoteVoidTaskBoolDeny v) { this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public void Create(RemoteVoidTaskStringDeny v) { this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public void Insert(RemoteVoidBoolDeny v) { this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public void Insert(RemoteVoidStringDeny v) { this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public void Insert(RemoteVoidTaskBoolDeny v) { this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public void Insert(RemoteVoidTaskStringDeny v) { this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public bool Create(RemoteTrueBoolBoolDeny v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteTrueBoolStringDeny v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteTrueBoolTaskBoolDeny v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteTrueBoolTaskStringDeny v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteTrueBoolBoolDeny v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteTrueBoolStringDeny v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteTrueBoolTaskBoolDeny v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteTrueBoolTaskStringDeny v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteFalseBoolBoolDeny v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteFalseBoolStringDeny v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteFalseBoolTaskBoolDeny v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteFalseBoolTaskStringDeny v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteFalseBoolBoolDeny v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteFalseBoolStringDeny v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteFalseBoolTaskBoolDeny v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteFalseBoolTaskStringDeny v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public async Task Create(RemoteTaskVoidBoolDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public async Task Create(RemoteTaskVoidStringDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public async Task Create(RemoteTaskVoidTaskBoolDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public async Task Create(RemoteTaskVoidTaskStringDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public async Task Insert(RemoteTaskVoidBoolDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public async Task Insert(RemoteTaskVoidStringDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public async Task Insert(RemoteTaskVoidTaskBoolDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public async Task Insert(RemoteTaskVoidTaskStringDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskTrueBoolBoolDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskTrueBoolStringDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskTrueBoolTaskBoolDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskTrueBoolTaskStringDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskTrueBoolBoolDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskTrueBoolStringDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskTrueBoolTaskBoolDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskTrueBoolTaskStringDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskFalseBoolBoolDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskFalseBoolStringDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskFalseBoolTaskBoolDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskFalseBoolTaskStringDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskFalseBoolBoolDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskFalseBoolStringDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskFalseBoolTaskBoolDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskFalseBoolTaskStringDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public void Create(RemoteVoidBoolRemoteDeny v) { this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public void Create(RemoteVoidStringRemoteDeny v) { this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public void Create(RemoteVoidTaskBoolRemoteDeny v) { this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public void Create(RemoteVoidTaskStringRemoteDeny v) { this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public void Insert(RemoteVoidBoolRemoteDeny v) { this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public void Insert(RemoteVoidStringRemoteDeny v) { this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public void Insert(RemoteVoidTaskBoolRemoteDeny v) { this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public void Insert(RemoteVoidTaskStringRemoteDeny v) { this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public bool Create(RemoteTrueBoolBoolRemoteDeny v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteTrueBoolStringRemoteDeny v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteTrueBoolTaskBoolRemoteDeny v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteTrueBoolTaskStringRemoteDeny v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteTrueBoolBoolRemoteDeny v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteTrueBoolStringRemoteDeny v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteTrueBoolTaskBoolRemoteDeny v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteTrueBoolTaskStringRemoteDeny v) { this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteFalseBoolBoolRemoteDeny v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteFalseBoolStringRemoteDeny v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteFalseBoolTaskBoolRemoteDeny v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public bool Create(RemoteFalseBoolTaskStringRemoteDeny v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteFalseBoolBoolRemoteDeny v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteFalseBoolStringRemoteDeny v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteFalseBoolTaskBoolRemoteDeny v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public bool Insert(RemoteFalseBoolTaskStringRemoteDeny v) { this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public async Task Create(RemoteTaskVoidBoolRemoteDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public async Task Create(RemoteTaskVoidStringRemoteDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public async Task Create(RemoteTaskVoidTaskBoolRemoteDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public async Task Create(RemoteTaskVoidTaskStringRemoteDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public async Task Insert(RemoteTaskVoidBoolRemoteDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public async Task Insert(RemoteTaskVoidStringRemoteDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public async Task Insert(RemoteTaskVoidTaskBoolRemoteDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Insert]
//		public async Task Insert(RemoteTaskVoidTaskStringRemoteDeny v) { await Task.Yield(); this.Received.Add(v); }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskTrueBoolBoolRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskTrueBoolStringRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskTrueBoolTaskBoolRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskTrueBoolTaskStringRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskTrueBoolBoolRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskTrueBoolStringRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskTrueBoolTaskBoolRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskTrueBoolTaskStringRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return true; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskFalseBoolBoolRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskFalseBoolStringRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskFalseBoolTaskBoolRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Create]
//		public async Task<bool> Create(RemoteTaskFalseBoolTaskStringRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskFalseBoolBoolRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskFalseBoolStringRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskFalseBoolTaskBoolRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return false; }

//		[Remote]
//		[Insert]
//		public async Task<bool> Insert(RemoteTaskFalseBoolTaskStringRemoteDeny v) { await Task.Yield(); this.Received.Add(v); return false; }
//	}

//	private IServiceScope clientScope;
//	private IAuthorizedAllCombinationsFactory authorizedObjectFactory = null!;
//	private AuthorizationAllCombinations authorizationClient;
//	private AuthorizationAllCombinations authorizationServer;
//	private IAuthorizedAllCombinations? writeAuthorizedObject;

//	public AuthorizationAllCombinationTests()
//	{
//		var scopes = ClientServerContainers.Scopes();
//		this.clientScope = scopes.client;
//		this.authorizedObjectFactory = this.clientScope.ServiceProvider.GetRequiredService<IAuthorizedAllCombinationsFactory>();
//		this.authorizationClient = this.clientScope.ServiceProvider.GetRequiredService<AuthorizationAllCombinations>();
//		this.authorizationServer = scopes.server.ServiceProvider.GetRequiredService<AuthorizationAllCombinations>();

//		var readParameter = new VoidBool();
//		this.writeAuthorizedObject = this.authorizedObjectFactory.Create(readParameter);
//	}

//	[Fact]
//	public async Task TestCreate()
//	{
//		var parameterTypes = Assembly.GetAssembly(type: typeof(AuthorizationAllCombinations))!
//		 .GetTypes()
//		 .Where(t => t.IsClass && t.BaseType!.Name.ToString() == "IDd")
//		 .ToList();

//		foreach (var parameterType in parameterTypes)
//		{
//			var parameter = (IDd)Activator.CreateInstance(parameterType)!;

//			var factoryCreateMethod = this.authorizedObjectFactory.GetType().GetMethods().Where(m => m.Name == "Create" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == parameterType).Single();

//			var result = factoryCreateMethod.Invoke(this.authorizedObjectFactory, new object[] { parameter });

//			var parameterName = parameterType.Name;

//			if (parameterName.Contains("Task") || parameterName.Contains("Remote"))
//			{
//				var resultTask = (Task)result!;
//				await resultTask;
//				result = resultTask.GetType().GetProperty("Result")!.GetValue(result);
//			}
//			else
//			{
//				Assert.Null(result as Task);
//			}

//			var authorizedObject = result as AuthorizedAllCombinations;

//			if (parameterName.Contains("Deny") || parameterName.Contains("FalseBool"))
//			{
//				Assert.Null(result);
//			}
//			else
//			{
//				Assert.NotNull(result);
//				Assert.Contains(parameter, authorizedObject!.Received);
//			}

//			if (parameterType.Name.Contains("Remote"))
//			{
//				Assert.DoesNotContain(parameter, this.authorizationClient.ReadReceived);
//				Assert.Contains(parameter, this.authorizationServer.ReadReceived);
//			}
//			else
//			{
//				Assert.Contains(parameter, this.authorizationClient.ReadReceived);
//				Assert.DoesNotContain(parameter, this.authorizationServer.ReadReceived);
//			}
//		}
//	}

//	[Fact]
//	public async Task TestCanCreate()
//	{
//		var parameterTypes = Assembly.GetAssembly(typeof(AuthorizationAllCombinations))!.GetTypes().Where(t => t.IsClass && t.BaseType!.Name.ToString() == "IDd").ToList();

//		foreach (var parameterType in parameterTypes)
//		{
//			var parameter = Activator.CreateInstance(parameterType)!;

//			var factoryCreateMethod = this.authorizedObjectFactory.GetType().GetMethods().Where(m => m.Name == "CanCreate" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == parameterType).Single();

//			var result = factoryCreateMethod.Invoke(this.authorizedObjectFactory, new object[] { parameter });

//			var parameterName = parameterType.Name;

//			if (parameterName.Contains("TaskString") || parameterName.Contains("TaskBool") || Regex.IsMatch(parameterName, @".\w+Remote\w*"))
//			{
//				var resultTask = (Task)result!;
//				await resultTask;
//				result = resultTask.GetType().GetProperty("Result")!.GetValue(result);
//			}
//			else
//			{
//				Assert.IsNotType<Task>(result);
//			}

//			Assert.IsType<Authorized>(result);

//			var authorized = (Authorized)result;

//			if (parameterName.Contains("Deny"))
//			{
//				Assert.False(authorized.HasAccess);
//			}
//			else
//			{
//				Assert.True(authorized.HasAccess);
//			}

//			if (Regex.IsMatch(parameterName, @".\w+Remote\w*"))
//			{
//				Assert.DoesNotContain(parameter, this.authorizationClient.ReadReceived);
//				Assert.Contains(parameter, this.authorizationServer.ReadReceived);
//			}
//			else
//			{
//				Assert.Contains(parameter, this.authorizationClient.ReadReceived);
//				Assert.DoesNotContain(parameter, this.authorizationServer.ReadReceived);
//			}
//		}
//	}

//	[Fact]
//	public async Task TestSave()
//	{
//		var parameterTypes = Assembly.GetAssembly(typeof(AuthorizationAllCombinations))!.GetTypes().Where(t => t.IsClass && t.BaseType!.Name.ToString() == "IDd").ToList();

//		foreach (var parameterType in parameterTypes)
//		{
//			var parameterName = parameterType.Name;
//			var parameter = Activator.CreateInstance(parameterType)!;

//			var factorySaveMethod = this.authorizedObjectFactory.GetType().GetMethods()
//				 .Where(m => m.Name == "Save" && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType == parameterType).Single();

//			var methodName = factorySaveMethod.Name.ToString();
//			var parameterText = string.Join(", ", factorySaveMethod.GetParameters().Select(p => p.ParameterType.Name));

//			object? result;

//			if (parameterType.Name.Contains("Deny"))
//			{
//				await Assert.ThrowsAsync<NotAuthorizedException>(() =>
//				{
//					try
//					{
//						if (parameterType.Name.Contains("Task") || parameterType.Name.Contains("Remote"))
//						{
//							return (Task)factorySaveMethod.Invoke(this.authorizedObjectFactory, [this.writeAuthorizedObject, parameter])!;
//						}
//						else
//						{
//							return Task.FromResult(factorySaveMethod.Invoke(this.authorizedObjectFactory, new object[] { this.writeAuthorizedObject!, parameter }));
//						}
//					}
//					catch (TargetInvocationException ex)
//					{
//						throw ex.InnerException!;
//					}
//				});

//				continue;
//			}
//			else
//			{
//				result = factorySaveMethod.Invoke(this.authorizedObjectFactory, new object[] { this.writeAuthorizedObject!, parameter });
//			}

//			if (parameterType.Name.Contains("Task") || parameterType.Name.Contains("Remote"))
//			{
//				var resultTask = (Task)result!;
//				await resultTask;
//				result = resultTask.GetType().GetProperty("Result")!.GetValue(result);
//			}
//			else
//			{
//				Assert.Null(result as Task);
//			}

//			var authorizedObject = result as AuthorizedAllCombinations;

//			if (parameterName.Contains("Deny") || parameterName.Contains("FalseBool"))
//			{
//				Assert.Null(result);
//			}
//			else
//			{
//				Assert.NotNull(result);
//				Assert.Contains(parameter, authorizedObject!.Received);
//			}

//			if (parameterType.Name.Contains("Remote"))
//			{
//				Assert.DoesNotContain(parameter, this.authorizationClient.WriteReceived);
//				Assert.Contains(parameter, this.authorizationServer.WriteReceived);
//			}
//			else
//			{
//				Assert.Contains(parameter, this.authorizationClient.WriteReceived);
//				Assert.DoesNotContain(parameter, this.authorizationServer.WriteReceived);
//			}
//		}
//	}


//	[Fact]
//	public async Task TestTrySave()
//	{
//		var parameterTypes = Assembly.GetAssembly(typeof(AuthorizationAllCombinations))!.GetTypes().Where(t => t.IsClass && t.BaseType!.Name.ToString() == "IDd").ToList();

//		foreach (var parameterType in parameterTypes)
//		{
//			var parameter = Activator.CreateInstance(parameterType)!;

//			var factoryCreateMethod = this.authorizedObjectFactory.GetType().GetMethods().Where(m => m.Name == "TrySave" && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType == parameterType).Single();

//			var result = factoryCreateMethod.Invoke(this.authorizedObjectFactory, new object[] { this.writeAuthorizedObject!, parameter });

//			var parameterName = parameterType.Name;

//			if (parameterName.Contains("Task") || parameterName.Contains("Remote"))
//			{
//				var resultTask = (Task)result!;
//				await resultTask;
//				result = resultTask.GetType().GetProperty("Result")!.GetValue(result);
//			}

//			var authorized = (Authorized<IAuthorizedAllCombinations>)result!;

//			if (parameterName.Contains("Deny"))
//			{
//				Assert.Null(authorized.Result);
//				Assert.False(authorized.HasAccess);
//			}
//			else if (parameterName.Contains("FalseBool"))
//			{
//				Assert.Null(authorized.Result);
//				Assert.True(authorized.HasAccess);
//			}
//			else
//			{
//				Assert.NotNull(authorized.Result);
//				Assert.Contains(parameter, authorized.Result.Received);
//			}

//			if (parameterType.Name.Contains("Remote"))
//			{
//				Assert.DoesNotContain(parameter, this.authorizationClient.WriteReceived);
//				Assert.Contains(parameter, this.authorizationServer.WriteReceived);
//			}
//			else
//			{
//				Assert.Contains(parameter, this.authorizationClient.WriteReceived);
//				Assert.DoesNotContain(parameter, this.authorizationServer.WriteReceived);
//			}
//		}
//	}



//	[Fact]
//	public async Task TestCanInsert()
//	{
//		var parameterTypes = Assembly.GetAssembly(typeof(AuthorizationAllCombinations))!.GetTypes().Where(t => t.IsClass && t.BaseType!.Name.ToString() == "IDd").ToList();

//		foreach (var parameterType in parameterTypes)
//		{
//			var parameter = Activator.CreateInstance(parameterType)!;

//			var factoryCreateMethod = this.authorizedObjectFactory.GetType().GetMethods().Where(m => m.Name == "CanInsert" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == parameterType).Single();

//			var result = factoryCreateMethod.Invoke(this.authorizedObjectFactory, [parameter]);

//			var parameterName = parameterType.Name;

//			if (parameterName.Contains("TaskString") || parameterName.Contains("TaskBool") || Regex.IsMatch(parameterName, @".\w+Remote\w*"))
//			{
//				var resultTask = (Task)result!;
//				await resultTask;
//				result = resultTask.GetType().GetProperty("Result")!.GetValue(result);
//			}
//			else
//			{
//				Assert.Null(result as Task);
//			}

//			Assert.IsType<Authorized>(result);

//			var authorized = (Authorized)result;

//			if (parameterName.Contains("Deny"))
//			{
//				Assert.False(authorized.HasAccess);
//			}
//			else
//			{
//				Assert.True(authorized.HasAccess);
//			}

//			if (Regex.IsMatch(parameterName, @".\w+Remote\w*"))
//			{
//				Assert.DoesNotContain(parameter, this.authorizationClient.WriteReceived);
//				Assert.Contains(parameter, this.authorizationServer.WriteReceived);
//			}
//			else
//			{
//				Assert.Contains(parameter, this.authorizationClient.WriteReceived);
//				Assert.DoesNotContain(parameter, this.authorizationServer.WriteReceived);
//			}
//		}
//	}

//}
