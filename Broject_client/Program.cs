using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace VotingClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;

            string serverIp = "127.0.0.1"; // IP-адрес сервера
            int port = 8888; // Порт сервера

            using (TcpClient client = new TcpClient())
            {
                try
                {
                    await client.ConnectAsync(serverIp, port);
                    Console.WriteLine("(i) Підключено до сервера.");

                    NetworkStream stream = client.GetStream();

                    // Основний цикл взаємодії з користувачем
                    while (true)
                    {
                        Console.WriteLine("\nОберіть команду:");
                        Console.WriteLine("1 - Увійти");
                        Console.WriteLine("2 - Створити опитування");
                        Console.WriteLine("3 - Редагувати опитування");
                        Console.WriteLine("4 - Видалити опитування");
                        Console.WriteLine("5 - Вийти");

                        string choice = Console.ReadLine();

                        switch (choice)
                        {
                            case "1":
                                await Login(stream);
                                break;
                            case "2":
                                await CreatePoll(stream);
                                break;
                            case "3":
                                await EditPoll(stream);
                                break;
                            case "4":
                                await DeletePoll(stream);
                                break;
                            case "5":
                                Console.WriteLine("(i) Завершення роботи.");
                                return;
                            default:
                                Console.WriteLine("(!) Невірна команда. Спробуйте знову.");
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"(!) Помилка: {ex.Message}");
                }
            }

            Console.WriteLine("(i) Клієнт завершив роботу.");
        }

        static async Task Login(NetworkStream stream)
        {
            Console.Write("Введіть ім'я користувача: ");
            string username = Console.ReadLine();
            Console.Write("Введіть пароль: ");
            string password = Console.ReadLine();
            string loginCommand = $"LOGIN|{username}|{password}";
            await SendCommand(stream, loginCommand);
            string loginResponse = await ReceiveResponse(stream);
            Console.WriteLine($"Сервер >> {loginResponse}");
        }

        static async Task CreatePoll(NetworkStream stream)
        {
            Console.Write("Введіть заголовок опитування: ");
            string title = Console.ReadLine();
            Console.Write("Введіть рівень складності: ");
            string difficulty = Console.ReadLine();

            List<string> questions = new List<string>();
            while (true)
            {
                Console.Write("Введіть питання (або 'done' для завершення): ");
                string questionText = Console.ReadLine();
                if (questionText.ToLower() == "done") break;

                List<string> options = new List<string>();
                while (true)
                {
                    Console.Write("Введіть варіант відповіді (або 'done' для завершення): ");
                    string option = Console.ReadLine();
                    if (option.ToLower() == "done") break;
                    options.Add(option);
                }

                string question = $"{questionText};{string.Join(";", options)}";
                questions.Add(question);
            }

            string createPollCommand = $"CREATE_POLL|{title}|{difficulty}|{string.Join("|", questions)}";
            await SendCommand(stream, createPollCommand);
            string createPollResponse = await ReceiveResponse(stream);
            Console.WriteLine($"Сервер >> {createPollResponse}");
        }

        static async Task EditPoll(NetworkStream stream)
        {
            Console.Write("Введіть заголовок опитування для редагування: ");
            string title = Console.ReadLine();
            Console.Write("Введіть новий рівень складності: ");
            string difficulty = Console.ReadLine();

            List<string> questions = new List<string>();
            while (true)
            {
                Console.Write("Введіть питання (або 'done' для завершення): ");
                string questionText = Console.ReadLine();
                if (questionText.ToLower() == "done") break;

                List<string> options = new List<string>();
                while (true)
                {
                    Console.Write("Введіть варіант відповіді (або 'done' для завершення): ");
                    string option = Console.ReadLine();
                    if (option.ToLower() == "done") break;
                    options.Add(option);
                }

                string question = $"{questionText};{string.Join(";", options)}";
                questions.Add(question);
            }

            string editPollCommand = $"EDIT_POLL|{title}|{difficulty}|{string.Join("|", questions)}";
            await SendCommand(stream, editPollCommand);
            string editPollResponse = await ReceiveResponse(stream);
            Console.WriteLine($"Сервер >> {editPollResponse}");
        }

        static async Task DeletePoll(NetworkStream stream)
        {
            Console.Write("Введіть заголовок опитування для видалення: ");
            string title = Console.ReadLine();
            string deletePollCommand = $"DELETE_POLL|{title}";
            await SendCommand(stream, deletePollCommand);
            string deletePollResponse = await ReceiveResponse(stream);
            Console.WriteLine($"Сервер >> {deletePollResponse}");
        }

        static async Task SendCommand(NetworkStream stream, string command)
        {
            byte[] data = Encoding.Unicode.GetBytes(command);
            await stream.WriteAsync(data, 0, data.Length);
        }

        static async Task<string> ReceiveResponse(NetworkStream stream)
        {
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            return Encoding.Unicode.GetString(buffer, 0, bytesRead);
        }
    }
}
