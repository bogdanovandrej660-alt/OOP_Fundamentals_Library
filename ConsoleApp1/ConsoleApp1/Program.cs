using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text;

namespace EventMonitoringSystem
{
    // ======================== Модель события ========================
    public class Event
    {
        public string Type { get; }      // CPU, Memory, Network
        public string Severity { get; }  // CRITICAL, WARNING, INFO
        public string Message { get; }
        public DateTime Timestamp { get; }

        public Event(string type, string severity, string message)
        {
            Type = type;
            Severity = severity;
            Message = message;
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Severity}: {Type} - {Message}";
        }
    }

    // ======================== Паттерн "Стратегия" ========================
    // Интерфейс стратегии форматирования сообщений
    public interface IMessageFormatter
    {
        string Format(Event eventData);
    }

    // Текстовый формат
    public class TextFormatter : IMessageFormatter
    {
        public string Format(Event eventData)
        {
            return eventData.ToString();
        }
    }

    // JSON формат
    public class JsonFormatter : IMessageFormatter
    {
        public string Format(Event eventData)
        {
            var obj = new
            {
                eventData.Type,
                eventData.Severity,
                eventData.Message,
                Timestamp = eventData.Timestamp.ToString("o")
            };
            return JsonSerializer.Serialize(obj);
        }
    }

    // HTML формат
    public class HtmlFormatter : IMessageFormatter
    {
        public string Format(Event eventData)
        {
            return $@"
<div class='event'>
    <span class='timestamp'>{eventData.Timestamp:yyyy-MM-dd HH:mm:ss}</span>
    <span class='severity {eventData.Severity.ToLower()}'>{eventData.Severity}</span>
    <span class='type'>{eventData.Type}</span>
    <p class='message'>{eventData.Message}</p>
</div>";
        }
    }

    // ======================== Паттерн "Наблюдатель" ========================
    // Интерфейс наблюдателя (подписчика)
    public interface IEventObserver
    {
        void Update(Event eventData);
    }

    // Абстрактный базовый класс для подписчиков
    // Содержит шаблонный метод (Template Method)
    public abstract class EventSubscriber : IEventObserver
    {
        protected IMessageFormatter _formatter;
        protected string _name;

        protected EventSubscriber(string name, IMessageFormatter formatter)
        {
            _name = name;
            _formatter = formatter;
        }

        // Паттерн "Шаблонный метод"
        // Общий алгоритм уведомления
        public void Update(Event eventData)
        {
            // Шаг 1: Фильтрация (могут переопределить наследники)
            if (!ShouldNotify(eventData))
                return;

            // Шаг 2: Форматирование сообщения (стратегия)
            string formattedMessage = _formatter.Format(eventData);

            // Шаг 3: Отправка уведомления (абстрактный метод)
            SendNotification(formattedMessage, eventData);

            // Шаг 4: Логирование (hook)
            AfterNotification(eventData);
        }

        // Шаг 1 - может быть переопределён
        protected virtual bool ShouldNotify(Event eventData)
        {
            // По умолчанию уведомляем только о CRITICAL
            return eventData.Severity == "CRITICAL";
        }

        // Шаг 3 - абстрактный, реализуется наследниками
        protected abstract void SendNotification(string formattedMessage, Event eventData);

        // Шаг 4 - hook, может быть переопределён
        protected virtual void AfterNotification(Event eventData)
        {
            // По умолчанию ничего не делаем
        }

        public void SetFormatter(IMessageFormatter formatter)
        {
            _formatter = formatter;
        }
    }

    // Конкретный подписчик: вывод в консоль
    public class ConsoleSubscriber : EventSubscriber
    {
        public ConsoleSubscriber(string name, IMessageFormatter formatter)
            : base(name, formatter) { }

        protected override void SendNotification(string formattedMessage, Event eventData)
        {
            Console.WriteLine($"[{_name}] Уведомление:");
            Console.WriteLine(formattedMessage);
            Console.WriteLine(new string('-', 50));
        }
    }

    // Конкретный подписчик: запись в файл
    public class FileSubscriber : EventSubscriber
    {
        private readonly string _filePath;

        public FileSubscriber(string name, IMessageFormatter formatter, string filePath)
            : base(name, formatter)
        {
            _filePath = filePath;
        }

        protected override void SendNotification(string formattedMessage, Event eventData)
        {
            // Шаблонный метод позволяет добавить дополнительные действия
            File.AppendAllText(_filePath, formattedMessage + Environment.NewLine);
            File.AppendAllText(_filePath, new string('-', 50) + Environment.NewLine);
        }

        protected override void AfterNotification(Event eventData)
        {
            Console.WriteLine($"[{_name}] Событие записано в файл {_filePath}");
        }
    }

    // ======================== Источник событий (Observable) ========================
    public class EventMonitor
    {
        private readonly List<IEventObserver> _observers = new List<IEventObserver>();
        private readonly Random _random = new Random();

        public void Subscribe(IEventObserver observer)
        {
            _observers.Add(observer);
            Console.WriteLine($"Подписчик добавлен: {observer.GetType().Name}");
        }

        public void Unsubscribe(IEventObserver observer)
        {
            _observers.Remove(observer);
            Console.WriteLine($"Подписчик удалён: {observer.GetType().Name}");
        }

        private void NotifyObservers(Event eventData)
        {
            foreach (var observer in _observers)
            {
                observer.Update(eventData);
            }
        }

        // Имитация мониторинга метрик
        public void StartMonitoring(int seconds = 30)
        {
            Console.WriteLine("Мониторинг запущен...\n");
            DateTime endTime = DateTime.Now.AddSeconds(seconds);

            while (DateTime.Now < endTime)
            {
                // Имитация сбора метрик
                double cpuLoad = _random.NextDouble() * 100;
                double memoryUsage = _random.NextDouble() * 100;
                double networkActivity = _random.NextDouble() * 1000;

                // Генерация событий при критических значениях
                if (cpuLoad > 85)
                {
                    var evt = new Event("CPU", "CRITICAL", $"Загрузка процессора: {cpuLoad:F1}%");
                    NotifyObservers(evt);
                }
                else if (cpuLoad > 70)
                {
                    var evt = new Event("CPU", "WARNING", $"Загрузка процессора: {cpuLoad:F1}%");
                    NotifyObservers(evt);
                }

                if (memoryUsage > 90)
                {
                    var evt = new Event("Memory", "CRITICAL", $"Использование памяти: {memoryUsage:F1}%");
                    NotifyObservers(evt);
                }

                if (networkActivity > 800)
                {
                    var evt = new Event("Network", "CRITICAL", $"Сетевая активность: {networkActivity:F1} MB/s");
                    NotifyObservers(evt);
                }

                System.Threading.Thread.Sleep(2000); // Пауза 2 секунды
            }

            Console.WriteLine("\nМониторинг завершён.");
        }
    }

    // ======================== Демонстрация работы ========================
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            // Создание источника событий
            var monitor = new EventMonitor();

            // Создание подписчиков с разными стратегиями форматирования
            var consoleText = new ConsoleSubscriber("Консоль (текст)", new TextFormatter());
            var consoleJson = new ConsoleSubscriber("Консоль (JSON)", new JsonFormatter());
            var fileHtml = new FileSubscriber("Файл (HTML)", new HtmlFormatter(), "events_log.html");

            // Подписка наблюдателей
            monitor.Subscribe(consoleText);
            monitor.Subscribe(consoleJson);
            monitor.Subscribe(fileHtml);

            // Запуск мониторинга
            monitor.StartMonitoring(15); // 15 секунд

            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }
    }
}