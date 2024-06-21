using System;
using System.IO;
using System.Net;
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
            TcpClient client = new TcpClient();

            try
            {
                await client.ConnectAsync("127.0.0.1", 8888);
                NetworkStream stream = client.GetStream();
                Console.WriteLine("(i) Підключено до сервера.");

                // Основний цикл взаємодії з користувачем
                while (true)
                {
                    Console.WriteLine("\nОберіть команду:");
                    Console.WriteLine("1 - Зареєструватися");
                    Console.WriteLine("2 - Увійти");
                    Console.WriteLine("3 - Вийти");
                    Console.Write("Ваш вибір -> ");

                    string choice = Console.ReadLine();

                    switch (choice)
                    {
                        case "1":
                            await Register(stream, client);
                            break;
                        case "2":
                            await Login(stream, client);
                            break;
                        case "3":
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

            Console.WriteLine("(i) Клієнт завершив роботу.");
        }



        static async Task Register(NetworkStream stream, TcpClient client)
        {
            Console.Write("Введіть ім'я користувача: ");
            string username = Console.ReadLine();
            Console.Write("Введіть пароль: ");
            string password = Console.ReadLine();
            Console.Write("Чи є користувач адміністратором? (yes/no): ");
            string isAdminInput = Console.ReadLine();
            bool isAdmin = isAdminInput.ToLower() == "yes";
            string registerCommand = $"REGISTER|{username}|{password}|{isAdmin}";
            await SendCommand(stream, registerCommand);
            string registerResponse = await ReceiveResponse(stream);
            if (isAdmin == true) { await AdminInterface(client); }
            else { await UserInterface(client); }
        }

        static async Task Login(NetworkStream stream, TcpClient client)
        {
            Console.Write("Введіть ім'я користувача: ");
            string username = Console.ReadLine();
            Console.Write("Введіть пароль: ");
            string password = Console.ReadLine();
            string loginCommand = $"LOGIN|{username}|{password}";
            await SendCommand(stream, loginCommand);
            string loginResponse = await ReceiveResponse(stream);
            string typeOfClient = loginResponse.Split('|')[1];
            if (typeOfClient == "ADMIN") { await AdminInterface(client); }
            else { await UserInterface(client); }
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

        static async Task UserInterface(TcpClient client)
        {
            NetworkStream stream = client.GetStream();

            Console.WriteLine("\nОберіть команду:");
            Console.WriteLine("1 - Пройти опитування");
            Console.WriteLine("2 - Вийти");
            Console.Write("Ваш вибір -> ");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    // await ChoosePoll(stream); --- ТИМУУУР, ТОБІ СЮДИ. РОЗПИШУ ВСЕ В DISCORD
                    break;
                case "2":
                    Console.WriteLine("(i) Завершення роботи.");
                    return;
                default:
                    Console.WriteLine("(!) Невірна команда. Спробуйте знову.");
                    break;
            }
        }

        static async Task AdminInterface(TcpClient client)
        {
            while (true)
            {
                NetworkStream stream = client.GetStream();

                Console.WriteLine("\nОберіть команду:");
                Console.WriteLine("1 - Створити опитування");
                Console.WriteLine("2 - Редагувати опитування");
                Console.WriteLine("3 - Видалити опитування");
                Console.WriteLine("4 - Вийти");
                Console.Write("Ваш вибір -> ");

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await CreatePoll(stream);
                        break;
                    case "2":
                        await EditPoll(stream);
                        break;
                    case "3":
                        await DeletePoll(stream);
                        break;
                    case "4":
                        Console.WriteLine("(i) Завершення роботи.");
                        return;
                    default:
                        Console.WriteLine("(!) Невірна команда. Спробуйте знову.");
                        break;
                }
            }
        }
    }
}
