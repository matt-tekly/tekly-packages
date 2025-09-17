using System;
using System.Text;
using Tekly.Common.Observables;

namespace Tekly.DataModels.Models
{
    public interface IModel : IDisposable
    {
        void Tick();
        void ToJson(StringBuilder sb);
    }
    
    public interface IValueModel : IModel, IComparable<IValueModel>
    {
        string ToDisplayString();
        ITriggerable<Unit> Modified { get; }
        bool IsTruthy { get; }
    }
}