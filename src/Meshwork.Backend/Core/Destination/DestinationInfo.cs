using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Meshwork.Backend.Core.Destination
{
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

        public bool IsSupported(Core core)
        {
            return core.DestinationManager.SupportsDestinationType(typeName);
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

        public string FriendlyName => DestinationTypeFriendlyNames.GetFriendlyName(TypeName);
    }
}