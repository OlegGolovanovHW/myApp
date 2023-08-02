using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;



namespace myApp
{
    public class ApplicationContext : DbContext
    {
        public DbSet<User> Users => Set<User>();
        public string connectionString;
        public ApplicationContext(string connectionString)
        {
            this.connectionString = connectionString;
            Database.EnsureCreated(); // гарантируем что БД создана
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(connectionString); //Устанавливаем подключение к БД через SQLite
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string fullName { get; set; }

        public string birthDate { get; set; }

        public string sex { get; set; }

    }
    internal class Program
    {
        //проверка даты на корректность
        public static bool isDataCorrect(string birthDate)
        {
            var date = birthDate.Split('.');
            if (date.Length == 3)
            {
                int n;
                foreach (var number in date)
                {
                    if (!int.TryParse(number, out n))
                    {
                        Console.WriteLine("Некорректная дата");
                        return false;
                    }
                }
            }
            return true;
        }
        //вычисление количества полных лет человека
        public static int currentAge(string birthDate)
        {
            var today = DateTime.Today;
            int birthDay = int.Parse(birthDate.Split('.')[0]);
            int birthMonth = int.Parse(birthDate.Split('.')[1]);
            int birthYear = int.Parse(birthDate.Split('.')[2]);
            int age = today.Year - birthYear;
            if (today.Month < birthMonth || (today.Month == birthMonth && today.Day < birthDay))
            {
                age--;
            }
            return age;
        }

        public static string GenerateName(int lenght)
        {
            Random rand = new Random();
            string[] letters = { "a", "e", "i", "o", "u", "ae", "y", "b", "c", "d", "f", "g", "h", "j", "k", "l", "m", "l", "n", "p", "q", "r", "s", "sh", "zh", "t", "v", "w", "x" };
            string name = letters[rand.Next(letters.Length)].ToUpper();  
            int cnt = 1;
            while (cnt < lenght)
            {
                name += letters[rand.Next(letters.Length)];
                cnt++;
            }
            return name;
        }

        public static string GenerateSex()
        {
            Random rand = new Random();
            int result = rand.Next(0, 2);
            if (result == 0) return "Male";
            return "Female";
        }

