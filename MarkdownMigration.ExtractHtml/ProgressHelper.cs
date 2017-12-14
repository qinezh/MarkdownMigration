using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ExtractHtml
{
    public class ProgressHelper
    {
        private string _prefix;
        private int _totalCount;
        private int _currentCount;
        private int _currentStep;
        private double _traceStep;
        private Stopwatch _watch;

        public static ProgressHelper CreateStartedInstance(int totalCount, string prefix = null)
        {
            var progressHelper = new ProgressHelper();
            progressHelper.Init(totalCount, prefix);
            progressHelper.Start();
            return progressHelper;
        }

        public ProgressHelper()
        {
        }

        public void Init(int totalCount, string prefix = null, double traceStep = 0.01)
        {
            _totalCount = totalCount;
            _prefix = prefix;
            _traceStep = traceStep;
            _currentCount = 0;
            _currentStep = 0;
        }

        public void Start()
        {
            _watch = Stopwatch.StartNew();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Increase(int addCount = 1)
        {
            //Interlocked.Add(ref _currentCount, addCount);
            _currentCount += addCount;

            var stepCount = (int)Math.Floor(_currentCount / (_totalCount * _traceStep));
            if (_currentStep < stepCount)
            {
                _currentStep = stepCount;
                var prefix = string.IsNullOrEmpty(_prefix) ? "" : string.Format("[{0}]", _prefix);
                var msg = string.Format("\r{0} Processed:{1}%, Duration:{2} mins {3} secs",
                    prefix,
                    _traceStep * _currentStep * 100,
                    (int)_watch.Elapsed.TotalMinutes,
                    _watch.Elapsed.Seconds);
                Console.Write(msg);
            }

            if (_currentCount >= _totalCount)
            {
                _watch.Stop();
                Console.WriteLine();
            }
        }
    }
}
