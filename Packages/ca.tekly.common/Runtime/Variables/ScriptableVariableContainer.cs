using Tekly.Common.LocalPrefs;
using UnityEngine;

namespace Tekly.Common.Variables
{
    [CreateAssetMenu(menuName = "Game/Variables/Container")]
    public class ScriptableVariableContainer : ScriptableObject
    {
        public ScriptableVariable[] Variables;

        public void Initialize(PrefsContainer prefsContainer)
        {
            foreach (var variable in Variables) {
                variable.Initialize(this, prefsContainer);
            }
        }
        
        public T Get<T>(string variableName) where T : ScriptableVariable
        {
            foreach (var variable in Variables) {
                if (variable.name == variableName) {
                    return variable as T;
                }
            }

            return null;
        }

        public void VariableChanged(ScriptableVariable scriptableVariable)
        {
            Debug.Log("Variable Changed: " + scriptableVariable.name);
        }
    }
}