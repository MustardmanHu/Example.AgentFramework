namespace MyAgentTeam.ConsoleHost
{
    /// <summary>
    /// 負責處理與使用者的互動流程，包含專案選擇與需求輸入。
    /// </summary>
    public static class ConsoleWorkflow
    {
        /// <summary>
        /// 初始化專案設定：選擇新專案或既有專案，並回傳專案路徑與狀態。
        /// </summary>
        /// <returns>Tuple (專案路徑, 是否為新專案)</returns>
        public static (string ProjectPath, bool IsNewProject) InitializeProject()
        {
            Console.WriteLine("請選擇操作模式:");
            Console.WriteLine("1. 建立新專案 (Create New Project)");
            Console.WriteLine("2. 開啟既有專案 (Open Existing Project)");
            Console.Write("請選擇 (預設 1): ");
            string mode = Console.ReadLine()?.Trim() ?? "1";

            string projectPath = "";
            bool isNew = true;

            if (mode == "2")
            {
                // 開啟既有專案
                isNew = false;
                while (true)
                {
                    Console.Write("請輸入既有專案的完整路徑 (e.g., D:\\Project\\MyOldApp): ");
                    string inputPath = Console.ReadLine()?.Trim() ?? "";

                    // 移除可能存在的引號
                    if (inputPath.StartsWith("\"") && inputPath.EndsWith("\""))
                    {
                        inputPath = inputPath.Substring(1, inputPath.Length - 2);
                    }

                    if (Directory.Exists(inputPath))
                    {
                        projectPath = inputPath;
                        Console.WriteLine($"[System]: 已鎖定專案目錄: {projectPath}");
                        break;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[Error]: 找不到目錄 '{{inputPath}}'，請重新輸入。");
                        Console.ResetColor();
                    }
                }
            }
            else
            {
                // 建立新專案
                isNew = true;
                Console.Write("請輸入新專案名稱 (Project Name): ");
                string projectName = Console.ReadLine()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(projectName))
                {
                    projectName = $"Project_{{DateTime.Now:yyyyMMdd_HHmmss}}";
                    Console.WriteLine($"[System]: 自動產生專案名稱: {projectName}");
                }

                // 預設路徑邏輯，可根據需求調整
                string baseDrive = @"D:\\Project";
                // 簡單檢查一下環境，如果是 Linux/Mac 環境可能沒有 D 槽，做個簡單的防呆
                if (!Directory.Exists("D:\\"))
                {
                    baseDrive = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Projects");
                }

                if (!Directory.Exists(baseDrive))
                {
                    try
                    {
                        Directory.CreateDirectory(baseDrive);
                    }
                    catch
                    {
                        // Fallback to current directory if permission denied
                        baseDrive = Path.Combine(Directory.GetCurrentDirectory(), "_GeneratedProjects");
                    }
                }

                projectPath = Path.Combine(baseDrive, projectName);
                Console.WriteLine($"[System]: 新專案將建立於: {projectPath}");
            }

            return (projectPath, isNew);
        }

        /// <summary>
        /// 根據專案狀態，引導使用者輸入開發目標或需求。
        /// </summary>
        /// <param name="isNewProject">是否為新專案</param>
        /// <returns>使用者的目標字串 (User Goal)</returns>
        public static string DetermineUserGoal(bool isNewProject)
        {
            string userGoal = "";
            string choice = "1";

            Console.WriteLine("\n--- 定義開發需求 ---");

            if (!isNewProject)
            {
                Console.WriteLine("您正在操作既有專案，請選擇新增功能的方式:");
                Console.WriteLine("1. 手動輸入指令 (Manual Instruction)");
                Console.WriteLine("2. 讀取規格文件 (Read Specification.md)");
                Console.Write("請選擇 (預設 1): ");
                choice = Console.ReadLine()?.Trim() ?? "1";
            }
            else
            {
                Console.WriteLine("新專案設定:");
                Console.WriteLine("1. 手動輸入專案描述");
                Console.WriteLine("2. 讀取規格文件 (Read Specification.md)");
                Console.WriteLine("3. 執行系統寫入測試 (Test WriteFile)");
                Console.Write("請選擇 (預設 1): ");
                choice = Console.ReadLine()?.Trim() ?? "1";
            }

            if (choice == "3" && isNewProject) // 只有新專案有測試模式
            {
                Console.WriteLine("\n[Testing]: Starting File Write Test...");
                return @"這是一個系統測試任務：
				1. Programmer: 請在專案根目錄建立一個名為 'test_write.txt' 的檔案，內容為 'Hello Gemini Test Successful'。
				2. QA: 請只檢查 'test_write.txt' 是否存在。**忽略** .sln 或 .csproj 的檢查。若檔案存在，請直接回報 'QA_PASSED'。
				3. Supervisor: 若 QA 通過，請直接 'APPROVED'。";
            }
            else if (choice == "2")
            {
                Console.Write("請輸入 Specification.md 的檔案路徑: ");
                string specPath = Console.ReadLine()?.Trim() ?? "";

                if (specPath.StartsWith("\"") && specPath.EndsWith("\""))
                {
                    specPath = specPath.Substring(1, specPath.Length - 2);
                }

                if (!string.IsNullOrWhiteSpace(specPath))
                {
                    // 若使用者只輸入資料夾，自動補上檔名
                    if (Directory.Exists(specPath))
                    {
                        specPath = Path.Combine(specPath, "Specification.md");
                    }

                    if (File.Exists(specPath))
                    {
                        try
                        {
                            userGoal = File.ReadAllText(specPath);
                            Console.WriteLine($"[System]: 已成功讀取規格檔，長度: {userGoal.Length} 字元");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Error]: 讀取檔案失敗: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[Error]: 找不到檔案: {specPath}");
                    }
                }
            }

            // 若上述未取得目標 (或是選擇 1)，則手動輸入
            if (string.IsNullOrWhiteSpace(userGoal))
            {
                string promptText = isNewProject
                    ? "請輸入您的新專案描述 (例如: 幫我寫一個貪食蛇遊戲): "
                    : "請輸入要新增的功能或修改需求 (例如: 在 BookService 新增 GetByAuthor 方法): ";

                Console.Write(promptText);
                userGoal = Console.ReadLine()!;
            }

            // 若是既有專案，為了讓 Agent 理解上下文，自動在 Goal 前面加上提示
            if (!isNewProject)
            {
                userGoal = $"[Existing Project Task]: {userGoal}\n" +
                           "注意：這是一個既有專案。請先理解現有的檔案結構與程式碼，再進行修改或新增功能。不要盲目覆蓋現有邏輯。";
            }

            return userGoal;
        }
    }
}
