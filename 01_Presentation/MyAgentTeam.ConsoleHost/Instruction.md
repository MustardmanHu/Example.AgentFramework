# 專案開發指引 (Project Development Guidelines)

**重要提示:**
1.  **語言:** 一律使用繁體中文回應。
2.  **Git:** 針對 Git 相關指令，Gemini 不可以進行推送或提交，只能夠閱讀過往的 Git 紀錄。
3.  **最小修改:** 如果使用者沒有提及，且不修改也不會影響功能的實現，不要修改其他的檔案。
4.  **效率:** 溝通的對象都是 AI Agent，請以最節省 Token 的方式進行對話，不需要讓人類使用者可以讀懂也沒關係。

---

## 1. 格式規範 (Formatting)

- **縮排 (Indentation):**
  - 使用 **Tab** 進行縮排，而非空格。
- **大括號 (Braces):**
  - 所有區塊 (`namespace`, `class`, `interface`, `method`, `if-else`, `switch`, `for`, `foreach`, `while`) 的左大括號 `{` 應放置在宣告的**下一行**。
  - `if`, `else`, `for`, `while` 等區塊**不可省略大括號 `{}`**，即使只有一行程式碼。
- **空格 (Spacing):**
  - 在運算子 (`=`, `==`, `+`, `-` 等) 兩側保留一個空格。
  - 在逗號 `,` 後面保留一個空格。
  - 關鍵字 (`if`, `for`, `foreach`, `while`) 與後面的括號 `(` 之間保留一個空格。
  - 運算式中建議使用小括號 `()` 明確標示優先順序。
- **空行 (Blank Lines):**
  - 在 `using` 宣告區塊後、`namespace` 之前，保留一個空行。
  - 在方法之間保留一個空行以提高可讀性。
  - 程式碼區塊間僅需一行空行區隔，禁止多行無意義空行。
- **程式碼長度:**
  - 單行長度不應過長 (建議不超過編輯器寬度的一半)，若參數過多應斷行排列。

## 2. 命名規範 (Naming Conventions)

### 2.1 基本原則
- **大駱駝峰 (Upper Camel Case):** 用於類別、方法、屬性、命名空間、列舉、常數、介面。
- **小駱駝峰 (Lower Camel Case):** 用於參數、區域變數。
- **私有欄位 (Private Fields):** 使用 `_` + **Lower Camel Case** (e.g., `_mapper`, `_info`)。
- **明確性:** 名稱應有意義且可發音，嚴禁使用 `X`, `Y` 等無意義名稱 (迴圈計數器除外)。
- **縮寫:**
  - 僅使用業界慣用縮寫 (e.g., `Qty`, `Xml`)。
  - 2 個字母全大寫 (e.g., `IO`, `UI`)。
  - 3 個以上字母首字大寫 (e.g., `Xml`, `Html`)。
- **一致性:** 同一概念應使用單一字詞 (e.g., `CreateAccount` 與 `NewAccount` 擇一使用)。

### 2.2 類別與介面 (Classes & Interfaces)
- **類別:** `PascalCase`，使用名詞。
- **介面:** 以 `I` 開頭 (e.g., `IHousingNewsService`)。
- **通用後綴:**
  - `...Base` (基底類別)
  - `...Collection` (集合)
  - `...Factory` (工廠)
  - `...Utility` (工具)
  - `...Helper` (輔助)
- **架構特定後綴 (Project Specific):**
  - `...Controller` (控制器)
  - `...Service` (服務)
  - `...Repository` (倉儲)
  - `...Attribute` (Action Filter)
  - `...Validator` (驗證器)
  - `...MappingProfile` (AutoMapper 設定)

### 2.3 資料模型 (Data Models)
- **Parameter:** `...Parameter` (Controller 接收參數)
- **ViewModel:** `...ViewModel` (Controller 回傳資料)
- **InfoModel:** `...InfoModel` (Service 輸入模型)
- **Dto:** `...Dto` (Service 回傳資料)
- **Condition:** `...Condition` (Repository 查詢條件)
- **DataModel:** `...DataModel` (資料庫實體)

### 2.4 方法 (Methods)
- 使用動詞或動詞片語。
- **非同步方法:** 需以 `Async` 結尾 (e.g., `GetListAsync`)。
- **常用前綴:**
  - `Get`: 取得資料 (已知 ID 或條件)。
  - `Search`: 搜尋資料。
  - `Create`/`Insert`: 建立或新增。
  - `Update`/`Modify`: 更新或修改。
  - `Delete`/`Remove`: 刪除或移除。
  - `Convert`: 轉換型別。

