using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using OpenAI_API;
using OpenAI_API.Chat;

namespace SimpleChatGPTVoice
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 設定 Azure Speech API 和 OpenAI API 金鑰
            string azureApiKey = "";
            string azureRegion = "";
            string openAiApiKey = "";

            var speechConfig = SpeechConfig.FromSubscription(azureApiKey, azureRegion);
            //speechConfig.SpeechRecognitionLanguage = "zh-TW"; // 設定語言為繁體中文
            //speechConfig.SpeechSynthesisVoiceName = "zh-TW-HsiaoChenNeural"; // 設定語音合成為預設女性聲音

            Console.WriteLine("請開始說話，按 ESC 結束互動。");

            using (var recognizer = new SpeechRecognizer(speechConfig))
            using (var synthesizer = new SpeechSynthesizer(speechConfig))
            {
                var chatGPT = new OpenAIAPI(openAiApiKey);

                while (true)
                {
                    try
                    {
                        Console.WriteLine("\n請說話...");
                        var recognitionResult = await recognizer.RecognizeOnceAsync();

                        if (recognitionResult.Reason == ResultReason.RecognizedSpeech)
                        {
                            string userInput = recognitionResult.Text.Trim();
                            Console.WriteLine($"你說：{userInput}");

                            // 使用 OpenAI Chat 完成回應
                            var chatRequest = new ChatRequest
                            {
                                Model = "gpt-4o",
                                Messages = new List<ChatMessage>
                                {
                                    new ChatMessage(ChatMessageRole.User, userInput)
                                },
                                MaxTokens = 150,
                                Temperature = 0.7
                            };

                            var chatResponse = await chatGPT.Chat.CreateChatCompletionAsync(chatRequest);
                            string gptResponse = chatResponse.Choices[0].Message.Content.Trim();
                            Console.WriteLine($"ChatGPT 回應：{gptResponse}");

                            // 使用語音合成讀出 ChatGPT 的回應
                            await synthesizer.SpeakTextAsync(gptResponse);
                        }
                        else if (recognitionResult.Reason == ResultReason.NoMatch)
                        {
                            Console.WriteLine("無法辨識語音，請再試一次。");
                        }
                        else if (recognitionResult.Reason == ResultReason.Canceled)
                        {
                            var cancellation = CancellationDetails.FromResult(recognitionResult);
                            Console.WriteLine($"語音辨識取消：{cancellation.Reason}");
                            if (cancellation.Reason == CancellationReason.Error)
                            {
                                Console.WriteLine($"錯誤詳情：{cancellation.ErrorDetails}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"發生錯誤：{ex.Message}");
                    }

                    // 按 ESC 結束程式
                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                    {
                        Console.WriteLine("結束互動。");
                        break;
                    }
                }
            }
        }
    }
}
