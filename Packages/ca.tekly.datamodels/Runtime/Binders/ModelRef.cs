using System;

namespace Tekly.DataModels.Binders
{
    [Serializable]
    public struct ModelRef
    {
        public string Path;

        public static ModelRef Create(string path)
        {
            return new ModelRef {
                Path = path
            };
        }
    }
}