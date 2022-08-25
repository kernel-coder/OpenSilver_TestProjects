#region Usings

using System;
using System.Collections.Generic;
using OpenRiaServices.DomainServices.Client;

#endregion

namespace Virtuoso.Core.Services
{
    public class DomainContextSubmitBatch
    {
        private List<SubmitOperation> _pendingOperations = new List<SubmitOperation>();
        private List<SubmitOperation> _failedOperations = new List<SubmitOperation>();
        private Action<DomainContextSubmitBatch> _completeAction;

        /// <summary> 
        /// Expose the count of pending operations 
        /// </summary> 
        public int PendingOperationCount => _pendingOperations.Count;

        /// <summary> 
        /// Expose the count of failed operations 
        /// </summary> 
        public int FailedOperationCount => _failedOperations.Count;

        /// <summary> 
        /// Expose an enumerator for all of the failed operations 
        /// </summary> 
        public IEnumerable<SubmitOperation> FailedOperations => _failedOperations;

        /// <summary> 
        /// Public constructor 
        /// </summary> 
        /// <param name="completeAction"></param> 
        public DomainContextSubmitBatch(Action<DomainContextSubmitBatch> completeAction)
        {
            _completeAction = completeAction;
        }

        /// <summary> 
        /// Used to add an operation to the batch 
        /// </summary> 
        /// <param name="loadOperation"></param> 
        public void Add(SubmitOperation submitOperation)
        {
            submitOperation.Completed += submitOperation_Completed;
            _pendingOperations.Add(submitOperation);
        }

        /// <summary> 
        /// Processes each operation as it completes and checks for errors 
        /// </summary> 
        /// <param name="sender"></param> 
        /// <param name="e"></param> 
        private void submitOperation_Completed(object sender, EventArgs e)
        {
            SubmitOperation submitOperation = sender as SubmitOperation;

            _pendingOperations.Remove(submitOperation);

            submitOperation.Completed -= submitOperation_Completed;

            if (submitOperation.Error != null)
            {
                _failedOperations.Add(submitOperation);
            }

            CheckForComplete();
        }

        /// <summary> 
        /// Called to check for all operations being complete and to fire our completed action 
        /// </summary> 
        private void CheckForComplete()
        {
            if (_pendingOperations.Count > 0)
            {
                return;
            }

            if (_completeAction != null)
            {
                _completeAction(this);
                _completeAction = null;
            }
        }
    }
}