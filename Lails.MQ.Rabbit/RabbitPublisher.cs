using MassTransit;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Lails.MQ.Rabbit;

	/// <summary>
	/// Реализация интерфейса IRabbitPublisher для публикации сообщений в RabbitMQ через MassTransit
	/// </summary>
	public class RabbitPublisher : IRabbitPublisher
	{
		/// <summary>
		/// Экземпляр MassTransit IBus для публикации сообщений
		/// </summary>
		private readonly IBus _bus;

		/// <summary>
		/// Инициализирует новый экземпляр класса RabbitPublisher
		/// </summary>
		/// <param name="bus">Экземпляр MassTransit IBus для публикации сообщений</param>
		/// <exception cref="ArgumentNullException">Выбрасывается, если bus равен null</exception>
		public RabbitPublisher(IBus bus)
		{
			_bus = bus ?? throw new ArgumentNullException(nameof(bus));
		}

		/// <summary>
		/// Асинхронно публикует сообщение в очередь RabbitMQ
		/// </summary>
		/// <typeparam name="T">Тип сообщения, должен быть классом</typeparam>
		/// <param name="message">Сообщение для публикации</param>
		/// <returns>Задача, представляющая асинхронную операцию публикации</returns>
		/// <exception cref="ArgumentNullException">Выбрасывается, если message равен null</exception>
		/// <exception cref="Exception">Выбрасывается при ошибке публикации</exception>
		public async Task PublishAsync<T>(T message) where T : class
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

		try
		{
			await _bus.Publish(message);
		}
			catch (Exception ex)
			{
				Log.Error(ex, "Произошла ошибка при публикации события через издатель {Publisher}. Событие: {Event}, Имя: {EventName}", nameof(RabbitPublisher), message, typeof(T).Name);

				throw;
			}
		}

		/// <summary>
		/// Отправляет сообщение с отложенной доставкой в указанное время
		/// </summary>
		/// <typeparam name="TMessage">Тип сообщения, должен быть классом</typeparam>
		/// <param name="message">Сообщение для отложенной отправки</param>
		/// <param name="scheduledTime">Время, когда сообщение должно быть доставлено (в UTC)</param>
		/// <returns>Идентификатор токена запланированного сообщения</returns>
		/// <exception cref="ArgumentNullException">Выбрасывается, если message равен null</exception>
		/// <exception cref="Exception">Выбрасывается при ошибке планирования сообщения</exception>
		public async Task<Guid> SendScheduledMessageAsync<TMessage>(TMessage message, DateTime scheduledTime) where TMessage : class
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			try
			{
				return await _bus.SendScheduledMessage(message, scheduledTime);
			}
			catch (Exception ex)
			{
				Log.Error(ex, $"Произошла ошибка при отправке запланированного сообщения. Сообщение: {message}, Время отправки: {scheduledTime}");

				throw;
			}
		}

		/// <summary>
		/// Отменяет ранее запланированное сообщение
		/// </summary>
		/// <param name="tokenId">Идентификатор токена запланированного сообщения, полученный из SendScheduledMessageAsync</param>
		/// <param name="messageType">Тип сообщения, которое нужно отменить</param>
		/// <returns>Задача, представляющая асинхронную операцию отмены</returns>
		/// <exception cref="ArgumentException">Выбрасывается, если tokenId равен Guid.Empty</exception>
		/// <exception cref="ArgumentNullException">Выбрасывается, если messageType равен null</exception>
		/// <exception cref="Exception">Выбрасывается при ошибке отмены сообщения</exception>
		public async Task CancelScheduledMessageAsync(Guid tokenId, Type messageType)
		{
			if (tokenId == Guid.Empty)
				throw new ArgumentException("Идентификатор токена не может быть пустым", nameof(tokenId));
			if (messageType == null)
				throw new ArgumentNullException(nameof(messageType));

			try
			{
				await _bus.CancelScheduledMessage(tokenId, messageType);
			}
			catch (Exception ex)
			{
				Log.Error(ex, $"Произошла ошибка при отмене запланированного сообщения. TokenId: {tokenId}");

				throw;
			}
		}
}