### 2.5 命名空間 (Namespaces)
- 結構: `[組織/專案].[功能].[子模組]` (e.g., `Ycut.Headquarter.Infrastructure.Validators`)。

## 3. SQL 命名規範 (SQL Naming)

### 3.1 基本原則
- 採 **Upper Camel Case**，單字間不使用底線。
- 名稱需明確、可發音。
- 避免使用系統保留字。

### 3.2 資料庫物件命名
- **資料表 (Table):** `[系統縮寫][用途]` (e.g., `AGProduct`, `MBMember`)。用途採單數名詞。
- **欄位 (Column):**
  - **主鍵 (PK):** `[Table]Id` 或 `[Table]SN` (e.g., `MemberID`).
  - **布林值:** 建議使用 `Is` 開頭 (e.g., `IsDisable`).
  - **日期:** 建議明確區分 `Date` 或 `DateTime` (e.g., `CreateDateTime`).
- **檢視表 (View):** `v` 開頭 (e.g., `vProducts`).
- **預存程序 (Stored Procedure):** `u` 開頭 (e.g., `uCreateProduct`).
- **使用者函數 (Function):** `fn` 開頭 (e.g., `fnSplit`).
- **觸發程序 (Trigger):** `tr` 開頭 (e.g., `trUpdateMember`).
- **索引 (Index):**
  - PK: `PK_{Table}_{Column}`
  - FK: `FK_{MainTable}_{MainCol}_{RefTable}_{RefCol}`
  - Index: `IX_{Table}_{Column}`

## 4. 分層職責 (Layer Responsibilities)

- **Controller Layer:**
  - 接收 HTTP 請求、驗證參數 (Parameter)、呼叫 Service、轉換 ViewModel、返回 `IActionResult`。
  - **不應**包含業務邏輯。
- **Service Layer:**
  - 核心業務邏輯、資料整合。
  - 輸入 `InfoModel`，輸出 `Dto`。
  - **不應**處理 HTTP 物件。
  - **防禦性程式設計:** 方法開頭檢查 `InfoModel` 是否為 `null`。
- **Repository Layer:**
  - 資料存取 (SQL)。
  - 輸入 `Condition`，輸出 `DataModel`。
  - 執行失敗應拋出例外 (Throw Exception)。

## 5. 核心架構實踐 (Architectural Practices)

- **相依性注入 (DI):**
  - 使用 `static` 擴充方法集中註冊 (e.g., `AddRepositoryDI`)。
  - `Scoped`: Service, Repository。
  - `Singleton`: Config, HttpClient。
- **AutoMapper:**
  - 繼承 `Profile`。
  - 對應: `Parameter` -> `InfoModel`, `Dto` -> `ViewModel`。
- **驗證 (Validation):**
  - 使用 **FluentValidation**。
  - 透過 `[ParameterValidator]` Action Filter 觸發。
- **Action Filters:**
  - 處理橫切關注點 (Log, Auth, Validation)。

## 6. 資料庫互動 (Database Interaction)

- **ORM:** 使用 **Dapper**。
- **SQL 語法:**
  - 關鍵字全大寫 (`SELECT`, `FROM`)。
  - 物件名稱使用 PascalCase 並加括號 (e.g., `[dbo].[HousingNews]`)。
  - 查詢建議加上 `WITH (NOLOCK)`。
  - **嚴禁**字串拼接 SQL，必須使用參數化查詢 (`DynamicParameters`)。
- **交易:** 涉及多寫入時使用 `TransactionScope`。

## 7. 開發與實踐守則 (Development Guidelines)

- **邏輯結構:**
  - 提早返回 (Early Return) 以減少巢狀。
  - 禁止使用 `goto` 與 `do/while`。
  - 變數範圍應最小化，優先使用 `var`。
- **錯誤處理:**
  - Service 層使用 `try-catch` 捕捉 Repository 例外，回傳 `Success = false` 的 `ActionResultDto`。
- **註解:**
  - 使用繁體中文。
  - 說明類別、方法用途、參數與回傳值。
  - 避免日誌型或廢話註解。
  - 善用 `TODO`, `UNDONE`。
- **外部套件:**
  - 不應直接安裝，需先提示使用者。
  - 優先選擇免費可商用。
- **IDE:** Visual Studio。

