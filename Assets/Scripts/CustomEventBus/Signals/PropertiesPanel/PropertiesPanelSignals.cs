using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.CustomEventBus.Signals.PropertiesPanel
{
    public class PropertiesTransformUpdateSignal
    {

    }
    public class ChangePropertiesProviderSignal
    {
        public readonly IPropertyProvider PropertyProvider;

        public ChangePropertiesProviderSignal(IPropertyProvider propertyProvider)
        {
            PropertyProvider = propertyProvider;
        }
    }
    public class TogglePropertiesSignal
    {

    }
    public class ChangeAnglesJOGSignal
    {
        public readonly RobotPropertyProvider Robot;

        public ChangeAnglesJOGSignal(RobotPropertyProvider robot)
        {
            Robot = robot;
        }
    }
    public class ChangeConfigJOGSignal
    {
        public readonly RobotPropertyProvider Robot;

        public ChangeConfigJOGSignal(RobotPropertyProvider robot)
        {
            Robot = robot;
        }
    }
    //public class HidePropertiesSignal
    //{

    //}
    public class ChangeNamePropertySignal
    {
        public readonly string Id;
        public readonly string Name;
        public ChangeNamePropertySignal(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
