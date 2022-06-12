namespace Cw.Sig
{
    using System;

    internal static class WorkerIdHelper
    {
        private static readonly string INCR_KEY = $"snid:{AppDomain.CurrentDomain.FriendlyName}";
        private static readonly string USED_KEY = $"snid:{AppDomain.CurrentDomain.FriendlyName}:used";

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
                $"{USED_KEY}:suf",
                now.ToUnixTimeSeconds(),
                now.AddMinutes(-min).ToUnixTimeSeconds());

            return obj == null
                ? new Random().Next(0, (int)maxWorkerId)
                : (long)obj;
        }

        /// <summary>
        /// SendHeartBeat for this workerId, means that this workerId is in used.
        /// </summary>
        /// <param name="workerId"></param>
        /// <param name="suffix"></param>
        public static void SendHeartBeat(long workerId, string suffix = "suf")
        {
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    RedisHelper.ZAdd($"{USED_KEY}:{suffix}", (DateTimeOffset.UtcNow.ToUnixTimeSeconds(), workerId));
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private const string LuaScript = @"
local iv = redis.call('INCR', KEYS[1])

if(tonumber(ARGV[1]) < tonumber(iv))
then
    local element = redis.call('ZRANGEBYSCORE', ARGV[2], 0, ARGV[4], 'LIMIT', '0', '5')
    if element ~= false and #element ~= 0 then
        redis.call('ZADD', ARGV[2], ARGV[3], element[1])
        return element[1]        
    end
else
    redis.call('ZADD', ARGV[2], ARGV[3], iv)
    return iv
end
";
    }
}
