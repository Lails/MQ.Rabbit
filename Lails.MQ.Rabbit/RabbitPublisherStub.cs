using System;
using System.Threading.Tasks;

namespace Lails.MQ.Rabbit;

	/// <summary>
	/// Заглушка реализации IRabbitPublisher для использования в тестах или когда публикация сообщений не требуется
	/// Все методы выполняются без реальной отправки сообщений в RabbitMQ
	/// </summary>
	public class RabbitPublisherStub : IRabbitPublisher
	{
		/// <summary>
		/// Асинхронно публикует сообщение (заглушка - возвращает завершенную задачу)
		/// </summary>
		/// <typeparam name="T">Тип сообщения, должен быть классом</typeparam>
		/// <param name="message">Сообщение для публикации (игнорируется)</param>
		/// <returns>Завершенная задача</returns>
		public Task PublishAsync<T>(T message) where T : class
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Отменяет запланированное сообщение (заглушка - возвращает завершенную задачу)
		/// </summary>
		/// <param name="tokenId">Идентификатор токена (игнорируется)</param>
		/// <param name="messageType">Тип сообщения (игнорируется)</param>
		/// <returns>Завершенная задача</returns>
		public Task CancelScheduledMessageAsync(Guid tokenId, Type messageType)
		{
			return Task.CompletedTask;
		}

		/// <summary>
		/// Отправляет сообщение с отложенной доставкой (заглушка - возвращает пустой Guid)
		/// </summary>
		/// <typeparam name="TMessage">Тип сообщения, должен быть классом</typeparam>
		/// <param name="message">Сообщение для отложенной отправки (игнорируется)</param>
		/// <param name="scheduledTime">Время доставки (игнорируется)</param>
		/// <returns>Задача, содержащая пустой Guid</returns>
		public Task<Guid> SendScheduledMessageAsync<TMessage>(TMessage message, DateTime scheduledTime) where TMessage : class
		{
			return Task.FromResult(Guid.Empty);
		}
	}
