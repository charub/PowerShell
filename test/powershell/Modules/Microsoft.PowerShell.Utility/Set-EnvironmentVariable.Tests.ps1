Describe "Set-EnvironmentVariable" -Tags 'CI' {
    BeforeAll {
        $TestVariable1 = New-Object psobject -Property @{Name="TestVariable1";Value="TestValue1";Scope=$null}
        $TestVariable2 = New-Object psobject -Property @{Name="TestVariable2";Value="TestValue2";Scope="Machine"}
        $NewValue = "SetEnvVariableTest"
        Remove-EnvironmentVariable $TestVariable1.Name -EA SilentlyContinue
    }
    AfterEach {
        Remove-EnvironmentVariable $TestVariable1.Name -EA SilentlyContinue
    }
    BeforeEach {
        New-EnvironmentVariable -Name $TestVariable1.Name -Value $TestVariable1.Value
    }
    It "Set-EnvironmentVariable should set value for an existing variable" {
        Set-EnvironmentVariable -Name $TestVariable1.Name -Value $NewValue
        $Var = Get-EnvironmentVariable -Name $TestVariable1.Name
        $Var.Name | Should be $TestVariable1.Name
        $Var.Value | Should be $NewValue
    }
    It "Set-EnvironmentVariable should set the variable in correct scope" -Skip:($IsLinux -Or $IsOSX) {
        New-EnvironmentVariable -Name $TestVariable2.Name -Value $TestVariable2.Value -Scope $TestVariable2.Scope
        Set-EnvironmentVariable -Name $TestVariable2.Name -Value $NewValue -Scope $TestVariable2.Scope
        $Var = Get-EnvironmentVariable -Name $TestVariable2.Name -Scope $TestVariable2.Scope
        $Var.Name | Should be $TestVariable2.Name
        $Var.Value | Should be $NewValue
        Remove-EnvironmentVariable -Name $TestVariable2.Name -Scope $TestVariable2.Scope -EA SilentlyContinue
    }
    It "Set-EnvironmentVariable should throw exception if variable does not exist" {
        try
        {
            $NonExistingVariable = "NonExistingVariable"
            Set-EnvironmentVariable -Name $NonExistingVariable -Value $NewValue -EA Stop
            throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId  | Should be "VariableNotFound,Microsoft.PowerShell.Commands.SetEnvironmentVariableCommand"
        }
    }
    It "Set-EnvironmentVariable should create a new variable if it does not already exist and 'force' is used" {
        $NonExistingVariable = "NonExistingVariable"
        Set-EnvironmentVariable -Name $NonExistingVariable -Value $NewValue -Force
        $Var = Get-EnvironmentVariable -Name $NonExistingVariable
        $Var.Name | Should be $NonExistingVariable
        $Var.Value | Should be $NewValue
        # Remove the variable 
        Remove-EnvironmentVariable -Name $NonExistingVariable -EA SilentlyContinue
    }
    It "Set-EnvironmentVariable should throw error if variable name contains wildcard character '*'" {
        try
        {
            Set-EnvironmentVariable -name "Test*" -value $TestVariable1.Value -EA Stop
            throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should be "InvalidArgument,Microsoft.PowerShell.Commands.SetEnvironmentVariableCommand"
        }
    }
    It "Set-EnvironmentVariable should return the variable with PassThru" {
        $var = Set-EnvironmentVariable -name $TestVariable1.Name -value $NewValue -PassThru
        $var.Name | Should be $TestVariable1.Name
        $var.Value | Should be $NewValue
    }
    It "Set-EnvironmentVariable should not accept an empty name" {
        try
        {
            Set-EnvironmentVariable -name "   " -value $TestVariable1.Value -EA Stop
            throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should be "InvalidArgument,Microsoft.PowerShell.Commands.SetEnvironmentVariableCommand"
        }
    }
    It "Set-EnvironmentVariable should not accept an empty value" {
        try
        {
            Set-EnvironmentVariable -name $TestVariable1.Name -value "   " -EA Stop
            throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should be "InvalidArgument,Microsoft.PowerShell.Commands.SetEnvironmentVariableCommand"
        }
    }
}
