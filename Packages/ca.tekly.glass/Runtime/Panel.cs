using System.Linq;
using UnityEngine;

namespace Tekly.Glass
{
    public class Panel : MonoBehaviour
    {
        public PanelAttachment[] Attachments;

        public PanelAttachment GetAttachment(string id)
        {
            return Attachments?.FirstOrDefault(x => x.name == id);
        }
    }
}