namespace ConsoleApp
{
    using System;
    using CSRedis;
    using Cw.Sig;

    class Program
    {
        static void Main(string[] args)
        {
            // important step!!!
            var csredis = new CSRedisClient("127.0.0.1:6379");
            RedisHelper.Initialization(csredis);

            Snowflake16IdGenerator idGenerator = new Snowflake16IdGenerator(suffix: "con");
            for (int i = 0; i < 20; i++)
            {
                Console.WriteLine($"====begin=={i}====");
                var id = idGenerator.NextId();
                Console.WriteLine(id);
                var (timestamp, workerId, seq) = Snowflake16IdGenerator.Parse(id);
                var dt = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).AddHours(8);
                Console.WriteLine(dt.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\t" + workerId + "\t" + seq);
                Console.WriteLine($"====end=={i}====");
            }

            Console.ReadKey();
        }
    }
}
