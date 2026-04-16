using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts.Models
{
    public class PLCComandsContainer
    {
        private readonly Dictionary<string, List<PLCCommandObject>> _plcCommandsBySource;
        public PLCComandsContainer()
        {
            _plcCommandsBySource = new Dictionary<string, List<PLCCommandObject>>();
        }

        // Добавление подпрограммы
        public void AddSubProgram(string plcId, PLCCommandObject plcCommandObject)
        {
            if (plcCommandObject == null)
                throw new ArgumentNullException(nameof(plcCommandObject));

            if (string.IsNullOrEmpty(plcId))
                throw new ArgumentException("SourceId не может быть пустым");

            if (!_plcCommandsBySource.TryGetValue(plcId, out var plcCommandsBySource))
            {
                plcCommandsBySource = new List<PLCCommandObject>();
                _plcCommandsBySource[plcId] = plcCommandsBySource;
            }

            plcCommandsBySource.Add(plcCommandObject);
        }

        // Получение всех подпрограмм для устройства в порядке добавления
        public List<PLCCommandObject> GetPlcCommandObjects(string sourceId, bool getOnlyActive = true)
        {
            if (_plcCommandsBySource.TryGetValue(sourceId, out var plcCommandsBySource))
            {
                if (getOnlyActive)
                {
                    return plcCommandsBySource.Where(p => p.Reference.activeInHierarchy).ToList();
                }
                return plcCommandsBySource;
            }

            return new List<PLCCommandObject>();
        }

    }
}
