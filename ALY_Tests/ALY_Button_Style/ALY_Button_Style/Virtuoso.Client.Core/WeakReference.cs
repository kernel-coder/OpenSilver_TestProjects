namespace Virtuoso.Client.Core
{
    //http://connect.microsoft.com/VisualStudio/feedback/details/98270/make-a-generic-form-of-weakreference-weakreference-t-where-t-class
#if SILVERLIGHT
    [System.Runtime.Serialization.DataContract]
#else
    [Serializable]
#endif
    public struct WeakReference<T> where T : class
    {
        private readonly System.WeakReference wrapped;

        public WeakReference(T target)
        {
            wrapped = new System.WeakReference(target);
        }

        public WeakReference(T target, bool trackResurrection)
        {
            wrapped = new System.WeakReference(target, trackResurrection);
        }

        public bool IsAlive => wrapped.IsAlive;

        public T Target
        {
            get
            {
                //Will throw ClassCastException if class is not expected object
                //i.e. in case if wrapped WeakReference was modified in some way outside of wrapper
                // if you wish to prevent this - use "wrapped.Target as T" construct thich will return "null" - valid return value in this rare situation
                return (T)wrapped.Target;
            }
            set { wrapped.Target = value; }
        }

        public bool TrackResurrection => wrapped.TrackResurrection;
    }
}