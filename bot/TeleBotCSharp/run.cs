using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;

namespace TeleBot
{
    class Run
    {
        static async Task Main(string[] args)
        {
            DataProgram.BD_Connect();
            await RunBot();
        }


        static async Task RunBot()
        {
            using var cts = new CancellationTokenSource();
            var bot = new TelegramBotClient(Constants.nsra_token, cancellationToken: cts.Token);
            var me = await bot.GetMe();

            bot.StartReceiving(
            Handlers.HandleUpdateAsync,
            Handlers.HandleErrorAsync,
            new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
            cancellationToken: cts.Token
            );


            Console.WriteLine($"Бот @{me.Username} запущен. Нажмите Enter для выхода.");

            Console.ReadLine();
            cts.Cancel();
        }
    }
}
