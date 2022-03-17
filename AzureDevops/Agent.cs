using System;

namespace AzureAgentAutoInstaller.AzureDevops
{
    public class Agent
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public IEnumerable<string> Tags { get; set; }
    }
}
