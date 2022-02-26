// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
using System.Text;

namespace Tekly.DataModels.Models
{
    public interface IModel : IDisposable
    {
        void Tick();
        void ToJson(StringBuilder sb);
    }

    public interface ITickable
    {
        void Tick();
    }
    
    public interface IValueModel : IModel
    {
        string ToDisplayString();
    }
}