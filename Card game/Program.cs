using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Collections;
using System.Runtime.InteropServices;

namespace Card_game
{
    class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetConsoleWindow();                                          // Функция получения дискриптора консоли
        static Graphics ConsoleGraphics;                                                  // Создаём графический контейнер для рисования в консоли
        public const int MaxCard = 36;
        public const string SourceCard = "img\\";
        public const int WidthCard = 46;
        public const int HeightCard = 62;
        public class Karta                                                                // Класс карты
        {
            public Image karta;
            public int weight_card;                                                       // Вес карты
            public Karta(string s, int n)
            {
                karta = Image.FromFile(s);
                weight_card = n;
            }
        }
        public class Player
        {
            public Queue Cards;
            public string Name;
            public int Position;
            Font font10;
            public Player(int n)
            {
                Name = "Игрок " + n;
                Position = n - 1;
                Cards = new Queue();
                font10 = new Font("verdana", 10);
            }
            public void ShowCards()                                                         // Вывод имеющихся карт
            {
                int i = 0;
                ConsoleGraphics.DrawString(Name, font10, Brushes.White, 20, Position * 90);
                foreach (Karta Card in Cards.ToArray().Reverse())
                    ConsoleGraphics.DrawImage(Card.karta, i++ * 13, 15 + Position * 90, WidthCard, HeightCard);
            }
        }
        public class Game
        {
            Karta[] cards;
            List<Player> players;
            List<Karta> cards_on_the_table;
            bool the_same_card, game_over, change_position;
            int j;
            public Game(int n)
            {
                if(n == 0)
                {
                    Console.WriteLine("Игра не возможна без игроков");
                    game_over = true;
                    return;
                }
                else if (n == 1)
                {
                    Console.WriteLine("Игрок не может играть сам с собой");
                    game_over = true;
                    return;
                }
                if (n > 8)
                {
                    Console.WriteLine("Слишком много игроков игра не возможна");
                    game_over = true;
                    return;
                }
                cards_on_the_table = new List<Karta>();
                players = new List<Player>();
                cards = new Karta[MaxCard];
                change_position = false;
                the_same_card = false;
                game_over = false;
                for (int i = 0; i < MaxCard; i++)                                             // Создаем карты
                {
                    try
                    {
                        cards[i] = new Karta(SourceCard + (i + 1).ToString() + ".png", i / 4 + 1);
                    }
                    catch (Exception ex)
                    {
                        game_over = true;
                        Console.WriteLine(ex.Message + " не удалось загрузить");
                    }
                }
                cards = cards.OrderBy(i => Guid.NewGuid()).ToArray();                          // Перемешиваем колоду
                Console.SetWindowSize(Console.WindowWidth, n * 8 + 5);
                for (int i = 0; i < n; i++)                                                    // Создаем игроков
                    players.Add(new Player(i + 1));
                ConsoleGraphics = Graphics.FromHwnd(GetConsoleWindow());
            }
            void ShowCards()                                                                    // Вывод карт игроков и на столе
            {
                ConsoleGraphics.Clear(Color.Black);
                foreach(var player in players)
                    player.ShowCards();

                for (int i = 0; i < cards_on_the_table.Count; i++)
                    ConsoleGraphics.DrawImage(cards_on_the_table[i].karta, 540, 15 + players[i].Position * 90, WidthCard, HeightCard);
            }
            public void Run()                                                                   // Запуст игры
            {
                if (game_over)
                    return;
                j = 0;
                for(int i = 0; i < MaxCard; i++)                                                // Раздаем карты игрокам
                {
                    players[j++].Cards.Enqueue(cards[i]);
                    if (j == players.Count)
                        j = 0;
                }
                while(!game_over)                                                               // Цикл игры
                {
                    ShowCards();
                    for (int i = 0; i < players.Count; i++)                                     // Выводим из игры игрока у которого нет карт
                    {
                        if (players[i].Cards.Count == 0)
                        {
                            change_position = true;
                            players.RemoveAt(i--);                                              // i-- Перемещаемся назад по списку чтобы не пропустить следующего,
                        }                                                                       // у которого возможно нет карт
                    }
                    if (change_position)
                    {
                        for (int i = 0; i < players.Count; i++)
                            players[i].Position = i;
                        change_position = false;
                    }
                    for (int i = 0; i < players.Count; i++)                                    // Игроки выкладывают карты на стол
                    {
                        cards_on_the_table.Add((Karta)players[i].Cards.Dequeue());
                        System.Threading.Thread.Sleep(800);
                        ShowCards();
                    }
                    System.Threading.Thread.Sleep(500);
                    j = 0;
                    for (int i = 1; i < cards_on_the_table.Count; i++)                          // Поиско самой большой карты
                    {
                        if (cards_on_the_table[j].weight_card < cards_on_the_table[i].weight_card)
                        {
                            j = i;
                            the_same_card = false;
                        }
                        else if (cards_on_the_table[j].weight_card == cards_on_the_table[i].weight_card)    // Если у игроков совпали карты
                            the_same_card = true;
                    }
                    Console.SetCursorPosition(30, players.Count * 8);
                    Console.WriteLine("Карты забирает игрок: " + players[j].Name);
                    foreach (Karta card in cards_on_the_table)                                  // Игрок у которого карта больше забирает карты
                        players[j].Cards.Enqueue(card);
                    cards_on_the_table.Clear();                                                 // Очистка стола
                    if (the_same_card)                                                          // Если у игроков совпали карты то забирает тот игрок, который выложил карту первым
                    {                                                                           // и становится в конец очереди, для уравновешивания шансов. К примеру:
                        players.Add(players[j]);                                                // если игроки 1, 2, 3, 4 по очереди вылаживают карты и у игроков 1 и 3
                        players.RemoveAt(j);                                                    // совпадают карты то игрок 1 забирает карты и становится в конец очереди,
                    }                                                                           // тогда игроки будут выкладывать карты в следующем порядке 2, 3, 4, 1
                    System.Threading.Thread.Sleep(1500);
                    ShowCards();
                    foreach (Player player in players)                                          // Проверка забрал ли один из игроков все карты
                        if (player.Cards.Count == MaxCard)
                        {
                            game_over = true;
                            ConsoleGraphics.Clear(Color.Black);
                            player.Position = 0;
                            player.ShowCards();
                            Console.SetCursorPosition(0, 8);
                            Console.WriteLine("Игра окончена игру выиграл: " + player.Name);
                        }
                }
            }
        }
        static void Main(string[] args)
        {
            Game game = new Game(4);
            game.Run();
        }
    }
}
