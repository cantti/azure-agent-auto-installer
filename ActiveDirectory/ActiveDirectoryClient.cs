using System.DirectoryServices;

namespace AzureAgentAutoInstaller.ActiveDirectory
{
    public class ActiveDirectoryClient
    {
        private readonly IConfiguration _configuration;

        public ActiveDirectoryClient(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IEnumerable<string> GetRackComputers()
        {
            var computers = new List<string>();

            using DirectoryEntry entry = new(_configuration.GetValue<string>("DirectoryEntryPath"));
            using DirectorySearcher searcher = new(entry);

            searcher.PropertiesToLoad.Add("description");

            searcher.Filter = "(objectClass=computer)";

            foreach (SearchResult resEnt in searcher.FindAll())
            {
                var directoryEntry = resEnt.GetDirectoryEntry();
                string computerName = directoryEntry.Name;
                if (computerName.StartsWith("CN="))
                    computerName = computerName.Remove(0, "CN=".Length);
                computers.Add(computerName);
            }

            computers.Sort();

            return computers;
        }
    }
}