# 圖書館書籍管理系統 API 規格書 (Library Books Management API Specification)

## 1. 專案概述 (Overview)
本專案旨在建立一個基於 .NET 8 的後端 API 服務，提供簡單的書籍管理功能。
系統需使用 Dapper 進行資料庫存取。

## 2. 資料模型 (Data Models)

### 2.1 資料庫實體 (Database Entity) - Table: `Books`

| 欄位名稱 (Column) | 資料型別 (Data Type) | 說明 (Description) | 備註 (Notes) |
| :--- | :--- | :--- | :--- |
| `BookId` | `int` | 書籍唯一識別碼 | Primary Key, Identity (Auto Increment) |
| `Title` | `nvarchar(200)` | 書籍標題 | Not Null |
| `Author` | `nvarchar(100)` | 作者 | Not Null |
| `Isbn` | `varchar(13)` | 國際標準書號 | Unique, Not Null |
| `PublishDate` | `datetime` | 出版日期 | |
| `Price` | `decimal(10, 2)` | 價格 | |
| `StockQty` | `int` | 庫存數量 | Default: 0 |
| `CreateDate` | `datetime` | 建立時間 | Default: GETDATE() |

## 3. API 介面規格 (API Interface Specifications)

### 3.1 取得書籍列表 (Get Books List)

*   **功能**: 查詢圖書館內現有的書籍清單，支援關鍵字搜尋。
*   **HTTP Method**: `GET`
*   **Route**: `/api/books`

#### 3.1.1 輸入參數 (Request Parameters - Query String)

| 參數名稱 | 型別 | 必填 | 說明 | 範例 |
| :--- | :--- | :--- | :--- | :--- |
| `keyword` | `string` | 否 | 搜尋關鍵字 (搜尋標題或作者) | "Harry Potter" |
| `pageIndex` | `int` | 否 | 頁碼 (預設 1) | 1 |
| `pageSize` | `int` | 否 | 每頁筆數 (預設 10) | 10 |

#### 3.1.2 輸出結果 (Response Body - JSON)

```json
{
  "success": true,
  "message": "",
  "data": {
    "totalCount": 100,
    "items": [
      {
        "bookId": 1,
        "title": "C# 程式設計",
        "author": "John Doe",
        "isbn": "9789571234567",
        "publishDate": "2023-01-01T00:00:00",
        "price": 500.00,
        "stockQty": 5
      },
      // ... more items
    ]
  }
}
```

### 3.2 新增書籍 (Add Book)

*   **功能**: 新增一本新書到系統中。
*   **HTTP Method**: `POST`
*   **Route**: `/api/books`

#### 3.2.1 輸入參數 (Request Body - JSON)

| 參數名稱 | 型別 | 必填 | 說明 | 限制 |
| :--- | :--- | :--- | :--- | :--- |
| `title` | `string` | 是 | 書籍標題 | 長度 < 200 |
| `author` | `string` | 是 | 作者 | 長度 < 100 |
| `isbn` | `string` | 是 | 國際標準書號 | 長度 10 或 13 |
| `publishDate` | `datetime` | 否 | 出版日期 | |
| `price` | `decimal` | 是 | 價格 | >= 0 |
| `stockQty` | `int` | 是 | 庫存數量 | >= 0 |

**範例 JSON:**

```json
{
  "title": "深入淺出 Design Patterns",
  "author": "Eric Freeman",
  "isbn": "9789861234567",
  "publishDate": "2024-05-20",
  "price": 680,
  "stockQty": 10
}
```

#### 3.2.2 輸出結果 (Response Body - JSON)

*   **成功 (Success - 200 OK):**

```json
{
  "success": true,
  "message": "新增成功",
  "data": 2  // 回傳新建立的 BookId
}
```

*   **失敗 (Failure - 400 Bad Request):**

```json
{
  "success": false,
  "message": "ISBN 已存在",
  "data": null
}
```

## 4. 非功能性需求 (Non-functional Requirements)
2.  **資料存取**: 使用 Dapper Micro-ORM。
4.  **設定檔**: 資料庫連線字串 (ConnectionString) 需讀取 `appsettings.json`。
5.  **錯誤處理**: 需有全域或 Service 層級的錯誤捕捉機制。
