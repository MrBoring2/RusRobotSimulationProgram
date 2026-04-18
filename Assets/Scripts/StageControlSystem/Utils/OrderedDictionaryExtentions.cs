using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Utils
{
    public static class OrderedDictionaryExtensions
    {
        public static int IndexOf(this OrderedDictionary dict, string key)
        {
            for (int i = 0; i < dict.Count; i++)
            {
                if (dict.Cast<DictionaryEntry>().ElementAt(i).Key.ToString() == key)
                {
                    return i;
                }
            }
            return -1;
        }

        public static bool MoveTo(this OrderedDictionary dict, string key, int newIndex)
        {
            var oldIndex = dict.IndexOf(key);
            if (oldIndex == -1) return false;

            if (oldIndex == newIndex) return true;

            var value = dict[key];
            dict.RemoveAt(oldIndex);

            if (newIndex > oldIndex) newIndex--;

            if (newIndex >= 0 && newIndex < dict.Count)
            {
                dict.Insert(newIndex, key, value);
            }
            else
            {
                dict.Add(key, value);
            }

            return true;
        }
    }
}
