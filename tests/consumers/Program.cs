// A fresh consumer of the packed or published package (ci.yml, verify-published.yml, through run.sh). It registers a
// component with [Cache] methods through the public API only, checks the answers, and prints the Autofac,
// Autofac.Extras.DynamicProxy and Castle.Core versions it actually loaded, which run.sh checks per combination.

using System;
using Autofac;
using CachingServiceWithAOP;
using CachingServiceWithAOP.CachingServices;
using CachingServiceWithAOP.Extensions;

public interface IClock
{
    int Next();

    string? Nothing();

    int Echo(string text, int n);
}

public class Clock : IClock
{
    private int _n;

    [Cache]
    public int Next()
    {
        return ++_n;
    }

    [Cache]
    public string? Nothing()
    {
        _n++;
        return null;
    }

    [Cache(0, 1, 0)]
    public int Echo(string text, int n)
    {
        return ++_n * 1000 + n;
    }
}

public static class Program
{
    public static int Main()
    {
        var builder = new ContainerBuilder();
        builder.RegisterCachingModule();
        builder.RegisterType<Clock>().As<IClock>().SingleInstance().EnableCacheInterception();

        bool ok;
        using (var container = builder.Build())
        {
            var clock = container.Resolve<IClock>();
            ok = clock.Next() == 1
                && clock.Next() == 1
                && clock.Nothing() == null
                && clock.Nothing() == null
                && clock.Echo("a", 5) == 3005
                && clock.Echo("a", 5) == 3005
                && clock.Echo("b", 5) == 4005;
        }

        var cache = new MemoryCacheService();
        ok = ok && cache.Get("consumer-key", () => "v") == "v" && cache.Get<string>("consumer-key") == "v";

        Console.WriteLine("Autofac " + typeof(ContainerBuilder).Assembly.GetName().Version);
        Console.WriteLine("Autofac.Extras.DynamicProxy " + typeof(Autofac.Extras.DynamicProxy.RegistrationExtensions).Assembly.GetName().Version);
        Console.WriteLine("Castle.Core " + typeof(Castle.DynamicProxy.IInvocation).Assembly.GetName().Version);
        Console.WriteLine("CachingServiceWithAOP " + typeof(CacheAttribute).Assembly.GetName().Version);
        Console.WriteLine(ok ? "consumer answers as expected" : "wrong answer");
        return ok ? 0 : 1;
    }
}
