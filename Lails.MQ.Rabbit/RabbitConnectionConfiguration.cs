using System.Configuration;

namespace Lails.MQ.Rabbit;

	/// <summary>
	/// Класс для чтения конфигурации подключения к RabbitMQ из файла конфигурации (app.config/web.config)
	/// </summary>
	internal class RabbitConnectionConfiguration : ConfigurationSection
	{
		/// <summary>
		/// Имя свойства для URL хоста RabbitMQ в конфигурации
		/// </summary>
		private const string HostUrlPropertyName = "hostUrl";

		/// <summary>
		/// Имя свойства для имени пользователя RabbitMQ в конфигурации
		/// </summary>
		private const string UserNamePropertyName = "userName";

		/// <summary>
		/// Имя свойства для пароля RabbitMQ в конфигурации
		/// </summary>
		private const string PasswordPropertyName = "password";

		/// <summary>
		/// Имя секции конфигурации в файле конфигурации
		/// </summary>
		public const string ConfigurationSectionName = "dataBusSection";

		/// <summary>
		/// Кэшированный экземпляр конфигурации
		/// </summary>
		private static RabbitConnectionConfiguration _current;

		/// <summary>
		/// Получает текущий экземпляр конфигурации из файла конфигурации
		/// </summary>
		public static RabbitConnectionConfiguration Current => _current ??= ConfigurationManager.GetSection(ConfigurationSectionName) as RabbitConnectionConfiguration;

		/// <summary>
		/// Получает или устанавливает URL хоста RabbitMQ
		/// </summary>
		/// <value>URL хоста RabbitMQ (по умолчанию: "rabbitmq://localhost/")</value>
		[ConfigurationProperty(HostUrlPropertyName, DefaultValue = "rabbitmq://localhost/", IsRequired = true)]
		public static string HostUrl
		{
			get => Current == null ? "rabbitmq://localhost/" : (string)Current[HostUrlPropertyName];
			set => Current[HostUrlPropertyName] = value;
		}

		/// <summary>
		/// Получает или устанавливает имя пользователя для подключения к RabbitMQ
		/// </summary>
		/// <value>Имя пользователя (по умолчанию: "guest")</value>
		[ConfigurationProperty(UserNamePropertyName, DefaultValue = "guest", IsRequired = true)]
		public static string UserName
		{
			get => Current == null ? "guest" : (string)Current[UserNamePropertyName];
			set => Current[UserNamePropertyName] = value;
		}

		/// <summary>
		/// Получает или устанавливает пароль для подключения к RabbitMQ
		/// </summary>
		/// <value>Пароль (по умолчанию: "guest")</value>
		[ConfigurationProperty(PasswordPropertyName, DefaultValue = "guest", IsRequired = true)]
		public static string Password
		{
			get => Current == null ? "guest" : (string)Current[PasswordPropertyName];
			set => Current[PasswordPropertyName] = value;
		}
	}