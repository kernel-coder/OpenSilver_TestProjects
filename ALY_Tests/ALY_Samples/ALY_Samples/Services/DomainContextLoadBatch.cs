#region Usings

using System;
using System.Collections.Generic;
using OpenRiaServices.DomainServices.Client;

#endregion

namespace Virtuoso.Core.Services
{
    /// <summary> 
    /// Simple LoadOperation batch manager 
    /// </summary> 
    /// <remarks> 
    /// This class allows you to start multiple load operations in parallel and still received a single 
    /// 'complete' action callback once all work is done. Load operation failures are also aggregated 
    /// and will be made available as part of the completion action callback. 
    /// </remarks> 
    /// http://blogs.msdn.com/b/smccraw/archive/2009/07/15/a-net-ria-services-data-load-batch-manager.aspx
    /// 
    public class DomainContextLoadBatch
    {
        private List<LoadOperation> _pendingOperations = new List<LoadOperation>();
        private List<LoadOperation> _failedOperations = new List<LoadOperation>();
        private Action<DomainContextLoadBatch> _completeAction;

        /// <summary> 
        /// Expose the count of failed operations 
        /// </summary> 
        public int FailedOperationCount => _failedOperations.Count;

        /// <summary> 
        /// Expose an enumerator for all of the failed operations 
        /// </summary> 
        public IEnumerable<LoadOperation> FailedOperations => _failedOperations;

        /// <summary> 
        /// Public constructor 
        /// </summary> 
        /// <param name="completeAction"></param> 
        public DomainContextLoadBatch(Action<DomainContextLoadBatch> completeAction)
        {
            _completeAction = completeAction;
        }

        /// <summary> 
        /// Used to add an operation to the batch 
        /// </summary> 
        /// <param name="loadOperation"></param> 
        public void Add(LoadOperation loadOperation)
        {
            loadOperation.Completed += loadOperation_Completed;
            _pendingOperations.Add(loadOperation);
        }

        /// <summary> 
        /// Processes each operation as it completes and checks for errors 
        /// </summary> 
        /// <param name="sender"></param> 
        /// <param name="e"></param> 
        private void loadOperation_Completed(object sender, EventArgs e)
        {
            LoadOperation loadOperation = sender as LoadOperation;

            _pendingOperations.Remove(loadOperation);

            loadOperation.Completed -= loadOperation_Completed;

            if (loadOperation.Error != null)
            {
                _failedOperations.Add(loadOperation);
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