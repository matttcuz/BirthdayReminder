using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Класс для хранения информации о человеке
public class BirthdayPerson
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime BirthDate { get; set; }

    public BirthdayPerson()
    {
        Name = string.Empty;
    }
}

// Класс для работы с базой данных дней рождений
public class BirthdayRepository
{
    private readonly string _databasePath;
    private readonly string _connectionString;

    public BirthdayRepository()
    {
        // Храним базу данных в папке AppData
        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string appFolder = Path.Combine(appDataFolder, "BirthdayReminder");
        
        // Создаем папку, если ее нет
        Directory.CreateDirectory(appFolder);
        
        _databasePath = Path.Combine(appFolder, "birthdays.db");
        _connectionString = $"Data Source={_databasePath}";
        
        InitializeDatabase();
    }

    // Инициализация базы данных
    private void InitializeDatabase()
    {
        // Создаем таблицу, если база данных новая
        if (!File.Exists(_databasePath))
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                
                var createTableCommand = connection.CreateCommand();
                createTableCommand.CommandText = @"
                    CREATE TABLE Birthdays (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        BirthDate TEXT NOT NULL
                    )";
                createTableCommand.ExecuteNonQuery();
            }
        }
    }

    // Добавление нового дня рождения
    public void AddBirthday(BirthdayPerson person)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            
            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO Birthdays (Name, BirthDate) VALUES (@name, @date)";
            command.Parameters.AddWithValue("@name", person.Name);
            command.Parameters.AddWithValue("@date", person.BirthDate.ToString("yyyy-MM-dd"));
            
            command.ExecuteNonQuery();
        }
    }

    // Удаление дня рождения
    public void DeleteBirthday(int id)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Birthdays WHERE Id = @id";
            command.Parameters.AddWithValue("@id", id);
            
            command.ExecuteNonQuery();
        }
    }

    // Обновление данных о дне рождения
    public void UpdateBirthday(BirthdayPerson person)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE Birthdays SET Name = @name, BirthDate = @date WHERE Id = @id";
            command.Parameters.AddWithValue("@id", person.Id);
            command.Parameters.AddWithValue("@name", person.Name);
            command.Parameters.AddWithValue("@date", person.BirthDate.ToString("yyyy-MM-dd"));
            
            command.ExecuteNonQuery();
        }
    }

    // Получение всех дней рождений
    public List<BirthdayPerson> GetAllBirthdays()
    {
        var birthdays = new List<BirthdayPerson>();
        
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Birthdays";
            
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    birthdays.Add(new BirthdayPerson
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        BirthDate = DateTime.Parse(reader.GetString(2))
                    });
                }
            }
        }
        
        return birthdays;
    }

    // Получение ближайших дней рождений
    public List<BirthdayPerson> GetUpcomingBirthdays(int count = 5)
    {
        var today = DateTime.Today;
        var currentYear = today.Year;
        
        return GetAllBirthdays()
            .Select(person => new {
                Person = person,
                NextDate = CalculateNextBirthday(person.BirthDate, today)
            })
            .OrderBy(x => x.NextDate)
            .Take(count)
            .Select(x => x.Person)
            .ToList();
    }

    // Вывод дней рождений, которые сегодня
    public List<BirthdayPerson> GetTodayBirthdays()
    {
        var today = DateTime.Today;
        return GetAllBirthdays()
            .Where(person => person.BirthDate.Month == today.Month && 
                           person.BirthDate.Day == today.Day)
            .ToList();
    }

    // Вывод пропущенных дней рождений в этом году
    public List<BirthdayPerson> GetMissedBirthdays()
    {
        var today = DateTime.Today;
        var currentYear = today.Year;
        
        return GetAllBirthdays()
            .Where(person => {
                var thisYearBirthday = new DateTime(currentYear, person.BirthDate.Month, person.BirthDate.Day);
                return thisYearBirthday < today;
            })
            .OrderByDescending(person => new DateTime(currentYear, person.BirthDate.Month, person.BirthDate.Day))
            .ToList();
    }

    // Вспомогательный метод для расчета следующей даты дня рождения
    private DateTime CalculateNextBirthday(DateTime birthDate, DateTime today)
    {
        int year = (birthDate.Month > today.Month || 
                   (birthDate.Month == today.Month && birthDate.Day >= today.Day))
                   ? today.Year 
                   : today.Year + 1;
        
        return new DateTime(year, birthDate.Month, birthDate.Day);
    }
}

// Основной класс приложения
public class BirthdayApp
{
    private readonly BirthdayRepository _repository;

    public BirthdayApp()
    {
        _repository = new BirthdayRepository();
    }

