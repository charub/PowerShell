Describe "Get-EnvironmentVariable" -Tags 'CI' {
    BeforeAll {
        $TestVariable1 = New-Object psobject -Property @{Name="TestVariable1";Value="TestValue1";Scope=$null}     
        $TestVariable2 = New-Object psobject -Property @{Name="TestVariable2";Value="TestValue2";Scope="Machine"}
    }
    BeforeEach {
        New-EnvironmentVariable -Name $TestVariable1.Name -Value $TestVariable1.Value
    }
    AfterEach {
        Remove-EnvironmentVariable -Name $TestVariable1.Name
    }
    
    It "Get-EnvironmentVariable should throw ItemNotFoundException for non-existing variable" {
        $NonExistingVariableName = "NonExistingVariableName"
        try
        {
            Get-EnvironmentVariable -Name $NonExistingVariableName -EA Stop
            Throw "Execution OK"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should be "VariableNotFound,Microsoft.PowerShell.Commands.GetEnvironmentVariableCommand"
        }
    }
    It "Get-EnvironmentVariable should return list of environment variables in default scope" {
        $result = Get-EnvironmentVariable        
        $result.Name -icontains "path" | Should be $True
        $result.Name -icontains "psmodulepath" | Should be $True
    }
    It "Get-EnvironmentVariable should work for existing variable name" {
        $var = Get-EnvironmentVariable -name $TestVariable1.Name
        $var.Name | Should be $TestVariable1.Name
        $var.Value | Should be $TestVariable1.Value
    }
    It "Get-EnvironmentVariable should return the variable from the correct scope" -Skip:($IsLinux -Or $IsOSX) {
        New-EnvironmentVariable -Name $TestVariable2.Name -Value $TestVariable2.Value -Scope $TestVariable2.Scope
        $var1 = Get-EnvironmentVariable -name $TestVariable2.Name -Scope $TestVariable2.Scope
        $var2 = Get-EnvironmentVariable -name $TestVariable2.Name -EA SilentlyContinue
        $var1.Name | Should be $TestVariable2.Name
        $var1.Value | Should be $TestVariable2.Value
        $var2 | Should be $null
        Remove-EnvironmentVariable -Name $TestVariable2.Name -Scope $TestVariable2.Scope
    }
    It "Get-EnvironmentVariable should return the variables matching a pattern" {
        $TestPattern = "TestVar*"
        $var = Get-EnvironmentVariable -name $TestPattern
        $var.Name | Should be $TestVariable1.Name
        $var.Value | Should be $TestVariable1.Value
    }
    It "Get-EnvironmentVariable should only return value of the variable when 'valueonly' is used" {
        $var = Get-EnvironmentVariable -name $TestVariable1.Name -ValueOnly
        $var | Should be $TestVariable1.Value
    }
}
