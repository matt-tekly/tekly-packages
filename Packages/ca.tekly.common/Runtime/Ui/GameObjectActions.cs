using UnityEngine;

namespace Tekly.Common.Ui
{
    public class GameObjectActions : MonoBehaviour
    {
        public void Toggle()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }
}