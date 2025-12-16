using System;
using System.Threading.Tasks;

namespace Lails.MQ.Rabbit;

	/// <summary>
	/// Интерфейс для публикации сообщений в RabbitMQ через MassTransit
	/// </summary>
	public interface IRabbitPublisher
	{
		/// <summary>
		/// Асинхронно публикует сообщение в очередь RabbitMQ
		/// </summary>
		/// <typeparam name="T">Тип сообщения, должен быть классом</typeparam>
		/// <param name="message">Сообщение для публикации</param>
		/// <returns>Задача, представляющая асинхронную операцию публикации</returns>
		/// <exception cref="Exception">Выбрасывается при ошибке публикации</exception>
		Task PublishAsync<T>(T message) where T : class;

		/// <summary>
		/// Отправляет сообщение с отложенной доставкой в указанное время
		/// </summary>
		/// <typeparam name="TMessage">Тип сообщения, должен быть классом</typeparam>
		/// <param name="message">Сообщение для отложенной отправки</param>
		/// <param name="scheduledTime">Время, когда сообщение должно быть доставлено</param>
		/// <returns>Идентификатор токена запланированного сообщения</returns>
		/// <exception cref="Exception">Выбрасывается при ошибке планирования сообщения</exception>
		Task<Guid> SendScheduledMessageAsync<TMessage>(TMessage message, DateTime scheduledTime) where TMessage : class;

		/// <summary>
		/// Отменяет ранее запланированное сообщение
		/// </summary>
		/// <param name="tokenId">Идентификатор токена запланированного сообщения</param>
		/// <param name="messageType">Тип сообщения, которое нужно отменить</param>
		/// <returns>Задача, представляющая асинхронную операцию отмены</returns>
		/// <exception cref="Exception">Выбрасывается при ошибке отмены сообщения</exception>
		Task CancelScheduledMessageAsync(Guid tokenId, Type messageType);
	}
