using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Scheduling;

namespace Lails.MQ.Rabbit;

	/// <summary>
	/// Расширения для работы с запланированными и периодическими сообщениями через MassTransit IBus
	/// </summary>
	public static class RabbitExtensions
	{
		/// <summary>
		/// Отправляет сообщение с отложенной доставкой в указанное время
		/// </summary>
		/// <typeparam name="TMessage">Тип сообщения для отправки</typeparam>
		/// <param name="bus">Экземпляр IBus для отправки сообщения</param>
		/// <param name="message">Сообщение для отложенной отправки</param>
		/// <param name="scheduledTime">Время, когда сообщение должно быть доставлено</param>
		/// <returns>Идентификатор токена запланированного сообщения</returns>
		/// <exception cref="ArgumentNullException">Выбрасывается, если bus или message равны null</exception>
		public static async Task<Guid> SendScheduledMessage<TMessage>(this IBus bus, TMessage message, DateTime scheduledTime)
		{
			if (bus == null)
				throw new ArgumentNullException(nameof(bus));
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var sendResult = await bus.CreateMessageScheduler()
				.ScheduleSend(new Uri($"rabbitmq://{bus.Address.Host}/{typeof(TMessage).Namespace}:{typeof(TMessage).Name}"), scheduledTime, message);
			return sendResult.TokenId;
		}

		/// <summary>
		/// Отменяет ранее запланированное сообщение
		/// </summary>
		/// <param name="bus">Экземпляр IBus для отмены сообщения</param>
		/// <param name="tokenId">Идентификатор токена запланированного сообщения</param>
		/// <param name="messageType">Тип сообщения, которое нужно отменить</param>
		/// <returns>Задача, представляющая асинхронную операцию отмены</returns>
		/// <exception cref="ArgumentNullException">Выбрасывается, если bus, tokenId или messageType равны null</exception>
		public static async Task CancelScheduledMessage(this IBus bus, Guid tokenId, Type messageType)
		{
			if (bus == null)
				throw new ArgumentNullException(nameof(bus));
			if (messageType == null)
				throw new ArgumentNullException(nameof(messageType));

			await bus.CreateMessageScheduler()
				.CancelScheduledSend(
					new Uri($"rabbitmq://{bus.Address.Host}/{messageType.Namespace}:{messageType.Name}"), tokenId);
		}

		/// <summary>
		/// Планирует периодическую отправку сообщения по расписанию
		/// </summary>
		/// <typeparam name="TMessage">Тип сообщения для периодической отправки</typeparam>
		/// <typeparam name="TRecurringSchedule">Тип расписания, должен наследоваться от DefaultRecurringSchedule</typeparam>
		/// <param name="bus">Экземпляр IBus для отправки сообщения</param>
		/// <param name="recurringSchedule">Расписание для периодической отправки</param>
		/// <param name="message">Сообщение для периодической отправки</param>
		/// <returns>Информация о запланированном периодическом сообщении</returns>
		/// <exception cref="ArgumentNullException">Выбрасывается, если bus, recurringSchedule или message равны null</exception>
		public static async Task<ScheduledRecurringMessage> ScheduleRecurringSend<TMessage, TRecurringSchedule>(this IBus bus, TRecurringSchedule recurringSchedule, TMessage message)
			where TRecurringSchedule : DefaultRecurringSchedule
		{
			if (bus == null)
				throw new ArgumentNullException(nameof(bus));
			if (recurringSchedule == null)
				throw new ArgumentNullException(nameof(recurringSchedule));
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var scheduledRecurringMessage = await bus.ScheduleRecurringSend(new Uri($"rabbitmq://{bus.Address.Host}/{typeof(TMessage).Namespace}:{typeof(TMessage).Name}"), recurringSchedule, message);

			return scheduledRecurringMessage;
		}

		/// <summary>
		/// Отменяет ранее запланированное периодическое сообщение по объекту ScheduledRecurringMessage
		/// </summary>
		/// <typeparam name="TMessage">Тип сообщения, должен быть классом</typeparam>
		/// <param name="bus">Экземпляр IBus для отмены сообщения</param>
		/// <param name="recurringMessage">Информация о запланированном периодическом сообщении</param>
		/// <returns>Задача, представляющая асинхронную операцию отмены</returns>
		/// <exception cref="ArgumentNullException">Выбрасывается, если bus или recurringMessage равны null</exception>
		public static async Task CancelScheduledRecurringMessage<TMessage>(this IBus bus, ScheduledRecurringMessage<TMessage> recurringMessage)
			where TMessage : class
		{
			if (bus == null)
				throw new ArgumentNullException(nameof(bus));
			if (recurringMessage == null)
				throw new ArgumentNullException(nameof(recurringMessage));

			await bus.CancelScheduledRecurringSend(recurringMessage);
		}

		/// <summary>
		/// Отменяет ранее запланированное периодическое сообщение по идентификатору и группе расписания
		/// </summary>
		/// <typeparam name="TMessage">Тип сообщения, должен быть классом</typeparam>
		/// <param name="bus">Экземпляр IBus для отмены сообщения</param>
		/// <param name="scheduleId">Идентификатор расписания</param>
		/// <param name="scheduleGroup">Группа расписания</param>
		/// <returns>Задача, представляющая асинхронную операцию отмены</returns>
		/// <exception cref="ArgumentNullException">Выбрасывается, если bus, scheduleId или scheduleGroup равны null</exception>
		public static async Task CancelScheduledRecurringMessage<TMessage>(this IBus bus, string scheduleId, string scheduleGroup)
			where TMessage : class
		{
			if (bus == null)
				throw new ArgumentNullException(nameof(bus));
			if (string.IsNullOrWhiteSpace(scheduleId))
				throw new ArgumentNullException(nameof(scheduleId));
			if (string.IsNullOrWhiteSpace(scheduleGroup))
				throw new ArgumentNullException(nameof(scheduleGroup));

			await bus.CancelScheduledRecurringSend(scheduleId, scheduleGroup);
		}
	}
