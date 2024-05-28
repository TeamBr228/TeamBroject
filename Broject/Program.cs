using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Broject;

Console.OutputEncoding = Encoding.Unicode;

try
{
    List<User> users = await UserReaderAsync(); // Список користувачів
    List<Poll> polls = await PollReaderAsync(); // Список опитувань

    users.Add(new User { Username = "admin", Password = "admin", IsAdmin = true }); // тестовий адмін

    TcpListener listener = new TcpListener(IPAddress.Any, 8888); // порт 8888
    listener.Start();
    Console.WriteLine("(i) Сервер запущено...");
    Console.WriteLine("(i) Чекаємо на підключення клієнтів...");

    while (true)
    {
        TcpClient client = listener.AcceptTcpClient();
        Console.WriteLine($"(i) Клієнт [{client.Client.RemoteEndPoint}] приєднався!");
        Task.Run(() => HandleClient(client, users, polls));
    }
}
catch (Exception e) { Console.WriteLine("(!) " + e.Message); }

//---------------------------------------------------------------------------------------

static async Task<List<User>> UserReaderAsync()
{
    List<User> usersAs = new List<User>();
    string rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"source\repos\TeamBr228\TeamBroject");
    string searchPattern = @"Users\*.xml";

    if (Directory.Exists(rootPath))
    {
        // Рекурсивний пошук файлів
        List<string> foundFiles = new List<string>();
        await SearchFilesRecursivelyAsync(rootPath, searchPattern, foundFiles);

        // Зчитування користувачів з кожного знайденого файлу
        foreach (string file in foundFiles)
        {
            List<User> usersFromFile = await ReadUsersFromFileAsync(file);
            usersAs.AddRange(usersFromFile);
        }
    }
    else
    {
        Console.WriteLine("(i) Директорія користувачів не існує.");
    }

    return usersAs;
}

static async Task SearchFilesRecursivelyAsync(string currentPath, string searchPattern, List<string> foundFiles)
{
    try
    {
        foreach (string file in Directory.GetFiles(currentPath, searchPattern, SearchOption.AllDirectories))
            foundFiles.Add(file);
    }
    catch (UnauthorizedAccessException) { /*Пропуск директорій, до яких немає доступу*/ }
}

static async Task<List<User>> ReadUsersFromFileAsync(string filePath)
{
    List<User> users = new List<User>();

    try
    {
        using (FileStream fStream = new FileStream(filePath, FileMode.Open))
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<User>));
            users = (List<User>)serializer.Deserialize(fStream);
        }
    }
    catch (Exception e) { Console.WriteLine($"(i) " + e.Message); }

    return users;
}

static async Task<List<Poll>> PollReaderAsync()
{
    List<Poll> pollsAs = new List<Poll>();
    string rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"source\repos\TeamBr228\TeamBroject");
    string searchPattern = @"Polls\*.xml";

    if (Directory.Exists(rootPath))
    {
        // Рекурсивний пошук файлів
        List<string> foundFiles = new List<string>();
        await SearchFilesRecursivelyAsync(rootPath, searchPattern, foundFiles);

        // Зчитування користувачів з кожного знайденого файлу
        foreach (string file in foundFiles)
        {
            List<Poll> usersFromFile = await ReadPollsFromFileAsync(file);
            pollsAs.AddRange(usersFromFile);
        }
    }
    else
    {
        Console.WriteLine("(i) Директорія опитувань не існує.");
    }

    return pollsAs;
}

static async Task<List<Poll>> ReadPollsFromFileAsync(string filePath)
{
    List<Poll> polls = new List<Poll>();

    try
    {
        using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<Poll>));
            polls = (List<Poll>)serializer.Deserialize(fileStream);
        }
    }
    catch (Exception e) { Console.WriteLine($"(i) " + e.Message); }

    return polls;
}

//---------------------------------------------------------------------------------------

