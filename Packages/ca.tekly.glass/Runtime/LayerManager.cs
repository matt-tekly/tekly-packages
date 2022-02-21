using System.Linq;
using UnityEngine;

namespace Tekly.Glass
{
    public class LayerManager : MonoBehaviour
    {
        [SerializeField] private Layer[] m_layers;

        public Layer Get(string id)
        {
            return m_layers.FirstOrDefault(x => x.Id == id);
        }
    }
}