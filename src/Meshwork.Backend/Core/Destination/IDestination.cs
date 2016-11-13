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
using Meshwork.Backend.Core.Transport;

namespace Meshwork.Backend.Core.Destination
{

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
