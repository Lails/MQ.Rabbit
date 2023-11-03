using System.Configuration;

namespace Lails.MQ.Rabbit
{

    internal class RabbitConnectionConfiguration : ConfigurationSection
    {
        private const string HostUrlPropertyName = "hostUrl";
        private const string UserNamePropertyName = "userName";
        private const string PasswordPropertyName = "password";

        public const string ConfigurationSectionName = "dataBusSection";

        private static RabbitConnectionConfiguration _current;

        public static RabbitConnectionConfiguration Current => _current ??= ConfigurationManager.GetSection(ConfigurationSectionName) as RabbitConnectionConfiguration;

        [ConfigurationProperty(HostUrlPropertyName, DefaultValue = "rabbitmq://localhost/", IsRequired = true)]
        public static string HostUrl
        {
            get => Current == null ? "rabbitmq://localhost/" : (string)Current[HostUrlPropertyName];
            set => Current[HostUrlPropertyName] = value;
        }

        [ConfigurationProperty(UserNamePropertyName, DefaultValue = "guest", IsRequired = true)]
        public static string UserName
        {
            get => Current == null ? "guest" : (string)Current[UserNamePropertyName];
            set => Current[UserNamePropertyName] = value;
        }

        [ConfigurationProperty(PasswordPropertyName, DefaultValue = "guest", IsRequired = true)]
        public static string Password
        {
            get => Current == null ? "guest" : (string)Current[PasswordPropertyName];
            set => Current[PasswordPropertyName] = value;
        }
    }
}