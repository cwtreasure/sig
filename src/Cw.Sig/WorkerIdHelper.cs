namespace Cw.Sig
{
    using System;

    internal static class WorkerIdHelper
    {
        private static readonly string INCR_KEY = $"snid:{AppDomain.CurrentDomain.FriendlyName}";
        private static readonly string USED_KEY = $"snid:{AppDomain.CurrentDomain.FriendlyName}:used";

        /// <summary>
        /// GetWorkerId, generate a new workerid from redis
        /// </summary>
        /// <param name="maxWorkerId">The max value of worker id</param>
        /// <param name="min">The life time of worker id</param>
        /// <param name="suffix">The redis key's suffix</param>
        /// <returns></returns>
        public static long GetWorkerId(long maxWorkerId, int min = 10, string suffix = "suf")
        {
            var now = DateTimeOffset.UtcNow;

            // keys[1] auto incr key for worker id
            // argv[1] max worker id
            // argv[2] used zset key
            // argv[3] current timestammp
            // argv[4] maybe expiry worker id, (current timestammp - specify min)
            var obj = RedisHelper.Eval(LuaScript,
                $"{INCR_KEY}:{suffix}",
                maxWorkerId,
                $"{USED_KEY}:{suffix}",
                now.ToUnixTimeSeconds(),
                now.AddMinutes(-min).ToUnixTimeSeconds());

            return obj == null
                // maybe throw exception will be better?
                // random value may cause duplicate id
                ? throw new Exception("Not found a usable worker id, try again later!") //new Random().Next(0, (int)maxWorkerId)
                : (long)obj;
        }

        /// <summary>
        /// SendHeartBeat for this workerId, means that this workerId is in use.
        /// </summary>
        /// <param name="workerId"></param>
        /// <param name="suffix"></param>
        public static void SendHeartBeat(long workerId, string suffix = "suf")
        {
            for (int i = 0; i < 2; i++)
            {
                try
                {
#if DEBUG
                    Console.WriteLine($"SendHeartBeat {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} {workerId} {suffix}");
#endif
                    RedisHelper.ZAdd($"{USED_KEY}:{suffix}", (DateTimeOffset.UtcNow.ToUnixTimeSeconds(), workerId));
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// Two things we need to know: Id and Is this Id in use
        /// 
        /// 1. INCR a key, will get a wid
        /// 2. If this wid less than MaxWorkerId
        ///     2.1 get the score of this wid
        ///         found and greater than expire timestamp, in use, chose a expire one.
        ///         not found, use this one
        /// 
        /// 3. If this wid greater than MaxWorkerId, chose a expire one.
        /// 
        /// How to chse a expire one?
        /// 
        /// Try to find out some expire(no in use) wid with a timestamp before life time, 
        /// if found, update this wid with current timestamp and return
        /// this wid, otherwise, nothing will be returned.
        /// </summary>
        private const string LuaScript = @"
local iv = redis.call('INCR', KEYS[1])

if(tonumber(ARGV[1]) < tonumber(iv) - 1)
then
    local element = redis.call('ZRANGEBYSCORE', ARGV[2], 0, ARGV[4], 'LIMIT', '0', '5')
    if element ~= false and #element ~= 0 then
        redis.call('ZADD', ARGV[2], ARGV[3], element[1])
        return tonumber(element[1])
    end
else
    local sc = redis.call('ZSCORE', ARGV[2], tonumber(iv) - 1)
    if (sc ~= false and tonumber(sc) > tonumber(ARGV[4]))
    then
        local element = redis.call('ZRANGEBYSCORE', ARGV[2], 0, ARGV[4], 'LIMIT', '0', '5')
        if element ~= false and #element ~= 0 then
            redis.call('ZADD', ARGV[2], ARGV[3], element[1])
            return tonumber(element[1])
        end
    else
        redis.call('ZADD', ARGV[2], ARGV[3], tonumber(iv) - 1)
        return tonumber(iv) - 1
    end
end
";
    }
}
