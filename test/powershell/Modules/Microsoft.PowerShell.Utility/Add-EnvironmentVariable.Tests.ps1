Describe "Add-EnvironmentVariable" -Tags 'CI' {
    BeforeAll {
        $TestVariable1 = New-Object psobject -Property @{Name="TestVariable1";Value="TestValue1";Scope=$null}
        $TestVariable2 = New-Object psobject -Property @{Name="TestVariable2";Value="TestValue2";Scope="Machine"}
        $NewValue = "AddEnvVariableTest"
        Remove-EnvironmentVariable $TestVariable1.Name -EA SilentlyContinue
    }
    AfterEach {
        Remove-EnvironmentVariable $TestVariable1.Name -EA SilentlyContinue
    }
    BeforeEach {
        New-EnvironmentVariable -Name $TestVariable1.Name -Value $TestVariable1.Value
    }
    It "Add-EnvironmentVariable should append new value to existing." {
        Add-EnvironmentVariable -Name $TestVariable1.Name -Value $NewValue
        $Var = Get-EnvironmentVariable -Name $TestVariable1.Name
        $Var.Name | Should be $TestVariable1.Name
        $Var.Value | Should be ($TestVariable1.Value + ";" + $NewValue)
    }
    It "Add-EnvironmentVariable should prepend new value to existing." {
        Add-EnvironmentVariable -Name $TestVariable1.Name -Value $NewValue -Prepend
        $Var = Get-EnvironmentVariable -Name $TestVariable1.Name
        $Var.Name | Should be $TestVariable1.Name
        $Var.Value | Should be ($NewValue + ";" +$TestVariable1.Value)
    }
    It "Add-EnvironmentVariable should set the variable in correct scope" -Skip: ($IsLinux -Or $IsOSX) {
        New-EnvironmentVariable -Name $TestVariable2.Name -Value $TestVariable2.Value -Scope $TestVariable2.Scope
        Add-EnvironmentVariable -Name $TestVariable2.Name -Value $NewValue -Scope $TestVariable2.Scope
        $Var = Get-EnvironmentVariable -Name $TestVariable2.Name -Scope $TestVariable2.Scope
        $Var.Name | Should be $TestVariable2.Name
        $Var.Value | Should be ($TestVariable2.Value + ";" + $NewValue)
        Remove-EnvironmentVariable -Name $TestVariable2.Name -Scope $TestVariable2.Scope -EA SilentlyContinue
    }
    It "Add-EnvironmentVariable should throw exception if variable does not exist" {
        try
        {
            $NonExistingVariable = "NonExistingVariable"
            Add-EnvironmentVariable -Name $NonExistingVariable -Value $NewValue -EA Stop
            throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId  | Should be "VariableNotFound,Microsoft.PowerShell.Commands.AddEnvironmentVariableCommand"
        }
    }
    It "Add-EnvironmentVariable should create a new variable if it does not already exist and 'force' is used" {
        $NonExistingVariable = "NonExistingVariable"
        Add-EnvironmentVariable -Name $NonExistingVariable -Value $NewValue -Force
        $Var = Get-EnvironmentVariable -Name $NonExistingVariable
        $Var.Name | Should be $NonExistingVariable
        $Var.Value | Should be $NewValue
        # Remove the variable 
        Remove-EnvironmentVariable -Name $NonExistingVariable -EA SilentlyContinue
    }
    It "Add-EnvironmentVariable should throw error if variable name contains wildcard character '*'" {
        try
        {
            Add-EnvironmentVariable -name "Test*" -value $TestVariable1.Value -EA Stop
            throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should be "InvalidArgument,Microsoft.PowerShell.Commands.AddEnvironmentVariableCommand"
        }
    }
    It "Add-EnvironmentVariable should return the variable with PassThru" {
        $var = Add-EnvironmentVariable -name $TestVariable1.Name -value $NewValue -PassThru
        $var.Name | Should be $TestVariable1.Name
        $var.Value | Should be ($TestVariable1.Value + ";" + $NewValue)
    }
    It "Add-EnvironmentVariable should not accept an empty name" {
        try
        {
            Add-EnvironmentVariable -name "   " -value $TestVariable1.Value -EA Stop
            throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should be "InvalidArgument,Microsoft.PowerShell.Commands.AddEnvironmentVariableCommand"
        }
    }
    It "Add-EnvironmentVariable should not accept an empty value" {
        try
        {
            Add-EnvironmentVariable -name $TestVariable1.Name -value "   " -EA Stop
            throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should be "InvalidArgument,Microsoft.PowerShell.Commands.AddEnvironmentVariableCommand"
        }
    }
}
