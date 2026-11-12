using System;

namespace Common
{
    /// <summary>
    /// Base class for disposable objects.
    /// </summary>
    public abstract class BaseDisposable : IDisposable
    {

        #region Fields

        private bool FDisposed = false;

        #endregion

        #region Methods

        #region Free

        /// <summary>
        /// This method is called when object is being disposed. Override this method to free resources.
        /// </summary>
        /// <param name="canAccessFinalizable">
        /// Indicates if the method can access Finalizable member objects.
        /// If canAccessFinalizable = false the method was called by GC and you can´t access finalizable member objects.
        /// If canAccessFinalizable = true the method was called by user and you can access all member objects.
        /// </param>
        protected virtual void Free(bool canAccessFinalizable)
        {
            FDisposed = true;
        }

        #endregion

        #region CheckDisposedWithException

        /// <summary>
        /// Checks if object is already disposed.
        /// </summary>
        protected void CheckDisposedWithException()
        {

            if (Disposed)
            {
                throw new ObjectDisposedException(this.ToString());
            }

        }

        #endregion

        #region Dispose

        /// <summary>
        /// Dispose object resources.
        /// </summary>
        public void Dispose()
        {

            lock (this)
            {

                if (!FDisposed)
                {
                    try
                    {
                        Free(true);
                    }
                    finally
                    {
                        FDisposed = true;
                    }
                }

            }

        }

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// Indicates is object is already disposed.
        /// </summary>
        protected bool Disposed
        {

            get
            {
                lock (this)
                {
                    return FDisposed;
                }
            }

            set
            {
                lock (this)
                {
                    FDisposed = value;
                }
            }

        }

        #endregion

    }
}
