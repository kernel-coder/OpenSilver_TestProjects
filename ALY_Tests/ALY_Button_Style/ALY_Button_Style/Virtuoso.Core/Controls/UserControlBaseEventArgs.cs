using System;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.Controls
{
    public class UserControlBaseEventArgs<T> : EventArgs where T : VirtuosoEntity
    {
        private T entity;
        private Type entityType;

        public UserControlBaseEventArgs(T entity)
        {
            this.entity = entity;
            // not shown - validation for input

            entityType = typeof(T);
        }
        private bool _Cancel = false;
        public bool Cancel
        {
            get { return this._Cancel; }
            set { _Cancel = value; } 
        }
        public Type EntityType
        {
            get { return this.entityType; }
        }

        public T Entity
        {
            get { return this.entity; }
        }
    }
}
