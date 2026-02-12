# AI Agent Team (Multi-Layer Architecture)

這是一個基於 **.NET 10** 與 **Microsoft Semantic Kernel** 的多代理人 (Multi-Agent) 協作系統。系統採用 **Google Gemini 2.5 Pro** 模型，模擬了一個完整的軟體開發團隊，能夠自動化執行從需求分析、架構設計、資料庫建模、程式碼撰寫、**單元測試撰寫**到**編譯與測試驗證**的全流程任務。

## 🌟 核心特色 (Features)

*   **完整團隊協作**: 包含 Supervisor (PM), System_Designer (架構師), DBA, Programmer (開發者), Second_Programmer (除錯者), **Tester** (測試工程師), Researcher (技術顧問) 與 QA (品質保證) 等 8 個專業角色。
*   **Gemini 2.5 Pro 驅動**: 預設使用 Google 最新模型，提供強大的推理、程式碼生成與跨代理人對話能力。
*   **自動化測試驗證**: 新增 **Tester** 角色負責撰寫 xUnit/MSTest 測試，**QA** 則強制執行 `dotnet build` 與 `dotnet test` 確保品質。
*   **深度除錯機制**: **Researcher** 具備聯網搜尋能力 (Google Search API)，當編譯失敗重複發生時，會自動搜尋解法。
*   **自定義工具攔截器 (Interceptor)**: 基於文本協議的工具系統，穩定執行檔案讀寫與 Shell 指令。
*   **嚴謹身份認同**: 所有 Agent 遵循強化的角色認同守則，確保不會發生角色冒充或職責混淆。

---

## 🛠️ 環境需求與準備 (Prerequisites)

在開始之前，請確保您的環境已安裝以下工具：

1.  **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)** (或最新預覽版)。
2.  **Google Gemini API Key**: 前往 [Google AI Studio](https://aistudio.google.com/) 申請。
3.  **Google Custom Search API Key** (選用，若需聯網搜尋功能): 前往 [Google Programmable Search Engine](https://developers.google.com/custom-search/v1/overview) 申請。
4.  **Google Search Engine ID (CX)** (選用): 前往 [Control Panel](https://programmablesearchengine.google.com/controlpanel/all) 建立搜尋引擎。

---

## ⚙️ 設定指南 (Configuration)

### 1. 設定 API Keys

請修改 `01_Presentation/MyAgentTeam.ConsoleHost/AppSettings.json` 檔案。若檔案不存在，請參考 `AppSettings.example.json` 建立。

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Google": {
    "ApiKey": "YOUR_GEMINI_API_KEY",      // 填入您的 Gemini API Key
    "ModelId": "gemini-2.5-pro"            // 預設模型
  },
  "GoogleSearch": {
    "ApiKey": "YOUR_GOOGLE_SEARCH_API_KEY", // (選用) 用於 Researcher 聯網搜尋
    "SearchEngineId": "YOUR_CX_ID"          // (選用) Google 搜尋引擎 ID
  }
}
```

### 2. 自定義開發規範 (Instruction.md)

系統預設會依照以下順序載入 Agent 的共同開發守則 (包含命名規範、架構分層、SQL 風格等)：

1.  **全域設定**: `D:\Project\Instructions\Instruction.md` (若存在，最優先載入)
2.  **專案預設**: `01_Presentation/MyAgentTeam.ConsoleHost/Instruction.md` (與執行檔同目錄)

您可以在此 Markdown 檔案中自定義：
*   C# 程式碼風格 (縮排、括號位置)。
*   命名規則 (PascalCase, camelCase)。
*   分層架構職責定義 (Controller, Service, Repository)。
*   SQL 與 Dapper 使用規範。

---

## 🚀 操作指南 (Operation Guide)

### 步驟 1: 啟動專案

進入專案根目錄，執行以下指令啟動 Console Host：

```bash
dotnet run --project 01_Presentation/MyAgentTeam.ConsoleHost/MyAgentTeam.ConsoleHost.csproj
```

### 步驟 2: 選擇操作模式

系統啟動後，會詢問您要進行的操作：

#### 模式 1: 建立新專案 (Create New Project)
1.  **輸入專案名稱**: 系統將在 `D:\Project\<專案名稱>` (或使用者目錄下的 `_GeneratedProjects`) 建立新資料夾。
2.  **定義需求**:
    *   **手動輸入**: 直接打字告訴 Supervisor 您想做的專案 (例如：「幫我寫一個貪食蛇遊戲」)。
    *   **讀取規格文件**: 輸入 `Specification.md` 的路徑，系統會讀取該檔案內容作為需求。
    *   **系統測試**: 選擇此選項會執行內建的寫入測試 (Hello World)。

#### 模式 2: 開啟既有專案 (Open Existing Project)
1.  **輸入專案路徑**: 貼上您硬碟中既有專案的完整路徑 (例如 `D:\Project\MyOldApp`)。
2.  **定義修改需求**:
    *   告訴 Agent 您想新增的功能或修改的 Bug (例如：「在 BookService 新增 GetByAuthor 方法」)。
    *   Agent 會自動分析現有的檔案結構與程式碼，並進行增量修改。

### 步驟 3: 觀察 Agent 協作

系統會顯示各個 Agent 的對話與思考過程：
*   **Supervisor**: 分析需求並指派任務。
*   **System_Designer**: 規劃架構並產出 `ArchitecturePlan.md`。
*   **Programmer**: 撰寫程式碼。
*   **Tester**: 撰寫測試案例。
*   **QA**: 執行 `dotnet build` 與 `dotnet test` 驗證。

當看到 `QA: QA_PASSED` 與 `Supervisor: APPROVED` 時，表示任務已完成。

---

## 📝 需求文件範本 (Specification.md)

若您的需求較為複雜，建議將其撰寫為 `Specification.md` 檔案，讓 Agent 一次讀取。範本如下：

```markdown
# 圖書館書籍管理系統 API 規格書

## 1. 專案概述
建立一個基於 .NET 10 的 Web API，提供書籍 CRUD 功能。

## 2. 資料模型 (Book)
- Id (int, PK)
- Title (string)
- Author (string)
- Price (decimal)

## 3. API 需求
- GET /api/books (分頁查詢)
- POST /api/books (新增書籍)
```

---

## 📂 專案結構說明

*   **01_Presentation**:
    *   `MyAgentTeam.ConsoleHost`: 程式進入點，包含 `ConsoleWorkflow.cs` (使用者互動流程) 與 `Program.cs` (依賴注入配置)。
*   **02_Application**:
    *   定義核心介面 `IAgentOrchestrator` 與資料模型。
*   **03_Infrastructure**:
    *   `Agents/`: 定義所有 Agent 的 Prompt 與職責 (`AgentDefinitions.cs`)。
    *   `Plugins/`: 實作檔案系統 (`FileSystemPlugin`)、Shell 指令執行 (`ShellPlugin`) 與 Google 搜尋 (`ResearchPlugin`)。
    *   `Services/`: 實作 Agent 協作邏輯 (`AgentOrchestrator`)。

---

## ⚠️ 常見問題與解決

1.  **找不到路徑**: 請確認您的 `D:\Project` 資料夾是否存在，或檢查程式輸出的預設路徑。
2.  **API Key 錯誤**: 若出現 401/403 錯誤，請檢查 `AppSettings.json` 中的 Key 是否正確且有額度。
3.  **編譯失敗**: Agent 雖然會自我修正，但若迴圈過多次，請檢查是否缺少系統層級的依賴 (如特定 .NET SDK 版本)。
