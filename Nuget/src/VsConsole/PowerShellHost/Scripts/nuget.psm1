# make sure we stop on exceptions
$ErrorActionPreference = "Stop"

# This object reprents the result value for tab expansion functions when no result is returned.
# This is so that we can distinguish it from $null, which has different semantics
$NoResultValue = New-Object PSObject -Property @{ NoResult = $true }

# Hashtable that stores tab expansion definitions
$TabExpansionCommands = New-Object 'System.Collections.Hashtable' -ArgumentList @([System.StringComparer]::InvariantCultureIgnoreCase)

function Register-TabExpansion {
<#
.SYNOPSIS
    Registers a tab expansion for the parameters of the specified command.
.DESCRIPTION
    Registers a tab expansion for the parameters of the specified command.
.PARAMETER Name
    Name of the command the expansion is for.
.EXAMPLE
    PS> Register-TabExpansion 'Set-Color', @{'color' = {'blue', 'green', 'red'}}
         This adds a tab expansion to the Set-Color command. Set-Color contains a single 
         parameter, color, with three possible expansion values.
#>
    [CmdletBinding()]
    param(
        [parameter(Mandatory = $true)]
        [string]$Name,
        [parameter(Mandatory = $true)]
        $Definition
    )

    # transfer $definition data into a new hashtable that compare values using InvariantCultureIgnoreCase
    $normalizedDefinition = New-Object 'System.Collections.Hashtable' -ArgumentList @([System.StringComparer]::InvariantCultureIgnoreCase)
    $definition.GetEnumerator() | % { $normalizedDefinition[$_.Name] = $_.Value }
        
    $TabExpansionCommands[$Name] = $normalizedDefinition
}

Register-TabExpansion 'Get-Package' @{
    'Source' = {
        GetPackageSources
    }
    'ProjectName' = {
        GetProjectNames
    }
}

Register-TabExpansion 'Install-Package' @{
    'Id' = {
        param($context)

        GetPackageIds (GetPackages $context)
    }
    'ProjectName' = {
        GetProjectNames
    }
    'Version' = {
        param($context)

        $parameters = @{}

        if ($context.Id) { $parameters.filter = $context.Id }
        if ($context.Source) { $parameters.source = $context.Source }
        if (IsPrereleaseSet $context) {
            $parameters.IncludePreRelease = $true 
        }

        $parameters.Remote = $true
        $parameters.AllVersions = $true
        GetPackageVersions $parameters $context 
    }
    'Source' = {
        GetPackageSources
    }
}

Register-TabExpansion 'Uninstall-Package' @{
    'Id' = {
        param($context)

        $parameters = @{}
        if ($context.id) { $parameters.filter = $context.id }

        GetPackageIds (Find-Package @parameters -ErrorAction SilentlyContinue)
    }
    'ProjectName' = {
        GetProjectNames
    }
    'Version' = {
        $parameters = @{}
        if ($context.id) { $parameters.filter = $context.id }

        GetPackageVersions $parameters $context
    }
}

Register-TabExpansion 'Update-Package' @{
    'Id' = {
        param($context)

        $parameters = @{}
        if ($context.id) { $parameters.filter = $context.id }

        GetPackageIds (Find-Package @parameters -ErrorAction SilentlyContinue)
    }
    'ProjectName' = {
        GetProjectNames
    }
    'Version' = {
        param($context)

        $parameters = @{}

        # Only show available versions if an id was specified
        if ($context.id) { 
            if ($context.Source) { $parameters.source = $context.Source }

            # Find the installed package (this might be nothing since we could hav a partial id)
            $packages = @(Get-Package $context.id | ?{ $_.Id -eq $context.id })

            $parameters.filter = $context.id 
            $parameters.Remote = $true
            $parameters.AllVersions = $true
            if (IsPrereleaseSet $context) {
                $parameters.IncludePreRelease = $true 
            }

            $versions = GetPackageVersions $parameters $context

            if($packages.Count) {
                $package = @($packages | Sort-Object Version)[0]

                # Only show versions that are higher than the lowest installed version
                $versions = $versions | ?{ $_ -gt $package.Version }
            }

            $versions
        }
    }
    'Source' = {
        GetPackageSources
    }
}

Register-TabExpansion 'Open-PackagePage' @{
    'Id' = {
        param($context)
        GetPackageIds (GetPackages $context)
    }
    'Version' = {
        param($context)

        $parameters = @{}

        if ($context.Id) { $parameters.filter = $context.Id }
        if ($context.Source) { $parameters.source = $context.Source }

        $parameters.Remote = $true
        $parameters.AllVersions = $true
        GetPackageVersions $parameters $context 
    }
    'Source' = {
        GetPackageSources
    }
}

Register-TabExpansion 'New-Package' @{ 'ProjectName' = { GetProjectNames } }
Register-TabExpansion 'Add-BindingRedirect' @{ 'ProjectName' = { GetProjectNames } }
Register-TabExpansion 'Get-Project' @{ 'Name' = { GetProjectNames } }

function HasProperty($context, $name) {
    return $context.psobject.properties | ? { $_.Name -eq $name }
}

function IsPrereleaseSet($context) {
	# Need to figure out a better way to do this. 
	return (HasProperty $context 'IncludePreRelease') -or (HasProperty $context 'PreRelease') -or (HasProperty $context 'Pre')
}

function GetPackages($context) {  
    $parameters = @{}

    if ($context.Id) { $parameters.filter = $context.Id }
    if ($context.Source) { $parameters.source = $context.Source }
    if (IsPrereleaseSet $context) {
        $parameters.IncludePreRelease = $true 
    }

    return Find-Package @parameters -Remote -ErrorAction SilentlyContinue
}

