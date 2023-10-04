using Tekly.DataModels.Models;
using UnityEngine;

namespace Tekly.DataModels.Binders.Collections
{
	public abstract class ItemTemplateProvider : MonoBehaviour
	{
		public abstract BinderContainer Get(ObjectModel model);
	}
}