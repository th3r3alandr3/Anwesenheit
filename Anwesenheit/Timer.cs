using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Anwesenheit
{
    class Timer
    {
        private DispatcherTimer dispatcherTimer;
        private Action TimerFunction;
        private TimeSpan timeSpan;
        private bool isRunning = false;

        public Timer(Action TimerFunction, int seconds = 30)
        {
            this.timeSpan = TimeSpan.FromSeconds(seconds);
            this.TimerFunction = TimerFunction;
            DispatcherTimerSetup();
        }

        public void Start()
        {
            isRunning = true;
            dispatcherTimer.Start();
        }

        public void Stop()
        {
            isRunning = false;
            dispatcherTimer.Stop();
        }

        public void Restart()
        {
            Start();
            Stop();
        }

        public bool Running()
        {
            return isRunning;
        }

        public int Interval
        {
            get => Convert.ToInt32(timeSpan.TotalSeconds);
            set => timeSpan = TimeSpan.FromSeconds(value);
        }

        private void DispatcherTimerSetup()
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = timeSpan;
        }

        private void DispatcherTimer_Tick(object sender, object e)
        {
            TimerFunction();
        }
    }
}