        public static string GenerateDate()
        {
            Random rand = new Random();
            var startDate = new DateTime(1950, 1, 1);
            var range = (DateTime.Today - startDate).Days;
            return startDate.AddDays(rand.Next(range)).ToString().Split(' ')[0];
        }

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Не задан параметр командной строки");
                return;
            }
            //Будем считать, что БД находится в одном каталоге с исполняемым файлом и называется myApp.db
            using (ApplicationContext db = new ApplicationContext("Data Source=myApp.db"))
            {
                switch (args[0])
                {
                    case "1":
                        db.Database.EnsureCreated();
                        Console.WriteLine("Таблица создана");
                        break;
                    case "2": //ФИО будем записывать без пробелов, дату вводить в виде DD-MM-YEAR
                        if (args.Length < 4)
                        {
                            Console.WriteLine("Недостаточное количество аргументов");
                            return;
                        }
                        else
                        {
                            if (isDataCorrect(args[2]))
                            {
                                var user = new User { fullName = args[1], birthDate = args[2], sex = args[3] };
                                db.Users.Add(user);
                                db.SaveChanges();
                                Console.WriteLine("Запись создана");
                            }
                            break;
                        }
                    case "3":
                        var uniqueUser = db.Users.GroupBy(u => new { u.fullName, u.birthDate }).Select
                        (g => new
                        {
                            FullName = g.Key.fullName,
                            BirthDate = g.Key.birthDate,
                            Sex = g.First().sex,
                            age = currentAge(g.Key.birthDate)
                        }).OrderBy(u => u.FullName).ToList();
                        Console.WriteLine("FullName:".PadRight(35) + "BirthDate:".PadRight(15) + "Sex:".PadRight(10) + "Age:");
                        foreach (var user in uniqueUser)
                        {
                            Console.WriteLine($"{user.FullName.PadRight(35)}{user.BirthDate.PadRight(15)}{user.Sex.PadRight(10)}{user.age}");
                        }
                        break;
                    case "4":
                        //Для 1000000 строк: длину фамилии будем выбирать случайным образом от 4 символов до 10, имени от 4 до 10, отчества от 7 до 10
                        for (int i = 0; i < 1000000; i++)
                        {
                            Random rand = new Random();
                            var user = new User { fullName = GenerateName(rand.Next(4,11)) + GenerateName(rand.Next(4, 11)) + GenerateName(rand.Next(7, 11)), birthDate = GenerateDate(), sex = GenerateSex()};
                            db.Users.Add(user);
                            //будем обновлять БД блоками для улучшения производительности
                            if (i % 100000 == 0)
                            {
                                Console.WriteLine("Добавлено 100000 записей");
                                db.SaveChanges();
                            }
                        }
                        db.SaveChanges();
                        //Заполнение автоматически 100 строк в которых пол мужской и ФИО начинается с "F".
                        for (int i = 0; i < 100; i++)
                        {
                            Random rand = new Random();
                            var user = new User { fullName = "F" + GenerateName(rand.Next(4, 10)).ToLower() + GenerateName(rand.Next(4, 11)) + GenerateName(rand.Next(7, 11)), birthDate = GenerateDate(), sex = "Male" };
                            db.Users.Add(user);
                        }
                        db.SaveChanges();
                        Console.WriteLine("Данные успешно добавлены");
                        break;
                    case "5":
                        //Результат выборки из таблицы по критерию: пол мужской, ФИО начинается с "F". Сделать замер времени выполнения.
                        //Вывод приложения должен содержать время.
                        var stopwatch = new Stopwatch();
                        stopwatch.Start();
                        var man = db.Users.Where(u => u.sex == "Male" && u.fullName.StartsWith("F")).ToList();
                        stopwatch.Stop();
                        foreach (var m in man)
                        {
                            Console.WriteLine($"{m.fullName.PadRight(35)}{m.birthDate.PadRight(15)}{m.sex.PadRight(10)}");
                        }
                        Console.WriteLine($"Время выполнения запроса: {stopwatch.ElapsedMilliseconds} ms");
                        break;
                    case "6":
                        //результат До:
                        stopwatch = new Stopwatch();
                        stopwatch.Start();
                        man = db.Users.Where(u => u.sex == "Male" && u.fullName.StartsWith("F")).ToList();
                        stopwatch.Stop();
                        long timeQ = stopwatch.ElapsedMilliseconds;

                        //к соответствующим столбцам добавляем индексы, таким образом производится оптимизация запроса
                        db.Database.ExecuteSqlRaw("CREATE INDEX user_fullName_idx ON Users(fullName)");
                        db.Database.ExecuteSqlRaw("CREATE INDEX user_sex_idx ON Users(sex)");

                        //результат После:
                        stopwatch = new Stopwatch();
                        stopwatch.Start();
                        man = db.Users.Where(u => u.sex == "Male" && u.fullName.StartsWith("F")).ToList();
                        stopwatch.Stop();
                        foreach (var m in man)
                        {
                            Console.WriteLine($"{m.fullName.PadRight(35)}{m.birthDate.PadRight(15)}{m.sex.PadRight(10)}");
                        }
                        Console.WriteLine($"Время выполнения запроса ДО оптимизации: {timeQ} ms");
                        Console.WriteLine($"Время выполнения запроса ПОСЛЕ оптимизации: {stopwatch.ElapsedMilliseconds} ms");

                        db.Database.ExecuteSqlRaw("DROP INDEX user_fullName_idx"); //удаляем индексы в конце для возможности тестирования
                        db.Database.ExecuteSqlRaw("DROP INDEX user_sex_idx");
                        break;
                    default: 
                        Console.WriteLine("неверный параметр командной строки");
                        break;
                }

            }


        }
    }
}