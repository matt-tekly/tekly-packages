using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tekly.Config
{
    public interface IConfigReader
    {
        bool Load(IDictionary<string, string> config);
        bool Get(string key, bool defaultValue);
        int Get(string key, int defaultValue);
        float Get(string key, float defaultValue);
        double Get(string key, double defaultValue);
        string Get(string key, string defaultValue);
    }

}