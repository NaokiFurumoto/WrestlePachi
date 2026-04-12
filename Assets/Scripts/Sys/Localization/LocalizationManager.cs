using UnityEngine;
namespace GameSys
{
    public class LocalizationManager : BehaviourSingleton<LocalizationManager>
    {
        [SerializeField]
        private LocalizationDatabase? m_Database = null;

        [SerializeField]
        private Language m_CurrentLanguage
            = Language.Japanese;

        protected override void _SetInstance()
        {
            if (s_Instance == null)
                s_Instance = this;
            else if (s_Instance != this)
                Destroy(gameObject);
        }

        public string GetText(string key)
        {
            if (m_Database == null)
                return key;

            var entry = m_Database.GetEntry(key);

            if (entry == null)
                return key;

            switch (m_CurrentLanguage)
            {
                case Language.Japanese:
                    return entry.japanese;

                case Language.English:
                    return entry.english;

                case Language.Korean:
                    return entry.korean;

                default:
                    return key;
            }
        }

        public void ChangeLanguage(Language language)
        {
            m_CurrentLanguage = language;
        }
    }
}

