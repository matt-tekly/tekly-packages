using System.Collections.Generic;
using System.Linq;

namespace Tekly.Sheets.Data
{
    public enum DataObjectType
    {
        Array,
        Object
    }
    
    /// <summary>
    /// A JSON like object that can easily add values at a given path.
    /// </summary>
    public class DataObject
    {
        public readonly Dictionary<PathKey, object> Object = new Dictionary<PathKey, object>();

        public readonly DataObjectType Type;

        public DataObject(DataObjectType type)
        {
            Type = type;
        }

        public override string ToString()
        {
            return Type.ToString();
        }

        public void Set(string path, object obj)
        {
            var keys = path.Split('.').Select(x => new PathKey(x)).ToArray();
            Set(keys, obj);
        }

        public void Set(int index, object obj)
        {
            var keys = new[] {
                new PathKey(index)
            };
            Set(keys, obj);
        }

        public void Set(PathKey[] path, object obj)
        {
            var currentObject = this;

            var lastIndex = path.Length - 1;
            for (var i = 0; i < lastIndex; i++) {
                var current = path[i];

                if (!currentObject.Object.TryGetValue(current, out var dataObj)) {
                    var nextKey = path[i + 1];
                    var newObj = new DataObject(nextKey.IsNumber ? DataObjectType.Array : DataObjectType.Object);
                    currentObject.Object.Add(current, newObj);
                    currentObject = newObj;
                } else {
                    currentObject = dataObj as DataObject;
                }
            }

            currentObject.Object[path[lastIndex]] = obj;
        }

        public object GetPath(string path)
        {
            var keys = path.Split('.').Select(x => new PathKey(x)).ToArray();
            return Get(keys);
        }

        public object Get(PathKey[] path)
        {
            var currentObject = this;

            var lastIndex = path.Length - 1;
            for (var i = 0; i < lastIndex; i++) {
                var current = path[i];
                if (!currentObject.Object.TryGetValue(current, out var obj)) {
                    return null;
                }

                currentObject = obj as DataObject;
            }
            
            return currentObject.Object[path[lastIndex]];
        }
        
        public bool TryGet<T>(string path, out T result)
        {
            var keys = path.Split('.').Select(x => new PathKey(x)).ToArray();
            return TryGet(keys, out result);
        }
        
        public bool TryGet<T>(PathKey[] path, out T result)
        {
            var currentObject = this;

            var lastIndex = path.Length - 1;
            for (var i = 0; i < lastIndex; i++) {
                var current = path[i];
                if (!currentObject.Object.TryGetValue(current, out var obj)) {
                    result = default;
                    return false;
                }

                currentObject = obj as DataObject;
            }

            var found = currentObject.Object.TryGetValue(path[lastIndex], out var objResult);
            result = (T) objResult;
            
            return found;
        }

        public void Delete(string path)
        {
            var keys = path.Split('.').Select(x => new PathKey(x)).ToArray();
            Delete(keys);
        }

        public void Delete(PathKey[] path)
        {
            var currentObject = this;

            var lastIndex = path.Length - 1;
            for (var i = 0; i < lastIndex; i++) {
                var current = path[i];

                if (!currentObject.Object.TryGetValue(current, out var dataObj)) {
                    return;
                }

                currentObject = dataObj as DataObject;
            }

            currentObject.Object.Remove(path[lastIndex]);
        }
    }
}
