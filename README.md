# AI Agent Team (Multi-Layer Architecture)

這是一個基於 **.NET 10** 與 **Microsoft Semantic Kernel** 的多代理人 (Multi-Agent) 協作系統。系統採用 Google Gemini 2.5 Pro 模型，模擬了一個完整的軟體開發團隊，能夠自動化執行從需求分析、架構設計、資料庫建模、程式碼撰寫、**單元測試撰寫**到**編譯與測試驗證**的全流程任務。

## 🌟 功能特色

*   **完整團隊協作**: 包含 Supervisor, System_Designer, DBA, Programmer, Second_Programmer, **Tester**, Researcher 與 QA 等 8 個專業角色。
*   **Gemini 2.5 Pro 驅動**: 預設使用 Google 最新模型，提供強大的推理、程式碼生成與跨代理人對話能力。
*   **自動化測試驗證**: 新增 **Tester** 角色負責撰寫 xUnit 測試，**QA** 則強制執行 `dotnet build` 與 `dotnet test` 確保品質。
*   **深度除錯機制**: **Researcher** 具備聯網搜尋能力，當編譯失敗重複發生時，會自動翻頁搜尋最多 200 筆相關資料以提供精確解法。
*   **自定義工具攔截器 (Interceptor)**: 基於文本協議的工具系統，穩定執行 `WriteFile`, `ReadFile`, `ListFiles` 與 `RunShellCommand`。
*   **智慧對話壓縮**: 透過 `SummarizingSelectionStrategy` 確保長對話下的 Token 效率，並精確保留身份標籤與關鍵字（如 `QA_PASSED`, `APPROVED`）。
*   **嚴謹身份認同**: 所有 Agent 遵循強化的角色認同守則，確保不會發生角色冒充或職責混淆。

## 🚀 快速開始

#### 🔑 如何取得 API Keys:
1.  **Gemini API Key**: 請前往 [Google AI Studio](https://aistudio.google.com/) 建立。
2.  **Google Custom Search API Key**: 請前往 [Google Programmable Search Engine](https://developers.google.com/custom-search/v1/overview) 點擊 "Get a key"。
3.  **Google Search Engine ID (CX)**: 請前往 [Control Panel](https://programmablesearchengine.google.com/controlpanel/all) 建立搜尋引擎。

## 📁 重要路徑與預設位置 (重要)

為確保系統正常運作，請確認以下檔案與路徑配置：

| 項目 | 預設路徑 / 檔案位置 | 說明 |
| :--- | :--- | :--- |
| **設定檔** | `01_Presentation/MyAgentTeam.ConsoleHost/AppSettings.json` | 存放 API Key 與模型設定。若無此檔請參考 `AppSettings.example.json`。 |
| **共同守則 (高優先)** | `D:\Project\Instructions\Instruction.md` | 系統會優先嘗試從此路徑載入 Agent 共同開發守則。 |
| **共同守則 (備援)** | `執行檔目錄/Instruction.md` | 若上述路徑不存在，則載入此目錄下的範本。 |
| **程式碼產出目錄** | `D:\Project\<專案名稱>` | Agent 生成的所有程式碼與測試專案將存放在此。 |
| **產出目錄 (Fallback)** | `專案目錄/_GeneratedCode` | 若 `D:\` 無法寫入，則會自動回退到此處。 |

> **提示**：若您想更改程式碼產出的磁碟路徑，請修改 `01_Presentation/MyAgentTeam.ConsoleHost/Program.cs` 中的 `baseDrive` 變數。

### 2. 核心組件說明

#### 代理人團隊 (The Team)
*   **Supervisor**: 專案經理，唯一有權發出 `APPROVED` 指令的決策者。
*   **System_Designer**: 架構師，負責 SOLID 設計與 Mermaid 圖表，並將文件存於 `docs/architecture/`。
*   **DBA**: 資料庫專家，專注於 T-SQL 設計與 Dapper 參數化查詢。
*   **Programmer / Second_Programmer**: 開發者，實作三層式架構，自動處理 NuGet 套件引用。
*   **Tester**: 自動化測試工程師，負責撰寫 xUnit 單元測試與整合測試。
*   **QA**: 品質把關者，執行 `dotnet build` 與 `dotnet test`，嚴禁在測試不通過時放行。
*   **Researcher**: 技術顧問，具備死循環偵測機制，負責解決技術難題。

#### 工具呼叫規範 (Tool Protocol)
*   **檔案操作**: `file_system.WriteFile(relativePath='...', content='''...''')`
*   **指令執行**: `shell.RunShellCommand(command='...')` (支援 `en-US` 輸出與 Exit Code 捕獲)
*   **深度搜尋**: `research.Search(query='...', count=10, startIndex=1)`

### 3. 執行專案

1.  確認已安裝 **.NET 10 SDK**。
2.  執行專案：
    ```bash
    dotnet run --project 01_Presentation/MyAgentTeam.ConsoleHost/MyAgentTeam.ConsoleHost.csproj
    ```
3.  **預設設定**: 產生的 Web API 專案預設啟動路徑為 `/swagger`。

## 📂 專案結構

*   **01_Presentation**: 控制台進入點，包含顏色標記輸出與測試模式。
*   **02_Application**: 定義核心介面 (`IAgentOrchestrator`) 與回應模型。
*   **03_Infrastructure**:
    *   `Agents/`: 代理人定義、身份認同守則與選擇策略。
    *   `Plugins/`: 包含檔案系統、Shell 執行、Google 搜尋與 429 重試處理器。
    *   `Strategies/`: 包含對話歷史壓縮與失敗終止條件 (`PROJECT_FAILED`)。

## 🛠️ 開發規範

本專案所有程式碼均附帶 **XML 格式的繁體中文註解**。
系統預設會從 `D:\Project\Instructions\Instruction.md` 或執行目錄載入共同守則。您可以修改守則來調整 Agent 的程式碼風格、命名規範與 NuGet 版本。

---
