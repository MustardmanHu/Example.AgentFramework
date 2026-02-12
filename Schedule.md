## Schedule
Schedule 範本使用說明

範本名稱：ea-schedule

- 除了使用 Hangfire 作為排程套件之外，保留了 Web API 所需的功能與套件，方便整合 Web API。
- Controller 需要自行建立。
- CoreProfiler 錄不到 Hangfire 執行的工作，因為這類的觀測工具都是觀測 HTTP request。可自行考慮是否要停用 CoreProfiler。

### 使用方式
>專案名稱請依規範命名，結尾建議使用 Schedule，例如：EA.MyNewProject.Schedule

建立新的 Schedule 專案

#### 方式1. 只建立專案
```
# 產生名為 EA.MyNewProject.Schedule 的專案
dotnet new ea-schedule -n EA.MyNewProject.Schedule
```

#### 方式2. 建立專案和 git
```
# 產生名為 EA.MyNewProject.Schedule 的專案，並協助執行 git init 和 git commit 一次
dotnet new ea-schedule -n EA.MyNewProject.Schedule --initGit
```

期間會代為執行 git 指令，執行時請協助輸入 `Y` 允許執行 (需要2次)
```
# ...略...
是否要執行此動作 [Y(是)|N(否)]?
Y
```

### 預先建立的功能
- .Net 7.0
- 三層式架構
- Hangfire
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

### 套件
僅列出重要的套件。

- Hangfire
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

### 專案主要結構
```
EA.ProjectTemplate
├─EA.ProjectTemplate.Common
|  ├─Aspects
│  └─Settings
├─EA.ProjectTemplate.Repository
│  ├─Helpers
│  ├─Implements
│  ├─Interfaces
│  └─Models
│      ├─Conditions
│      └─DataModels
├─EA.ProjectTemplate.Service
│  ├─Implements
│  ├─Interfaces
│  ├─Mappings
│  └─Models
│      ├─Dtos
│      └─ResultDtos
├─EA.ProjectTemplate.HangfireJob
│  ├─Implements
│  └─Interfaces
└─EA.ProjectTemplate
    ├─Infrastructure
    │  ├─ApplicationBuilderExtensions
    │  ├─Filters
    │  ├─HangfireMisc
    │  ├─Mappings
    │  ├─Middlewares
    │  ├─ServiceCollectionExtensions
    │  ├─Swagger
    │  └─Validators
    ├─Models
    │  ├─OutputModels
    │  └─Parameters
    └─Properties
```