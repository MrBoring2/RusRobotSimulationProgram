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
    public class ShowPropertiesSignal
    {
        public readonly IPropertyProvider PropertyProvider;

        public ShowPropertiesSignal(IPropertyProvider propertyProvider)
        {
            PropertyProvider = propertyProvider;
        }
    }

    public class HidePropertiesSignal
    {

    }
    public class ChangeNamePropertySignal
    {

    }
}
