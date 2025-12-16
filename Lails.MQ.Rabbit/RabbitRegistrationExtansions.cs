using Lails.MQ.Rabbit.Consumer;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Lails.MQ.Rabbit;

	/// <summary>
	/// Расширения для регистрации и настройки RabbitMQ и MassTransit в контейнере зависимостей
	/// </summary>
	public static class RabbitRegistrationExtansions
	{
		/// <summary>
		/// Флаг, указывающий, используется ли Quartz для планирования сообщений
		/// </summary>
		private static bool _useQuartz;

		/// <summary>
		/// Настраивает подключение к RabbitMQ с возможностью использования SSL/TLS сертификатов и Quartz для планирования сообщений
		/// </summary>
		/// <param name="cfg">Конфигуратор MassTransit для RabbitMQ</param>
		/// <param name="configuration">Конфигурация приложения для получения параметров подключения</param>
		/// <returns>Конфигуратор MassTransit для цепочки вызовов</returns>
		/// <exception cref="ArgumentNullException">Выбрасывается, если cfg, configuration или обязательные параметры подключения равны null</exception>
		/// <exception cref="UriFormatException">Выбрасывается, если URL хоста имеет неверный формат</exception>
		/// <exception cref="System.IO.FileNotFoundException">Выбрасывается, если указанный путь к сертификату не существует</exception>
		/// <remarks>
		/// Параметры конфигурации:
		/// - RABBITMQ_HOSTURL: URL хоста RabbitMQ (например, "rabbitmq://localhost/")
		/// - RABBITMQ_USERNAME: Имя пользователя для подключения
		/// - RABBITMQ_PASSWORD: Пароль для подключения
		/// - RABBITMQ_QUARTZ_QUEUE_NAME: Имя очереди Quartz (опционально, если указано, будет использоваться планировщик сообщений)
		/// - CERTIFICATE_PFX_PATH: Путь к файлу сертификата .pfx (опционально)
		/// - CERTIFICATE_PFX_PASSWORD: Пароль для сертификата (опционально)
		/// - Domain:Base: Доменное имя для SSL сертификата (опционально)
		/// </remarks>
		public static IRabbitMqBusFactoryConfigurator AddDataBusConfiguration(
			this IRabbitMqBusFactoryConfigurator cfg,
			IConfiguration configuration)
    {
        var userName = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? configuration["RABBITMQ_USERNAME"];
        var password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? configuration["RABBITMQ_PASSWORD"];
        var certitifactePath = Environment.GetEnvironmentVariable("CERTIFICATE_PFX_PATH") ?? configuration["CERTIFICATE_PFX_PATH"];
        var certitifactePassword = Environment.GetEnvironmentVariable("CERTIFICATE_PFX_PASSWORD") ?? configuration["CERTIFICATE_PFX_PASSWORD"];
        var domainName = Environment.GetEnvironmentVariable("Domain:Base") ?? configuration["Domain:Base"];
        
        ArgumentNullException.ThrowIfNull(nameof(cfg));
        ArgumentNullException.ThrowIfNull(nameof(userName));
        ArgumentNullException.ThrowIfNull(nameof(password));

        var hostUrl = configuration["RABBITMQ_HOSTURL"];
        var quartzQueueName = configuration["RABBITMQ_QUARTZ_QUEUE_NAME"];
        _useQuartz = string.IsNullOrEmpty(quartzQueueName) == false;

        cfg.Host(new Uri(hostUrl), h =>
        {
            h.Username(userName);
            h.Password(password);
            //try user certificate
            if (!string.IsNullOrWhiteSpace(certitifactePath)
                && !string.IsNullOrWhiteSpace(certitifactePassword)
                && !string.IsNullOrWhiteSpace(domainName))
            {
                h.UseSsl(h =>
                {
                    h.ServerName = domainName;
                    h.Protocol = System.Security.Authentication.SslProtocols.Tls12;
                    var pfxData = File.ReadAllBytes(certitifactePath);
                    var certitifacte = new X509Certificate2(pfxData, certitifactePassword, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
                    h.Certificate = certitifacte;
                });
            }
				else
				{
					Log.Warning("SSL сертификат не используется");
					Console.WriteLine("SSL сертификат не используется");
				}
        });


        if (_useQuartz)
        {
            cfg.UseMessageScheduler(new Uri($"rabbitmq://{hostUrl}/{quartzQueueName}"));
        }

        return cfg;
    }

		/// <summary>
		/// Регистрирует Consumer с настройкой политики повторных попыток, планируемых повторных доставок и ограничения конкурентности
		/// </summary>
		/// <typeparam name="TConsumer">Тип consumer, должен наследоваться от BaseConsumer&lt;TContract&gt;</typeparam>
		/// <typeparam name="TContract">Тип контракта сообщения, должен быть классом</typeparam>
		/// <param name="cfg">Конфигуратор MassTransit для RabbitMQ</param>
		/// <param name="registration">Контекст регистрации MassTransit</param>
		/// <param name="retryCount">Количество повторных попыток обработки сообщения при ошибке</param>
		/// <param name="intervalMin">Интервал в минутах между повторными попытками</param>
		/// <param name="concurrencyLimit">Количество одновременных экземпляров consumer для обработки сообщений (0 = без ограничения, используется партиционирование)</param>
		/// <returns>Конфигуратор MassTransit для цепочки вызовов</returns>
		/// <exception cref="ArgumentNullException">Выбрасывается, если cfg или registration равны null</exception>
		/// <exception cref="ArgumentException">Выбрасывается, если retryCount или intervalMin меньше или равны нулю</exception>
		/// <remarks>
		/// Имя очереди формируется автоматически как: {TConsumer.FullName}_{TContract}
		/// Если Quartz настроен (через RABBITMQ_QUARTZ_QUEUE_NAME), будет использоваться планируемая повторная доставка с интервалами: 5, 15, 30, 60, 120, 240 минут
		/// Если concurrencyLimit > 0, устанавливается ограничение конкурентности, иначе используется партиционирование по MessageId
		/// Очередь создается как Durable (постоянная) и не AutoDelete (не удаляется автоматически)
		/// </remarks>
		public static IRabbitMqBusFactoryConfigurator RegisterConsumerWithRetry<TConsumer, TContract>(
			this IRabbitMqBusFactoryConfigurator cfg,
			IRegistrationContext registration,
			int retryCount,
			int intervalMin,
			int concurrencyLimit = 0)
			where TConsumer : BaseConsumer<TContract>
			where TContract : class
    {
			if (cfg == null)
				throw new ArgumentNullException(nameof(cfg));
			if (registration == null)
				throw new ArgumentNullException(nameof(registration));
			if (retryCount <= 0)
				throw new ArgumentException("Количество повторных попыток должно быть больше нуля", nameof(retryCount));
			if (intervalMin <= 0)
				throw new ArgumentException("Интервал должен быть больше нуля", nameof(intervalMin));

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
		/// Регистрирует IRabbitPublisher как Singleton в контейнере зависимостей для публикации сообщений в RabbitMQ
		/// </summary>
		/// <param name="serviceCollection">Коллекция сервисов для регистрации зависимостей</param>
		/// <returns>Коллекция сервисов для цепочки вызовов</returns>
		/// <exception cref="ArgumentNullException">Выбрасывается, если serviceCollection равен null</exception>
		public static IServiceCollection RegisterRabbitPublisher(this IServiceCollection serviceCollection)
		{
			if (serviceCollection == null)
				throw new ArgumentNullException(nameof(serviceCollection));

			serviceCollection.AddSingleton<IRabbitPublisher, RabbitPublisher>();

			return serviceCollection;
		}

		/// <summary>
		/// Регистрирует IRabbitPublisher как Singleton с использованием заглушки (RabbitPublisherStub) для тестирования или разработки
		/// Заглушка не выполняет реальную отправку сообщений в RabbitMQ
		/// </summary>
		/// <param name="serviceCollection">Коллекция сервисов для регистрации зависимостей</param>
		/// <returns>Коллекция сервисов для цепочки вызовов</returns>
		/// <exception cref="ArgumentNullException">Выбрасывается, если serviceCollection равен null</exception>
		public static IServiceCollection RegisterRabbitPublisherStub(this IServiceCollection serviceCollection)
		{
			if (serviceCollection == null)
				throw new ArgumentNullException(nameof(serviceCollection));

			serviceCollection.AddSingleton<IRabbitPublisher, RabbitPublisherStub>();

			return serviceCollection;
		}
}
