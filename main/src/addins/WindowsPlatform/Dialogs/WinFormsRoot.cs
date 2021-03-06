using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using CustomControls.OS;

namespace MonoDevelop.Platform
{
    class WinFormsRoot : Form
    {
		// From OpenFileDialogEx.cs
		private SetWindowPosFlags UFLAGSHIDE =     
			SetWindowPosFlags.SWP_NOACTIVATE |
			SetWindowPosFlags.SWP_NOOWNERZORDER |
			SetWindowPosFlags.SWP_NOMOVE |
			SetWindowPosFlags.SWP_NOSIZE | 
			SetWindowPosFlags.SWP_HIDEWINDOW;
		
		IDisposable nativeDialogWindow;		
		bool watchForActivate;
		
        public WinFormsRoot()
        {
            this.Text = "";
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(-32000, -32000);
            this.ShowInTaskbar = false;
            Show();
			Win32.SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, UFLAGSHIDE);
			watchForActivate = true;
        }
		
		protected override void OnClosing (CancelEventArgs args)
		{
			base.OnClosing (args);
			if (nativeDialogWindow != null)
				nativeDialogWindow.Dispose ();
		}

        protected override void WndProc(ref Message m)
        {	
			if (m.Msg == (int)Msg.WM_ACTIVATE && watchForActivate) {
				watchForActivate = false;
				nativeDialogWindow = new NativeDialogWindow (m.LParam);
			}

            base.WndProc(ref m);
        }
		
		// The CommonDialog's Form Handle
		class NativeDialogWindow : NativeWindow, IDisposable
		{
			IntPtr handle;
			
			public NativeDialogWindow (IntPtr handle)
			{
				this.handle = handle;
				AssignHandle (handle);
			}
			
			public void Dispose ()
			{
				ReleaseHandle ();
			}
			
			protected override void WndProc (ref Message m)
			{
				switch (m.Msg) {
					case (int) Msg.WM_ENTERIDLE:
					case (int) Msg.WM_WINDOWPOSCHANGED:
						MonoDevelop.Ide.DispatchService.RunPendingEvents ();
						break;
				}
				
				base.WndProc (ref m);
			}
		}
    }

    class WinFormsRunner
    {
        bool firstRun = true;
        EventHandler action;

        public void Run(EventHandler action)
        {
            this.action = action;
            Application.Idle += WinFormsIdle;
            Application.Run();
            Application.Idle -= WinFormsIdle;
        }

        void WinFormsIdle(object sender, EventArgs e)
        {
            if (firstRun)
            {
                firstRun = false;
                action(null, null);
            }
            else
                MonoDevelop.Ide.DispatchService.RunPendingEvents();
        }
    }
}
