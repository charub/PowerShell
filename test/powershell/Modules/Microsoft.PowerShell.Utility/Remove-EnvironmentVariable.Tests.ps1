Describe "Remove-EnvironmentVariable" -Tags 'CI' {
    BeforeAll {
        $TestVariable1 = New-Object psobject -Property @{Name="TestVariable1";Value="TestValue1";Scope=$null}
        $TestVariable2 = New-Object psobject -Property @{Name="TestVariable2";Value="TestValue2";Scope="Machine"}
        $NewValue = "RemoveEnvVariableTest"
    }
    BeforeEach {
        New-EnvironmentVariable -Name $TestVariable1.Name -Value $TestVariable1.Value
    }
    AfterEach {
        Remove-EnvironmentVariable $TestVariable1.Name -EA SilentlyContinue
    }
    It "Remove-EnvironmentVariable should remove a given environment variable" {
        $VariableBeforeRemove = Get-EnvironmentVariable -Name $TestVariable1.Name
        Remove-EnvironmentVariable -Name $TestVariable1.Name
        $VariableAfterRemove = Get-EnvironmentVariable -Name $TestVariable1.Name -EA SilentlyContinue
        $VariableBeforeRemove |  Should not be $null
        $VariableAfterRemove | Should be $null
    }
    It "Remove-EnvironmentVariable should remove a variable from a given scope" -Skip:($IsLinux -Or $IsOSX){
        New-EnvironmentVariable -Name $TestVariable2.Name -Value $TestVariable2.Value -Scope $TestVariable2.Scope
        $VariableBeforeRemove = Get-EnvironmentVariable -Name $TestVariable2.Name -Scope $TestVariable2.Scope
        Remove-EnvironmentVariable -Name $TestVariable2.Name -Scope $TestVariable2.Scope
        $VariableAfterRemove = Get-EnvironmentVariable -Name $TestVariable2.Name -Scope $TestVariable2.Scope -EA SilentlyContinue
        $VariableBeforeRemove | Should not be $null
        $VariableAfterRemove | Should be $null
    }
    It "Remove-EnvironmentVariable should remove all variables matching a given pattern" {
        $VariableBeforeRemove = Get-EnvironmentVariable -Name "TestVar*" 
        Remove-EnvironmentVariable -Name "TestVar*"
        $VariableAfterRemove = Get-EnvironmentVariable -Name "TestVar*" -EA SilentlyContinue
        $VariableBeforeRemove | Should not be $null
        $VariableAfterRemove | Should be $null
    }
    It "Remove-EnvironmentVariable should throw exception if variable does not exist" {
        try
        {
            $NonExistingVariable = "NonExistingVariable"
            Remove-EnvironmentVariable -Name $NonExistingVariable -EA Stop
            throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId  | Should be "VariableNotFound,Microsoft.PowerShell.Commands.RemoveEnvironmentVariableCommand"
        }
    }
    It "Remove-EnvironmentVariable should not accept an empty name" {
        try
        {
            Remove-EnvironmentVariable -name "   " -EA Stop
            throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should be "InvalidArgument,Microsoft.PowerShell.Commands.RemoveEnvironmentVariableCommand"
        }
    }
}
