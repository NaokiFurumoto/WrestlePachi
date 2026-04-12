using System.Collections.Generic;
using UnityEngine;

namespace GameSys
{
    [CreateAssetMenu(fileName = "LocalizationDatabase",
        menuName = "Localization/Create Database")]
    public class LocalizationDatabase : ScriptableObject   // ← ここ追加
    {
        public List<LocalizedTextEntry> entries =
            new List<LocalizedTextEntry>();

        public LocalizedTextEntry GetEntry(string key)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].key == key)
                    return entries[i];
            }
            return null;
        }
    }
}

