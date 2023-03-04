using Tekly.Common.Registrys;
using Tekly.Common.Utils;
using UnityEngine;

namespace Tekly.Injectors
{
    [CreateAssetMenu(menuName = "Tekly/Injectors/Ref")]
    public class InjectorContainerRef : RegisterableRef<InjectorContainer>
    {
        public override IRegistry<InjectorContainer> Registry => InjectorContainerRegistry.Instance;
    }
}