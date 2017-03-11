Describe "New-EnvironmentVariable" -Tags 'CI' {
    BeforeAll {
        $TestVariable1 = New-Object psobject -Property @{Name="TestVariable1";Value="TestValue1";Scope=$null}
        $TestVariable2 = New-Object psobject -Property @{Name="TestVariable2";Value="TestValue2";Scope="Machine"}
        Remove-EnvironmentVariable $TestVariable1.Name -EA SilentlyContinue
    }
    AfterEach {
        Remove-EnvironmentVariable $TestVariable1.Name -EA SilentlyContinue
    }
    
    It "New-EnvironmentVariable should create a new variable" {
        New-EnvironmentVariable -Name $TestVariable1.Name -Value $TestVariable1.Value
        $Var = Get-EnvironmentVariable -Name $TestVariable1.Name
        $Var.Name | Should be $TestVariable1.Name
        $Var.Value | Should be $TestVariable1.Value
    }
    It "New-EnvironmentVariable should create the variable in correct scope" -Skip:($IsLinux -Or $IsOSX){
        New-EnvironmentVariable -Name $TestVariable2.Name -Value $TestVariable2.Value -Scope $TestVariable2.Scope
        $var1 = Get-EnvironmentVariable -Name $TestVariable2.Name -Scope $TestVariable2.Scope
        $var2 = Get-EnvironmentVariable -Name $TestVariable2.Name -EA SilentlyContinue
        $var1.Name | Should be $TestVariable2.Name
        $var1.Value | Should be $TestVariable2.Value
        $var2 | Should be $null
        Remove-EnvironmentVariable -Name $TestVariable2.Name -Scope $TestVariable2.Scope -EA SilentlyContinue
    }
    It "New-EnvironmentVariable should throw exception if it already exists" {
        New-EnvironmentVariable -name $TestVariable1.Name -value $TestVariable1.Value
        try
        {
            New-EnvironmentVariable -name $TestVariable1.Name -value $TestVariable1.Value -EA Stop
            throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId  | Should be "VariableAlreadyExists,Microsoft.PowerShell.Commands.NewEnvironmentVariableCommand"
        }

        $Var = Get-EnvironmentVariable -Name $TestVariable1.Name
        $Var.Name | Should be $TestVariable1.Name
        $Var.Value | Should be $TestVariable1.Value
    }
    It "New-EnvironmentVariable should override the existing variable if force is used" {
        New-EnvironmentVariable -name $TestVariable1.Name -value $TestVariable1.Value
        New-EnvironmentVariable -name $TestVariable1.Name -value $TestVariable2.Value -Force
        $Var = Get-EnvironmentVariable -Name $TestVariable1.Name
        $Var.Name | Should be $TestVariable1.Name
        $Var.Value | Should be $TestVariable2.Value
    }
    It "New-EnvironmentVariable should throw error if variable name contains wildcard character '*'" {
        try
        {
            New-EnvironmentVariable -name "Test*" -value $TestVariable1.Value -EA Stop
            throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should be "InvalidArgument,Microsoft.PowerShell.Commands.NewEnvironmentVariableCommand"
        }
    }
    It "New-EnvironmentVariable should return the variable with PassThru" {
        $var = New-EnvironmentVariable -name $TestVariable1.Name -value $TestVariable1.Value -PassThru
        $var.Name | Should be $TestVariable1.Name
        $var.Value | Should be $TestVariable1.Value
    }
    It "New-EnvironmentVariable should not accept an empty name" {
        try
        {
            New-EnvironmentVariable -name "   " -value $TestVariable1.Value -EA Stop
            throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should be "InvalidArgument,Microsoft.PowerShell.Commands.NewEnvironmentVariableCommand"
        }
    }
    It "New-EnvironmentVariable should not accept an empty value" {
        try
        {
            New-EnvironmentVariable -name $TestVariable1.Name -value "   " -EA Stop
            throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should be "InvalidArgument,Microsoft.PowerShell.Commands.NewEnvironmentVariableCommand"
        }
    }
}
