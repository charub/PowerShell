/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Internal;
#if CORECLR

// Some APIs are missing from System.Environment. We use System.Management.Automation.Environment as a proxy type:

//  - for missing APIs, System.Management.Automation.Environment has extension implementation.

//  - for existing APIs, System.Management.Automation.Environment redirect the call to System.Environment.

using Environment = System.Management.Automation.Environment;
using EnvironmentVariableTarget = System.Management.Automation.EnvironmentVariableTarget;
#endif
namespace Microsoft.PowerShell.Commands
{

    internal sealed class EnvironmentVariable
    {
        public string Name { get; } = String.Empty;
        public string Value { get; }
        public EnvironmentVariableTarget Scope;
        public EnvironmentVariable() { }
        public EnvironmentVariable(string name, string value) {
            this.Name = name;
            this.Value = value;
            this.Scope = EnvironmentVariableTarget.Process;
        }
        public EnvironmentVariable(string name, string value, EnvironmentVariableTarget scope)
        {
            this.Name = name;
            this.Value = value;
            this.Scope = scope;
        }
    }
    /// <summary>
    /// Base class for all environment variable commands.
    /// 
    /// Because -Scope is defined in EnvironmentVariableCommandBase, all derived commands
    /// must implement -Scope.
    /// </summary>

    public abstract class EnvironmentVariableCommandBase : PSCmdlet
    {
        #region Parameters

        /// <summary>
        /// Selects active scope to work with; used for all environment variable commands.
        /// </summary>
        [Parameter]
        [ValidateSet("Process", "Machine", "User")]
        public string Scope
        {
            get
            {
                return scope;
            }

            set
            {
                scope = value;
            }
        }
        private string scope = "Process";
        #endregion parameters


        #region helpers

