using System;
using System.Collections.Generic;

namespace Broject
{
    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsAdmin { get; set; }
        public List<VoteResult> Results { get; set; } = new List<VoteResult>();

        public override string ToString()
        {
            string info =
                $"Ім'я користувача: {Username}\n" +
                $"Пароль: {Password}\n";

            if (IsAdmin)
                info += "Є адміністратором\n";
            else
                info += "Не адміністратор \n";

            info += "Результати:\n";
            foreach (var res in Results)
                info += $"- Назва: {res.PollTitle}, Відсоток: {res.Percentage}%\n";

            return info;
        }
    }

    public class Poll
    {
        public string Title { get; set; }
        public string Difficulty { get; set; }
        public List<Question> Questions { get; set; } = new List<Question>();

        public override string ToString()
        {
            string info = $"Назва: {Title}\n" +
                $"Складність: {Difficulty}\n" +
                $"Кількість питань: {Questions.Count}\n";

            return info;
        }
    }

    public class Question
    {
        public string Text { get; set; }
        public List<string> Options { get; set; } = new List<string>();
        public List<string> CorrectOptions { get; set; } = new List<string>();

        public override string ToString()
        {
            int i = 1;
            string info = $"Питання: {Text}\nВаріанти відповідей:\n";
            foreach (var opt in Options)
            {
                info += $"{i} - {opt}\n";
                i++;
            }

            return info;
        }
    }

    public class VoteResult
    {
        public string PollTitle { get; set; }
        public int QuesCount { get; set; }
        public int CorrAnswersCount { get; set; }
        public int WrongAnswersCount { get { return QuesCount - CorrAnswersCount; } }
        public int Percentage
        {
            get
            {
                return QuesCount == 0 ? 0 : (100 * CorrAnswersCount / QuesCount);
            }
        }

        public override string ToString()
        {
            string info =
                $"Назва: {PollTitle}\n" +
                $"Кількість питань: {QuesCount}\n" +
                $"Правильні/Неправильні відповіді: {CorrAnswersCount} / {WrongAnswersCount}\n" +
                $"Відсоток: {Percentage}%\n";

            return info;
        }
    }
}
