# Install Azure PowerShell
# https://docs.microsoft.com/en-us/powershell/azure/install-az-ps?view=azps-5.2.0
# Install-Module -Name Az -AllowClobber -Scope CurrentUser

# Resource group should already exist
Param(
    [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
    [String]
    $resourceGroupName,

    [Parameter(Mandatory=$true, ValueFromPipeline=$true)]
    [String]
    $subscriptionId
)

$ErrorActionPreference = "Stop"

Write-Host "Awaiting sign-in..."
$quiet = Connect-AzAccount -DeviceCode
Write-Host "Selecting subscription..."
$quiet = Set-AzContext -Subscription $subscriptionId

Write-Host "Applying ARM template..."
$deploymentResults = New-AzResourceGroupDeployment `
	-Name "$resourceGroupName-ServicesDeployment" `
	-ResourceGroupName $resourceGroupName `
	-TemplateFile .\azure-resources.json `
	-TemplateParameterFile .\azure-parameters.json `

Write-Host "Bot web app: $($deploymentResults.Outputs.webAppName.Value)";
Write-Host "Function web app: $($deploymentResults.Outputs.funcAppName.Value)";

Write-Host "Waiting for 2 minutes to ensure services are ready..."
Start-Sleep -Seconds (2 * 60)

$currentPath = (Get-Location).Path

Write-Host "Publishing Web App..."
$pathToZip = "$currentPath\FindMe.Bot.zip"
$quiet = Publish-AzWebApp -ResourceGroupName $resourceGroupName -Name $deploymentResults.Outputs.webAppName.Value -ArchivePath $pathToZip -Force

Write-Host "Publishing Function App..."
$pathToZip = "$currentPath\FindMe.Func.zip"
$quiet = Publish-AzWebApp -ResourceGroupName $resourceGroupName -Name $deploymentResults.Outputs.funcAppName.Value -ArchivePath $pathToZip -Force

$quiet = Disconnect-AzAccount

Write-Host "Deployment completed."
