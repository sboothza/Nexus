using System.Diagnostics.Metrics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nexus.Library;
using Nexus.Library.Components;
using Nexus.Library.Modules;

namespace Nexus.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        Assert.Pass();
    }

    [Test]
    public void TestDeserialize()
    {
        using (ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole()))
        {
            ILogger logger = factory.CreateLogger("Program");
            var meter = new Meter("Program", "1.0.0");
            ComponentFactory.RegisterComponents(logger);
            var yaml = File.ReadAllText("config.yaml");
            using (var manager = new Manager("Components", logger, meter))
            {
                var component = ConfigParser.Parse(yaml, logger, meter, manager);
                Console.WriteLine(component);
            }
        }
    }

    [Test]
    public void TestList()
    {
        using (ILoggerFactory factory =
               LoggerFactory.Create(builder => builder
                   .SetMinimumLevel(LogLevel.Information)
                   .AddConsole()))
        {
            ILogger logger = factory.CreateLogger("Program");
            var meter = new Meter("Program", "1.0.0");
            ComponentFactory.RegisterComponents(logger);
            using (var manager = new Manager("Components", logger, meter))
            {
                var components = ConfigParser.ParseList("Components", logger, meter, manager);
                Console.WriteLine(components.ExpandToString());
            }
        }
    }

    [Test]
    public void TestManager()
    {
        using (ILoggerFactory factory = LoggerFactory.Create(builder => builder
                   .SetMinimumLevel(LogLevel.Information)
                   .AddConsole()))
        {
            ILogger logger = factory.CreateLogger("Program");
            var meter = new Meter("Program", "1.0.0");
            ComponentFactory.RegisterComponents(logger);
            using (var manager = new Manager("Components", logger, meter))
            {
                var response = manager.Query("BindingDoGet", new DataMessage
                {
                    Data = "apirequest"
                }).Result;
                Assert.That(response?.Data, Is.EqualTo("OK"));

                var json = JsonSerializer.Serialize(new ApiRequest
                {
                    Name = "Bob"
                });
                response = manager.Query("BindingDoPost", new DataMessage
                {
                    Data = json
                }).Result;
                Assert.That(response?.Data, Is.EqualTo("OK for Bob"));
            }
        }
    }

    [Test]
    public void TestPublish()
    {
        using (ILoggerFactory factory = LoggerFactory.Create(builder => builder
                   .SetMinimumLevel(LogLevel.Information)
                   .AddConsole()))
        {
            ILogger logger = factory.CreateLogger("Program");
            var meter = new Meter("Program", "1.0.0");
            ComponentFactory.RegisterComponents(logger);
            using (var manager = new Manager("Components", logger, meter))
            {
                var tc = new TestClass
                {
                    StringValue = "String",
                    IntValue = 2
                };
                var json = JsonSerializer.Serialize(tc);

                manager.Query("Publish", new DataMessage
                {
                    Data = json,
                    ExtraInfo = new Dictionary<string, string>
                    {
                        {
                            "topic", "Test"
                        }
                    }
                }).Wait();
            }
        }
    }

    [Test]
    public void TestPublishKafka()
    {
        using (ILoggerFactory factory = LoggerFactory.Create(builder => builder
                   .SetMinimumLevel(LogLevel.Information)
                   .AddConsole()))
        {
            ILogger logger = factory.CreateLogger("Program");
            var meter = new Meter("Program", "1.0.0");
            ComponentFactory.RegisterComponents(logger);
            using (var manager = new Manager("Components", logger, meter))
            {
                var tc = new TestClass
                {
                    StringValue = "String",
                    IntValue = 2
                };
                var json = JsonSerializer.Serialize(tc);
                manager.Query("PublishKafka", new DataMessage
                {
                    Data = json,
                    ExtraInfo = new Dictionary<string, string>
                    {
                        {
                            "topic", "Test"
                        }
                    }
                }).Wait();
            }
        }
    }

    [Test]
    public void TestSub()
    {
        using (var factory = LoggerFactory.Create(builder => builder
                   .SetMinimumLevel(LogLevel.Information)
                   .AddConsole()))
        {
            var logger = factory.CreateLogger("Program");
            var meter = new Meter("Program", "1.0.0");
            ComponentFactory.RegisterComponents(logger);
            using (var manager = new Manager("Components", logger, meter))
            {
                var receivedMessage = false;

                var tc = new TestClass
                {
                    StringValue = "String",
                    IntValue = 2
                };
                var json = JsonSerializer.Serialize(tc);
                manager.Query("Publish", new DataMessage
                {
                    Data = json,
                    ExtraInfo = new Dictionary<string, string>
                    {
                        {
                            "topic", "Test"
                        }
                    }
                }).Wait();
                Thread.Sleep(1000);

                Assert.That(receivedMessage, Is.True);
            }
        }
    }

    [Test]
    public void TestSubKafka()
    {
        using (var factory = LoggerFactory.Create(builder => builder
                   .SetMinimumLevel(LogLevel.Information)
                   .AddConsole()))
        {
            var logger = factory.CreateLogger("Program");
            var meter = new Meter("Program", "1.0.0");
            ComponentFactory.RegisterComponents(logger);
            using (var manager = new Manager("Components", logger, meter))
            {
                var receivedMessages = 0;

                var tc = new TestClass
                {
                    StringValue = "String",
                    IntValue = 2
                };
                var json = JsonSerializer.Serialize(tc);
                
                manager.Query("PublishKafka", new DataMessage
                {
                    Data = json,
                    ExtraInfo = new Dictionary<string, string>
                    {
                        {
                            "topic", "Test"
                        }
                    }
                }).Wait();

                tc = new TestClass
                {
                    StringValue = "String2",
                    IntValue = 3
                };
                json = JsonSerializer.Serialize(tc);
                
                manager.Query("PublishKafka", new DataMessage
                {
                    Data = json,
                    ExtraInfo = new Dictionary<string, string>
                    {
                        {
                            "topic", "Test"
                        }
                    }
                }).Wait();

                tc = new TestClass
                {
                    StringValue = "String3",
                    IntValue = 4
                };
                json = JsonSerializer.Serialize(tc);
                manager.Query("Publish", new DataMessage
                {
                    Data = json,
                    ExtraInfo = new Dictionary<string, string>
                    {
                        {
                            "topic", "Test"
                        }
                    }
                }).Wait();

                Thread.Sleep(5000);

                Assert.That(receivedMessages, Is.EqualTo(3));
                ;
            }
        }
    }

    // [Test]
    // public void TestInvokeLocalCall()
    // {
    //     using (var factory = LoggerFactory.Create(builder => builder
    //                .SetMinimumLevel(LogLevel.Information)
    //                .AddConsole()))
    //     {
    //         var logger = factory.CreateLogger("Program");
    //         var meter = new Meter("Program", "1.0.0");
    //         ComponentFactory.RegisterComponents(logger);
    //         using (var manager = new Manager("BasicComponents", logger, meter))
    //         {
    //             var ti = new TestInvokeLocal();
    //             manager.Return<string, string>("TestInvokeLocal", "Test").Wait();
    //             Assert.IsTrue(TestInvokeLocal.Called);
    //         }
    //     }
    // }

    [Test]
    public void TestSchedule()
    {
        using (var factory = LoggerFactory.Create(builder => builder
                   .SetMinimumLevel(LogLevel.Information)
                   .AddConsole()))
        {
            var logger = factory.CreateLogger("Program");
            var meter = new Meter("Program", "1.0.0");
            ComponentFactory.RegisterComponents(logger);
            using (var manager = new Manager("BasicComponents", logger, meter))
            {
                var ti = new TestInvokeLocal();
                Thread.Sleep(new TimeSpan(0, 15, 0));
                Assert.IsTrue(TestInvokeLocal.Called);
            }
        }
    }
}

public class TestInvokeLocal
{
    public static bool Called { get; set; }

    public void TestInvoke(string value)
    {
        Console.WriteLine($"Called with {value}");
        Called = true;
    }
}