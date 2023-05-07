using OpenAI.GPT3;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace GitChatGPT
{
	class Program
	{
		static async Task Main(string[] args)
		{
			var config = new ConfigurationBuilder()
			   .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
				.AddEnvironmentVariables()
				.Build();

			var apiKey = config.GetSection("apiKey").Get<string>();

			while (true)
			{
				Console.Write(">");
				string userInput = Console.ReadLine()?.ToLowerInvariant() ?? "";
				string response = string.Empty;

				var _openAIService = new OpenAIService(new OpenAiOptions()
				{
					ApiKey = apiKey
				});

				var _completionResult = await _openAIService.Completions.CreateCompletion(new CompletionCreateRequest()
				{
					Prompt = $"[SYSTEM]: Outpupt a git command only from the following instructions, no explanation or other text \n --- \n Instructions: {userInput} \n Git Command:",
					Model = Models.TextDavinciV2,
					Temperature = 0.5F,
					MaxTokens = 100,
					N = 1
				});

				if (_completionResult.Successful)
				{
					response = _completionResult.Choices[0].Text;
				}
				else
				{
					if (_completionResult.Error == null)
					{
						throw new Exception("Unknown Error");
					}
					Console.WriteLine($"{_completionResult.Error.Code}: {_completionResult.Error.Message}");
				}

				var gitCommands = response.Trim(' ', '\r', '\n')
										  .Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

				if (gitCommands == null)
				{
					Console.WriteLine(response);
					continue;
				}
				foreach (var cmd in gitCommands)
				{
					string gitCommand = cmd.Trim().ToLowerInvariant();
					if (gitCommand.StartsWith("git "))
					{
						Console.Write($"{gitCommand} (y/n)");
						var confirm = Console.ReadLine()?.ToLower();
						if ((confirm == "y") || (confirm == ""))
						{
							ExecuteCommand(gitCommand);
						}
						Console.WriteLine("");
					}
					else
					{
						Console.Write($"{gitCommand}");
					}
				}
			}
		}



		static void ExecuteCommand(string command)
		{
			Process process = new Process();

			process.StartInfo.FileName = "cmd.exe";
			process.StartInfo.Arguments = "/c " + command;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.CreateNoWindow = true;

			var ret = process.Start();
			if (ret)
			{
				process.WaitForExit();
				var output = process.StandardOutput.ReadToEnd();
				if (!string.IsNullOrEmpty(output))
					Console.WriteLine(output);
				var error = process.StandardError.ReadToEnd();
				if (!string.IsNullOrEmpty(error))
					Console.WriteLine(error);
			}
			else
			{
				Console.WriteLine("failed to run..");
			}
		}

	}
}