function GetProjectNames {
    $uniqueNames = @(Get-Project -All | Select-Object -ExpandProperty ProjectName)
    
    $simpleNames = Get-Project -All | Select-Object -ExpandProperty Name
    $safeNames = @($simpleNames | Group-Object | Where-Object { $_.Count -eq 1 } | Select-Object -ExpandProperty Name)

    ($uniqueNames + $safeNames) | Select-Object -Unique | Sort-Object
}

function GetPackageIds($packages) {
    $packages | Select-Object -ExpandProperty Id -Unique
}

function GetPackageSources() {
    $allSources = [NuGet.VisualStudio.AggregatePackageSource]::GetEnabledPackageSourcesWithAggregate()
    $allSources | Select-Object -ExpandProperty Name
}

function GetPackageVersions($parameters, $context) {
    Find-Package @parameters -ExactMatch -ErrorAction SilentlyContinue | Select -ExpandProperty Version | %{
        # Convert to version if the we're looking at the version as a string
        if($_ -is [string]) { 
            [NuGet.SemanticVersion]::Parse($_) 
        } else { 
            $_ 
        }  
    } | Sort-Object -Descending
}

function NugetTabExpansion($line, $lastWord) {
    # Parse the command
    $parsedCommand = [NuGetConsole.Host.PowerShell.CommandParser]::Parse($line)

    # Get the command definition
    $definition = $TabExpansionCommands[$parsedCommand.CommandName]

    # See if we've registered a command for intellisense
    if($definition) {
        # Get the command that we're trying to show intellisense for
        $command = Get-Command $parsedCommand.CommandName -ErrorAction SilentlyContinue

        if($command) {            
            # We're trying to find out what parameter we're trying to show intellisense for based on 
            # either the name of the an argument or index e.g. "Install-Package -Id " "Install-Package "
            
            $argument = $parsedCommand.CompletionArgument
            $index = $parsedCommand.CompletionIndex

            if(!$argument -and $index -ne $null) {                
                do {
                    # Get the argument name for this index
                    $argument = GetArgumentName $command $index

                    if(!$argument) {
                        break
                    }
                    
                    # If there is already a value for this argument, then check the next one index.
                    # This is so we don't show duplicate intellisense e.g. "Install-Package -Id elmah {tab}".
                    # The above statement shouldn't show intellisense for id since it already has a value
                    if($parsedCommand.Arguments[$argument] -eq $null) {
                        $value = $parsedCommand.Arguments[$index]

                        if(!$value) {
                            $value = ''
                        }
                        $parsedCommand.Arguments[$argument] = $value
                        break
                    }
                    else {
                        $index++
                    }

                } while($true);    
            }

            if($argument) {
                # Populate the arguments dictionary with the name and value of the 
                # associated index. i.e. for the command "Install-Package elmah" arguments should have
                # an entries with { 0, "elmah" } and { "Id", "elmah" }
                $arguments = New-Object 'System.Collections.Hashtable' -ArgumentList @([System.StringComparer]::InvariantCultureIgnoreCase)

                $parsedCommand.Arguments.Keys | Where-Object { $_ -is [int] } | %{
                    $argName = GetArgumentName $command $_
                    $arguments[$argName] = $parsedCommand.Arguments[$_]
                }

                # Copy the arguments over to the parsed command arguments
                $arguments.Keys | %{ 
                    $parsedCommand.Arguments[$_] = $arguments[$_]
                }

                # If the argument is a true argument of this command and not a partial argument
                # and there is a non null value (empty is valid), then we execute the script block
                # for this parameter (if specified)
                $action = $definition[$argument]
                $argumentValue = $parsedCommand.Arguments[$argument]

                if($command.Parameters[$argument] -and 
                   $argumentValue -ne $null -and
                   $action) {
                    $context = New-Object PSObject -Property $parsedCommand.Arguments
                    
                    $results = @(& $action $context)

                    if($results.Count -eq 0) {
                        return $null
                    }

                    # Use the argument value to filter results
                    $results = $results | %{ $_.ToString() } | Where-Object { $_.StartsWith($argumentValue, "OrdinalIgnoreCase") }

                    return NormalizeResults $results
                }
            }
        }
    } 

    return $NoResultValue
}

function NormalizeResults($results) {
    $results | %{
        $result = $_

        # Add quotes to a result if it contains whitespace or a quote
        $addQuotes = $result.Contains(" ") -or $result.Contains("'") -or $result.Contains("`t")
        
        if($addQuotes) {
            $result = "'" + $result.Replace("'", "''") + "'"
        }

        return $result
    }
}

function GetArgumentName($command, $index) {    
    # Next we try to find the parameter name for the parameter index (in the default parameter set)
    $parameterSet = $Command.DefaultParameterSet

    if(!$parameterSet) {
        $parameterSet = '__AllParameterSets'
    }

    return $command.Parameters.Values | ?{ $_.ParameterSets[$parameterSet].Position -eq $index } | Select -ExpandProperty Name
}

function Format-ProjectName {
    param(
        [parameter(position=0, mandatory=$true)]
        [validatenotnull()]
        $Project,
        [parameter(position=1, mandatory=$true)]
        [validaterange(6, 1000)]
        [int]$ColWidth
    )

    # only perform special formatting for web site projects
    if ($project.kind -ne "{E24C65DC-7377-472B-9ABA-BC803B73C61A}") {
        return $project.name
    }

    [NuGet.VisualStudio.PathHelper]::SmartTruncate($project.name, $ColWidth)
}