        /// <summary>
        /// Gets the matching variable for the specified name or pattern,
        /// using the Scope parameters defined in the base class.
        /// </summary>
        /// 
        /// <param name="name">
        /// The name or pattern of the variables to retrieve.
        /// </param>
        /// 
        /// <param name="lookupScope">
        /// The scope to do the lookup in. If null or empty, the
        /// lookupScope defaults to 'Process'
        /// </param>
        /// 
        /// <param name="wildCardAllowed">
        /// Tells if wilcard is allowed for search or not.
        /// </param>
        ///
        /// <returns>
        /// A dictionary contatining name and value of the variables
        /// matching the name, pattern or Scope.
        /// </returns>
        /// 
        internal List<EnvironmentVariable> GetMatchingVariables(string name, string lookupScope, bool wildCardAllowed)
        {
            List<EnvironmentVariable> result = new List<EnvironmentVariable>();
            if (String.IsNullOrEmpty(name) && wildCardAllowed)
            {
                name = "*";
            }
            WildcardPattern nameFilter =
                WildcardPattern.Get(
                name,
                WildcardOptions.IgnoreCase);
            EnvironmentVariableTarget target = GetEnvironmentVariableTarget(lookupScope);
            IDictionary variableTable = Environment.GetEnvironmentVariables(target);
            if(null != variableTable)
            {
                foreach (DictionaryEntry entry in variableTable)
                {
                    string key = (string)entry.Key;
                    string value = (string)entry.Value;
                    if(nameFilter.IsMatch(key))
                    {
                        result.Add(new EnvironmentVariable(key, value, target));
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// Return the EnvironmentVariableTarget corresponding to the scope
        /// </summary>
        /// <param name="scope">
        ///  Lookup scope for retreiving EnvironmentVariableTarget
        /// </param>
        ///
        private EnvironmentVariableTarget GetEnvironmentVariableTarget(string scope)
        {
            EnvironmentVariableTarget target;
            if (String.Equals(Scope, "machine", StringComparison.OrdinalIgnoreCase))
            {
                target = EnvironmentVariableTarget.Machine;
            }
            else if (String.Equals(Scope, "user", StringComparison.OrdinalIgnoreCase))
            {
                target = EnvironmentVariableTarget.User;
            }
            else
            {
                target = EnvironmentVariableTarget.Process;
            }
            return target;
        }

        /// <summary>
        /// Sets the value of an existing variable or
        /// create a new variable if it does not exist.
        /// </summary>
        /// 
        /// <param name="variable">
        /// The name of the variables to retrieve.
        /// </param>
        /// 
        /// <param name="value">
        /// The new value of the variable
        /// </param>
        /// 
        /// <param name="scope">
        ///  The scope tp create the variable.
        /// </param>
        ///
        protected void SetVariable(string variable, string value, string scope)
        {
            Environment.SetEnvironmentVariable(variable, value, GetEnvironmentVariableTarget(scope));
        }

        /// <summary>
        /// Set the value of an existing variable
        /// or create new variable if it does not exist already.
        /// </summary>
        /// 
        /// <param name="name">
        /// The name of the variable.
        /// </param>
        /// 
        /// <param name="value">
        /// new value of the variable.
        /// rules apply.
        /// </param>
        /// 
        /// <param name="scope">
        /// The scope of the variable. If scope is null or empty,
        /// default scope 'Process' is used.
        /// </param>
        /// 
        /// <param name="force">
        /// use force operation
        /// rules apply.
        /// </param>
        ///  
        /// <param name="passthru">
        /// return the variable if passThru is set to True.
        /// rules apply.
        /// </param>
        ///  
        /// <param name="action">
        /// Name of the action which is being performed. This will
        /// be displayed to the user when whatif parameter is specified. 
        /// (New Environment Variable\Set Environment Variable).
        /// </param>
        /// 
        /// <param name="target">
        /// Name of the target resource being acted upon. This will
        /// be displayed to the user when whatif parameter is specified.
        /// </param>
        ///
        protected void SetVariable(string name, string value, string scope, bool force, bool passthru, string action, string target)
        {
            // If Force is not specified, see if the variable already exists
            // in the specified scope. If the scope isn't specified, then
            // check to see if it exists in the current scope.
            if (!force)
            {
                List<EnvironmentVariable> varFound = GetMatchingVariables(name, Scope, false);
                SessionStateException exception = null;
                if (String.Equals(action, VariableCommandStrings.NewEnvironmentVariableAction) && varFound.Count > 0)
                {
                    exception = new SessionStateException(
                            name,
                            SessionStateCategory.Variable,
                            "VariableAlreadyExists",
                            SessionStateStrings.VariableAlreadyExists,
                            ErrorCategory.ResourceExists);
                    ThrowTerminatingError(
                        new ErrorRecord(
                            exception.ErrorRecord,
                            exception));
                    return;
                }
                else if (String.Equals(action, VariableCommandStrings.SetEnvironmentVariableAction) && varFound.Count == 0)
                {
                    exception = new ItemNotFoundException(
                            name,
                            "VariableNotFound",
                            SessionStateStrings.VariableNotFound);
                }
                if (null != exception)
                {
                    ThrowTerminatingError(
                           new ErrorRecord(
                               exception.ErrorRecord,
                               exception));
                    return;
                }
            }

            // Since the variable doesn't exist or -Force was specified,
            // Call should process to validate the set with the user.

            if (ShouldProcess(target, action))
            {
                Environment.SetEnvironmentVariable(name, value, GetEnvironmentVariableTarget(scope));
                if (passthru)
                {
                    EnvironmentVariable newVariable = new EnvironmentVariable(name, value);
                    WriteObject(newVariable);
                }
            }
        }
        /// <summary>
        /// Set the value of an existing variable
        /// or create new variable if it does not exist already.
        /// </summary>
        /// 
        /// <param name="paramName">
        /// Parameter to validate.
        /// </param>
        /// <param name="paramValue">
        /// Parameter value to validate.
        /// </param>
        /// <param name="isWildCardAllowed">
        /// Tells if the wildcard value is allowed for the parameter
        /// </param>
        protected void ValidateParameter(string paramName, string paramValue, Boolean isWildCardAllowed)
        {
            Exception exception = null;
            string errorMessage = null;
            if (String.IsNullOrWhiteSpace(paramValue))
            {
                errorMessage = "Value cannot be null or whitespace";
            }
            else if (!isWildCardAllowed && WildcardPattern.ContainsWildcardCharacters(paramValue))
            {
                errorMessage = "Wildcard character '*' is not allowed";
            }
            if (null != errorMessage)
            {
                exception = new ArgumentException(errorMessage, paramName);
                ErrorRecord error = new ErrorRecord(exception, "InvalidArgument", ErrorCategory.InvalidArgument, paramValue);
                ThrowTerminatingError(error);
            }
        }
        /// <summary>
        /// Check if the Scope Paramater is 'Process' or other.
        /// Only 'Process' parameter value is supported on Unix.
        /// </summary>
        protected void ValidateScopeParameter()
        {
            if(!string.Equals(Scope, "Process", StringComparison.OrdinalIgnoreCase))
            {
                Exception exception = new ArgumentException("This scope value is not supported on Unix!", "Scope");
                ErrorRecord error = new ErrorRecord(exception, "InvalidArgument", ErrorCategory.InvalidArgument, Scope);
                ThrowTerminatingError(error);
            }
        }

        #endregion helpers
    }

    /// <summary>
    /// This class implements Get-EnvironmentVariable command
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "EnvironmentVariable")]
    [OutputType(typeof(EnvironmentVariable))]
    public sealed class GetEnvironmentVariableCommand : EnvironmentVariableCommandBase
    {
        #region parameters

        /// <summary>
        /// Name of the Environment Variable
        /// </summary>
        [Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNull]
        public string[] Name
        {
            get
            {
                return name;
            }

            set
            {
                if (value == null)
                {
                    value = new string[] { "*" };
                }
                name = value;
            }
        }
        private string[] name = new string[] { "*" };



        /// <summary>
        /// Output only the value(s) of the requested variable(s).
        /// </summary>
        [Parameter]
        public SwitchParameter ValueOnly
        {
            get
            {
                return valueOnly;
            }
            set
            {
                valueOnly = value;
            }
        }
        private bool valueOnly;

        #endregion parameters
        /// <summary>
        /// The implementation of the Get-EnvironmentVariable command
        /// </summary>
        /// 
        protected override void ProcessRecord()
        {
#if UNIX
            ValidateScopeParameter();
#endif
            foreach (string varName in name)
            {
                List<EnvironmentVariable> matchingVariables = GetMatchingVariables(varName, Scope,true);
                matchingVariables.Sort(
                    delegate (EnvironmentVariable left, EnvironmentVariable right)
                    {
                        return StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name);
                    });
                bool matchFound = false;
                foreach (EnvironmentVariable variable in matchingVariables)
                {
                    matchFound = true;
                    if (ValueOnly)
                    {
                        WriteObject(variable.Value);
                    }
                    else
                    {
                        WriteObject(variable);
                    }
                }

                if (!matchFound)
                {
                    ItemNotFoundException itemNotFound =
                        new ItemNotFoundException(
                            varName,
                            "VariableNotFound",
                            SessionStateStrings.VariableNotFound);

                    WriteError(
                        new ErrorRecord(
                            itemNotFound.ErrorRecord,
                            itemNotFound));
                }
            }
        }
    }

