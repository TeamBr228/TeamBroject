using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Broject;

Console.OutputEncoding = Encoding.Unicode;

List<User> users = new List<User>(); // Список користувачів
List<Poll> polls = new List<Poll>(); // Список опитувань

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
