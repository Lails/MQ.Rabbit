using MassTransit;
using System.Threading.Tasks;

namespace Lails.MQ.Rabbit.Consumer;

	/// <summary>
	/// Базовый абстрактный класс для реализации потребителей сообщений из RabbitMQ
	/// Предоставляет обертку над методом Consume, делегируя выполнение в абстрактный метод ConsumeImplementation
	/// </summary>
	/// <typeparam name="TEvent">Тип события/сообщения, которое будет обрабатываться потребителем</typeparam>
	public abstract class BaseConsumer<TEvent> : IConsumer<TEvent> where TEvent : class
	{
		/// <summary>
		/// Обрабатывает входящее сообщение из очереди RabbitMQ
		/// </summary>
		/// <param name="context">Контекст потребления сообщения, содержащий само сообщение и метаданные</param>
		/// <returns>Задача, представляющая асинхронную операцию обработки сообщения</returns>
		public async Task Consume(ConsumeContext<TEvent> context)
		{
			await ConsumeImplementation(context);
		}

		/// <summary>
		/// Абстрактный метод для реализации логики обработки сообщения в производных классах
		/// </summary>
		/// <param name="context">Контекст потребления сообщения, содержащий само сообщение и метаданные</param>
		/// <returns>Задача, представляющая асинхронную операцию обработки сообщения</returns>
		protected abstract Task ConsumeImplementation(ConsumeContext<TEvent> context);
	}