    /// <summary>
    /// This class implements New-EnvironmentVariable command
    /// </summary>
    [Cmdlet(VerbsCommon.New, "EnvironmentVariable", SupportsShouldProcess = true, HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113285")]
    [OutputType(typeof(EnvironmentVariable))]
    public sealed class NewEnvironmentVariableCommand : EnvironmentVariableCommandBase
    {
        #region parameters

        /// <summary>
        /// Name of the PSVariable
        /// </summary>
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true, Mandatory = true)]
        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }
        private string name;

        /// <summary>
        /// Value of the PSVariable
        /// </summary>
        [Parameter(Position = 1, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                this._value = value;
            }
        }
        private string _value;

        /// <summary>
        /// Force the operation to make the best attempt at setting the variable.
        /// </summary>
        [Parameter]
        public SwitchParameter Force
        {
            get
            {
                return force;
            }

            set
            {
                force = value;
            }
        }
        private bool force;

        /// <summary>
        /// The variable object should be passed down the pipeline.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru
        {
            get
            {
                return passThru;
            }
            set
            {
                passThru = value;
            }
        }
        private bool passThru;

        #endregion parameters

        /// <summary>
        /// Checks to see if Name and Value variables have valid values.
        /// </summary>
        protected override void BeginProcessing()
        {
            ValidateParameter("Name", Name, false);
            ValidateParameter("Value", Value, true);
#if UNIX
            ValidateScopeParameter();
#endif
        }

