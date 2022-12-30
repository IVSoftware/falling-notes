In your code, there is a unit of work that we'll call "move notes" and you're doing this at set intervals. The tricky thing is that we really don't know how long it takes to move the notes. Obviously moving 10,000 notes will take longer than moving 10, and if it takes (for example) a whole second to move them all and you're attempting to to this 10 times a second then things can get gummed up.

One thing you could try is a different kind of timer loop, basically:
1. Move all the notes, no matter how long it takes.
2. Wait for some time interval to expire.
3. Repeat

***
Here's how this could look in the MainForm class:

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
            while(!Disposing)
            {
                // This is a worker task. Blocking here
                // isn't going to block the UI thread.
                Thread.Sleep(TimeSpan.FromMilliseconds(100));
                // Execute some 'work' synchronously. This prevents
                // work from piling up if it can't be completed.
                TimerTick?.Invoke(this, EventArgs.Empty);
            }
        }
        internal static event EventHandler TimerTick;
        .
        .
        .
    }

***
Your current code is iterating a list of Notes and instructing them to move one by one. Have you considered making the notes smart enough to move themselves? The way you would do this is to make a `Note` class that can respond to the timer events fired by the main form.

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

***
To make sure that the motion doesn't get bogged down I did a simple test where I'm adding a `Note` for every mouse click. (This should still leave plenty of work to adapt this to the game that is your assignment.) 

[![screenshot][1]][1]

    public partial class MainForm : Form
    {
        .
        .
        .
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


  [1]: https://i.stack.imgur.com/EpMkq.png
