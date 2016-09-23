using EasyNetQ.Interception;

namespace EasyNetQ.Blocker.Extensions
{
    public static class ServiceRegisterExtensions
    {
        public static IServiceRegister RegisterXXXServices(this IServiceRegister register)
        {
            return register
                .Register<IAdvancedBus, BusThatSendsConfirmations>()
                .Register<IProduceConsumeInterceptor, GenerateMessageIdInterceptor>();
        }    
    }
}