        /// <summary>
        /// Add objects received on the pipeline to an ArrayList of values, to
        /// take the place of the Value parameter if none was specified on the
        /// command line. 
        /// </summary>
        /// 
        protected override void ProcessRecord()
        {
            string action = VariableCommandStrings.NewEnvironmentVariableAction;
            string target = StringUtil.Format(VariableCommandStrings.NewEnvironmentVariableTarget, Name, Value);
            SetVariable(Name, Value, Scope, Force, passThru, action, target);
        }// ProcessRecord
    } // NewEnvironmentVariableCommand

    /// <summary>
    /// This class implements Set-EnvironmentVariable command
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "EnvironmentVariable", SupportsShouldProcess = true)]
    [OutputType(typeof(EnvironmentVariable))]
    public sealed class SetEnvironmentVariableCommand : EnvironmentVariableCommandBase
    {
        #region parameters

        /// <summary>
        /// Name of the environment Variable to set
        /// </summary>
        [Parameter(Position = 0, ValueFromPipelineByPropertyName = true, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }
        private string name;

        /// <summary>
        /// new value of the Environment Variable
        /// </summary>
        [Parameter(Position = 1, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }
        private string _value;

        /// <summary>
        /// Force the operation to make the best attempt at setting the variable.
        /// </summary>
        [Parameter]
        public SwitchParameter Force
        {
            get
            {
                return force;
            }

            set
            {
                force = value;
            }
        }
        private bool force;

        /// <summary>
        /// The variable object should be passed down the pipeline.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru
        {
            get
            {
                return passThru;
            }
            set
            {
                passThru = value;
            }
        }
        private bool passThru;

        #endregion parameters

        /// <summary>
        /// Checks to see if Name and Value variables have valid values.
        /// </summary>
        protected override void BeginProcessing()
        {
            ValidateParameter("Name", name, false);
            ValidateParameter("Value", Value, true);
#if UNIX
            ValidateScopeParameter();
#endif
        }

        /// <summary>
        /// Sets the variable if the variable exists.
        /// If variable does not exist and 'Force' is not used,
        /// throw exception. If 'Force' is used, create new variable.
        /// </summary>
        /// 
        protected override void ProcessRecord()
        {
            string action = VariableCommandStrings.SetEnvironmentVariableAction;
            string target = StringUtil.Format(VariableCommandStrings.SetEnvironmentVariableTarget, Name, Value);
            SetVariable(Name, Value, Scope, Force, passThru, action, target);
        }
    }// SetEnvironmentVariableCommand

    /// <summary>
    /// This class implements Remove-EnvironmentVariable command
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "EnvironmentVariable", SupportsShouldProcess = true)]
    [OutputType(typeof(EnvironmentVariable))]
    public sealed class RemoveEnvironmentVariableCommand : EnvironmentVariableCommandBase
    {
        #region parameters

        /// <summary>
        /// Name of the Environment Variable(s) to remove
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string[] Name
        {
            get
            {
                return names;
            }

            set
            {
                names = value;
            }
        }
        private string[] names;

        #endregion parameters

        /// <summary>
        /// Removes the matching variables from the specified scope
        /// </summary>
        ///
        protected override void ProcessRecord()
        {
#if UNIX
            ValidateScopeParameter();
#endif
            // Removal of variables happens in the Process scope if the
            // scope wasn't explicitly specified by the user.
            foreach (string varName in names)
            {
                ValidateParameter("Name", varName, true);
                List<EnvironmentVariable> matchingVariables =
                    GetMatchingVariables(varName, Scope, true);

                if (matchingVariables.Count == 0)
                {
                    // Since the variable wasn't found, write an error.
                    ItemNotFoundException itemNotFound =
                        new ItemNotFoundException(
                            varName,
                            "VariableNotFound",
                            SessionStateStrings.VariableNotFound);

                    WriteError(
                        new ErrorRecord(
                            itemNotFound.ErrorRecord,
                            itemNotFound));

                    continue;
                }
                string action = VariableCommandStrings.RemoveEnvironmentVariableAction;
                string target = StringUtil.Format(VariableCommandStrings.RemoveEnvironmentVariableTarget, Name);
                if (ShouldProcess(target, action))
                {
                    foreach (EnvironmentVariable matchingVariable in matchingVariables)
                    {
                        // Environment Variable can be removed by 
                        // setting empty value.
                        SetVariable(matchingVariable.Name, "", Scope);
                    }
                }
            }
        } // ProcessRecord
    } // RemoveEnvironmentVariableCommand

    /// <summary>
    /// This class implements Add-EnvironmentVariable command
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "EnvironmentVariable", SupportsShouldProcess = true)]
    [OutputType(typeof(EnvironmentVariable))]
    public sealed class AddEnvironmentVariableCommand : EnvironmentVariableCommandBase
    {
        #region parameters

        /// <summary>
        /// Name of the Environment Variable
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }
        private string name;

        /// <summary>
        /// Value of the Environment Variable
        /// </summary>
        [Parameter(Position = 1, Mandatory = true)]
        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                this._value = value;
            }
        }
        private string _value;

        /// <summary>
        /// Force the operation to make the best attempt at creating the variable.
        /// </summary>
        [Parameter]
        public SwitchParameter Force
        {
            get
            {
                return force;
            }

            set
            {
                force = value;
            }
        }
        private bool force;

        /// <summary>
        /// The new value should be prepended to the current value.
        /// By default, the value is appended
        /// </summary>
        [Parameter]
        public SwitchParameter Prepend
        {
            get
            {
                return prepend;
            }
            set
            {
                prepend = value;
            }
        }
        private bool prepend = false;
        /// <summary>
        /// The variable object should be passed down the pipeline.
        /// </summary>
        [Parameter]
        public SwitchParameter PassThru
        {
            get
            {
                return passThru;
            }
            set
            {
                passThru = value;
            }
        }
        private bool passThru;

        #endregion parameters
        
        /// <summary>
        /// Checks to see if the name and value parameters were
        /// valid
        /// </summary>
        protected override void BeginProcessing()
        {
         
            ValidateParameter("Name", name, false);
            ValidateParameter("Value", Value, true);
#if UNIX
            ValidateScopeParameter();
#endif
        }
        /// <summary>
        /// Append\Prepend the new value to variable's existing value.
        /// If Variable does not exist and 'Force' is used, create new variable
        /// </summary>
        /// 
        protected override void ProcessRecord()
        {
            // If Force is not specified, see if the variable already exists
            // in the specified scope.
            List<EnvironmentVariable> result = GetMatchingVariables(name, Scope, false);
            if (!Force)
            {
                if (result.Count == 0)
                {
                    ItemNotFoundException itemNotFound =
                         new ItemNotFoundException(
                             name,
                             "VariableNotFound",
                             SessionStateStrings.VariableNotFound);

                    ThrowTerminatingError(
                        new ErrorRecord(
                            itemNotFound.ErrorRecord,
                            itemNotFound));
                    return;
                }
            }

            // Since the variable doesn't exist or -Force was specified,
            // Call should process to validate the set with the user.
            string action = VariableCommandStrings.AddEnvironmentVariableAction;
            string target = StringUtil.Format(VariableCommandStrings.AddEnvironmentVariableTarget, Name, Value);
            if (ShouldProcess(target, action))
            {
                string newVarValue = null;
                if (result.Count == 0)
                {
                    newVarValue = Value;
                }
                else
                {
                    EnvironmentVariable variable = result[0];
                    string curValue = variable.Value;
                    if (prepend)
                    {
                        newVarValue = Value + ";" + curValue;
                    }
                    else
                    {
                        newVarValue = curValue + ";" + Value;
                    }
                }
                SetVariable(Name, newVarValue, Scope);
                if (passThru)
                {
                    EnvironmentVariable newVariable = new EnvironmentVariable(name, newVarValue);
                    WriteObject(newVariable);
                }
            }
        }// ProcessRecord
    } // AddEnvironmentVariableCommand

}

