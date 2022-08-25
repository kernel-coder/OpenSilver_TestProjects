#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OpenRiaServices.DomainServices.Client;

#endregion

namespace Virtuoso.Core.Events
{
    public enum DataLoadType
    {
        SERVER = 0,
        LOCAL = 1 //offline - loaded from disk
    }

    public class MultiErrorEventArgs : EventArgs
    {
        readonly List<Exception> _errors = new List<Exception>();
        readonly List<string> _entity_errors = new List<string>();

        public MultiErrorEventArgs(List<Exception> errors, DataLoadType loadType = DataLoadType.SERVER)
        {
            _errors = errors;
            DataLoadType = loadType;
        }

        public MultiErrorEventArgs(List<Exception> errors, List<string> entity_errors,
            DataLoadType loadType = DataLoadType.SERVER)
        {
            _errors = errors;
            _entity_errors = entity_errors;
            DataLoadType = loadType;
        }

        public DataLoadType DataLoadType { get; set; }

        public List<Exception> Errors => _errors;

        public List<String> EntityErrors => _entity_errors;
    }

    public class ErrorEventArgs : EventArgs
    {
        readonly Exception _error;
        readonly ObservableCollection<String> _entity_errors = new ObservableCollection<String>();

        ObservableCollection<System.ComponentModel.DataAnnotations.ValidationResult> _entity_validation_errors =
            new ObservableCollection<System.ComponentModel.DataAnnotations.ValidationResult>();

        public ErrorEventArgs(Exception ex, DataLoadType loadType = DataLoadType.SERVER)
        {
            _error = ex;
            DataLoadType = loadType;
        }

        public ErrorEventArgs(Exception ex, ObservableCollection<String> entity_errors,
            DataLoadType loadType = DataLoadType.SERVER)
        {
            _error = ex;
            _entity_errors = entity_errors;
            DataLoadType = loadType;
        }

        public ErrorEventArgs(Exception ex, ObservableCollection<String> entity_errors,
            ObservableCollection<System.ComponentModel.DataAnnotations.ValidationResult> entity_validation_errors,
            DataLoadType loadType = DataLoadType.SERVER)
        {
            _error = ex;
            _entity_errors = entity_errors;
            _entity_validation_errors = entity_validation_errors;
            DataLoadType = loadType;
        }

        public DataLoadType DataLoadType { get; set; }

        public Exception Error => _error;

        public Object UserState { get; set; }

        public String Message => _error?.Message;

        public ObservableCollection<System.ComponentModel.DataAnnotations.ValidationResult> EntityValidationResults =>
            _entity_validation_errors;

        public ObservableCollection<String> EntityErrors => _entity_errors;
    }

    public class SubmitOperationEventArgs : ErrorEventArgs
    {
        public SubmitOperation SubmitOperation { get; private set; }
        Exception _error;
        ObservableCollection<String> _entity_errors = new ObservableCollection<String>();

        ObservableCollection<System.ComponentModel.DataAnnotations.ValidationResult> _entity_validation_errors =
            new ObservableCollection<System.ComponentModel.DataAnnotations.ValidationResult>();

        public SubmitOperationEventArgs(SubmitOperation sop, DataLoadType loadType = DataLoadType.SERVER) : base(
            sop.Error, loadType)
        {
            SubmitOperation = sop;
            _error = sop.Error;
            DataLoadType = loadType;
        }

        public SubmitOperationEventArgs(SubmitOperation sop, ObservableCollection<String> entity_errors,
            DataLoadType loadType = DataLoadType.SERVER) : base(sop.Error, entity_errors, loadType)
        {
            SubmitOperation = sop;
            _error = sop.Error;
            _entity_errors = entity_errors;
            DataLoadType = loadType;
        }

        public SubmitOperationEventArgs(SubmitOperation sop, ObservableCollection<String> entity_errors,
            ObservableCollection<System.ComponentModel.DataAnnotations.ValidationResult> entity_validation_errors,
            DataLoadType loadType = DataLoadType.SERVER)
            : base(sop.Error, entity_errors, entity_validation_errors, loadType)
        {
            SubmitOperation = sop;
            _error = sop.Error;
            _entity_errors = entity_errors;
            _entity_validation_errors = entity_validation_errors;
            DataLoadType = loadType;
        }
    }
}