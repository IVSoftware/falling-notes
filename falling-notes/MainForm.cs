using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace falling_notes
{
    public partial class MainForm : Form
    {
        public MainForm() => InitializeComponent();
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _timerTask = Task.Run(() => execTimer(), _cts.Token);
            Disposed += (sender, e) => _cts.Cancel();
        }
        private Task _timerTask;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private void execTimer()
        {
            while(true)
            {
                // This is a worker task. Blocking here
                // isn't going to block the UI thread.
                if (_cts.IsCancellationRequested) break;
                Thread.Sleep(TimeSpan.FromMilliseconds(100));
                if (_cts.IsCancellationRequested) break;
                // Execute some 'work' synchronously. This prevents
                // work from piling up if it can't be completed.
                TimerTick?.Invoke(this, EventArgs.Empty);
            }
        }
        internal static event EventHandler TimerTick;
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            var button = new Note(e.Location)
            {
                Name = $"button{++_buttonIndex}",
            };
            Controls.Add(button);
            Text = $"{++_buttonCount} Active Notes";
        }
        protected override void OnControlRemoved(ControlEventArgs e)
        {
            base.OnControlRemoved(e);
            if(e.Control is Note)
            {
                Text = $"{--_buttonCount} Active Notes";
            }
        }
        private int
            _buttonCount = 0,
            _buttonIndex = 0;
    }
    
    class Note : Button
    {
        public Note(Point location)
        {
            Size = new Size(25, 25);    // Arbitrary for testing
            BackColor = Color.Black;
            Location = location;
            // Subscribe to the timer tick
            MainForm.TimerTick += onTimerTick;
        }
        private void onTimerTick(object sender, EventArgs e)
        {
            BeginInvoke(new Action(() =>
            {
                // Execute the move
                Location = new Point(Location.X, Location.Y + 10); 

                // Detect whether the location has moved off of the
                // main form and if so have the Note remove itself.
                if(!Parent.ClientRectangle.Contains(Location))
                {
                    MainForm.TimerTick -= onTimerTick;
                    Parent.Controls.Remove(this);
                }               
            }));
        }
    }
}
