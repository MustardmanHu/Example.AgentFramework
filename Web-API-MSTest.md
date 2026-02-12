## Web API MSTest
Web API + 單元測試（MSTest）範本使用說明

範本名稱：ea-webapi-mstest

### 使用方式
>專案名稱請依規範命名

建立新的 Web API 專案

#### 方式1. 只建立專案
```
# 產生名為 EA.MyNewProject 的專案
dotnet new ea-webapi-mstest -n EA.MyNewProject
```

#### 方式2. 建立專案和 git
```
# 產生名為 EA.MyNewProject 的專案，並協助執行 git init 和 git commit 一次
dotnet new ea-webapi-mstest -n EA.MyNewProject --initGit
```

期間會代為執行 git 指令，執行時請協助輸入 `Y` 允許執行 (需要2次)
```
# ...略...
是否要執行此動作 [Y(是)|N(否)]?
Y
```

### 預先建立的功能

#### WebAPI
- .Net 7/8/9
- 三層式架構
- DatabaseHelper (公司內部 Dapper 套件)
- 透過 Setting 套件取得資料庫連線字串、URL
- ResponseWrapper
- Mapster
- FluentValidation
- Swagger 多版本 API 的設定
- 機敏資料網址 (coreprofiler, swagger, healthcheck 等) 存取 IP 白名單
- CoreProfiler (原生寫法, AOP, Action Filter)
- 串接 Exceptionless
- Dockerfile
- W3C Logging (選用，請自行解開註解)

#### 單元測試
- MSTest
- Common, Repository, Service, WebService 的測試專案
- Repository 預先提供 LocalDB 或 Container 建立資料庫的程式碼

### 套件
僅列出重要的套件。

#### WebAPI
- AspectInjector
- CoreProfiler
- Dapper
- Evertrust.Core.Dapper.AspNetCore
- Evertrust.Core.Logging
- Evertrust.ResponseWrapper
- Evertrust.Setting.Connections
- Evertrust.Setting.Url
- Exceptionless
- FluentValidation
- Mapster
- Microsoft.Data.SqlClient
- Microsoft.VisualStudio.SlowCheetah

#### 單元測試
- AutoFixture
- AwesomeAssertions
- MSTest.TestAdapter
- MSTest.TestFramework
- NSubstitute
- Testcontainers

### 專案主要結構
```
EA.ProjectTemplate
├─EA.ProjectTemplate.Common
│  ├─Aspects
│  └─Settings
├─EA.ProjectTemplate.CommonTests
├─EA.ProjectTemplate.Repository
│  ├─Helpers
│  ├─Implements
│  ├─Interfaces
│  └─Models
│     ├─Conditions
│     └─DataModels
├─EA.ProjectTemplate.RepositoryTests
│  ├─Enum
│  ├─Implements
│  ├─Settings
│  ├─TestData
│  │  └─TableSchemas
│  └─TestUtilities
│      └─Database
│          ├─LocalDb
│          └─Mssql
├─EA.ProjectTemplate.Service
│  ├─Implements
│  ├─Interfaces
│  ├─Mappings
│  └─Models
│     ├─Dtos
│     └─ResultDtos
├─EA.ProjectTemplate.ServiceTests
│  └─Implements
├─EA.ProjectTemplate.WebService
│  ├─Controllers
│  │  ├─Version1
│  │  └─Version2
│  ├─Infrastructure
│  │  ├─Filters
│  │  ├─Mappings
│  │  ├─Middlewares
│  │  ├─ServiceCollectionExtensions
│  │  ├─Swagger
│  │  └─Validators
│  ├─Models
│  │  ├─OutputModels
│  │  └─Parameters
│  └─Properties
└─EA.ProjectTemplate.WebServiceTests
```