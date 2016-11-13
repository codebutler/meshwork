using System;
using System.Collections.Generic;

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
}