    // Запуск приложения
    public void Run()
    {
        Console.WriteLine("Добро пожаловать в Поздравлятор!");
        Console.WriteLine();
        
        ShowUpcomingBirthdays(true);
        
        while (true)
        {
            ShowMainMenu();
            var choice = Console.ReadLine();
            Console.Clear();

            switch (choice)
            {
                case "1":
                    AddNewBirthday();
                    break;
                case "2":
                    ShowAllBirthdays();
                    break;
                case "3":
                    ShowUpcomingBirthdays();
                    break;
                case "4":
                    ShowTodayBirthdays();
                    break;
                case "5":
                    ShowMissedBirthdays();
                    break;
                case "6":
                    DeleteBirthday();
                    break;
                case "7":
                    EditBirthday();
                    break;
                case "8":
                    Console.WriteLine("До свидания!");
                    return;
                default:
                    Console.WriteLine("Неверный выбор. Попробуйте еще раз.");
                    break;
            }
        }
    }

    // Отображение главного меню
    private void ShowMainMenu()
    {
        Console.WriteLine("\nГлавное меню:");
        Console.WriteLine("1. Добавить день рождения");
        Console.WriteLine("2. Показать все дни рождения");
        Console.WriteLine("3. Показать ближайшие дни рождения");
        Console.WriteLine("4. Показать сегодняшние дни рождения");
        Console.WriteLine("5. Показать пропущенные дни рождения");
        Console.WriteLine("6. Удалить запись");
        Console.WriteLine("7. Редактировать запись");
        Console.WriteLine("8. Выход");
        Console.Write("Выберите действие: ");
    }

    // Добавление нового дня рождения
    private void AddNewBirthday()
    {
        Console.WriteLine("Добавление нового дня рождения");
        
        string name = GetValidName();
        DateTime date = GetValidBirthDate();
        
        _repository.AddBirthday(new BirthdayPerson { Name = name, BirthDate = date });
        Console.WriteLine("\nЗапись успешно добавлена!");
    }

    // Получение корректного имени
    private string GetValidName()
    {
        string name;
        while (true)
        {
            Console.Write("Введите имя (не может быть пустым): ");
            name = Console.ReadLine()?.Trim() ?? string.Empty;
            
            if (!string.IsNullOrWhiteSpace(name))
                break;
                
            Console.WriteLine("Ошибка: имя не может быть пустым!");
        }
        return name;
    }

    // Получение корректной даты рождения
    private DateTime GetValidBirthDate()
    {
        DateTime date;
        while (true)
        {
            Console.Write("Введите дату рождения (гггг-мм-дд): ");
            string input = Console.ReadLine() ?? string.Empty;
            
            if (DateTime.TryParse(input, out date))
            {
                if (date <= DateTime.Today)
                    break;
                    
                Console.WriteLine("Ошибка: дата рождения не может быть в будущем!");
            }
            else
            {
                Console.WriteLine("Ошибка: неверный формат даты!");
            }
        }
        return date;
    }

    // Отображение всех дней рождений
    private void ShowAllBirthdays()
    {
        var birthdays = _repository.GetAllBirthdays();
        
        if (birthdays.Count == 0)
        {
            Console.WriteLine("Список дней рождения пуст.");
            return;
        }

        Console.WriteLine("Все дни рождения:");
        foreach (var person in birthdays.OrderBy(p => p.BirthDate))
        {
            Console.WriteLine($"[ID: {person.Id}] {person.Name} - {person.BirthDate:dd.MM.yyyy}");
        }
    }

    // Отображение ближайших дней рождений
    private void ShowUpcomingBirthdays(bool firstRun = false)
    {
        var todayBirthdays = _repository.GetTodayBirthdays();
        var upcomingBirthdays = _repository.GetUpcomingBirthdays();

        if (todayBirthdays.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\nСегодня празднуют:");
            foreach (var person in todayBirthdays)
            {
                int age = DateTime.Today.Year - person.BirthDate.Year;
                Console.WriteLine($"  {person.Name} - {age} лет!");
            }
            Console.ResetColor();
        }

        if (upcomingBirthdays.Count > 0)
        {
            if (!firstRun || todayBirthdays.Count == 0)
            {
                Console.WriteLine("\nБлижайшие дни рождения:");
            }
            
            foreach (var person in upcomingBirthdays)
            {
                var nextDate = new DateTime(
                    person.BirthDate.Month > DateTime.Today.Month || 
                    (person.BirthDate.Month == DateTime.Today.Month && person.BirthDate.Day >= DateTime.Today.Day)
                        ? DateTime.Today.Year 
                        : DateTime.Today.Year + 1,
                    person.BirthDate.Month,
                    person.BirthDate.Day);
                
                int daysLeft = (nextDate - DateTime.Today).Days;
                Console.WriteLine($"  {person.Name} - {person.BirthDate:dd.MM} (через {daysLeft} дн.)");
            }
        }
        else if (todayBirthdays.Count == 0)
        {
            Console.WriteLine("\nБлижайшие дни рождения не найдены.");
        }
    }

