using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.ServiceProcess;
using AzureAgentAutoInstaller.ActiveDirectory;
using AzureAgentAutoInstaller.AzureDevops;

namespace AzureAgentAutoInstaller
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly AzureDevopsClient _azureDevopsClient;
        private readonly ActiveDirectoryClient _activeDirectoryClient;
        private readonly IConfiguration _configuration;

        public Worker(
            ILogger<Worker> logger,
            AzureDevopsClient azureDevopsClient,
            ActiveDirectoryClient activeDirectoryClient,
            IConfiguration configuration)
        {
            _logger = logger;
            _azureDevopsClient = azureDevopsClient;
            _activeDirectoryClient = activeDirectoryClient;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var computersFromAD = _activeDirectoryClient.GetRackComputers();

                var computersFromEnvironment = await _azureDevopsClient.GetComputersFromEnvironment();

                var installList = computersFromAD.Where(x => !computersFromEnvironment.Any(c => c.Name == x)
                                                             || computersFromEnvironment.Any(c => c.Status == "offline" && c.Name == x));

                foreach (var computer in installList)
                {
                    var serviceController = new ServiceController("winrm", computer);
                    if (serviceController.Status != ServiceControllerStatus.Running)
                    {
                        serviceController.Start();
                        serviceController.WaitForStatus(ServiceControllerStatus.Running);
                    }

                    var connectionInfo = new WSManConnectionInfo
                    {
                        ComputerName = computer
                    };

                    var runspace = RunspaceFactory.CreateRunspace(connectionInfo);

                    runspace.Open();

                    using PowerShell ps = PowerShell.Create();

                    ps.Runspace = runspace;

                    ps.AddScript(File.ReadAllText("install.ps1"));

                    ps.AddParameter("computerName", computer);
                    ps.AddParameter("pat", _configuration.GetValue<string>("AzureDevOps:Pat"));
                    ps.AddParameter("organizationUrl", $"https://dev.azure.com/{_configuration.GetValue<string>("AzureDevOps:Organization")}");
                    ps.AddParameter("project", _configuration.GetValue<string>("AzureDevOps:Project"));

                    try
                    {
                        var pipelineObjects = await ps.InvokeAsync();
                    }
                    catch (Exception ex)
                    {
                        continue;
                    }
                    finally
                    {
                        runspace.Close();
                    }
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}