# Lails.MQ.Rabbit

Библиотека для работы с RabbitMQ через MassTransit в .NET приложениях. Предоставляет удобные расширения для регистрации и настройки RabbitMQ, публикации сообщений, создания потребителей с автоматической обработкой ошибок и повторных попыток.

## Возможности

- ✅ Простая регистрация и настройка RabbitMQ через MassTransit
- ✅ Асинхронная публикация сообщений
- ✅ Отложенная отправка сообщений (scheduled messages)
- ✅ Периодическая отправка сообщений (recurring messages)
- ✅ Автоматическая регистрация Consumer с настройкой retry политик
- ✅ Поддержка SSL/TLS сертификатов
- ✅ Интеграция с Quartz для планирования сообщений
- ✅ Настройка конкурентности обработки сообщений
- ✅ Планируемая повторная доставка сообщений при ошибках
- ✅ Заглушка для тестирования (RabbitPublisherStub)

## Установка

Установите пакет через NuGet Package Manager:

```bash
dotnet add package Lails.MQ.Rabbit
```

Или через Package Manager Console:

```powershell
Install-Package Lails.MQ.Rabbit
```

## Требования

- .NET 8.0 или выше
- RabbitMQ сервер
- MassTransit (включается автоматически как зависимость)

## Быстрый старт

### 1. Настройка конфигурации

Добавьте параметры подключения к RabbitMQ в `appsettings.json`:

```json
{
  "RABBITMQ_HOSTURL": "rabbitmq://localhost/",
  "RABBITMQ_USERNAME": "guest",
  "RABBITMQ_PASSWORD": "guest"
}
```

Или используйте переменные окружения:

```bash
export RABBITMQ_HOSTURL=rabbitmq://localhost/
export RABBITMQ_USERNAME=guest
export RABBITMQ_PASSWORD=guest
```

### 2. Регистрация в DI контейнере

В методе `ConfigureServices` (или `Program.cs` для .NET 6+):

```csharp
using Lails.MQ.Rabbit;
using MassTransit;

public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // Регистрация IRabbitPublisher
    services
        .RegisterRabbitPublisher()
        .AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                // Настройка подключения к RabbitMQ
                cfg.AddDataBusConfiguration(configuration);
            });
        });
}
```

### 3. Публикация сообщений

```csharp
public class MyService
{
    private readonly IRabbitPublisher _publisher;

    public MyService(IRabbitPublisher publisher)
    {
        _publisher = publisher;
    }

    public async Task SendMessageAsync()
    {
        var message = new MyMessage { Id = 1, Text = "Hello RabbitMQ" };
        
        // Асинхронная публикация
        await _publisher.PublishAsync(message);
    }
}

public class MyMessage
{
    public int Id { get; set; }
    public string Text { get; set; }
}
```

### 4. Создание Consumer

Создайте класс, наследующийся от `BaseConsumer<T>`:

```csharp
using Lails.MQ.Rabbit.Consumer;
using MassTransit;

public class MyMessageConsumer : BaseConsumer<MyMessage>
{
    protected override async Task ConsumeImplementation(ConsumeContext<MyMessage> context)
    {
        var message = context.Message;
        
        // Ваша логика обработки сообщения
        Console.WriteLine($"Received message: {message.Text}");
        
        // Если произошла ошибка, сообщение будет автоматически повторено
        // согласно настройкам retry политики
    }
}
```

### 5. Регистрация Consumer

```csharp
services
    .RegisterRabbitPublisher()
    .AddMassTransit(x =>
    {
        // Регистрация consumer
        x.AddConsumer<MyMessageConsumer>();

        x.UsingRabbitMq((context, cfg) =>
        {
            cfg.AddDataBusConfiguration(configuration);

            // Регистрация consumer с настройкой retry политики
            // retryCount: 3 попытки
            // intervalMin: интервал 1 минута между попытками
            // concurrencyLimit: 10 одновременных обработчиков
            cfg.RegisterConsumerWithRetry<MyMessageConsumer, MyMessage>(
                context, 
                retryCount: 3, 
                intervalMin: 1, 
                concurrencyLimit: 10);
        });
    });
```

### 6. Запуск Bus

В методе `Configure` (или в `Program.cs`):

```csharp
public void Configure(IApplicationBuilder app, IBusControl busControl)
{
    // Запуск MassTransit bus
    busControl.Start();
    
    // При остановке приложения
    // busControl.Stop();
}
```

## Конфигурация

### Параметры подключения

