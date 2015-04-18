using System;

namespace CachingServiceWithAOP.Tests.Infrastructure
{
    public interface IExampleService
    {
        int ReturnIntThatWasPassedIn(int input);
        DateTime GetCurrentTime();
        DateTime GetCurrentTimeWithSingleTickCachingViaAttribute();
        DateTime GetCurrentTimeWithOneDayCacheAttribute();
    }

    public class TestExampleServiceImplementation : IExampleService
    {
        [Cache]
        public int ReturnIntThatWasPassedIn(int input)
        {
            return input;
        }

        [Cache]
        public DateTime GetCurrentTime()
        {
            return DateTime.UtcNow;
        }

        [Cache(1)]
        public DateTime GetCurrentTimeWithSingleTickCachingViaAttribute()
        {
            return DateTime.UtcNow;
        }

        [Cache(1, 0, 0, 0)]
        public DateTime GetCurrentTimeWithOneDayCacheAttribute()
        {
            return DateTime.UtcNow;
        }

        public Guid ReturnGuidThatWasPassedIn(Guid input)
        {
            return input;
        }
    }
}