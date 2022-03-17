param ($computerName, $pat, $organizationUrl, $project)

$ErrorActionPreference = "Stop";

If (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent() ).IsInRole( [Security.Principal.WindowsBuiltInRole] "Administrator"))
{ throw "Run application as administrator" };

If (-NOT (Test-Path $env:SystemDrive\'azagent')) { mkdir $env:SystemDrive\'azagent' };

Set-Location $env:SystemDrive\'azagent';

$destFolder = "A1";

if (Test-Path ($destFolder)) {
    try {
       .\$destFolder\Config.cmd remove --auth PAT --token $pat --unattended
    }
    catch{}

    #hack to delete folder with long file names. Remove-Item will fail trying to remove it. But robocopy can do it.
    mkdir delete-me
    robocopy delete-me $destFolder /purge
    rmdir delete-me
} else {
    mkdir $destFolder;
}

Set-Location $destFolder;

$agentZip = "$PWD\agent.zip";

$DefaultProxy = [System.Net.WebRequest]::DefaultWebProxy; $securityProtocol = @();

$securityProtocol += [Net.ServicePointManager]::SecurityProtocol; $securityProtocol += [Net.SecurityProtocolType]::Tls12; [Net.ServicePointManager]::SecurityProtocol = $securityProtocol;

$WebClient = New-Object Net.WebClient;

$Uri = 'https://vstsagentpackage.azureedge.net/agent/2.195.1/vsts-agent-win-x64-2.195.1.zip';

if ($DefaultProxy -and (-not $DefaultProxy.IsBypassed($Uri))) {
    $WebClient.Proxy = New-Object Net.WebProxy($DefaultProxy.GetProxy($Uri).OriginalString, $True);
};

$WebClient.DownloadFile($Uri, $agentZip);

Add-Type -AssemblyName System.IO.Compression.FileSystem;

[System.IO.Compression.ZipFile]::ExtractToDirectory( $agentZip, "$PWD");

.\config.cmd --environment --environmentname "desktop" --agent $env:COMPUTERNAME --runasservice --work '_work' --url $organizationUrl --projectname $project --auth PAT --token $pat --addvirtualmachineresourcetags --replace --unattended; Remove-Item $agentZip;