| Параметр | Описание | Обязательный | Пример |
|----------|----------|--------------|--------|
| `RABBITMQ_HOSTURL` | URL хоста RabbitMQ | Да | `rabbitmq://localhost/` |
| `RABBITMQ_USERNAME` | Имя пользователя | Да | `guest` |
| `RABBITMQ_PASSWORD` | Пароль | Да | `guest` |
| `RABBITMQ_QUARTZ_QUEUE_NAME` | Имя очереди Quartz для планирования | Нет | `quartz-queue` |
| `CERTIFICATE_PFX_PATH` | Путь к SSL сертификату (.pfx) | Нет | `C:\certs\cert.pfx` |
| `CERTIFICATE_PFX_PASSWORD` | Пароль для сертификата | Нет | `password123` |
| `Domain:Base` | Доменное имя для SSL | Нет | `example.com` |

### Пример полной конфигурации

```json
{
  "RABBITMQ_HOSTURL": "rabbitmq://rabbitmq.example.com:5672/vhost",
  "RABBITMQ_USERNAME": "myuser",
  "RABBITMQ_PASSWORD": "mypassword",
  "RABBITMQ_QUARTZ_QUEUE_NAME": "quartz-queue",
  "CERTIFICATE_PFX_PATH": "C:\\certs\\rabbitmq.pfx",
  "CERTIFICATE_PFX_PASSWORD": "certpassword",
  "Domain": {
    "Base": "rabbitmq.example.com"
  }
}
```

## API Документация

### IRabbitPublisher

Интерфейс для публикации сообщений в RabbitMQ.

#### Методы

##### `PublishAsync<T>(T message)`
Асинхронно публикует сообщение в очередь RabbitMQ.

**Параметры:**
- `message` - Сообщение для публикации

**Возвращает:** `Task` - Задача, представляющая асинхронную операцию

**Пример:**
```csharp
var message = new MyMessage { Id = 1, Text = "Hello" };
await _publisher.PublishAsync(message);
```

##### `SendScheduledMessageAsync<TMessage>(TMessage message, DateTime scheduledTime)`
Отправляет сообщение с отложенной доставкой в указанное время.

**Параметры:**
- `message` - Сообщение для отложенной отправки
- `scheduledTime` - Время, когда сообщение должно быть доставлено (UTC)

**Возвращает:** `Task<Guid>` - Идентификатор токена запланированного сообщения

**Пример:**
```csharp
var message = new MyMessage { Id = 1, Text = "Delayed message" };
var scheduledTime = DateTime.UtcNow.AddHours(1);
var tokenId = await _publisher.SendScheduledMessageAsync(message, scheduledTime);
```

##### `CancelScheduledMessageAsync(Guid tokenId, Type messageType)`
Отменяет ранее запланированное сообщение.

**Параметры:**
- `tokenId` - Идентификатор токена запланированного сообщения
- `messageType` - Тип сообщения, которое нужно отменить

**Возвращает:** `Task` - Задача, представляющая асинхронную операцию

**Пример:**
```csharp
await _publisher.CancelScheduledMessageAsync(tokenId, typeof(MyMessage));
```

### RabbitRegistrationExtansions

Расширения для регистрации и настройки RabbitMQ.

#### `RegisterRabbitPublisher()`
Регистрирует `IRabbitPublisher` как Singleton в контейнере зависимостей.

**Пример:**
```csharp
services.RegisterRabbitPublisher();
```

#### `RegisterRabbitPublisherStub()`
Регистрирует заглушку `RabbitPublisherStub` для тестирования или разработки. Заглушка не выполняет реальную отправку сообщений.

**Пример:**
```csharp
services.RegisterRabbitPublisherStub();
```

#### `AddDataBusConfiguration(IConfiguration configuration)`
Настраивает подключение к RabbitMQ с поддержкой SSL/TLS и Quartz.

**Параметры:**
- `configuration` - Конфигурация приложения

**Пример:**
```csharp
cfg.AddDataBusConfiguration(configuration);
```

#### `RegisterConsumerWithRetry<TConsumer, TContract>(IRegistrationContext registration, int retryCount, int intervalMin, int concurrencyLimit = 0)`
Регистрирует Consumer с настройкой политики повторных попыток.

**Параметры:**
- `registration` - Контекст регистрации MassTransit
- `retryCount` - Количество повторных попыток при ошибке
- `intervalMin` - Интервал в минутах между повторными попытками
- `concurrencyLimit` - Количество одновременных экземпляров consumer (0 = без ограничения)

**Пример:**
```csharp
cfg.RegisterConsumerWithRetry<MyMessageConsumer, MyMessage>(
    context, 
    retryCount: 3, 
    intervalMin: 1, 
    concurrencyLimit: 10);
```

