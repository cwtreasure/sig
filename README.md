# sig

sig = Snowflake Id Generator

## Feature

- Redis based management of workid
- Parse a Snowflake Id to base info(timestamp, workid, sequence)
- Length of the Id contains 16 or 19

## Usage

```cs
// Important Step!!!
var csredis = new CSRedisClient("127.0.0.1:6379");
RedisHelper.Initialization(csredis);

// Keep IdGenerator Singleton!!!
// 19
Snowflake19IdGenerator idGenerator = new Snowflake19IdGenerator(suffix: "con");
var id = idGenerator.NextId();
var (timestamp, workerId, seq) = Snowflake19IdGenerator.Parse(id);
var dt = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
Console.WriteLine(dt.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\t" + workerId + "\t" + seq);

// 16
Snowflake19IdGenerator xx = .....
```
