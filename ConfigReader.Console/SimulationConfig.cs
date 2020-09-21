using System;
using System.Collections.Generic;

namespace TGW.ConfigReader.Console
{
    public sealed class SimulationConfig
    {
        private  static readonly object _locker = new object();
        private static SimulationConfig _instance = null;

        private SimulationConfig()
        {
            Parameters = new Dictionary<string, string>();
        }

        public static SimulationConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_locker)
                    {
                        if (_instance == null)
                            _instance = new SimulationConfig();
                    }
                }

                return _instance;
            }
        }


        public Dictionary<string, string> Parameters { get; }

        public bool TryGetConfigValue<T>(string parameterName, out T value) where T : IConvertible
        {
            value = default(T);
            bool success = false;
            
            if (Parameters.TryGetValue(parameterName, out string parameterValue))
            {
                try
                {
                    value = (T) Convert.ChangeType(parameterValue, typeof(T));
                    success = true;
                }
                catch { }

            }
            return success;
        }

        public T GetConfigValue<T>(string parameterName) where T : IConvertible
        {
            if (Parameters.TryGetValue(parameterName, out string parameterValue))
            {
                return (T)Convert.ChangeType(parameterValue, typeof(T));
            }
            else
            {
                throw new ApplicationException($"Simulation config parameter {parameterName} not exists");
            }
        }
    }
}
