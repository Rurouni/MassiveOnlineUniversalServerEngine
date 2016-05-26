using System;
using System.Fabric;
using MOUSE.Core.Interfaces.Configuration;
using Newtonsoft.Json.Linq;

namespace MOUSE.Core.Azure.ServiceFabric
{
    public class FabricConfigProvider<TConfig> : IConfigProvider<TConfig>
        where TConfig : class
    {
        public FabricConfigProvider(string configurationSectionName)
        {
            CodePackageActivationContext activationContext = FabricRuntime.GetActivationContext();
            ConfigurationPackage configPackage = activationContext.GetConfigurationPackageObject("Config");
            if (!configPackage.Settings.Sections.Contains(configurationSectionName))
            {
                throw new ApplicationException($"Configuration section {configurationSectionName} not found");
            }
            var section = configPackage.Settings.Sections[configurationSectionName];
            var obj = new JObject();
            foreach (var setting in section.Parameters)
            {
                obj.Add(setting.Name, setting.Value);   
            }

            Config = obj.ToObject<TConfig>();
        }

        public TConfig Config { get; }
    }
}