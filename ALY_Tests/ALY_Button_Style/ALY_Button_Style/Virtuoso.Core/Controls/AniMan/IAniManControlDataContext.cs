using GalaSoft.MvvmLight.Command;
using System;
using System.Windows.Controls;
using Virtuoso.Core.Controls.AniMan;

namespace Virtuoso.Core.Controls
{
    public class AniManUIStruct :  IDisposable
    {
        public AniManForWoundLocation AniMan { get; private set; }
        public Canvas Canvas1 { get; private set; }
        public Canvas Canvas2 { get; private set; }
        public Canvas Canvas3 { get; private set; }
        public Canvas Canvas4 { get; private set; }
        public Canvas Canvas5 { get; private set; }
        public Canvas Canvas6 { get; private set; }
        public Canvas Canvas7 { get; private set; }

        public bool WasSuccess { get; set; }

        // Flag: Has Dispose already been called?
        bool disposed = false;

        public AniManUIStruct(AniManForWoundLocation animan, Canvas canvas1, Canvas canvas2, Canvas canvas3, Canvas canvas4, Canvas canvas5, Canvas canvas6, Canvas canvas7)
        {
            this.AniMan = animan;
            this.Canvas1 = canvas1;
            this.Canvas2 = canvas2;
            this.Canvas3 = canvas3;
            this.Canvas4 = canvas4;
            this.Canvas5 = canvas5;
            this.Canvas6 = canvas6;
            this.Canvas7 = canvas7;
            this.WasSuccess = false;
        }   

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
              
            }

            this.AniMan = null;
            this.Canvas1 = null;
            this.Canvas2 = null;
            this.Canvas3 = null;
            this.Canvas4 = null;
            this.Canvas5 = null;
            this.Canvas6 = null;
            this.Canvas7 = null;

            disposed = true;
        }
    }

    public interface IAniManControlDataContext
    {
        RelayCommand<AniManUIStruct> AniManToggleDisplay { get; set; }

        RelayCommand<AniManUIStruct> AniManSelectPartOnSilhouette { get; set; }

        RelayCommand<AniManUIStruct> AniManSelectPartOnOptionsMenu { get; set; }

        RelayCommand<AniManUIStruct> AniManReset { get; set; }
    }
}
