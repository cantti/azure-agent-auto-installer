using System;

namespace AzureAgentAutoInstaller.AzureDevops.RestDto
{
    public class TaskAgentDto
    {
        public string Name { get; set; }
        public string Status { get; set; }
    }

    public class DeploymentMachineDto
    {
        public TaskAgentDto Agent { get; set; }
    }

    public class DeploymentGroupDto
    {
        public IEnumerable<DeploymentMachineDto> Machines { get; set; }
    }
}
