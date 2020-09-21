using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using TGW.ConfigReader.Interfaces.Settings;
using Microsoft.Extensions.Logging;
using TGW.ConfigReader.Interfaces;

namespace TGW.ConfigReader.Console
{
    public class ConsoleSetup : IConsoleSetup, IHostedService
    {
        #region CONSTANTS

        private const string APP_CONFIG_PATH = @".\Configs\appSettings.json";
        private const string PARAMETER_REGEX = @"^[A-Za-z0-9]+:\t*[^\t\s]*";

        #endregion

        #region FIELDS

        private static ConsoleSettings _consoleSettings;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<ConsoleSetup> _logger;

        #endregion

        #region CONSTRUCTORS

        public ConsoleSetup(IFileSystem fileSystem, ILogger<ConsoleSetup> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        #endregion

        public void InitConsoleSettings()
        {

            this._logger.LogInformation($"Starting {nameof(InitConsoleSettings)}");
            
            if (_consoleSettings == null)
            {
                if (!_fileSystem.File.Exists(APP_CONFIG_PATH))
                    throw new ApplicationException($"Application config not found at {APP_CONFIG_PATH}!");

                var configBuilder = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile(APP_CONFIG_PATH, false)
                   .Build();
                _consoleSettings = configBuilder.Get<ConsoleSettings>();
            }

            Validate();

            ReadSimulationConfigurationFiles();

            this._logger.LogInformation($"Finished {nameof(InitConsoleSettings)}");

            StartDemo();

        }

        private void StartDemo()
        {
            this._logger.LogInformation("Starting demo..");


            this._logger.LogInformation("Printing all simulation config values:");
            this._logger.LogInformation("ParameterName:ParameterValue");
            foreach (KeyValuePair<string, string> parameter in SimulationConfig.Instance.Parameters)
            {
                this._logger.LogInformation(parameter.Key + ":" + parameter.Value);
            }


            this._logger.LogInformation("This is an example how config values should be read.");
            if (SimulationConfig.Instance.TryGetConfigValue<int>("numberOfAisles", out int numberOfAisles))
                this._logger.LogInformation($"Read simulation config value numberOfAisles. Value {numberOfAisles}, resolved type {numberOfAisles.GetType()}");


            this._logger.LogInformation("Or other way without Try...");
            var value = SimulationConfig.Instance.GetConfigValue<int>("numberOfAisles");
            this._logger.LogInformation($"Read simulation config value numberOfAisles. Value {value}, resolved type {value.GetType()}");


            this._logger.LogInformation("Finished demo");
            System.Console.ReadKey();
        }


        private void ReadSimulationConfigurationFiles()
        {
            var orderedConfigs = _consoleSettings.SimulationConfigSettings.OrderBy(x => x.ConfigPriority);

            foreach (SimulationConfigSetting modelSetting in orderedConfigs)
            {
                ReadSimulationConfig(modelSetting.ConfigPath);
            }
        }


        private void Validate()
        {
            //checking if config priorities are unique
            var result = _consoleSettings.SimulationConfigSettings.GroupBy(x => x.ConfigPriority)
               .Where(g => g.Count() > 1)
               .Select(y => y.GetEnumerator())
               .ToList();

            if (result.Count > 0)
                throw new ApplicationException("Simulation config settings priorities must be unique!");

        }


        private void ReadSimulationConfig(string configPath)
        {
            this._logger.LogInformation("Checking if config file {ConfigFile} file exists", configPath);
            if (!_fileSystem.File.Exists(configPath))
                throw new ApplicationException($"Can not find simulation config at {configPath}");

            this._logger.LogInformation("Config file {ConfigFile} exists. Reading config file.", configPath);
            var configLines = _fileSystem.File.ReadLines(configPath);

            foreach (string configLine in configLines)
            {
                var matches = Regex.Matches(configLine, PARAMETER_REGEX);

                if(matches.Count>1)
                    throw new ApplicationException($"Multiple parameters ({string.Join(", ",matches.Select(x=>x.Value))}) found in one line! Config file: {configPath}");

                if ( matches.Count == 1 )
                {
                    var parameter = SplitParameterNameFromValue(matches[0].Value);

                    this._logger.LogInformation("Successfully parsed config parameter. Parameter name {ParameterName}, parameter value {ParameterValue}. Trying to Add to SimulationConfig instance.", parameter.Key, parameter.Value);
                    //if setting already exists then override, because configs are reading by priority going from lowest to highest 
                    if (!SimulationConfig.Instance.Parameters.TryAdd(parameter.Key, parameter.Value)) 
                    {
                        this._logger.LogInformation("Parameter {ParameterName} already exists. Overriding with new value", parameter.Key);
                        SimulationConfig.Instance.Parameters[parameter.Key] = parameter.Value;
                    }
                    this._logger.LogInformation("Successfully added to Simulation config instance.");
                }
                else
                {
                    this._logger.LogDebug($"Skipping config line, because not matched search pattern. Line {configLine}");
                }
            }
        }

        private KeyValuePair<string, string> SplitParameterNameFromValue(string line)
        {
            int firstColonIndex = line.IndexOf(':');
            string parameterName = line.Substring(0, firstColonIndex);
            string parameterValue = line.Substring(firstColonIndex +1).Trim();
            return new KeyValuePair<string, string>(parameterName, parameterValue);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                InitConsoleSettings();
            }
            catch (Exception e)
            {
                this._logger.LogError(e, "Error occured.");
                throw;
            }

             return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
