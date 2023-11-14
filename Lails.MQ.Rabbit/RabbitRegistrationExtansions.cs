using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Lails.MQ.Rabbit.Consumer;
using System;

namespace Lails.MQ.Rabbit
{
    public static class RabbitRegistrationExtansions
    {
        private static bool _useQuartz;

        /// <summary>
        /// Конфигурация с подключением к rabbit и quartz
        /// </summary>
        /// <param name="cfg"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IRabbitMqBusFactoryConfigurator AddDataBusConfiguration(
            this IRabbitMqBusFactoryConfigurator cfg,
            IConfiguration configuration)
        {
            var userName = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? configuration["RABBITMQ_USERNAME"];
            var password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? configuration["RABBITMQ_PASSWORD"];
            var hostUrl = configuration["RABBITMQ_HOSTURL"];
            var quartzQueueName = configuration["RABBITMQ_QUARTZ_QUEUE_NAME"];
            _useQuartz = string.IsNullOrEmpty(quartzQueueName) == false;

            cfg.Host(new Uri(hostUrl), h =>
            {
                h.Username(userName);
                h.Password(password);
            });

            if (_useQuartz)
            {
                cfg.UseMessageScheduler(new Uri($"rabbitmq://{hostUrl}/{quartzQueueName}"));
            }

            return cfg;
        }

        /// <summary>
        /// Регистрация Consumert<T> с возможностью повторения
        /// </summary>
        /// <typeparam name="TConsumer"></typeparam>
        /// <typeparam name="TContract"></typeparam>
        /// <param name="cfg"></param>
        /// <param name="registration"></param>
        /// <param name="retryCount">Количество повторений</param>
        /// <param name="intervalMin">Интервал между повторениями </param>
        /// <param name="concurrencyLimit">Количество одновременных эзкемпляров</param>
        /// <returns></returns>
        public static IRabbitMqBusFactoryConfigurator RegisterConsumerWithRetry<TConsumer, TContract>(
            this IRabbitMqBusFactoryConfigurator cfg,
            IRegistrationContext registration,
            int retryCount,
            int intervalMin,
            int concurrencyLimit = 0)
            where TConsumer : BaseConsumer<TContract>
            where TContract : class
        {
            var queueName = $"{typeof(TConsumer).FullName}_{typeof(TContract)}";

            cfg.ReceiveEndpoint(queueName, e =>
            {
                if (_useQuartz)
                {
                    e.UseScheduledRedelivery(r => r.Intervals(
                        TimeSpan.FromMinutes(5),
                        TimeSpan.FromMinutes(15),
                        TimeSpan.FromMinutes(30),
                        TimeSpan.FromMinutes(60),
                        TimeSpan.FromMinutes(120),
                        TimeSpan.FromMinutes(240)));
                }

                e.UseMessageRetry(configurator =>
                {
                    configurator.Interval(retryCount, TimeSpan.FromMinutes(intervalMin));
                });

                e.ConfigureConsumer<TConsumer>(registration, ccfg =>
                {
                    if (concurrencyLimit > 0)
                    {
                        ccfg.UseConcurrencyLimit(concurrencyLimit);
                        ccfg.UseConcurrentMessageLimit(concurrencyLimit);
                    }
                    else
                    {
                        ccfg.Message<TContract>(m => m.UsePartitioner(1, context => context.MessageId.Value));
                    }
                });

            });

            cfg.AutoDelete = false;
            cfg.Durable = true;

            return cfg;
        }


        /// <summary>
        /// Регистрация IRabbitPublisher для публикации сообщений
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <returns></returns>
        public static IServiceCollection RegisterRabbitPublisher(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IRabbitPublisher, RabbitPublisher>();

            return serviceCollection;
        }

        public static IServiceCollection RegisterRabbitPublisherStub(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IRabbitPublisher, RabbitPublisherStub>();

            return serviceCollection;
        }
    }
}
