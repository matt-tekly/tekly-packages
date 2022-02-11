// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

namespace Tekly.DataModels.Models
{
    public class StringValueModel : BasicValueModel
    {
        public StringValueModel(string value) : base(value) { }
    }
    
    public class NumberValueModel : BasicValueModel
    {
        public NumberValueModel(double value) : base(value) { }
    }

    public class BoolValueModel : BasicValueModel
    {
        public BoolValueModel(bool value) : base(value) { }
    }
}