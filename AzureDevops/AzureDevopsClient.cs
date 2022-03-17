using AzureAgentAutoInstaller.AzureDevops.RestDto;
using FluentResults;
using RestSharp;
using RestSharp.Authenticators;

namespace AzureAgentAutoInstaller.AzureDevops
{
    public class AzureDevopsClient
    {
        private readonly RestClient _client;
        private readonly IConfiguration _configuration;

        public AzureDevopsClient(IConfiguration configuration)
        {
            _configuration = configuration;
            _client = new RestClient("https://dev.azure.com/{organization}/{project}/_apis")
                .AddDefaultUrlSegment("organization", configuration.GetValue<string>("AzureDevOps:Organization"))
                .AddDefaultUrlSegment("project", configuration.GetValue<string>("AzureDevOps:Project"))
                .AddDefaultQueryParameter("api-version", "6.1-preview.1");
            _client.Authenticator = new HttpBasicAuthenticator("", configuration.GetValue<string>("AzureDevOps:Pat"));
        }

        public async Task<IEnumerable<Agent>> GetComputersFromEnvironment()
        {
            var environmentRequest = new RestRequest("distributedtask/environments/{environmentId}")
                .AddUrlSegment("environmentId", 45)
                .AddQueryParameter("expands", "resourceReferences");

            var environmentResponse = await _client.ExecuteAsync<EnvironmentDto>(environmentRequest);

            if (!environmentResponse.IsSuccessful)
            {
                throw environmentResponse.ErrorException;
            }

            //only deployment group contains status of agent
            var deploymentGroupRequest = new RestRequest("distributedtask/deploymentgroups/{deploymentGroupId}");

            var deploymentGroupResponse = await _client.ExecuteAsync<DeploymentGroupDto>(deploymentGroupRequest);

            if (!environmentResponse.IsSuccessful)
            {
                throw environmentResponse.ErrorException;
            }

            return environmentResponse.Data.Resources
                .GroupJoin(
                    deploymentGroupResponse.Data.Machines,
                    x => x.Name,
                    x => x.Agent.Name,
                    (a, b) => new
                    {
                        a.Name,
                        a.Tags,
                        b.SingleOrDefault().Agent.Status
                    }
                )
                .Select(x => new Agent { Name = x.Name, Status = x.Status, Tags = x.Tags.Distinct() });
        }
    }
}