**Особенности:**
- Имя очереди формируется автоматически: `{TConsumer.FullName}_{TContract}`
- Если Quartz настроен, используется планируемая повторная доставка с интервалами: 5, 15, 30, 60, 120, 240 минут
- Если `concurrencyLimit > 0`, устанавливается ограничение конкурентности
- Если `concurrencyLimit = 0`, используется партиционирование по MessageId
- Очередь создается как Durable (постоянная) и не AutoDelete

### BaseConsumer<TEvent>

Базовый абстрактный класс для реализации потребителей сообщений.

**Пример:**
```csharp
public class MyMessageConsumer : BaseConsumer<MyMessage>
{
    protected override async Task ConsumeImplementation(ConsumeContext<MyMessage> context)
    {
        // Ваша логика обработки
        var message = context.Message;
        await ProcessMessage(message);
    }
}
```

## Расширенные примеры

### Отложенная отправка сообщений

```csharp
// Отправить сообщение через 1 час
var scheduledTime = DateTime.UtcNow.AddHours(1);
var tokenId = await _publisher.SendScheduledMessageAsync(message, scheduledTime);

// Отменить запланированное сообщение
await _publisher.CancelScheduledMessageAsync(tokenId, typeof(MyMessage));
```

### Периодическая отправка сообщений

```csharp
using MassTransit.Scheduling;

// Использование расширений IBus для периодической отправки
var schedule = new DefaultRecurringSchedule
{
    ScheduleId = "daily-report",
    ScheduleGroup = "reports",
    CronExpression = "0 0 9 * * ?" // Каждый день в 9:00
};

var recurringMessage = await _bus.ScheduleRecurringSend(schedule, message);
```

### Consumer с ограничением конкурентности

```csharp
// Ограничить обработку до 5 одновременных сообщений
cfg.RegisterConsumerWithRetry<MyMessageConsumer, MyMessage>(
    context, 
    retryCount: 3, 
    intervalMin: 1, 
    concurrencyLimit: 5);
```

### Consumer без ограничения конкурентности (с партиционированием)

```csharp
// Использовать партиционирование по MessageId (concurrencyLimit = 0)
cfg.RegisterConsumerWithRetry<MyMessageConsumer, MyMessage>(
    context, 
    retryCount: 3, 
    intervalMin: 1, 
    concurrencyLimit: 0);
```

### Использование заглушки для тестирования

```csharp
// В тестах или разработке
services.RegisterRabbitPublisherStub();

// Все вызовы PublishAsync будут выполнены без реальной отправки
var publisher = serviceProvider.GetService<IRabbitPublisher>();
await publisher.PublishAsync(message); // Ничего не произойдет
```

## Обработка ошибок

Библиотека автоматически логирует все ошибки через Serilog. При ошибке публикации исключение перехватывается, логируется и пробрасывается дальше.

### Retry политика

При регистрации Consumer через `RegisterConsumerWithRetry` настраивается автоматическая повторная обработка сообщений при ошибках:

1. **Immediate Retry** - немедленные повторные попытки (настраивается через `retryCount` и `intervalMin`)
2. **Scheduled Redelivery** - планируемая повторная доставка (если Quartz настроен):
   - 5 минут
   - 15 минут
   - 30 минут
   - 60 минут
   - 120 минут
   - 240 минут

## Логирование

Библиотека использует Serilog для логирования. Все операции публикации и ошибки логируются автоматически.

Пример логов:
```
[ERROR] An error occurred while publishing the event via publisher RabbitPublisher. Event: {...}, Name: MyMessage
```

## Тестирование

Для тестирования используйте `RabbitPublisherStub`, который не выполняет реальную отправку сообщений:

```csharp
// В тестах
services.RegisterRabbitPublisherStub();

var publisher = serviceProvider.GetService<IRabbitPublisher>();
await publisher.PublishAsync(testMessage); // Заглушка, ничего не отправляется
```

## Зависимости

- MassTransit (8.1.1)
- MassTransit.RabbitMQ (8.1.1)
- MassTransit.EntityFrameworkCore (8.1.1)
- Serilog (3.0.1)
- System.Configuration.ConfigurationManager (7.0.0)

## Версионирование

Текущая версия: **8.0.0**

## Лицензия

[Укажите вашу лицензию]

## Поддержка

[Укажите контакты для поддержки]

## Примеры использования

Полный пример приложения можно найти в проекте `Lails.MQ.Rabbit.Tests`.

