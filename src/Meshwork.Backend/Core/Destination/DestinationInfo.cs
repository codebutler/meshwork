using System;
using System.Collections.Generic;
using Meshwork.Common.Serialization;

namespace Meshwork.Backend.Core.Destination
{
    public class DestinationInfo
    {
        public string TypeName { get; set; }

        public string[] Data { get; set; }

        [DontSerialize]
        public bool Local { get; set; }

        public bool IsOpenExternally { get; set; }

        public bool IsSupported(Core core)
        {
            return core.DestinationManager.SupportsDestinationType(TypeName);
        }

        public IDestination CreateDestination (Core core)
        {
            if (!Local) {
                throw new InvalidOperationException("May not call CreateDestination() on non-local DestinationInfo. Use CreateAndAddDestination() instead.");
            }
			
            var destinationType = Type.GetType(TypeName);
            var destination = (IDestination)Activator.CreateInstance(destinationType, core, this);
            return destination;
        }

        public IDestination CreateAndAddDestination (Core core, List<IDestination> parentList)
        {
            var destinationType = Type.GetType(TypeName);
            var destination = (IDestination)Activator.CreateInstance(destinationType, core, this);

            ((DestinationBase)destination).ParentList = parentList.AsReadOnly();
            parentList.Add(destination);
			
            return destination;
        }

        [DontSerialize]
        public string FriendlyName => DestinationTypeFriendlyNames.GetFriendlyName(TypeName);
    }
}