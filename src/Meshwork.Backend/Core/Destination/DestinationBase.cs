using System.Collections.Generic;
using Meshwork.Backend.Core.Transport;

namespace Meshwork.Backend.Core.Destination
{
    public abstract class DestinationBase : IDestination
    {
        protected DestinationBase(Core core)
        {
            Core = core;
        }

        public abstract ITransport CreateTransport (ulong connectionType);

        public abstract DestinationInfo CreateDestinationInfo();

        public Core Core { get; }

        public abstract bool IsExternal {
            get;
        }

        public abstract bool CanConnect {
            get;
        }

        // FIXME: Make readonly
        public bool IsOpenExternally { get; protected set; } = false;

        public IList<IDestination> ParentList { get; protected internal set; }

        public string FriendlyTypeName => DestinationTypeFriendlyNames.GetFriendlyName(GetType().ToString());

        public abstract int CompareTo (IDestination other);
    }
}