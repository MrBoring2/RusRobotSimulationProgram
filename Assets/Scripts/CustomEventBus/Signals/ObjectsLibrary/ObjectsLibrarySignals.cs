using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.CustomEventBus.Signals.ObjectsLibrary
{
    public class ShowObjectsLibrarySignal
    {
        public readonly string ParentId;

        public ShowObjectsLibrarySignal(string parentId)
        {
            ParentId = parentId;
        }
    }
}