    // Отображение сегодняшних дней рождений
    private void ShowTodayBirthdays()
    {
        var todayBirthdays = _repository.GetTodayBirthdays();

        if (todayBirthdays.Count == 0)
        {
            Console.WriteLine("Сегодня никто не празднует день рождения.");
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\nСегодняшние дни рождения:");
        foreach (var person in todayBirthdays)
        {
            int age = DateTime.Today.Year - person.BirthDate.Year;
            Console.WriteLine($"  {person.Name} - {age} лет!");
        }
        Console.ResetColor();
    }

    // Отображение пропущенных дней рождений
    private void ShowMissedBirthdays()
    {
        var missedBirthdays = _repository.GetMissedBirthdays();
        
        if (missedBirthdays.Count == 0)
        {
            Console.WriteLine("Нет пропущенных дней рождения в этом году.");
            return;
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\nПропущенные дни рождения:");
        foreach (var person in missedBirthdays)
        {
            var dateThisYear = new DateTime(DateTime.Today.Year, person.BirthDate.Month, person.BirthDate.Day);
            int daysPassed = (DateTime.Today - dateThisYear).Days;
            Console.WriteLine($"  {person.Name} - {person.BirthDate:dd.MM} ({daysPassed} дн. назад)");
        }
        Console.ResetColor();
    }

    // Удаление дня рождения
    private void DeleteBirthday()
    {
        ShowAllBirthdays();
        var birthdays = _repository.GetAllBirthdays();
        
        if (birthdays.Count == 0)
            return;

        int id = GetValidId("Введите ID записи для удаления: ");
        if (id == -1) return;

        Console.Write($"Вы действительно хотите удалить запись с ID {id}? (y/n): ");
        string confirm = Console.ReadLine()?.ToLower() ?? string.Empty;
        
        if (confirm == "y")
        {
            _repository.DeleteBirthday(id);
            Console.WriteLine("\nЗапись успешно удалена!");
        }
        else
        {
            Console.WriteLine("\nУдаление отменено.");
        }
    }

    // Редактирование дня рождения
    private void EditBirthday()
    {
        ShowAllBirthdays();
        var birthdays = _repository.GetAllBirthdays();
        
        if (birthdays.Count == 0)
            return;

        int id = GetValidId("Введите ID записи для редактирования: ");
        if (id == -1) return;

        var personToEdit = birthdays.FirstOrDefault(p => p.Id == id);
        if (personToEdit == null)
        {
            Console.WriteLine("\nЗапись с указанным ID не найдена!");
            return;
        }

        Console.WriteLine($"\nРедактирование записи ID: {personToEdit.Id}");
        Console.WriteLine($"Текущее имя: {personToEdit.Name}");
        Console.Write("Новое имя (оставьте пустым для сохранения текущего): ");
        string newName = Console.ReadLine()?.Trim() ?? string.Empty;
        
        Console.WriteLine($"\nТекущая дата: {personToEdit.BirthDate:dd.MM.yyyy}");
        Console.Write("Новая дата (гггг-мм-дд, оставьте пустым для сохранения текущей): ");
        string newDateInput = Console.ReadLine()?.Trim() ?? string.Empty;

        // Обновляем данные, если введены новые значения
        if (!string.IsNullOrWhiteSpace(newName))
        {
            personToEdit.Name = newName;
        }

        if (!string.IsNullOrWhiteSpace(newDateInput))
        {
            if (DateTime.TryParse(newDateInput, out DateTime newDate))
            {
                if (newDate <= DateTime.Today)
                {
                    personToEdit.BirthDate = newDate;
                }
                else
                {
                    Console.WriteLine("\nОшибка: дата рождения не может быть в будущем! Дата не изменена.");
                }
            }
            else
            {
                Console.WriteLine("\nОшибка: неверный формат даты! Дата не изменена.");
            }
        }

        _repository.UpdateBirthday(personToEdit);
        Console.WriteLine("\nЗапись успешно обновлена!");
    }

    // Получение корректного ID
    private int GetValidId(string prompt)
    {
        int id;
        while (true)
        {
            Console.Write(prompt);
            string input = Console.ReadLine()?.Trim() ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(input))
                return -1;
                
            if (int.TryParse(input, out id))
                break;
                
            Console.WriteLine("Ошибка: неверный формат ID!");
        }
        return id;
    }
}

// Точка входа в приложение
class Program
{
    static void Main(string[] args)
    {
        var app = new BirthdayApp();
        app.Run();
    }
}