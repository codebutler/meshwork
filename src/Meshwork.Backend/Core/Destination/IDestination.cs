//
// IDestination.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Meshwork.Backend.Core.Transport;

namespace Meshwork.Backend.Core.Destination
{
	public delegate void DestinationEventHandler (IDestination destination);

	public interface IDestinationSource 
	{
		event DestinationEventHandler DestinationAdded;
		event DestinationEventHandler DestinationRemoved;

		void Update ();

		Type DestinationType { 
			get;
		}

		Type ListenerType {
			get;
		}

		IList<IDestination> Destinations {
			get;
		}
	}

	public static class DestinationTypeFriendlyNames
	{
		static Dictionary<string,string> friendlyNames = new Dictionary<string,string>();

		public static void RegisterFriendlyName (Type type, string friendlyName)
		{
			friendlyNames[type.ToString()] = friendlyName;
		}

		public static string GetFriendlyName (Type type)
		{
			return GetFriendlyName(type.ToString());
		}

		public static string GetFriendlyName (string typeName)
		{
			if (friendlyNames.ContainsKey(typeName)) {
				return friendlyNames[typeName];
			} else {
				return typeName;
			}
		}
	}

	/*
	public static class DestinationFactory
	{
		static Dictionary<string, IDestinationSource> sources = new Dictionary<string, IDestinationSource>();

		public static void RegisterSource (IDestinationSource source)
		{
			sources[source.DestinationType.ToString()] = source;
		}

		public static bool SupportsDestinationType (string typeName)
		{
			return sources.ContainsKey(typeName);
		}

		public static IDestination Create (DestinationInfo info)
		{
			if (!SupportsDestinationType(info.TypeName)) {
				throw new InvalidOperationException(String.Format("Destination type {0} is not supported.", info.TypeName));
			}

			Type type = sources[info.TypeName].DestinationType;

			IDestination destination;
			destination = (IDestination)type.InvokeMember("CreateFromInfo",
			                                              BindingFlags.Static, 
								      null, null, new object[]{info});
			return destination;
		}

		public static ITransportListener CreateListener (DestinationInfo info)
		{
			throw new NotImplementedException();
		}
	}*/

	[Serializable]
	public class DestinationInfo
	{
		bool         openExternal;
		bool         local;
		string       typeName;
		string[]     data;

		public string TypeName {
			get {
				return typeName;
			}
			set {
				typeName = value;
			}
		}

		public string[] Data {
			get {
				return data;
			}
			set {
				data = value;
			}
		}

		[XmlIgnore]
		public bool Local {
			get {
				return local;
			}
			set {
				local = value;
			}
		}

		public bool IsOpenExternally {
			get {
				return openExternal;
			}
			set {
				openExternal = value;
			}
		}

		[XmlIgnore]
		public bool Supported {
			get {
				return Core.DestinationManager.SupportsDestinationType(typeName);
			}
		}

		public IDestination CreateDestination ()
		{
			if (!Local) {
				throw new InvalidOperationException("May not call CreateDestination() on non-local DestinationInfo. Use CreateAndAddDestination() instead.");
			}
			
			Type destinationType = Type.GetType(typeName);
			IDestination destination = (IDestination)Activator.CreateInstance(destinationType, new object[] { this });
			return destination;
		}

		public IDestination CreateAndAddDestination (List<IDestination> parentList)
		{
			Type destinationType = Type.GetType(typeName);
			IDestination destination = (IDestination)Activator.CreateInstance(destinationType, new object[] { this });

			((DestinationBase)destination).ParentList = parentList.AsReadOnly();
			parentList.Add(destination);
			
			return destination;
		}

		public string FriendlyName {
			get {
				return DestinationTypeFriendlyNames.GetFriendlyName(this.TypeName);
			}
		}
	}

	public abstract class DestinationBase : IDestination
	{
		public abstract ITransport CreateTransport (ulong connectionType);

		public abstract DestinationInfo CreateDestinationInfo();

		public abstract bool IsExternal {
			get;
		}

		public abstract bool CanConnect {
			get;
		}

		protected bool isOpenExternally = false;

		public bool IsOpenExternally {
			get {
				return isOpenExternally;
			}
		}

		protected IList<IDestination> parentList;

		public IList<IDestination> ParentList {
			get {
				return parentList;
			}
			internal set {
				parentList = value;
			}
		}

		public string FriendlyTypeName {
			get {
				return DestinationTypeFriendlyNames.GetFriendlyName(this.GetType().ToString());
			}
		}
		
		public abstract int CompareTo (IDestination other);
	}

	public interface IDestination : IComparable<IDestination>
	{
		DestinationInfo CreateDestinationInfo();

		ITransport CreateTransport(ulong connectionType);

		bool IsExternal {
			get;
		}

		bool CanConnect {
			get;
		}

		bool IsOpenExternally {
			get;
		}

		IList<IDestination> ParentList {
			get;
		}

		string FriendlyTypeName {
			get;
		}
	}
}
