namespace Cw.Sig
{
    using System;
    using System.Threading;

    public class Snowflake16IdGenerator
    {
        // Just modify those value for your applications that you need
        public const long Twepoch = 1288834974657L;

        private const int WorkerIdBits = 6;

        private const int SequenceBits = 8;

        private const long MaxWorkerId = -1L ^ (-1L << WorkerIdBits);

        private const long SequenceMask = -1L ^ (-1L << SequenceBits);

        private const int WorkerIdShift = SequenceBits;

        public const int TimestampLeftShift = SequenceBits + WorkerIdBits;

        private long _sequence = 0L;
        private long _lastTimestamp = -1L;

        public long WorkerId { get; protected set; }

        public long Sequence
        {
            get { return _sequence; }
            internal set { _sequence = value; }
        }

        private readonly Timer _timer;

        public Snowflake16IdGenerator(int min = 10, string suffix = "suf")
        {
            WorkerId = WorkerIdHelper.GetWorkerId(maxWorkerId: MaxWorkerId, min: min, suffix: suffix);
            _sequence = 0;

            // send heart beat, let other understand that this workerid is in use !!
            _timer = new Timer((x) => WorkerIdHelper.SendHeartBeat((long)x, suffix), WorkerId, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(5));
        }

        private readonly object _lock = new object();

        /// <summary>
        /// Next Snowflake Id
        /// </summary>
        /// <returns></returns>
        public long NextId()
        {
            lock (_lock)
            {
                var timestamp = TimeGen();
                if (timestamp < _lastTimestamp)
                {
                    throw new Exception(string.Format("timestamp maybe error here, reject for a new id about {0}.", _lastTimestamp - timestamp));
                }

                if (_lastTimestamp == timestamp)
                {
                    _sequence = (_sequence + 1) & SequenceMask;

                    if (_sequence == 0)
                    {
                        timestamp = TilNextMillis(_lastTimestamp);
                    }
                }
                else
                {
                    _sequence = 0;
                }

                _lastTimestamp = timestamp;

                return ((timestamp - Twepoch) << TimestampLeftShift) | (WorkerId << WorkerIdShift) | _sequence;
            }
        }

        private long TilNextMillis(long lastTimestamp)
        {
            var timestamp = TimeGen();
            while (timestamp <= lastTimestamp)
            {
                timestamp = TimeGen();
            }

            return timestamp;
        }

        private long TimeGen()
            => DateTimeOffset.Now.ToUnixTimeMilliseconds();

        public void Dispose()
        {
            _timer?.Change(TimeSpan.FromHours(24), TimeSpan.FromHours(24));
            _timer?.Dispose();
            Console.WriteLine("dispose");
        }

        /// <summary>
        /// Parse id created by <see cref="Snowflake16IdGenerator"/> 
        /// </summary>
        /// <param name="id">snowflake id</param>
        /// <returns>(timestamp, workerid, seq)</returns>
        public static (long timestamp, int workerId, int seq) Parse(long id)
        {
            // 0x3F 63 2^6 - 1
            // 0x0FF 255 2^8 - 1
            var time = id >> TimestampLeftShift;
            var workerId = (int)((id >> WorkerIdShift) & 0x3F);
            var seq = (int)(id & 0x0FF);

            return (time + Twepoch, workerId, seq);
        }
    }
}
