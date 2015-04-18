using System;

namespace CachingServiceWithAOP.Tests.Infrastructure
{
    public class TestDomainObjA
    {
        private readonly Guid _testValue;

        public TestDomainObjA(Guid testValue)
        {
            _testValue = testValue;
        }

        public Guid TestValue
        {
            get { return _testValue; }
        }
    }
}
