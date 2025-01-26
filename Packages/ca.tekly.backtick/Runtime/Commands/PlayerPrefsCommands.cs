using UnityEngine;

namespace Tekly.Backtick.Commands
{
    public class PlayerPrefsCommands : ICommandSource
    {
        [Command("prefs.float")]
        [Help("Get or set a PlayerPrefs float")]
        public string Float(string pref, float? value)
        {
            if (value.HasValue) {
                PlayerPrefs.SetFloat(pref, value.Value);
            }

            if (!PlayerPrefs.HasKey(pref)) {
                return $"Pref {pref.Gray()} is not set.";
            }
            
            return $"{pref} = [{PlayerPrefs.GetFloat(pref).ToString().Gray()}]";
        }
        
        [Command("prefs.string")]
        [Help("Get or set a PlayerPrefs string")]
        public string String(string pref, string value = null)
        {
            if (value != null) {
                PlayerPrefs.SetString(pref, value);
            }

            if (!PlayerPrefs.HasKey(pref)) {
                return $"Pref {pref.Gray()} is not set.";
            }
            
            return $"{pref} = [{PlayerPrefs.GetString(pref).Gray()}]";
        }
        
        [Command("prefs.int")]
        [Help("Get or set a PlayerPrefs int")]
        public string Int(string pref, int? value)
        {
            if (value.HasValue) {
                PlayerPrefs.SetInt(pref, value.Value);
            }

            if (!PlayerPrefs.HasKey(pref)) {
                return $"Pref {pref.Gray()} is not set.";
            }
            
            return $"{pref} = [{PlayerPrefs.GetInt(pref).ToString().Gray()}]";
        }
        
        [Command("prefs.delete")]
        [Help("Delete a pref")]
        public string Delete(string pref)
        {
            PlayerPrefs.DeleteKey(pref);
            return $"PlayerPref [{pref.Gray()}] has been deleted.";
        }
        
        [Command("prefs.clear")]
        [Help("Clear all prefs")]
        public string DeleteAllPrefs()
        {
            PlayerPrefs.DeleteAll();
            return "All PlayerPrefs have been cleared.";
        }
    }
}