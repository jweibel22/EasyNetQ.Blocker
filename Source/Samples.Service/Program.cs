using System;
using EasyNetQ;
using EasyNetQ.AutoSubscribe;
using EasyNetQ.Blocker.Extensions;
using EasyNetQ.DI;
using EasyNetQ.Loggers;
using Ninject;
using Samples.Shared;

namespace Samples.Service
{
    class Program
    {
        static void Main(string[] args)
        {
            var kernel = new StandardKernel();
            kernel.RegisterAsEasyNetQContainerFactory();

            var bus = RabbitHutch.CreateBus("host=vmdcvppt1", reg => reg.RegisterXXXServices().Register<IEasyNetQLogger, ConsoleLogger>());

            kernel.Bind<IMessagePublisher>().ToConstant(new RabbitMessagePublisher(bus));
            kernel.Bind<Stock>().To<Stock>().InSingletonScope(); //the "Stock" consumer stores state in a field variable, make sure it's not lost between requests

            var autoSubscriber = new AutoSubscriber(bus, "SystemTest")
            {
                ConfigureSubscriptionConfiguration = x => x.WithAutoDelete(),
                AutoSubscriberMessageDispatcher = new NinjectMessageDispatcher(kernel)
            };

            autoSubscriber.Subscribe(typeof(Stock).Assembly);

            Console.WriteLine("Press any key to exit");
            Console.ReadLine();

            bus.Dispose();
        }
    }
}