static void HandleClient(TcpClient client, List<User> users, List<Poll> polls)
{
    NetworkStream stream = client.GetStream();
    byte[] buffer = new byte[1024];
    int bytesRead;
    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
    {
        string data = Encoding.Unicode.GetString(buffer, 0, bytesRead);
        Console.WriteLine($"Кл. [{client.Client.RemoteEndPoint}] >> {data}");

        string[] parts = data.Split('|');
        string command = parts[0];

        // автентифікація
        if (command == "LOGIN")
        {
            string username = parts[1];
            string password = parts[2];
            bool isAdmin = AuthenticateUser(username, password, users);
            byte[] response = Encoding.Unicode.GetBytes($"AUTH|{(isAdmin ? "ADMIN" : "USER")}");
            stream.Write(response, 0, response.Length);
        }

        // створення опитування
        else if (command == "CREATE_POLL")
        {
            if (parts.Length >= 4)
            {
                string title = parts[1];
                string difficulty = parts[2];
                List<Question> questions = new List<Question>();

                // отримуємо дані про питання та варіанти відповідей
                for (int i = 3; i < parts.Length; i++)
                {
                    string[] questionData = parts[i].Split(';');
                    string questionText = questionData[0];
                    List<string> options = questionData.Skip(1).ToList();
                    questions.Add(new Question { Text = questionText, Options = options });
                }

                // створюємо нове опитування
                Poll newPoll = new Poll { Title = title, Difficulty = difficulty, Questions = questions };
                polls.Add(newPoll);
                byte[] response = Encoding.Unicode.GetBytes("POLL_CREATED");
                stream.Write(response, 0, response.Length);
            }
            else
            {
                byte[] response = Encoding.Unicode.GetBytes("ERROR|INVALID_FORMAT");
                stream.Write(response, 0, response.Length);
            }
        }

        // редагування опитування
        else if (command == "EDIT_POLL")
        {
            if (parts.Length >= 4)
            {
                string title = parts[1];
                string difficulty = parts[2];
                List<Question> questions = new List<Question>();

                // дані про питання та варіанти відповідей
                for (int i = 3; i < parts.Length; i++)
                {
                    string[] questionData = parts[i].Split(';');
                    string questionText = questionData[0];
                    List<string> options = questionData.Skip(1).ToList();
                    questions.Add(new Question { Text = questionText, Options = options });
                }
                // знаходимо опитування за заголовком та оновлюємо його
                Poll existingPoll = polls.FirstOrDefault(p => p.Title == title);
                if (existingPoll != null)
                {
                    existingPoll.Difficulty = difficulty;
                    existingPoll.Questions = questions;
                    byte[] response = Encoding.Unicode.GetBytes("POLL_UPDATED");
                    stream.Write(response, 0, response.Length);
                }
                else
                {
                    byte[] response = Encoding.Unicode.GetBytes("ERROR|POLL_NOT_FOUND");
                    stream.Write(response, 0, response.Length);
                }
            }
            else
            {
                byte[] response = Encoding.Unicode.GetBytes("ERROR|INVALID_FORMAT");
                stream.Write(response, 0, response.Length);
            }
        }

        // видалення опитування
        else if (command == "DELETE_POLL")
        {
            if (parts.Length >= 2)
            {
                string title = parts[1];
                // знаходимо опитування за заголовком та видаляємо його
                Poll existingPoll = polls.FirstOrDefault(p => p.Title == title);
                if (existingPoll != null)
                {
                    polls.Remove(existingPoll);
                    byte[] response = Encoding.Unicode.GetBytes("POLL_DELETED");
                    stream.Write(response, 0, response.Length);
                }
                else
                {
                    byte[] response = Encoding.Unicode.GetBytes("ERROR|POLL_NOT_FOUND");
                    stream.Write(response, 0, response.Length);
                }
            }
            else
            {
                byte[] response = Encoding.Unicode.GetBytes("ERROR|INVALID_FORMAT");
                stream.Write(response, 0, response.Length);
            }
        }

        Console.WriteLine($"Сервер >> {data}");
    }

    client.Close();
    Console.WriteLine("(!) Клієнта було від'єднано!");
}

static bool AuthenticateUser(string username, string password, List<User> users)
{
    User user = users.FirstOrDefault(u => u.Username == username && u.Password == password);
    return user != null && user.IsAdmin;
}