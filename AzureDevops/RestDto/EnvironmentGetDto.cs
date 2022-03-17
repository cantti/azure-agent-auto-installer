namespace AzureAgentAutoInstaller.AzureDevops.RestDto
{
    public class ResourceDto
    {
        public string Name { get; set; }
        public IEnumerable<string> Tags { get; set; }
    }

    public class EnvironmentDto
    {
        public IEnumerable<ResourceDto> Resources { get; set; }
    }
}